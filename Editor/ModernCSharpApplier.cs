using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Compilation;

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

    static readonly string[] PackageAssemblies = { "ModernCSharp.Editor" };

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

    // Relative to the project folder, where Unity runs csc. The package path changes with every
    // Library/PackageCache/...@hash update, so it is resolved on each apply.
    public static string? GetGeneratorPath() {
        var root = GetPackageRoot();
        if (root == null) {
            Debug.LogError("[C# Language] Can't find the Modern C# for Unity package folder.");
            return null;
        }

        var path = Path.Combine(root, "Generators~", RspFile.GeneratorFileName);
        if (!File.Exists(path)) {
            Debug.LogError($"[C# Language] Polyfill generator not found: {path}");
            return null;
        }

        return PathFilter.Normalize(Path.GetRelativePath(Directory.GetCurrentDirectory(), path));
    }

    static string? GetPackageRoot() {
        var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ModernCSharpApplier).Assembly);
        if (package != null) {
            return package.resolvedPath;
        }

        // Not installed as a package, e.g. copied into Assets/: the asmdef lives in <root>/Editor/.
        var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(typeof(ModernCSharpApplier).Assembly.GetName().Name);
        var editorFolder = asmdef == null ? null : Path.GetDirectoryName(Path.GetFullPath(asmdef));
        return editorFolder == null ? null : Path.GetDirectoryName(editorFolder);
    }

    internal static void Apply(IEnumerable<string> asmdefPaths, bool includeRoot) {
        var settings = ModernCSharpSettings.instance;
        var targets = asmdefPaths
            .Where(path => GetStatus(path, settings) == AsmdefStatus.Managed)
            .Select(GetRspPath)
            .ToList();

        if (includeRoot && settings.ManageRootRsp) {
            targets.Add(RootRspPath);
        }

        var generatorPath = GetGeneratorPath();
        var defines = DefineSymbols.For(settings.LanguageVersion, settings.Nullable);
        var changed = targets
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => RspFile.Write(path, settings.LanguageVersion, settings.Nullable, defines, generatorPath))
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
