// Feature: File-local types
// Status: Works

file static class Helper {
    public static int Twice(int x) => x * 2;
}

public static class Api {
    public static int Run() => Helper.Twice(2);
}
