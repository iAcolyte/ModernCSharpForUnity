// Polyfill for C# 12 collection expressions on custom collection types: netstandard2.1 doesn't ship this type.
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public sealed class CollectionBuilderAttribute: Attribute {
    public CollectionBuilderAttribute(Type builderType, string methodName) {
        BuilderType = builderType;
        MethodName = methodName;
    }

    public Type BuilderType { get; }

    public string MethodName { get; }
}

