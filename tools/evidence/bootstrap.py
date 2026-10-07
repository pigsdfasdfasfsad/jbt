#!/usr/bin/env python3
from __future__ import annotations
import hashlib,json,os,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1<<20),b''):h.update(b)
 return h.hexdigest()

def main():
 manifest=json.loads((ROOT/'evidence/catalog/archive_manifest.json').read_text())
 assert len(manifest)==6, f'expected six immutable source archive records, got {len(manifest)}'
 assert all(len(x['sha256'])==64 for x in manifest)
 recovery=json.loads((ROOT/'evidence/recovery/source_archive_verification.json').read_text())
 assert recovery['all_match'] is True
 by={x['archive']:x for x in recovery['archives']}
 for x in manifest:
  assert x['archive'] in by
  assert by[x['archive']]['expected_sha256']==x['sha256']
  assert by[x['archive']]['match'] is True
 # Optional live rehash when raw evidence is deliberately mounted outside Git.
 raw=os.environ.get('TWR_EVIDENCE_DIR')
 if raw:
  rawp=Path(raw)
  names={
   '01-other-scripts-and-information.zip':'other scripts and information.zip',
   '02-videos-of-game.zip':'videos of game.zip','03-TestPlace.zip':'TestPlace.zip',
   '04-scripts.zip':'scripts.zip','05-images.zip':'images.zip','06-image-docs.zip':'image-docs.zip'}
  for x in manifest:
   p=rawp/names[x['archive']]; assert p.is_file(),p; assert sha(p)==x['sha256'],p
  print('evidence baseline verified: 6 archives (live bytes rehashed)')
 else: print('evidence baseline verified: 6 archives (recovery-time hashes + checked-in manifest)')
if __name__=='__main__': main()
