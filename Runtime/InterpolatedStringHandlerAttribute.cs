// Polyfill for C# 10 custom interpolated string handlers: netstandard2.1 doesn't ship this type.
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class InterpolatedStringHandlerAttribute: Attribute {
}
