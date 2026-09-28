// Feature: Required members
// Status: Polyfill

using System.Diagnostics.CodeAnalysis;

public sealed class SpawnRequest {
    public required string Prefab { get; init; }
    public int Count { get; init; } = 1;

    public SpawnRequest() { }

    [SetsRequiredMembers]
    public SpawnRequest(string prefab) {
        Prefab = prefab;
    }
}

public static class Usage {
    public static SpawnRequest Run() => new() { Prefab = "Goblin" };
}
