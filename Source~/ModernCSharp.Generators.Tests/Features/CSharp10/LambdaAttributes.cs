// Feature: Attributes on lambdas
// Status: Works

using System;
using System.Diagnostics.CodeAnalysis;

public static class Lambdas {
    public static Func<string?, string> Run() => [return: NotNull] (string? s) => s ?? "";
}
