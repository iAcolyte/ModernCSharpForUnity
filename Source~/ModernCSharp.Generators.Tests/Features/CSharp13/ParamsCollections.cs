// Feature: `params` collections
// Status: Works

using System;
using System.Collections.Generic;

public static class Damage {
    public static int Total(params ReadOnlySpan<int> hits) {
        var sum = 0;
        foreach (var hit in hits) {
            sum += hit;
        }

        return sum;
    }

    public static int Count(params IEnumerable<string> tags) => 0;

    public static int Run() => Total(10, 25, 5) + Count("fire", "crit");
}
