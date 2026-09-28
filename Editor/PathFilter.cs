using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModernCSharp.Editor;

internal static class PathFilter {
    public static string Normalize(string path) => path.Replace('\\', '/').Trim().TrimEnd('/');

    public static bool IsIncluded(string asmdefPath, ModernCSharpSettings settings) =>
        IsIncluded(asmdefPath, settings.FilterMode, settings.Paths);

    // An asmdef matches an entry when its folder is the entry itself or lies inside it.
    public static bool IsIncluded(string asmdefPath, PathFilterMode mode, IEnumerable<string> paths) {
        var folder = Normalize(Path.GetDirectoryName(asmdefPath) ?? string.Empty);
        var matched = paths
            .Select(Normalize)
            .Where(entry => entry.Length > 0)
            .Any(entry => IsSameOrInside(folder, entry));

        return mode == PathFilterMode.Whitelist ? matched : !matched;
    }

    static bool IsSameOrInside(string folder, string entry) =>
       folder.Equals(entry, StringComparison.OrdinalIgnoreCase) ||
       folder.StartsWith(entry + "/", StringComparison.OrdinalIgnoreCase);
}
