// Feature: [CallerArgumentExpression]
// Status: Polyfill

using System;
using System.Runtime.CompilerServices;

public static class Check {
    public static void That(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null) {
        if (!condition) throw new InvalidOperationException(expression);
    }

    public static void Run(int hp) => That(hp > 0);
}
