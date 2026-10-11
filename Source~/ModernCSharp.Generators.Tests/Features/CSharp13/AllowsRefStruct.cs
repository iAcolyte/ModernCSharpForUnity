// Feature: allows ref struct
// Status: Error
// Error: CS9240

using System;

public static class Parser {
    public static int Count<T>(T source) where T : allows ref struct => 0;

    public static int Run() => Count(new ReadOnlySpan<int>());
}
