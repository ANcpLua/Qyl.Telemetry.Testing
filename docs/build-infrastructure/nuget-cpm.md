# NuGet and central package management

## One intentional version policy

Adopt a root `Directory.Packages.props` for projects sharing this repository's
package policy. Enable `ManagePackageVersionsCentrally` there and declare each
ordinary dependency's version once. Remove corresponding `Version` attributes
from `PackageReference`; retain dependency ownership and metadata such as
`PrivateAssets="all"` in the consuming project.

Only the nearest `Directory.Packages.props` is automatically imported. A nested
file replaces the automatic parent lookup unless it explicitly imports the
parent. Avoid separate test/build version catalogs without a concrete need.

Before changing declarations, inventory package references in projects and
imports, conditional references, evaluated version properties and any overrides.
Check restored package evidence where resolution matters. Keep conversion and
dependency upgrades distinct; centralization alone does not justify changing
frameworks or moving to the newest package versions. Do not flatten a required
framework-specific version split.

## Keep different version mechanisms distinct

| Mechanism | Owner |
| --- | --- |
| Ordinary `PackageReference` versions | `Directory.Packages.props` |
| .NET SDK and `MSTest.Sdk` selection | `global.json` |
| Published library product version | One release version declaration, consumed by packaging and release tooling |
| Coordinated package families or values also used by asset acquisition | Shared named properties when those uses justify them |
| `PackageDownload` exact ranges | The download declaration and its shared version property; CPM does not replace it |

If using `Version.props`, import it before any `PackageVersion` or other item
that consumes its properties. Do not rely on a later project-body assignment.
Retain properties also used by release scripts or RID package acquisition;
remove obsolete ones only after checking all remaining references. Use
`VersionOverride` only for a justified exception, not as a default migration tool.

The two existing `global.json` files declare the same .NET SDK `10.0.401` and
MSTest SDK `4.5.1`. Consolidation needs one policy, not an incidental upgrade.
Resolve the actual MTP/framework packages after any relevant input changes.

## Candidate packages are a separate dependency boundary

Source-level unit and integration tests can use `ProjectReference`. End-to-end
package verification must consume the packed candidate through `PackageReference`
and avoid a source-reference fallback to the library under test. The merged
library must not retain a reference to its own published package.

Pack the candidate before restoring its consumers. Do not assume a root solution
restore can obtain an unpublished candidate that has not been packed yet. Make
candidate version and package identity explicit in the staged build graph.

Use an isolated consumer/cache and deliberate NuGet source mapping so the
candidate package comes from its candidate feed while external dependencies come
from the intended public sources. Merely adding two feeds does not guarantee
which one supplies an overlapping package ID/version. Public verification must
restore the immutable public package without access to the candidate feed.

CPM does not govern scratch projects generated outside its directory hierarchy.
Testing's existing `eng/release/smoke.sh` embeds package versions in such a
project. Prefer a maintained consumer or explicitly supply its version policy;
do not assume removing versions in the main checkout fixes that drift.

Preserve the proto license and notices already shipped by the library. Keep
machine credentials and cached packages outside this documentation and source
tree. Source configuration may name a feed without containing its secret.
