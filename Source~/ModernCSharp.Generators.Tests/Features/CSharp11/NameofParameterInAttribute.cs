// Feature: nameof of a parameter in a method attribute
// Status: Works

using System.Diagnostics.CodeAnalysis;

public static class Names {
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Trim(string? value) => value?.Trim();
}
