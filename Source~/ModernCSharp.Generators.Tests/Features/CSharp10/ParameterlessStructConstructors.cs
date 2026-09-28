// Feature: Parameterless struct constructors and field initializers
// Status: Caveats

public struct Stats {
    public int Health = 100;
    public float Speed;

    public Stats() {
        Speed = 5f;
    }
}
