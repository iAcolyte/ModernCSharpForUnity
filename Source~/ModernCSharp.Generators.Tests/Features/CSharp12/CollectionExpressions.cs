// Feature: Collection expressions
// Status: Works

using System;
using System.Collections.Generic;

public static class Collections {
    public static int[] Array() => [1, 2, 3];

    public static List<int> List(int[] more) => [0, .. more];

    public static int Sum() {
        Span<int> span = [1, 2, 3];
        return span[0] + span[2];
    }
}
