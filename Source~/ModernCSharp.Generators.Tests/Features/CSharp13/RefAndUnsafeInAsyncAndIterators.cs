// Feature: ref and unsafe in iterators and async methods
// Status: Works

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class Chunks {
    public static async Task<int> SumAsync(int[] data) {
        await Task.Yield();
        ReadOnlySpan<int> span = data;
        var sum = 0;
        foreach (ref readonly var value in span) {
            sum += value;
        }

        return sum;
    }

    public static IEnumerable<int> Lengths(string[] lines) {
        foreach (var line in lines) {
            ReadOnlySpan<char> trimmed = line.AsSpan().Trim();
            var length = trimmed.Length;
            yield return length;
        }
    }
}
