// Feature: UTF-8 string literals
// Status: Works

using System;

public static class Protocol {
    public static ReadOnlySpan<byte> Header => "GAME"u8;
}
