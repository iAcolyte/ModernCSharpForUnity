// Polyfill for C# 10 [CallerArgumentExpression]: netstandard2.1 doesn't ship this type.
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CallerArgumentExpressionAttribute: Attribute {
    public CallerArgumentExpressionAttribute(string parameterName) {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}

