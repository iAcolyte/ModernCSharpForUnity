using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace ModernCSharp.Editor;

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
[FilePath("ProjectSettings/ModernCSharpSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public sealed class ModernCSharpSettings: ScriptableSingleton<ModernCSharpSettings> {
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
}
