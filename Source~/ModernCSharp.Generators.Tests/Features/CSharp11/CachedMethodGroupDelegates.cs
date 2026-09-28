// Feature: Cached method group delegates
// Status: Works

using System;
using System.Collections.Generic;

public static class Filters {
    static bool IsEven(int x) => x % 2 == 0;

    public static List<int> Run(List<int> items) => items.FindAll(IsEven);
}
