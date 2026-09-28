using System.Linq;

using Microsoft.CodeAnalysis;

namespace ModernCSharp.Generators;

// Adds internal copies of the compiler types netstandard2.1 lacks for C# 9–12 features.
// A type is skipped when the compilation can already see one, so there are no CS0433/CS0436 conflicts.
[Generator(LanguageNames.CSharp)]
public sealed class PolyfillGenerator: IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var missing = context.CompilationProvider.Select(static (compilation, _) =>
            Polyfills.All
                .Where(polyfill => !IsAccessible(compilation, polyfill.MetadataName))
                .Select(polyfill => polyfill.MetadataName)
                .ToArray());

        context.RegisterSourceOutput(missing, static (output, names) => {
            foreach (var polyfill in Polyfills.All.Where(polyfill => names.Contains(polyfill.MetadataName))) {
                output.AddSource($"ModernCSharp.{polyfill.Name}.g.cs", polyfill.Source);
            }
        });
    }

    static bool IsAccessible(Compilation compilation, string metadataName) =>
        compilation.GetTypesByMetadataName(metadataName)
            .Any(type => compilation.IsSymbolAccessibleWithin(type, compilation.Assembly));
}
