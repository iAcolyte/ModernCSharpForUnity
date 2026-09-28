// Feature: scoped and [UnscopedRef]
// Status: Polyfill

using System;
using System.Diagnostics.CodeAnalysis;

public struct Counter {
    int value;

    [UnscopedRef]
    public ref int Value => ref value;
}

public static class Spans {
    public static int Sum(scoped ReadOnlySpan<int> items) {
        var sum = 0;
        foreach (var item in items) sum += item;
        return sum;
    }
}
