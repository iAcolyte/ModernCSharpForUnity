// Feature: Partial properties and indexers
// Status: Works

public partial class PlayerModel {
    public partial int Health { get; set; }
}

// The other half usually comes from a source generator.
public partial class PlayerModel {
    int health;

    public partial int Health {
        get => health;
        set => health = value < 0 ? 0 : value;
    }
}
