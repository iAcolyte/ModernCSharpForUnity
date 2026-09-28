// Feature: Explicit lambda return type
// Status: Works

public static class Lambdas {
    public static object? Run() {
        var parse = object? (string s) => s.Length > 0 ? s : null;
        return parse("x");
    }
}
