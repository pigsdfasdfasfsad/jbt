from pathlib import Path
import json,sys
ROOT=Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path: sys.path.insert(0,str(ROOT))
def load(rel): return json.loads((ROOT/rel).read_text(encoding='utf-8'))
