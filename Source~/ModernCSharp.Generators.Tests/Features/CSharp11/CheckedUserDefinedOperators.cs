// Feature: Checked user-defined operators
// Status: Works

public readonly struct Gold {
    public readonly int Value;

    public Gold(int value) => Value = value;

    public static Gold operator +(Gold a, Gold b) => new(a.Value + b.Value);

    public static Gold operator checked +(Gold a, Gold b) => new(checked(a.Value + b.Value));
}
