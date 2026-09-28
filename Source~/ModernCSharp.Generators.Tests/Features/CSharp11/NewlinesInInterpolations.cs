// Feature: Newlines in interpolations
// Status: Works

public static class Text {
    public static string Describe(int hp) => $"State: {hp switch {
        > 50 => "healthy",
        > 0 => "wounded",
        _ => "dead",
    }}";
}
