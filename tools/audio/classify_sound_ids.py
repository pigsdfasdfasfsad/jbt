#!/usr/bin/env python3
"""Label owner-held audio IDs from source object names and instance paths.

No sound bytes are retrieved, no authentication/asset gates bypassed, and
heuristic categories are NOT verified individual sound contents.
"""
import argparse
import collections
import csv
import re
from pathlib import Path

RULES = [
    ("weapon_gunfire",r"gunshot|shoot|firearm|bullet|shotgun|gunfire"),
    ("weapon_reload",r"reload|magazine|bolt|cocking|chamber|shellinsert"),
    ("weapon_explosion",r"explosion|grenade|rocket|rpg|detonat|blast|molotov"),
    ("zombie_vocal",r"zombie|infected|burster|bloater|bolter|sprinter|groan|growl|roar|scream"),
    ("player_health",r"healing|damage|hurt|bandage|heartbeat|death|revive"),
    ("vehicle",r"engine|truck|vehicle|helicopter|siren|motor"),
    ("environment",r"ambient|environment|ocean|waves|wind|bird|rain|water|forest|airintake|airouttake|radiator|thunder|foghorn"),
    ("objective",r"objective|generator|pump|switch|radio|lever|supply|crate|restock|fortification|vending|door|gate"),
    ("music",r"music|ost|theme|victory|defeat|countdown|wave(start|end|clear)|round"),
    ("interface",r"ui|click|button|notification|purchase|shop|armory|levelup|credit|popup|menu"),
    ("movement",r"footstep|jump|walk|run|sprint|land|slide|swing"),
]

def categorize(row):
    text = (row.get("sound_names","")+" "+row.get("source_paths","")).lower()
    matching = [name for name,pattern in RULES if re.search(pattern,text)]
    return matching[0] if matching else "unclassified",matching

def process(source: Path,destination: Path):
    with source.open(encoding="utf-8-sig",newline="") as file:
        reader = csv.DictReader(file)
        original = reader.fieldnames or []
        rows = []
        for item in reader:
            label,matching = categorize(item)
            rows.append({**item,"inferred_category":label,
                         "possible_categories":" | ".join(matching),
                         "basis":"SOURCE PATHS ONLY - REVIEW REQUIRED"})
    destination.parent.mkdir(parents=True,exist_ok=True)
    with destination.open("w",encoding="utf-8",newline="") as file:
        writer = csv.DictWriter(file,fieldnames=original+[
            "inferred_category","possible_categories","basis"])
        writer.writeheader()
        writer.writerows(rows)
    print("TWR_AUDIO_IDS_CATEGORIZED total="+str(len(rows))+
          " distribution="+str(dict(collections.Counter(
              item["inferred_category"] for item in rows))))

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--input",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    process(args.input,args.output)
