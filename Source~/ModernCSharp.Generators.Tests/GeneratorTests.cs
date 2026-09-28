using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace ModernCSharp.Generators.Tests;

public sealed class GeneratorTests {
    static readonly string[] AllTypes = {
        "System.Runtime.CompilerServices.IsExternalInit",
        "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
        "System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute",
        "System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
        "System.Runtime.CompilerServices.RequiredMemberAttribute",
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute",
        "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
        "System.Diagnostics.CodeAnalysis.UnscopedRefAttribute",
        "System.Runtime.CompilerServices.CollectionBuilderAttribute",
        "System.Diagnostics.CodeAnalysis.ExperimentalAttribute",
    };

    const string IsExternalInitHint = "ModernCSharp.IsExternalInit.g.cs";
    const string UsesInit = "public record Point(int X, int Y);";

    [Test]
    public void EmitsEveryPolyfill() {
        var result = TestCompiler.Compile("");

        Assert.That(result.GeneratedHintNames,
            Is.EquivalentTo(AllTypes.Select(name => $"ModernCSharp.{name.Split('.').Last()}.g.cs")));
        foreach (var name in AllTypes) {
            Assert.That(result.Compilation.GetTypeByMetadataName(name), Is.Not.Null, name);
        }
    }

    [TestCase(LanguageVersion.CSharp9)]
    [TestCase(LanguageVersion.CSharp10)]
    [TestCase(LanguageVersion.CSharp11)]
    [TestCase(LanguageVersion.CSharp12)]
    public void GeneratedCodeHasNoDiagnostics(LanguageVersion version) {
        var result = TestCompiler.Compile("", version);

        Assert.That(result.Diagnostics, Is.Empty, result.Format());
    }

    [Test]
    public void EveryPolyfillIsInternal() {
        var result = TestCompiler.Compile("");

        foreach (var name in AllTypes) {
            var type = result.Compilation.GetTypeByMetadataName(name)!;
            Assert.That(type.DeclaredAccessibility, Is.EqualTo(Accessibility.Internal), name);
        }
    }

    [Test]
    public void SkipsTypeDeclaredInOwnSource() {
        var result = TestCompiler.Compile(new[] {
            "namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }",
            UsesInit,
        });

        Assert.That(result.GeneratedHintNames, Has.Length.EqualTo(AllTypes.Length - 1).And.No.Member(IsExternalInitHint));
        Assert.That(result.Diagnostics, Is.Empty, result.Format());
    }

    [Test]
    public void SkipsPublicTypeFromReferencedAssembly() {
        var thirdParty = TestCompiler.EmitReference(
            "namespace System.Runtime.CompilerServices { public static class IsExternalInit { } }", "ThirdParty");

        var result = TestCompiler.Compile(UsesInit, references: new[] { thirdParty });

        Assert.That(result.GeneratedHintNames, Has.No.Member(IsExternalInitHint));
        Assert.That(result.Diagnostics, Is.Empty, result.Format());
    }

    [Test]
    public void SkipsInternalTypeVisibleThroughInternalsVisibleTo() {
        var friend = TestCompiler.EmitReference(
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Test\")]\n" + UsesInit.Replace("Point", "Other"),
            "Friend", withGenerator: true);

        var result = TestCompiler.Compile(UsesInit, references: new[] { friend });

        Assert.That(result.GeneratedHintNames, Is.Empty, "every polyfill comes from Friend");
        Assert.That(result.Diagnostics, Is.Empty, result.Format());
    }

    [Test]
    public void GeneratesWhenReferencedCopyIsInternal() {
        // Like UnityEngine.TestRunner: an internal IsExternalInit the compilation can't see.
        var hidden = TestCompiler.EmitReference(
            "namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }", "Hidden");

        var result = TestCompiler.Compile(UsesInit, references: new[] { hidden });

        Assert.That(result.GeneratedHintNames, Has.Member(IsExternalInitHint));
        Assert.That(result.Diagnostics, Is.Empty, result.Format());
    }

    // Unity 6000.6 compiles with Roslyn 4.10 and can't load a generator built against a newer one.
    [Test]
    public void ReferencesRoslynNoNewerThanUnity() {
        var roslyn = typeof(PolyfillGenerator).Assembly.GetReferencedAssemblies()
            .Single(name => name.Name == "Microsoft.CodeAnalysis");

        Assert.That(roslyn.Version, Is.LessThanOrEqualTo(new Version(4, 10)));
    }
}
