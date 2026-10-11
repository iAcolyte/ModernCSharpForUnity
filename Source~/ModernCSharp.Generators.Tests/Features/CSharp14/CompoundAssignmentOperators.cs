// Feature: User-defined compound assignment operators
// Status: Polyfill

public sealed class DamageBuffer {
    public float Total;

    public void operator +=(float amount) {
        Total += amount;
    }

    public void operator ++() {
        Total++;
    }
}

public static class Usage {
    public static void Run(DamageBuffer buffer) {
        buffer += 12.5f;
        buffer++;
    }
}
