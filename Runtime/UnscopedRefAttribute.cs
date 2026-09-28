// Polyfill for C# 11 ref safety rules ([UnscopedRef]): netstandard2.1 doesn't ship this type.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class UnscopedRefAttribute: Attribute {
}
