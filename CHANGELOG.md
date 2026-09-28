# Changelog

## [0.1.0] - 2026-09-28

### Added
- `ModernCSharp.Polyfills`: public polyfills for C# 10–12 compiler-required types (`IsExternalInit`, `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`, `SetsRequiredMembersAttribute`, `CallerArgumentExpressionAttribute`, `InterpolatedStringHandlerAttribute`, `InterpolatedStringHandlerArgumentAttribute`, `UnscopedRefAttribute`, `CollectionBuilderAttribute`, `ExperimentalAttribute`).
- Project Settings page (Edit → Project Settings → C# Language): language version 9–12, nullable toggle, path blacklist/whitelist.
- Automatic `csc.rsp` management next to each `.asmdef` in `Assets` and in `Assets/csc.rsp`.
