// Feature: [OverloadResolutionPriority]
// Status: Polyfill

using System;
using System.Runtime.CompilerServices;

public static class Log {
    public static int Write(string message) => 1;

    [OverloadResolutionPriority(1)]
    public static int Write(ReadOnlySpan<char> message) => 2;

    public static int Run() => Write("spawned");
}
