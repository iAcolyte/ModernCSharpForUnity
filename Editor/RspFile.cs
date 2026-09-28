using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModernCSharp.Editor;

// Updates only -langversion, -nullable and the polyfill generator in a csc.rsp and keeps every other option as is.
internal static class RspFile {
    const string LangVersionOption = "langversion";
    const string NullableOption = "nullable";
    public const string GeneratorFileName = "ModernCSharp.Generators.dll";

    // Returns true when the file was created or changed.
    // A null generatorPath removes the generator; other analyzers are kept.
    public static bool Write(string path, LanguageVersion version, bool nullable, string? generatorPath) {
        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var lines = existing == null
            ? new List<string>()
            : existing.Split('\n').Select(line => line.TrimEnd('\r')).ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) {
            lines.RemoveAt(lines.Count - 1);
        }

        Upsert(lines, LangVersionOption, $"-langversion:{(int)version}");
        Upsert(lines, NullableOption, nullable ? "-nullable:enable" : "-nullable:disable");

        // Also drops a stale path, e.g. from an older Library/PackageCache/...@hash folder.
        lines.RemoveAll(IsGenerator);
        if (generatorPath != null) {
            lines.Add(generatorPath.Contains(' ') ? $"-a:\"{generatorPath}\"" : $"-a:{generatorPath}");
        }

        var content = string.Join("\n", lines) + "\n";
        if (content == existing) {
            return false;
        }

        File.WriteAllText(path, content);
        return true;
    }

    static void Upsert(List<string> lines, string option, string value) {
        var index = lines.FindIndex(line => IsOption(line, option));
        if (index < 0) {
            lines.Add(value);
            return;
        }

        lines[index] = value;
        // Duplicates would override the value we just wrote, since csc takes the last one.
        for (var i = lines.Count - 1; i > index; i--) {
            if (IsOption(lines[i], option)) {
                lines.RemoveAt(i);
            }
        }
    }

    static bool IsGenerator(string line) {
        if (!IsOption(line, "a") && !IsOption(line, "analyzer")) {
            return false;
        }

        var trimmed = line.Trim();
        var value = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim('"');
        return value.Replace('\\', '/').EndsWith("/" + GeneratorFileName, StringComparison.OrdinalIgnoreCase)
            || value.Equals(GeneratorFileName, StringComparison.OrdinalIgnoreCase);
    }

    // Matches -option, /option, -option:value, -option+ and -option-.
    static bool IsOption(string line, string option) {
        var trimmed = line.Trim();
        if (trimmed.Length <= option.Length || (trimmed[0] != '-' && trimmed[0] != '/')) {
            return false;
        }

        var name = trimmed.Substring(1);
        if (!name.StartsWith(option, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return name.Length == option.Length || name[option.Length] is ':' or '+' or '-';
    }
}
