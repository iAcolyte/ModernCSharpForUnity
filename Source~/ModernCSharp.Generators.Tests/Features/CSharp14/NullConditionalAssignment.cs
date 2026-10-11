// Feature: Null-conditional assignment
// Status: Works

using System;

public sealed class Target {
    public int Health;
    public event Action? Died;

    public void Kill() => Died?.Invoke();
}

public static class Usage {
    public static void Run(Target? target) {
        target?.Health = 0;
        target?.Health -= 10;
        target?.Died += () => { };
    }
}
