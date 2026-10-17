#!/usr/bin/env python3
"""Wait for the immutable public package and exercise it from an isolated consumer."""
from pathlib import Path
import json
import os
import re
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

version = sys.argv[1]
if not re.fullmatch(r'\d+\.\d+\.\d+', version):
    raise SystemExit('Expected x.y.z')
root = Path(__file__).resolve().parents[2]
url = 'https://api.nuget.org/v3-flatcontainer/qyl.telemetry.testing/index.json'
print(f'Verifying Qyl.Telemetry.Testing {version}', flush=True)
try:
    deadline = time.monotonic() + 2700
    while True:
        try:
            with urllib.request.urlopen(url + f'?fresh={time.time_ns()}', timeout=30) as response:
                if version in json.load(response)['versions']:
                    break
        except (urllib.error.URLError, TimeoutError):
            pass
        if time.monotonic() >= deadline:
            raise RuntimeError('Public package was not indexed within 45 minutes')
        time.sleep(10)
    # The publisher deletes its cache on exit. A detached verifier must own a
    # separate cache, and must restore the public package rather than reuse the
    # candidate that the pre-publication consumer installed there.
    with tempfile.TemporaryDirectory(prefix='qyl-testing-public-nuget.') as packages:
        print('Public consumer uses its own fresh package cache.', flush=True)
        subprocess.run([str(root / 'eng/release/smoke.sh'), version], cwd=root, check=True,
                       env={**os.environ, 'NUGET_PACKAGES': packages})
    print(f'VERIFIED Qyl.Telemetry.Testing {version}: public-only restore and live receiver smoke passed', flush=True)
except Exception as error:
    print(f'FAILED Qyl.Telemetry.Testing {version}: {error}', flush=True)
    raise SystemExit(1)
