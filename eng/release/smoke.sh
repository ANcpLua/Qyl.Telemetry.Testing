#!/usr/bin/env bash
set -euo pipefail
version=${1:?Expected published version}
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 2
scratch=$(mktemp -d "${TMPDIR:-/tmp}/qyl-testing-consumer.XXXXXXXX")
trap 'rm -rf "$scratch"' EXIT
cat >"$scratch/smoke.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><PackageReference Include="Qyl.Telemetry.Testing" Version="$version"/></ItemGroup></Project>
XML
cat >"$scratch/NuGet.Config" <<'XML'
<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>
XML
cat >"$scratch/Program.cs" <<'CS'
using System.Net;
using System.Net.Http.Headers;
using Qyl.Telemetry.Testing;
await using var receiver = await OtlpReceiver.StartAsync();
using var http = new HttpClient();
using var body = new ByteArrayContent([]);
body.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
using var response = await http.PostAsync(new Uri(receiver.Endpoint, "v1/traces"), body);
if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"Unexpected status: {response.StatusCode}");
var snapshot = receiver.Snapshot();
if (snapshot.Receipts.Count != 1 || snapshot.Receipts[0] is not { Signal: "traces", Accepted: 0, Rejected: 0, Error: null }) throw new InvalidOperationException("Wrong empty-request receipt");
using var malformed = new ByteArrayContent([0x80]);
malformed.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
using var rejected = await http.PostAsync(new Uri(receiver.Endpoint, "v1/traces"), malformed);
if (rejected.StatusCode != HttpStatusCode.BadRequest) throw new InvalidOperationException("Malformed protobuf was accepted");
if (receiver.Snapshot().Receipts.Count != 2 || receiver.Snapshot().Receipts[1].Error is null) throw new InvalidOperationException("Missing error evidence");
Console.WriteLine("Package receiver accepted protobuf and retained malformed-request evidence.");
CS
NUGET_PACKAGES="$scratch/packages" dotnet restore "$scratch/smoke.csproj" --configfile "$scratch/NuGet.Config"
NUGET_PACKAGES="$scratch/packages" dotnet run --project "$scratch/smoke.csproj" --configuration Release --no-restore
