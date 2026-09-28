// Feature: Inline arrays
// Status: Error
// Error: CS0246

using System.Runtime.CompilerServices;

[InlineArray(4)]
public struct WheelBuffer {
    float element;
}
