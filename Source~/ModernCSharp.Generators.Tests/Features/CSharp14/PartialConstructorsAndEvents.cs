// Feature: Partial constructors and events
// Status: Works

using System;

public partial class Inventory {
    public partial Inventory(int capacity);

    public partial event Action Changed;
}

public partial class Inventory {
    readonly int capacity;
    Action? changed;

    public partial Inventory(int capacity) {
        this.capacity = capacity;
    }

    public partial event Action Changed {
        add => changed += value;
        remove => changed -= value;
    }

    public int Capacity => capacity;

    public void Notify() => changed?.Invoke();
}
