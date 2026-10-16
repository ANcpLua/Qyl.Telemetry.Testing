#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet tool restore
if [[ -n ${QYL_LOCAL_FEED:-} ]]; then
  dotnet restore Telemetry.Testing.slnx --source "$QYL_LOCAL_FEED" --source https://api.nuget.org/v3/index.json
  dotnet restore tests/Qyl.Telemetry.Testing.Probes --source "$QYL_LOCAL_FEED" --source https://api.nuget.org/v3/index.json
else
  dotnet restore Telemetry.Testing.slnx
  dotnet restore tests/Qyl.Telemetry.Testing.Probes
fi
exec dotnet run --project eng/build/_build.csproj --configuration Release -- "$@"
