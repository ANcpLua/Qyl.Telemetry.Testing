# NuGet and central package management

## Central versions

With Central Package Management enabled, ordinary package versions are declared
as `PackageVersion` items in `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Google.Protobuf" Version="3.36.2" />
  </ItemGroup>
</Project>
```

A consuming project's dependency remains a `PackageReference`:

```xml
<PackageReference Include="Google.Protobuf" />
```

CPM centralizes version selection, not dependency ownership. Metadata such as
`PrivateAssets`, aliases and generated path properties belongs to the relevant
reference. `VersionOverride` provides a project-specific version exception when
that feature is enabled.

Only the nearest `Directory.Packages.props` is automatically imported. A nested
file can explicitly import its parent. Conditional central items can express
framework-specific version differences.

## Version mechanisms

| Mechanism | Meaning |
| --- | --- |
| `PackageVersion` | Version selection for an ordinary centrally managed package reference |
| `global.json` SDK selection | .NET CLI SDK and MSBuild SDK resolution |
| Project `Version` | Product/package version used by packaging |
| Shared MSBuild version property | A value usable by package declarations, scripts or asset acquisition |
| `PackageDownload` | Package acquisition with its own exact version-range declaration |

Property expansion occurs when the consuming declaration evaluates. A version
property defined after its `PackageVersion` use does not retroactively change
that item's earlier value. An imported `Version.props` can supply values before
the central package declarations that consume them.

The inspected starting projects declare .NET SDK `10.0.401` and MSTest SDK
`4.5.1` in `global.json`. Those declarations differ from ordinary package
versions; restored assets describe the packages actually resolved.

## Source references and package references

`ProjectReference` connects a project graph and its outputs. `PackageReference`
resolves a package identity and dependency graph. A source-reference test and a
package-consumer test therefore exercise different delivery paths.

A candidate feed exposes a locally packed package. Its consumer restore depends
on that package already existing. A generated scratch project outside the
repository's directory hierarchy does not inherit the repository's CPM file.
The existing `eng/release/smoke.sh` creates such a project and supplies inline
package versions.

## Feeds and caches

`NuGet.Config` describes package sources and can map package ID patterns to
sources. Source mapping controls eligible sources; merely adding multiple feeds
does not identify which feed supplies an overlapping package ID/version.

Restore writes assets, generated imports and package-folder information. Changing
`NUGET_PACKAGES` changes package storage; existing assets may still point at the
previous location. An isolated package cache distinguishes a fresh package
consumer from one using an already cached candidate.

The prototype's collector acquisition combines a RID-specific `PackageDownload`
with a copy from the restored package's tools directory. That asset acquisition
is distinct from the dependencies published by the testing library.
