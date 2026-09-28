// Feature: Improved definite assignment
// Status: Works

using System.Collections.Generic;

public static class Lookup {
    public static int Get(Dictionary<string, int>? map) {
        if (map?.TryGetValue("hp", out var hp) == true) {
            return hp;
        }

        return 0;
    }
}
