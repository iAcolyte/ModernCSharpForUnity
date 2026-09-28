// Feature: record struct
// Status: Works

public record struct Cell(int X, int Y);

public static class Grid {
    public static bool Same(Cell a, Cell b) => a == b;
}
