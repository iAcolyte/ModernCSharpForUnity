// Feature: The Lock type
// Status: Error
// Error: CS0246

using System.Threading;

public sealed class Counter {
    readonly Lock gate = new();
    int value;

    public int Increment() {
        lock (gate) {
            return ++value;
        }
    }
}
