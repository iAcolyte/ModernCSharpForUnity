// Feature: Pattern match `Span<char>` on a constant string
// Status: Works

using System;

public static class Commands {
    public static bool IsQuit(ReadOnlySpan<char> input) => input is "quit";
}
