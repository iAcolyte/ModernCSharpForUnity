using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ModernCSharp.Generators.Tests;

sealed class CompileResult {
    public CompileResult(Compilation compilation, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<SyntaxTree> generatedTrees) {
        Compilation = compilation;
        Diagnostics = diagnostics;
        GeneratedTrees = generatedTrees;
    }

    public Compilation Compilation { get; }

    // Compiler diagnostics of the final compilation plus the generator's own diagnostics.
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public ImmutableArray<SyntaxTree> GeneratedTrees { get; }

    public Diagnostic[] Errors => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

    public Diagnostic[] Warnings => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();

    public string[] GeneratedHintNames => GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).OrderBy(name => name).ToArray();

    public string Format() => string.Join("\n", Diagnostics.Select(d => d.ToString()));
}

// Compiles the way Unity does: netstandard2.1, a library, with the polyfill generator from csc.rsp.
static class TestCompiler {
    static readonly MetadataReference NetStandard = MetadataReference.CreateFromFile(GetMetadata("NetStandardReference"));

    public static string ReadmePath => GetMetadata("PackageReadme");

    public static CompileResult Compile(
        IEnumerable<string> sources,
        LanguageVersion languageVersion = LanguageVersion.CSharp12,
        bool withGenerator = true,
        IEnumerable<MetadataReference>? references = null,
        string assemblyName = "Test") {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)),
            new[] { NetStandard }.Concat(references ?? Enumerable.Empty<MetadataReference>()),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        if (!withGenerator) {
            return new CompileResult(compilation, compilation.GetDiagnostics(), ImmutableArray<SyntaxTree>.Empty);
        }

        var driver = CSharpGeneratorDriver.Create(new[] { new PolyfillGenerator().AsSourceGenerator() }, parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var generated = driver.GetRunResult().GeneratedTrees;
        return new CompileResult(output, generatorDiagnostics.AddRange(output.GetDiagnostics()), generated);
    }

    public static CompileResult Compile(string source, LanguageVersion languageVersion = LanguageVersion.CSharp12, bool withGenerator = true,
        IEnumerable<MetadataReference>? references = null, string assemblyName = "Test") =>
        Compile(new[] { source }, languageVersion, withGenerator, references, assemblyName);

    // Emits a helper assembly into memory, standing in for a third-party DLL or another asmdef.
    public static MetadataReference EmitReference(string source, string assemblyName, bool withGenerator = false) {
        var result = Compile(source, withGenerator: withGenerator, assemblyName: assemblyName);
        using var stream = new MemoryStream();
        var emit = result.Compilation.Emit(stream);
        if (!emit.Success) {
            throw new InvalidOperationException($"Can't emit {assemblyName}:\n" + string.Join("\n", emit.Diagnostics));
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static string GetMetadata(string key) =>
        typeof(TestCompiler).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == key).Value
        ?? throw new InvalidOperationException($"Assembly metadata {key} is missing.");
}
