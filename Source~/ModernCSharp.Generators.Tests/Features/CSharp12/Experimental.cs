// Feature: [Experimental]
// Status: Polyfill

using System.Diagnostics.CodeAnalysis;

[Experimental("PG0001")]
public static class NewPathfinder {
    public static int Find() => 0;
}

public static class Usage {
#pragma warning disable PG0001
    public static int Run() => NewPathfinder.Find();
#pragma warning restore PG0001
}
