// Feature: Primary constructors
// Status: Works

public sealed class Health(int max) {
    int current = max;

    public int Current => current;

    public void Damage(int amount) => current -= amount;
}
