# Changelog

## [0.1.0] - 2026-09-28

### Added
- Polyfills source generator, connected through `csc.rsp` (`-a:`) and hidden from Unity in `Generators~/`: emits `internal` C# 9–12 compiler-required types into every managed assembly and skips types the assembly already sees (`IsExternalInit`, `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`, `SetsRequiredMembersAttribute`, `CallerArgumentExpressionAttribute`, `InterpolatedStringHandlerAttribute`, `InterpolatedStringHandlerArgumentAttribute`, `UnscopedRefAttribute`, `CollectionBuilderAttribute`, `ExperimentalAttribute`).
- Project Settings page (Edit → Project Settings → C# Language): language version 9–12, nullable toggle, path blacklist/whitelist.
- `MODERN_CSHARP_9_OR_NEWER` … `MODERN_CSHARP_12_OR_NEWER` and `MODERN_CSHARP_NULLABLE` define symbols in every managed `csc.rsp`.
- Automatic `csc.rsp` management next to each `.asmdef` in `Assets` and in `Assets/csc.rsp`.
- Tests: generator and C# 10–12 feature matrix (`dotnet test`, GitHub Actions), EditMode tests of the Editor logic.
