// Feature: Static abstract interface members (generic math)
// Status: Error
// Error: CS8919

public interface IZero<T> where T : IZero<T> {
    static abstract T Zero { get; }
}
