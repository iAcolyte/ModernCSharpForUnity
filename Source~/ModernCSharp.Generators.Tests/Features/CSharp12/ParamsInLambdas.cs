// Feature: params in lambdas
// Status: Works

public static class Lambdas {
    public static int Run() {
        var sum = (params int[] values) => values.Length;
        return sum(1, 2, 3);
    }
}
