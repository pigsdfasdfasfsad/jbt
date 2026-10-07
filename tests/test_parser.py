import gzip,json,zipfile
from pathlib import Path
import pytest
from tools.maps.rbxlx_static_parser import parse_bytes,input_members,write_ir
SAMPLE=b'''<roblox><Item class="Model" referent="R1"><Properties><string name="Name">Workspace</string></Properties><Item class="Part" referent="R2"><Properties><string name="Name">Spawn</string><bool name="Anchored">true</bool><CoordinateFrame name="CFrame"><X>1</X><Y>2</Y><Z>3</Z><R00>1</R00><R11>1</R11><R22>1</R22></CoordinateFrame><Vector3 name="Size"><X>4</X><Y>5</Y><Z>6</Z></Vector3><Color3 name="Color"><R>0.1</R><G>0.2</G><B>0.3</B></Color3><Content name="MeshId"><url>rbxassetid://123</url></Content></Properties></Item><Item class="LocalScript" referent="R3"><Properties><string name="Name">Client</string><ProtectedString name="Source">error('must never execute')</ProtectedString></Properties></Item></Item></roblox>'''
def test_parse_hierarchy_and_names():
 r=parse_bytes(SAMPLE,'x.rbxlx'); assert [x['name'] for x in r]==['Workspace','Spawn','Client']; assert r[1]['parent_ref']=='R1'
def test_parse_cframe(): assert parse_bytes(SAMPLE,'x')[1]['properties']['CFrame']['X']==1.0
def test_parse_size(): assert parse_bytes(SAMPLE,'x')[1]['properties']['Size']['Z']==6.0
def test_parse_color(): assert parse_bytes(SAMPLE,'x')[1]['properties']['Color']['G']==0.2
def test_parse_content_url(): assert parse_bytes(SAMPLE,'x')[1]['properties']['MeshId']=='rbxassetid://123'
def test_script_source_is_inert_text():
 r=parse_bytes(SAMPLE,'x')[2]; assert r['properties']['Source']=="error('must never execute')" and r['script_source_inert'] is True
def test_zip_member_read(tmp_path):
 z=tmp_path/'x.zip'
 with zipfile.ZipFile(z,'w') as f:f.writestr('A.rbxlx',SAMPLE)
 rows=list(input_members(z,None)); assert rows[0][0]=='A.rbxlx' and rows[0][1]==SAMPLE
def test_ir_header_says_scripts_not_executed(tmp_path):
 out=tmp_path/'x.jsonl.gz'; write_ir(parse_bytes(SAMPLE,'x'),out,'x','0'*64)
 with gzip.open(out,'rt') as f: h=json.loads(next(f)); assert h['scripts_executed'] is False and h['instance_count']==3
