// Feature: Extended property patterns
// Status: Works

public sealed class Weapon { public int Damage; }
public sealed class Player { public Weapon Weapon = new(); }

public static class Rules {
    public static bool IsDangerous(Player player) => player is { Weapon.Damage: > 50 };
}
