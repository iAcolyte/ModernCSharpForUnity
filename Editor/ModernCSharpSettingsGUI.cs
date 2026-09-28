using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;

namespace ModernCSharp.Editor;

// Draws the Project Settings page.
internal static class ModernCSharpSettingsGUI {
    static class Content {
        public static readonly GUIContent LanguageVersion = new("Language Version", "Written as -langversion to every managed csc.rsp.");
        public static readonly GUIContent Nullable = new("Nullable", "Writes -nullable:enable when on and -nullable:disable when off.");
        public static readonly GUIContent ManageRootRsp = new("Manage Assets/csc.rsp", "Assets/csc.rsp configures Assembly-CSharp and Assembly-CSharp-Editor.");
        public static readonly GUIContent AutoApply = new("Auto Apply", "Update csc.rsp files on editor load, when an asmdef is added and when these settings change.");
        public static readonly GUIContent FilterMode = new("Path Filter", "Blacklist skips asmdefs in the listed folders, Whitelist manages only them.");
        public static readonly GUIContent Paths = new("Folders");
        public static readonly GUIContent Assemblies = new("Assemblies");
        public const string BlacklistHint = "csc.rsp is managed for every asmdef in Assets except those inside the listed folders.";
        public const string WhitelistHint = "csc.rsp is managed only for asmdefs inside the listed folders.";
    }

    static ReorderableList? pathList;
    static bool pathsChanged;
    static bool showAssemblies;
    static List<string>? asmdefs;

    static ModernCSharpSettingsGUI() {
        EditorApplication.projectChanged += () => asmdefs = null;
    }

    public static void Draw() {
        var settings = ModernCSharpSettings.instance;

        EditorGUI.BeginChangeCheck();
        var version = (LanguageVersion)EditorGUILayout.EnumPopup(Content.LanguageVersion, settings.LanguageVersion);
        var nullable = EditorGUILayout.Toggle(Content.Nullable, settings.Nullable);
        var manageRootRsp = EditorGUILayout.Toggle(Content.ManageRootRsp, settings.ManageRootRsp);
        var autoApply = EditorGUILayout.Toggle(Content.AutoApply, settings.AutoApply);

        EditorGUILayout.Space();
        var filterMode = (PathFilterMode)EditorGUILayout.EnumPopup(Content.FilterMode, settings.FilterMode);
        if (EditorGUI.EndChangeCheck()) {
            settings.LanguageVersion = version;
            settings.Nullable = nullable;
            settings.ManageRootRsp = manageRootRsp;
            settings.AutoApply = autoApply;
            settings.FilterMode = filterMode;
            Commit(settings);
        }

        EditorGUILayout.HelpBox(
            settings.FilterMode == PathFilterMode.Whitelist ? Content.WhitelistHint : Content.BlacklistHint,
            MessageType.None);

        GetPathList(settings).DoLayoutList();
        if (pathsChanged) {
            pathsChanged = false;
            Commit(settings);
        }

        DrawAssemblies(settings);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply to All Assemblies")) {
            ModernCSharpApplier.ApplyAll();
        }
    }

    static void Commit(ModernCSharpSettings settings) {
        settings.Save();
        if (settings.AutoApply) {
            ModernCSharpApplier.ApplyAll();
        }
    }

    static ReorderableList GetPathList(ModernCSharpSettings settings) {
        if (pathList != null && ReferenceEquals(pathList.list, settings.Paths)) {
            return pathList;
        }

        pathList = new ReorderableList(settings.Paths, typeof(string), true, true, true, true) {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, Content.Paths),
            drawElementCallback = (rect, index, _, _) => DrawPath(settings, rect, index),
            onAddCallback = _ => {
                var picked = PickFolder("Assets");
                if (picked != null) {
                    settings.Paths.Add(picked);
                    pathsChanged = true;
                }
                GUIUtility.ExitGUI();
            },
            onRemoveCallback = list => {
                settings.Paths.RemoveAt(list.index);
                pathsChanged = true;
            },
            onReorderCallback = _ => pathsChanged = true,
        };
        return pathList;
    }

    static void DrawPath(ModernCSharpSettings settings, Rect rect, int index) {
        const float buttonWidth = 24f;
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        var fieldRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - 4f, rect.height);
        var buttonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);

        // Delayed so a half-typed path doesn't trigger a rewrite of every csc.rsp.
        var value = EditorGUI.DelayedTextField(fieldRect, settings.Paths[index]);
        if (value != settings.Paths[index]) {
            settings.Paths[index] = PathFilter.Normalize(value);
            pathsChanged = true;
        }

        if (GUI.Button(buttonRect, "…")) {
            var picked = PickFolder(settings.Paths[index]);
            if (picked != null) {
                settings.Paths[index] = picked;
                Commit(settings);
            }
            GUIUtility.ExitGUI();
        }
    }

    // Returns a project-relative path such as Assets/Plugins, or null when cancelled or outside Assets.
    static string? PickFolder(string current) {
        var projectRoot = PathFilter.Normalize(Path.GetDirectoryName(Application.dataPath) ?? string.Empty);
        var start = Directory.Exists(current) ? current : "Assets";
        var absolute = EditorUtility.OpenFolderPanel("Select Folder", start, string.Empty);
        if (string.IsNullOrEmpty(absolute)) {
            return null;
        }

        absolute = PathFilter.Normalize(absolute);
        if (!absolute.StartsWith(projectRoot + "/Assets", StringComparison.OrdinalIgnoreCase)) {
            EditorUtility.DisplayDialog("C# Language", "Pick a folder inside Assets.", "OK");
            return null;
        }

        return absolute.Substring(projectRoot.Length + 1);
    }

    static void DrawAssemblies(ModernCSharpSettings settings) {
        showAssemblies = EditorGUILayout.Foldout(showAssemblies, Content.Assemblies, true);
        if (!showAssemblies) {
            return;
        }

        asmdefs ??= ModernCSharpApplier.FindAsmdefs();
        using var indent = new EditorGUI.IndentLevelScope();

        if (settings.ManageRootRsp) {
            EditorGUILayout.LabelField("Predefined assemblies", ModernCSharpApplier.RootRspPath);
        }

        foreach (var path in asmdefs) {
            var status = ModernCSharpApplier.GetStatus(path, settings) switch {
                AsmdefStatus.Managed => "managed",
                AsmdefStatus.Package => "package (own csc.rsp)",
                _ => "skipped",
            };
            EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(path), status);
        }
    }
}
