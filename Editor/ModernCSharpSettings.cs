using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

// A block namespace on purpose: Unity doesn't link a script with a file-scoped namespace to its class,
// and ScriptableSingleton then can't load the saved settings back.
namespace ModernCSharp.Editor {
    public enum LanguageVersion {
        [InspectorName("C# 9")] CSharp9 = 9,
        [InspectorName("C# 10")] CSharp10 = 10,
        [InspectorName("C# 11")] CSharp11 = 11,
        [InspectorName("C# 12")] CSharp12 = 12,
        [InspectorName("C# 13")] CSharp13 = 13,
        [InspectorName("C# 14")] CSharp14 = 14,
    }

    // The range the editor's compiler accepts in -langversion:
    // Roslyn 4.3 up to Unity 6000.4, Roslyn 4.10 in 6000.5–6000.6, Roslyn 5.6 since 6000.7.
    public static class LanguageVersions {
        public const LanguageVersion Min = LanguageVersion.CSharp9;
#if UNITY_6000_7_OR_NEWER
        public const LanguageVersion Max = LanguageVersion.CSharp14;
#elif UNITY_6000_5_OR_NEWER
        public const LanguageVersion Max = LanguageVersion.CSharp12;
#else
        public const LanguageVersion Max = LanguageVersion.CSharp10;
#endif

        public static bool IsSupported(LanguageVersion version) => version >= Min && version <= Max;

        public static LanguageVersion Clamp(LanguageVersion version) => version < Min ? Min : version > Max ? Max : version;
    }

    public enum PathFilterMode {
        // Every asmdef under Assets except the listed folders.
        Blacklist,
        // Only asmdefs inside the listed folders.
        Whitelist,
    }

    // Stored in ProjectSettings/ so the whole team shares the same compiler options.
    [FilePath(AssetPath, FilePathAttribute.Location.ProjectFolder)]
    public sealed class ModernCSharpSettings: ScriptableSingleton<ModernCSharpSettings> {
        const string AssetPath = "ProjectSettings/ModernCSharpSettings.asset";
        const string MissingScript = "m_Script: {fileID: 0}";

        [SerializeField] LanguageVersion languageVersion = LanguageVersions.Min;
        [SerializeField] bool nullable = true;
        [SerializeField] bool manageRootRsp = true;
        [SerializeField] bool autoApply = true;
        [SerializeField] PathFilterMode filterMode = PathFilterMode.Blacklist;
        [SerializeField] List<string> paths = new();

        public LanguageVersion LanguageVersion {
            get => languageVersion;
            set => languageVersion = value;
        }

        // What goes to csc.rsp. The stored value stays as is: the team may open the project in a newer editor.
        public LanguageVersion EffectiveLanguageVersion => LanguageVersions.Clamp(languageVersion);

        public bool Nullable {
            get => nullable;
            set => nullable = value;
        }

        // Assets/csc.rsp configures the predefined assemblies (Assembly-CSharp, Assembly-CSharp-Editor).
        public bool ManageRootRsp {
            get => manageRootRsp;
            set => manageRootRsp = value;
        }

        // Rewrites csc.rsp files on editor load, on asmdef import and on every settings change.
        public bool AutoApply {
            get => autoApply;
            set => autoApply = value;
        }

        public PathFilterMode FilterMode {
            get => filterMode;
            set => filterMode = value;
        }

        public List<string> Paths => paths;

        public void Save() => Save(true);

        // Before 0.2.1 the file was saved without a script reference, and Unity never loads such a file back.
        // Restores the reference; must run before the first access to instance.
        internal static void RepairSavedFile() {
            if (!File.Exists(AssetPath)) {
                return;
            }

            var text = File.ReadAllText(AssetPath);
            if (!text.Contains(MissingScript)) {
                return;
            }

            var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(typeof(ModernCSharpSettings).Assembly.GetName().Name);
            var folder = asmdef == null ? null : Path.GetDirectoryName(asmdef);
            var guid = folder == null ? null : AssetDatabase.AssetPathToGUID(PathFilter.Normalize(folder) + "/" + nameof(ModernCSharpSettings) + ".cs");
            if (string.IsNullOrEmpty(guid)) {
                return;
            }

            File.WriteAllText(AssetPath, text.Replace(MissingScript, $"m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}"));
        }
    }
}
