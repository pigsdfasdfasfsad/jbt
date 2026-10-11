"""Pass36 native Windows gameplay lifecycle is beyond domain-only wave smoke."""
from conftest import ROOT

def test_15_wave_native_scene_pipeline_and_export_gate():
    bootstrap=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
    for text in ('--smoke-pass36','TWR_SMOKE_PASS36_NATIVE_WAVES_OK',
                 'RunPass36NativeWaveSmoke','waves=15 transitions=','game._Process(600)',
                 'game.WaveStage != "RESULTS"','game.ActiveInfectedCount != 0'):
        assert text in bootstrap
    assert '--smoke-pass36' in workflow
    assert 'twr-pass36-native-wave-lifecycle' in workflow
    assert 'make_pass30_synthetic_sidecars.py --output $outputDir --clean' in workflow
    assert 'TWR-Pass36-Windows-x64-NoPrivateAssets' in workflow
