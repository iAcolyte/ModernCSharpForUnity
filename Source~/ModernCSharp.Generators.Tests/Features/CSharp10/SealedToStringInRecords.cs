// Feature: sealed ToString in records
// Status: Works

public abstract record Entity {
    public sealed override string ToString() => GetType().Name;
}

public record Enemy : Entity;
