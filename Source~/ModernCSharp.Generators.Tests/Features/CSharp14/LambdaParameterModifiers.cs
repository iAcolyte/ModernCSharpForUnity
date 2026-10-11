// Feature: Modifiers on simple lambda parameters
// Status: Works

public delegate bool TryParse<T>(string text, out T value);

public static class Parsers {
    public static readonly TryParse<int> Int = (text, out value) => int.TryParse(text, out value);
}
