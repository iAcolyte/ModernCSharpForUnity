// Polyfill for C# 10 custom interpolated string handlers: netstandard2.1 doesn't ship this type.
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class InterpolatedStringHandlerArgumentAttribute: Attribute {
    public InterpolatedStringHandlerArgumentAttribute(string argument) {
        Arguments = new[] { argument };
    }

    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) {
        Arguments = arguments;
    }

    public string[] Arguments { get; }
}

