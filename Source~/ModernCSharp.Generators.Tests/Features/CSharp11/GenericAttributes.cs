// Feature: Generic attributes
// Status: Caveats

using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RequireComponent<T> : Attribute { }

[RequireComponent<string>]
public sealed class Player { }
