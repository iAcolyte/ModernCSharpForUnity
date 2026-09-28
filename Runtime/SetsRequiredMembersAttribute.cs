// Polyfill for C# 11 `required` members: netstandard2.1 doesn't ship this type.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class SetsRequiredMembersAttribute: Attribute {
}

