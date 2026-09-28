using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace ModernCSharp.Generators.Tests;

public enum FeatureStatus {
    // Compiles without the polyfill generator.
    Works,
    // Fails without the generator and compiles with it.
    Polyfill,
    // Compiles; the caveat in the README is about Unity or the runtime, not the compiler.
    Caveats,
    // Fails with the error from the snippet header even with the generator.
    Error,
}

public sealed class Feature {
    public Feature(string path) {
        Path = path;
        Source = File.ReadAllText(path);
        Name = Header("Feature") ?? throw new FormatException($"{path}: no // Feature: header");
        Status = Enum.Parse<FeatureStatus>(Header("Status") ?? throw new FormatException($"{path}: no // Status: header"));
        Error = Header("Error");
        Version = int.Parse(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)!)!.Replace("CSharp", ""));
    }

    public string Path { get; }

    public string Source { get; }

    public string Name { get; }

    public FeatureStatus Status { get; }

    public string? Error { get; }

    public int Version { get; }

    public override string ToString() => $"C# {Version}: {Name}";

    string? Header(string key) {
        var match = Regex.Match(Source, $@"^// {key}: (.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}

// Every C# 10–12 feature from the README, compiled the way Unity 6000.6 compiles it.
public sealed class FeatureTests {
    static readonly Dictionary<string, FeatureStatus> ReadmeStatuses = new() {
        ["✅ Works"] = FeatureStatus.Works,
        ["🧩 Needs a polyfill"] = FeatureStatus.Polyfill,
        ["⚠️ With caveats"] = FeatureStatus.Caveats,
        ["❌ Doesn't work"] = FeatureStatus.Error,
    };

    public static IEnumerable<TestCaseData> Features() =>
        LoadFeatures().Select(feature => new TestCaseData(feature).SetName($"{{m}}(C# {feature.Version}: {feature.Name})"));

    [TestCaseSource(nameof(Features))]
    public void CompilesAsDocumented(Feature feature) {
        var withGenerator = TestCompiler.Compile(feature.Source);

        switch (feature.Status) {
            case FeatureStatus.Works:
                var withoutGenerator = TestCompiler.Compile(feature.Source, withGenerator: false);
                Assert.That(withoutGenerator.Errors, Is.Empty, withoutGenerator.Format());
                break;
            case FeatureStatus.Polyfill:
                Assert.That(TestCompiler.Compile(feature.Source, withGenerator: false).Errors, Is.Not.Empty,
                    "compiles without the generator, so it doesn't need a polyfill");
                Assert.That(withGenerator.Errors, Is.Empty, withGenerator.Format());
                break;
            case FeatureStatus.Caveats:
                Assert.That(withGenerator.Errors, Is.Empty, withGenerator.Format());
                break;
            case FeatureStatus.Error:
                Assert.That(feature.Error, Is.Not.Null, "// Error: header is required for Status: Error");
                Assert.That(withGenerator.Errors.Select(d => d.Id), Has.Member(feature.Error), withGenerator.Format());
                break;
        }
    }

    [Test]
    public void SnippetsMatchReadme() {
        var readme = ParseReadme();
        var snippets = LoadFeatures().ToDictionary(f => f.Name, f => (f.Version, f.Status));

        Assert.Multiple(() => {
            Assert.That(snippets.Keys.Except(readme.Keys), Is.Empty, "snippets missing from the README");
            Assert.That(readme.Keys.Except(snippets.Keys), Is.Empty, "README features without a snippet");
            foreach (var (name, expected) in readme.Where(pair => snippets.ContainsKey(pair.Key))) {
                Assert.That(snippets[name], Is.EqualTo(expected), name);
            }
        });
    }

    static IEnumerable<Feature> LoadFeatures() =>
        Directory.GetFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Features"), "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new Feature(path));

    // "### C# 11" sections, "#### <feature>" headings and "**✅ Works**" status lines.
    static Dictionary<string, (int Version, FeatureStatus Status)> ParseReadme() {
        var result = new Dictionary<string, (int, FeatureStatus)>();
        int? version = null;
        string? feature = null;
        foreach (var line in File.ReadLines(TestCompiler.ReadmePath)) {
            var section = Regex.Match(line, @"^### C# (\d+)$");
            if (section.Success) {
                version = int.Parse(section.Groups[1].Value);
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal)) {
                version = null;
            } else if (line.StartsWith("#### ", StringComparison.Ordinal)) {
                feature = line.Substring(5).Trim();
            } else if (version != null && feature != null && line.StartsWith("**", StringComparison.Ordinal)) {
                var status = ReadmeStatuses.FirstOrDefault(pair => line == $"**{pair.Key}**");
                if (status.Key != null) {
                    result.Add(feature, (version.Value, status.Value));
                    feature = null;
                }
            }
        }

        return result;
    }
}
