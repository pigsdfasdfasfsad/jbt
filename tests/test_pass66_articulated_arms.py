"""Pass66: articulated camera-anchored arms following original weapon welds.

The source TestPlace Survivor1 limb sizes are 1 x 2 x 1 studs. The
camera-view arm solver is a documented approximation and must never
claim original avatar mesh or Motor6D shoulder/elbow data.
"""
from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_two_bone_solver_bounded_by_original_r6_stud_scale():
    arm=read("src/Twr.Godot/Scripts/Pass66ArmKinematics.cs")
    assert "public const float NominalSegmentStuds=1f" in arm
    assert "public const float MaxReachScale=1.65f" in arm
    assert "RobloxUnits.MetersPerStud" in arm
    assert "Math.Clamp(" in arm
    assert "float.IsFinite" in arm
    assert "MaxReachScale" in arm
    assert "upper*upper+reach*reach-fore*fore" in arm
    assert "MathF.Sqrt(Math.Max(0f,upper*upper-along*along))" in arm
    assert "Vector3.Down*.55f" in arm
    assert "bend=bend.Normalized()" in arm
    assert "public static bool TrySegment" in arm
    assert "Math.Abs(basis.Determinant()-1f)" in arm
    assert "return new Solution(shoulder,elbow,projected" in arm
    assert "SpendAmmo" not in arm and "AwardKill" not in arm
    assert "HttpClient" not in arm and "Process.Start" not in arm


def test_camera_local_arm_assembly_is_independent_of_weapon_recoil():
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    assert "private Node3D? _armRig66;" in view
    assert 'Name="ArmRig66"' in view
    assert "AddChild(_armRig66);" in view
    assert "BuildSourceArms();" in view
    assert "UpdateSourceArms();" in view
    assert "Pass66ArmKinematics.Solve(true,leftWrist)" in view
    assert "Pass66ArmKinematics.Solve(false,rightWrist)" in view
    assert "SourceArmProxyMeshCount=8" in view
    for name in ("LeftUpperArm66","LeftForeArm66","LeftElbow66",
                 "RightUpperArm66","RightForeArm66","RightElbow66"):
        assert name in view
    assert "Pass66ArmKinematics.Shoulder(true)" in view
    assert "Pass66ArmKinematics.Shoulder(false)" in view
    assert "_rig.Transform*(_leftHandProxy.Transform*cuff)" in view
    assert "_rig.Transform*(_rightHandProxy.Transform*cuff)" in view
    assert "new Vector3(1f,length,1f)" in view
    assert "if(_armRig66 is not null)" in view
    assert "RemoveChild(_armRig66);" in view
    assert "if(_armRig66 is not null)_armRig66.Visible=false;" in view
    assert "SourceHandProxyMeshCount+=7" in view
    assert "OriginalWeaponSourceRuntime.TryBuild(_rig!, spec.Name)" in view
    assert "public void Fire(bool aiming = false)" in view
    assert "public void Reload(double seconds)" in view


def test_native_game_smoke_covers_weapon_anim_arms_and_rig_lifecycle():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    original=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert "RunPass66Smoke()" in boot
    assert "TWR_SMOKE_PASS66_ARTICULATED_ARMS_OK" in boot
    for marker in (
        "deterministic_finite_two_bone_ik=true",
        "reach_limit_and_degenerate_input_guards=true",
        "two_upper_and_two_forearm_meshes=true",
        "source_glock_reload_elbows_follow=true",
        "source_mossberg_left_pump_elbow_follows=true",
        "source_mosin_right_bolt_elbow_follows=true",
        "source_hand_and_arm_rest_reset=true",
        "source_showroom_hides_approximate_arms=true",
        "weapon_switch_arm_rig_cleanup=true",
        "no_original_avatar_mesh_claim=true",
        "no_gameplay_ammo_or_damage_changes=true"
    ):
        assert marker in boot
    assert 'or "--smoke-pass66"' in original
    assert 'or "--smoke-pass66"' in hierarchy
    assert "twr-pass66-articulated-source-arms" in ci
    assert "make_pass64_bolt_shell_fixture.py" in ci
    assert "--smoke-pass66" in ci
    assert "TWR_SMOKE_PASS66_ARTICULATED_ARMS_OK" in ci
    assert "Owner-private weapon motor hierarchy sidecar remained" in ci
    for num in range(59,67):
        assert "--smoke-pass"+str(num) in ci
    assert ci.count("if: false && success()")>=14
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")


def test_arms_remain_render_only_not_fabricated_source_motor_data():
    render=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    solver=read("src/Twr.Godot/Scripts/Pass66ArmKinematics.cs")
    assert "NOT recovered" not in render or "original" in render
    assert "not original character assets" in render
    assert "Motor6D" in solver
    assert "shoulder" in solver.lower() and "elbow" in solver.lower()
    assert "new CylinderMesh" in render and "new SphereMesh" in render
    assert "original Roblox" in solver
    assert "No Roblox network access" in render
