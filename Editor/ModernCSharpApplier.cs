using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace ModernCSharp.Editor;

public enum AsmdefStatus {
    Managed,
    Skipped,
    // The package's own assemblies keep the csc.rsp they ship with.
    Package,
}

public static class ModernCSharpApplier {
    public const string RootRspPath = "Assets/csc.rsp";

    static readonly string[] PackageAssemblies = { "ModernCSharp.Polyfills", "ModernCSharp.Editor" };

    public static void ApplyAll() => Apply(FindAsmdefs(), includeRoot: true);

    // Only Assets/: files in Packages/ and Library/PackageCache are read-only.
    public static List<string> FindAsmdefs() =>
        AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static AsmdefStatus GetStatus(string asmdefPath, ModernCSharpSettings settings) {
        if (PackageAssemblies.Contains(Path.GetFileNameWithoutExtension(asmdefPath))) {
            return AsmdefStatus.Package;
        }

        return PathFilter.IsIncluded(asmdefPath, settings) ? AsmdefStatus.Managed : AsmdefStatus.Skipped;
    }

    public static string GetRspPath(string asmdefPath) =>
        PathFilter.Normalize(Path.GetDirectoryName(asmdefPath) ?? string.Empty) + "/csc.rsp";

    internal static void Apply(IEnumerable<string> asmdefPaths, bool includeRoot) {
        var settings = ModernCSharpSettings.instance;
        var targets = asmdefPaths
            .Where(path => GetStatus(path, settings) == AsmdefStatus.Managed)
            .Select(GetRspPath)
            .ToList();

        if (includeRoot && settings.ManageRootRsp) {
            targets.Add(RootRspPath);
        }

        var changed = targets
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => RspFile.Write(path, settings.LanguageVersion, settings.Nullable))
            .ToList();

        if (changed.Count == 0) {
            return;
        }

        AssetDatabase.StartAssetEditing();
        try {
            foreach (var path in changed) {
                AssetDatabase.ImportAsset(path);
            }
        } finally {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[C# Language] Updated {changed.Count} csc.rsp:\n{string.Join("\n", changed)}");
    }
}
