using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace ModernCSharp.Editor;

public enum LanguageVersion {
    [InspectorName("C# 9")] CSharp9 = 9,
    [InspectorName("C# 10")] CSharp10 = 10,
    [InspectorName("C# 11")] CSharp11 = 11,
    [InspectorName("C# 12")] CSharp12 = 12,
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
    [SerializeField] LanguageVersion languageVersion = LanguageVersion.CSharp12;
    [SerializeField] bool nullable = true;
    [SerializeField] bool manageRootRsp = true;
    [SerializeField] bool autoApply = true;
    [SerializeField] PathFilterMode filterMode = PathFilterMode.Blacklist;
    [SerializeField] List<string> paths = new();

    public LanguageVersion LanguageVersion {
        get => languageVersion;
        set => languageVersion = value;
    }

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
