// Feature: Default lambda parameters
// Status: Works

public static class Lambdas {
    public static int Run() {
        var damage = (int amount, float multiplier = 1f) => (int)(amount * multiplier);
        return damage(10);
    }
}
