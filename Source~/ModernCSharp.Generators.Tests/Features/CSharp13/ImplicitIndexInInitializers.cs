// Feature: Implicit index access in object initializers
// Status: Works

public sealed class Hotbar {
    public string[] Slots = new string[4];
}

public static class Usage {
    public static Hotbar Run() => new() { Slots = { [0] = "sword", [^1] = "potion" } };
}
