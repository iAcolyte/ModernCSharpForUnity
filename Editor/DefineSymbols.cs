using System.Collections.Generic;

namespace ModernCSharp.Editor;

// Scripting define symbols written to every managed csc.rsp, e.g. #if MODERN_CSHARP_11_OR_NEWER.
public static class DefineSymbols {
    public const string Prefix = "MODERN_CSHARP_";
    public const string Nullable = Prefix + "NULLABLE";

    public static string OrNewer(LanguageVersion version) => $"{Prefix}{(int)version}_OR_NEWER";

    // MODERN_CSHARP_9_OR_NEWER is always present: it marks an assembly managed by the package.
    public static IReadOnlyList<string> For(LanguageVersion version, bool nullable) {
        var symbols = new List<string>();
        for (var v = (int)LanguageVersion.CSharp9; v <= (int)version; v++) {
            symbols.Add(OrNewer((LanguageVersion)v));
        }

        if (nullable) {
            symbols.Add(Nullable);
        }

        return symbols;
    }
}
