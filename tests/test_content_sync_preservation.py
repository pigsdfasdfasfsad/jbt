import json
from pathlib import Path
import pytest
from tools.content.sync_godot_content import sync, STATE, BACKUPS, digest


def fixture(tmp_path):
    src=tmp_path/'content'; src.mkdir()
    dst=tmp_path/'src/Twr.Godot/Content'; dst.mkdir(parents=True)
    (src/'metadata.json').write_bytes(b'{"canonical":1}')
    return src,dst


def test_unknown_private_scene_survives_every_sync(tmp_path):
    src,dst=fixture(tmp_path)
    private=dst/'Maps/Laboratory.scene.jsonl.gz';private.parent.mkdir();private.write_bytes(b'private original geometry')
    sync(tmp_path);sync(tmp_path)
    assert private.read_bytes()==b'private original geometry'
    assert (dst/'metadata.json').read_bytes()==(src/'metadata.json').read_bytes()


def test_conflicting_metadata_saved_before_replace(tmp_path):
    src,dst=fixture(tmp_path)
    original=b'{"custom":true}';(dst/'metadata.json').write_bytes(original)
    result=sync(tmp_path)
    assert result['backups']==1
    assert (tmp_path/BACKUPS/digest(original)/'metadata.json').read_bytes()==original
    assert (dst/'metadata.json').read_bytes()==(src/'metadata.json').read_bytes()
    assert sync(tmp_path)['backups']==0


def test_owned_stale_files_retired_but_user_modified_stale_preserved(tmp_path):
    src,dst=fixture(tmp_path)
    (src/'retired.json').write_bytes(b'old generated')
    (src/'edited.json').write_bytes(b'old generated')
    sync(tmp_path)
    (dst/'edited.json').write_bytes(b'user modified')
    (src/'retired.json').unlink();(src/'edited.json').unlink()
    result=sync(tmp_path)
    assert not (dst/'retired.json').exists()
    assert (dst/'edited.json').read_bytes()==b'user modified'
    assert result['modified_stale_preserved']==1


def test_first_sync_never_deletes_unowned_metadata(tmp_path):
    src,dst=fixture(tmp_path);(dst/'installed.json').write_bytes(b'private metadata')
    sync(tmp_path);assert (dst/'installed.json').read_bytes()==b'private metadata'


def test_ordinary_canonical_update_does_not_make_private_backup(tmp_path):
    src,dst=fixture(tmp_path);sync(tmp_path)
    (src/'metadata.json').write_bytes(b'next generated version')
    assert sync(tmp_path)['backups']==0
    assert (dst/'metadata.json').read_bytes()==b'next generated version'


@pytest.mark.parametrize('name',['../outside','/absolute','dir\\escape','C:/escape'])
def test_malicious_manifest_refused_before_any_write(tmp_path,name):
    src,dst=fixture(tmp_path);(dst/'metadata.json').write_bytes(b'unchanged')
    state=tmp_path/STATE;state.parent.mkdir(parents=True)
    state.write_text(json.dumps({'version':1,'owned':{name:'a'*64}}))
    with pytest.raises(ValueError):sync(tmp_path)
    assert (dst/'metadata.json').read_bytes()==b'unchanged'


def test_linked_destination_and_linked_input_are_refused(tmp_path):
    src,dst=fixture(tmp_path)
    outside=tmp_path/'outside';outside.write_bytes(b'not changed')
    try:(dst/'metadata.json').symlink_to(outside)
    except OSError:pytest.skip('Link creation not available')
    with pytest.raises(ValueError):sync(tmp_path)
    assert outside.read_bytes()==b'not changed'
    (dst/'metadata.json').unlink();(src/'link').symlink_to(outside)
    with pytest.raises(ValueError):sync(tmp_path)


def test_absent_or_empty_canonical_tree_fails_without_deleting(tmp_path):
    src,dst=fixture(tmp_path);sync(tmp_path);(src/'metadata.json').unlink()
    with pytest.raises(ValueError):sync(tmp_path)
    assert (dst/'metadata.json').exists()


def test_public_export_still_rejects_preserved_private_content(tmp_path):
    from tools.build.package_source_safe import validate_embedded_inputs
    src,dst=fixture(tmp_path);(dst/'Laboratory.scene.jsonl.gz').write_bytes(b'private')
    sync(tmp_path)
    with pytest.raises(ValueError):validate_embedded_inputs(dst.parent)
