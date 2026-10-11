// Feature: Implicit span conversions
// Status: Works

using System;

public static class Buffers {
    static int Sum<T>(ReadOnlySpan<T> values) => values.Length;

    static void Clear<T>(Span<T> values) => values.Clear();

    public static int Run(int[] scores) {
        Clear(scores);
        return Sum(scores) + scores.Length;
    }
}
