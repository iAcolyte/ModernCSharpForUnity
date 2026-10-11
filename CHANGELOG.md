# Changelog

## [0.2.1] - 2026-10-11

### Fixed
- Settings were reset to defaults on every editor load: Unity didn't link `ModernCSharpSettings` to its script because of the file-scoped namespace, so the saved `ProjectSettings/ModernCSharpSettings.asset` was never loaded back. A file saved by an earlier version is repaired on load and its values start to apply.

## [0.2.0] - 2026-10-11

### Added
- C# 13 and 14 in Language Version, available in Unity 6000.7 or newer (Roslyn 5.6), with the `MODERN_CSHARP_13_OR_NEWER` and `MODERN_CSHARP_14_OR_NEWER` define symbols.
- Polyfill: `OverloadResolutionPriorityAttribute`.
- C# 13–14 in the feature matrix: 18 features with snippets. Each snippet is now compiled as the language version it was added in, with Roslyn 5.6.

## [0.1.1] - 2026-10-11

### Fixed
- Compilation errors in Unity 6000.0–6000.4, whose Roslyn 4.3 supports C# 10 at most: the package's own assemblies now compile with `-langversion:10`, and the polyfills generator is built against `Microsoft.CodeAnalysis.CSharp` 4.3 instead of 4.8, which the older compiler could not load.

### Changed
- Language Version defaults to C# 9 instead of C# 12.
- C# 11 and 12 can be selected only in Unity 6000.5 or newer. A higher stored version is kept, but `csc.rsp` files and `MODERN_CSHARP_*_OR_NEWER` symbols are written for the highest version the editor supports.

## [0.1.0] - 2026-09-28

### Added
- Polyfills source generator, connected through `csc.rsp` (`-a:`) and hidden from Unity in `Generators~/`: emits `internal` C# 9–12 compiler-required types into every managed assembly and skips types the assembly already sees (`IsExternalInit`, `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`, `SetsRequiredMembersAttribute`, `CallerArgumentExpressionAttribute`, `InterpolatedStringHandlerAttribute`, `InterpolatedStringHandlerArgumentAttribute`, `UnscopedRefAttribute`, `CollectionBuilderAttribute`, `ExperimentalAttribute`).
- Project Settings page (Edit → Project Settings → C# Language): language version 9–12, nullable toggle, path blacklist/whitelist.
- `MODERN_CSHARP_9_OR_NEWER` … `MODERN_CSHARP_12_OR_NEWER` and `MODERN_CSHARP_NULLABLE` define symbols in every managed `csc.rsp`.
- Automatic `csc.rsp` management next to each `.asmdef` in `Assets` and in `Assets/csc.rsp`.
- Tests: generator and C# 10–12 feature matrix (`dotnet test`, GitHub Actions), EditMode tests of the Editor logic.
