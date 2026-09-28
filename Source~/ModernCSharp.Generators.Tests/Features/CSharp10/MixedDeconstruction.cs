// Feature: Mixed deconstruction
// Status: Works

public static class Deconstruction {
    public static int Run() {
        int x;
        (x, var y) = (1, 2);
        return x + y;
    }
}
