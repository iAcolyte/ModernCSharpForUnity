// Polyfill for C# 12 [Experimental]: netstandard2.1 doesn't ship this type.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
    AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
    Inherited = false)]
public sealed class ExperimentalAttribute: Attribute {
    public ExperimentalAttribute(string diagnosticId) {
        DiagnosticId = diagnosticId;
    }

    public string DiagnosticId { get; }

    public string? UrlFormat { get; set; }
}

