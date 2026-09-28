using UnityEditor;

namespace ModernCSharp.Editor;

internal static class ModernCSharpSettingsProvider {
    [SettingsProvider]
    static SettingsProvider Create() =>
        new("Project/C# Language", SettingsScope.Project) {
            guiHandler = _ => ModernCSharpSettingsGUI.Draw(),
            keywords = new[] { "C#", "langversion", "nullable", "csc.rsp", "asmdef" },
        };
}
