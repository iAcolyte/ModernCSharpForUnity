// Feature: Natural type of lambdas
// Status: Works

public static class Lambdas {
    public static int Run() {
        var twice = (int x) => x * 2;
        return twice(21);
    }
}
