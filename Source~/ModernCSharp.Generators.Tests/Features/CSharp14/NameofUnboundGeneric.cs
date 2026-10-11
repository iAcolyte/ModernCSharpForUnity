// Feature: nameof with unbound generic types
// Status: Works

using System.Collections.Generic;

public static class Names {
    public const string Pool = nameof(List<>);
    public const string Map = nameof(Dictionary<,>);
}
