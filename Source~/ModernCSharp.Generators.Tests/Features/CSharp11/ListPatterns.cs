// Feature: List patterns
// Status: Works

public static class Combos {
    public static string Match(int[] keys) => keys switch {
        [1, 2, 3] => "combo",
        [1, .. var rest] => $"starts with 1, {rest.Length} more",
        [] => "empty",
        _ => "none",
    };
}
