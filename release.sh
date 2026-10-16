#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
mode=${1:-}
if [[ $mode == --verify || $mode == --wait ]]; then
  [[ $# == 2 ]] || { echo "Expected one version after $mode." >&2; exit 2; }
  version=${2:-}
else
  [[ $# == 1 ]] || { echo "Expected only a version; unsupported flags are refused." >&2; exit 2; }
  version=$mode
fi
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'usage: ./release.sh <x.y.z> | --verify <x.y.z> | --wait <x.y.z>' >&2; exit 2; }
log="$PWD/artifacts/release-$version-verify.log"
if [[ $mode == --wait ]]; then
  [[ -f $log ]] || { echo "No verification log: $log" >&2; exit 1; }
  # Keep foreground waits bounded; the detached verifier owns the long registry wait.
  for ((i=0; i<240; i++)); do
    if tail -n 1 "$log" | grep -q '^VERIFIED '; then cat "$log"; exit 0; fi
    if tail -n 1 "$log" | grep -q '^FAILED '; then cat "$log"; exit 1; fi
    sleep 1
  done
  tail -n 30 "$log"
  echo 'Verification still running; use --wait again.'
  exit 3
fi
if [[ $mode == --verify ]]; then
  set +e
  python3 eng/release/verify.py "$version" 2>&1 | tee -a "$log"
  verification_statuses=("${PIPESTATUS[@]}")
  set -e
  if (( verification_statuses[0] != 0 )); then
    exit "${verification_statuses[0]}"
  fi
  exit "${verification_statuses[1]}"
fi
[[ $(git branch --show-current) == main ]] || { echo 'Release from main.' >&2; exit 1; }
[[ -z $(git status --porcelain) ]] || { echo 'Commit all source changes before release.' >&2; exit 1; }
git fetch origin main
ahead=$(git rev-parse HEAD)
[[ $ahead == "$(git rev-parse origin/main)" ]] || { echo 'Local main must equal origin/main.' >&2; exit 1; }
python3 - "$version" <<'PY'
from pathlib import Path
import sys, xml.etree.ElementTree as E
v=sys.argv[1]
assert E.parse('Directory.Build.props').findtext('.//Version') == v, 'Version does not match'
text=Path('CHANGELOG.md').read_text()
assert '\n## '+v+'\n' in text, 'Changelog entry is missing'
notes=text.split('\n## '+v+'\n',1)[1].split('\n## ',1)[0].strip()+'\n'
Path('artifacts').mkdir(exist_ok=True)
Path('artifacts/release-'+v+'-notes.md').write_text(notes)
PY
if git show-ref --verify --quiet "refs/tags/v$version"; then echo 'Release tag already exists; inspect that release instead.' >&2; exit 1; fi
# Every release performs the required native checks; no local candidate feed is accepted.
unset QYL_LOCAL_FEED
previous_packages=${NUGET_PACKAGES-}
previous_packages_set=${NUGET_PACKAGES+x}
release_cache=$(mktemp -d "${TMPDIR:-/tmp}/qyl-testing-release-nuget.XXXXXXXX")
cleanup_release_cache() {
  local release_status=$?
  trap - EXIT
  if [[ -n $previous_packages_set ]]; then
    export NUGET_PACKAGES="$previous_packages"
  else
    unset NUGET_PACKAGES
  fi
  # Restore rewrites packageFolders, generated NuGet imports and every referenced
  # project's assets, including the runner probes. Removing the cache first leaves
  # otherwise valid --no-restore builds pointing at packages that no longer exist.
  echo 'Restoring checkout package paths before removing the release cache.'
  if dotnet restore Telemetry.Testing.slnx --source https://api.nuget.org/v3/index.json --force &&
     dotnet restore eng/build/_build.csproj --source https://api.nuget.org/v3/index.json --force; then
    rm -rf "$release_cache" || release_status=1
  else
    echo "Checkout restore failed; retained release cache: $release_cache" >&2
    # Keep the cache while any assets might still reference it. Cleanup must not
    # hide an earlier gate failure or turn a failed cleanup into a successful release.
    if (( release_status == 0 )); then release_status=1; fi
  fi
  exit "$release_status"
}
trap cleanup_release_cache EXIT
export NUGET_PACKAGES="$release_cache"
dotnet restore Telemetry.Testing.slnx --source https://api.nuget.org/v3/index.json
dotnet restore eng/build/_build.csproj --source https://api.nuget.org/v3/index.json
./build.sh Pack
package="$PWD/artifacts/packages-release/Qyl.Telemetry.Testing.$version.nupkg"
[[ -f $package ]] || { echo "Missing package: $package" >&2; exit 1; }
python3 - "$package" "$version" <<'PYZIP'
import sys, xml.etree.ElementTree as E, zipfile
with zipfile.ZipFile(sys.argv[1]) as archive:
    for name in ('README.md','THIRD-PARTY-NOTICES.md','licenses/opentelemetry-proto/LICENSE','lib/net10.0/Qyl.Telemetry.Testing.dll'):
        assert archive.read(name), 'Missing or empty package entry: '+name
    manifest=E.fromstring(archive.read('Qyl.Telemetry.Testing.nuspec'))
    ns={'n':'http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd'}
    assert manifest.findtext('n:metadata/n:version', namespaces=ns) == sys.argv[2]
PYZIP
./eng/release/smoke.sh "$version" "$PWD/artifacts/packages-release"
key_file="$HOME/.config/vitals/nuget-qyl-manual-publish-key"
[[ -s $key_file ]] || { echo "Missing NuGet credential registered in Vitals: $key_file" >&2; exit 1; }
dotnet nuget push "$package" --source https://api.nuget.org/v3/index.json --api-key "$(<"$key_file")"
git tag -a "v$version" -m "Qyl.Telemetry.Testing $version"
git push origin "v$version"
gh release create "v$version" --verify-tag --title "Qyl.Telemetry.Testing $version" --notes-file "artifacts/release-$version-notes.md" "$package"
mkdir -p artifacts
python3 - "$PWD" "$version" "$log" <<'PY'
import os, subprocess, sys
root,version,log=sys.argv[1:]
with open(log,'ab',buffering=0) as output:
 subprocess.Popen([sys.executable,root+'/eng/release/verify.py',version],stdin=subprocess.DEVNULL,stdout=output,stderr=subprocess.STDOUT,cwd=root,start_new_session=True)
PY
printf 'Published; detached registry verification: %s\n' "$log"
