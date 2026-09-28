using System;
using System.Collections.Generic;
using System.IO;

using ModernCSharp.Editor;

using NUnit.Framework;

public sealed class RspFileTests {
    const string Generator = "Library/PackageCache/com.iacolyte.modern-csharp@new/Generators~/ModernCSharp.Generators.dll";
    const string StaleGenerator = "Library/PackageCache/com.iacolyte.modern-csharp@old/Generators~/ModernCSharp.Generators.dll";
    const string Defines12 =
        "-define:MODERN_CSHARP_9_OR_NEWER;MODERN_CSHARP_10_OR_NEWER;MODERN_CSHARP_11_OR_NEWER;MODERN_CSHARP_12_OR_NEWER;MODERN_CSHARP_NULLABLE";

    string path = null!;

    [SetUp]
    public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"csc-{Guid.NewGuid():N}.rsp");

    [TearDown]
    public void TearDown() => File.Delete(path);

    [Test]
    public void CreatesMissingFile() {
        Assert.That(Write(), Is.True);

        Assert.That(Lines(), Is.EqualTo(new[] { "-langversion:12", "-nullable:enable", Defines12, "-a:" + Generator }));
    }

    [Test]
    public void SkipsUnchangedFile() {
        Write();
        var before = File.ReadAllText(path);

        Assert.That(Write(), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
    }

    [TestCase("-langversion:9", "-nullable:disable")]
    [TestCase("/langversion:9", "/nullable:disable")]
    [TestCase("-langversion:latest", "-nullable-")]
    [TestCase("-LangVersion:10", "-nullable+")]
    public void ReplacesExistingOptions(string langVersion, string nullable) {
        Given(langVersion, nullable);

        Write();

        Assert.That(Lines(), Is.EqualTo(new[] { "-langversion:12", "-nullable:enable", Defines12, "-a:" + Generator }));
    }

    [Test]
    public void RemovesDuplicateOptions() {
        Given("-langversion:12", "-langversion:9", "-nullable:enable", "-nullable:disable");

        Write();

        Assert.That(Lines(), Is.EqualTo(new[] { "-langversion:12", "-nullable:enable", Defines12, "-a:" + Generator }));
    }

    [Test]
    public void KeepsForeignOptionsInPlace() {
        Given("-warnaserror+", "-langversion:9", "-unsafe", "-nullable:disable", "-nowarn:0169");

        Write();

        Assert.That(Lines(), Is.EqualTo(new[] {
            "-warnaserror+", "-langversion:12", "-unsafe", "-nullable:enable", Defines12, "-nowarn:0169", "-a:" + Generator,
        }));
    }

    [Test]
    public void NormalizesLineEndingsAndTrailingBlankLines() {
        File.WriteAllText(path, "-langversion:12\r\n-nullable:enable\r\n\r\n\r\n");

        Write();

        Assert.That(File.ReadAllText(path), Does.Not.Contain("\r"));
        Assert.That(File.ReadAllText(path), Does.EndWith(Generator + "\n").And.Not.EndWith("\n\n"));
    }

    [Test]
    public void DoesNotConfuseSimilarOptions() {
        Given("-additionalfile:ModernCSharp.Generators.dll", "-nullablefoo", "-debug:portable");

        Write();

        Assert.That(Lines(), Does.Contain("-additionalfile:ModernCSharp.Generators.dll")
            .And.Contain("-nullablefoo")
            .And.Contain("-debug:portable"));
    }

    // Generator

    [Test]
    public void ReplacesStaleGeneratorPath() {
        Given("-langversion:12", "-nullable:enable", "-a:" + StaleGenerator);

        Write();

        Assert.That(Lines(), Has.Member("-a:" + Generator).And.No.Member("-a:" + StaleGenerator));
    }

    [TestCase("-analyzer:" + StaleGenerator)]
    [TestCase("/a:" + StaleGenerator)]
    [TestCase("-a:\"Old Project/Generators~/ModernCSharp.Generators.dll\"")]
    [TestCase(@"-a:Library\PackageCache\old\Generators~\ModernCSharp.Generators.dll")]
    public void RecognizesGeneratorInAnyForm(string line) {
        Given(line);

        Write();

        Assert.That(Lines(), Has.No.Member(line));
    }

    [Test]
    public void RemovesGeneratorWhenPathIsNull() {
        Given("-a:" + StaleGenerator);

        Write(generator: null);

        Assert.That(string.Join("\n", Lines()), Does.Not.Contain("ModernCSharp.Generators.dll"));
    }

    [Test]
    public void QuotesPathWithSpaces() {
        Write(generator: "My Game/Generators~/ModernCSharp.Generators.dll");

        Assert.That(Lines(), Has.Member("-a:\"My Game/Generators~/ModernCSharp.Generators.dll\""));
    }

    [Test]
    public void KeepsForeignAnalyzers() {
        Given("-a:Analyzers/Other.dll", "-analyzer:Analyzers/Another.dll");

        Write();

        Assert.That(Lines(), Has.Member("-a:Analyzers/Other.dll").And.Member("-analyzer:Analyzers/Another.dll"));
    }

    // Define symbols

    [Test]
    public void DefineSymbolsForVersion() {
        Assert.That(DefineSymbols.For(LanguageVersion.CSharp11, nullable: true), Is.EqualTo(new[] {
            "MODERN_CSHARP_9_OR_NEWER", "MODERN_CSHARP_10_OR_NEWER", "MODERN_CSHARP_11_OR_NEWER", "MODERN_CSHARP_NULLABLE",
        }));
        Assert.That(DefineSymbols.For(LanguageVersion.CSharp9, nullable: false), Is.EqualTo(new[] { "MODERN_CSHARP_9_OR_NEWER" }));
    }

    [Test]
    public void DropsSymbolsOfHigherVersions() {
        Write();

        Write(LanguageVersion.CSharp10, nullable: false);

        Assert.That(Lines(), Has.Member("-define:MODERN_CSHARP_9_OR_NEWER;MODERN_CSHARP_10_OR_NEWER"));
        Assert.That(string.Join("\n", Lines()), Does.Not.Contain("MODERN_CSHARP_11").And.Not.Contain("NULLABLE"));
    }

    [TestCase("-define:FOO;MODERN_CSHARP_10_OR_NEWER", "-define:FOO")]
    [TestCase("-d:FOO,MODERN_CSHARP_NULLABLE,BAR", "-d:FOO;BAR")]
    [TestCase("/define:MODERN_CSHARP_9_OR_NEWER;FOO", "/define:FOO")]
    public void StripsOwnSymbolsFromForeignDefines(string line, string expected) {
        Given(line);

        Write();

        Assert.That(Lines(), Has.Member(expected).And.Member(Defines12));
    }

    [TestCase("-define:MODERN_CSHARP_10_OR_NEWER")]
    [TestCase("-d:MODERN_CSHARP_9_OR_NEWER;MODERN_CSHARP_NULLABLE")]
    public void RemovesLineWithOnlyOwnSymbols(string line) {
        Given(line);

        Write();

        Assert.That(Lines(), Has.No.Member(line));
        Assert.That(Lines(), Has.Exactly(1).StartsWith("-define:"));
    }

    [Test]
    public void KeepsForeignDefines() {
        Given("-define:BAR");

        Write();

        Assert.That(Lines(), Has.Member("-define:BAR").And.Member(Defines12));
    }

    [Test]
    public void DefinesGoAfterNullable() {
        Given("-nullable:enable", "-langversion:12", "-unsafe");

        Write();

        Assert.That(Lines(), Is.EqualTo(new[] { "-nullable:enable", Defines12, "-langversion:12", "-unsafe", "-a:" + Generator }));
    }

    bool Write(LanguageVersion version = LanguageVersion.CSharp12, bool nullable = true, string? generator = Generator) =>
        RspFile.Write(path, version, nullable, DefineSymbols.For(version, nullable), generator);

    void Given(params string[] lines) => File.WriteAllText(path, string.Join("\n", lines) + "\n");

    IReadOnlyList<string> Lines() => File.ReadAllText(path).TrimEnd('\n').Split('\n');
}
