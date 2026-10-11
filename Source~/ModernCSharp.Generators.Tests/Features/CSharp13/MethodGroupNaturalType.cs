// Feature: Method group natural type
// Status: Works

using System;

public sealed class Cooldown {
    public void Reset() { }
}

public static class CooldownExtensions {
    public static void Reset(this Cooldown cooldown, float delay) { }
}

public static class Usage {
    // Before C# 13 the extension overload made the type of `cooldown.Reset` ambiguous.
    public static Delegate Run(Cooldown cooldown) {
        var reset = cooldown.Reset;
        return reset;
    }
}
