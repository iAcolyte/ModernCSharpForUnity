using System;
using System.Linq;

using UnityEditor;

namespace ModernCSharp.Editor;

[InitializeOnLoad]
internal sealed class AsmdefWatcher: AssetPostprocessor {
    static AsmdefWatcher() {
        // Covers a fresh install: rsp files appear right after the package compiles.
        EditorApplication.delayCall += () => {
            if (ModernCSharpSettings.instance.AutoApply) {
                ModernCSharpApplier.ApplyAll();
            }
        };
    }

    static void OnPostprocessAllAssets(
       string[] importedAssets,
       string[] deletedAssets,
       string[] movedAssets,
       string[] movedFromAssetPaths) {
        if (!ModernCSharpSettings.instance.AutoApply) {
            return;
        }

        var asmdefs = importedAssets
            .Concat(movedAssets)
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .Where(path => path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (asmdefs.Count == 0) {
            return;
        }

        // Importing from inside a postprocessor is unsafe, so defer to the next editor tick.
        EditorApplication.delayCall += () => ModernCSharpApplier.Apply(asmdefs, includeRoot: false);
    }
}
