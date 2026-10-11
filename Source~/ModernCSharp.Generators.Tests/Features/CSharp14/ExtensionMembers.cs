// Feature: Extension members
// Status: Works

using System.Collections.Generic;
using System.Linq;

public struct Cell {
    public int X;
    public int Y;
}

public static class Extensions {
    extension<T>(IEnumerable<T> source) {
        public bool IsEmpty => !source.Any();

        public IEnumerable<T> Twice() => source.Concat(source);
    }

    extension(Cell) {
        public static Cell Zero => default;

        public static Cell operator +(Cell a, Cell b) => new() { X = a.X + b.X, Y = a.Y + b.Y };
    }
}

public static class Usage {
    public static bool Run(List<int> enemies) => enemies.IsEmpty && (Cell.Zero + Cell.Zero).X == 0;
}
