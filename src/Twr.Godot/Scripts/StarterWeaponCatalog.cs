using Twr.Domain.Services;

namespace Twr.Godot;

public enum StarterWeaponKind
{
    Semi,
    Shotgun,
    Melee
}

public sealed record StarterWeaponSpec(
    string Name,
    StarterWeaponKind Kind,
    float Damage,
    int MagazineCapacity,
    double Rpm,
    double ReloadSeconds,
    float Range,
    float SpreadDegrees,
    int Pellets,
    double ActionSeconds);

public static class StarterWeaponCatalog
{
    // All listed module values are source-backed. The Sawn Off pellet count and
    // melee reach are not present in the surviving weapon modules and remain
    // explicit reconstruction parameters.
    public static readonly StarterWeaponSpec Glock17 = new(
        StarterLoadoutService.Glock17,
        StarterWeaponKind.Semi,
        15f,
        18,
        400,
        1.85,
        1000f,
        0f,
        1,
        0.225);

    public static readonly StarterWeaponSpec SawnOff = new(
        StarterLoadoutService.SawnOff,
        StarterWeaponKind.Shotgun,
        14.5f,
        2,
        300,
        3.7025,
        1000f,
        5f,
        8, // APPROXIMATED pellet count; surviving module provides Spread but not pellet count.
        0.565);

    public static readonly StarterWeaponSpec TwoByFour = new(
        StarterLoadoutService.TwoByFour,
        StarterWeaponKind.Melee,
        15.5f,
        0,
        0,
        0,
        4.5f, // APPROXIMATED melee reach.
        0f,
        1,
        0.70125);

    public static StarterWeaponSpec Get(string name) => name switch
    {
        StarterLoadoutService.SawnOff => SawnOff,
        StarterLoadoutService.TwoByFour => TwoByFour,
        _ => Glock17
    };
}
