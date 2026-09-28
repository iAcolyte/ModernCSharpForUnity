# Modern C# for Unity

The `com.iacolyte.modern-csharp` Unity package enables C# 10–12 and nullable reference types in Unity 6:

- it adds a Project Settings page that creates and updates `csc.rsp` next to every `.asmdef` in `Assets`;
- it adds a source generator, connected through the same `csc.rsp`, that emits polyfills for compiler-required types that `netstandard2.1` lacks.

## Installation

Open **Window → Package Manager → + → Add package from git URL** and enter the repository URL:

```
https://github.com/iAcolyte/ModernCSharpForUnity.git
```

To pin a specific version, add a tag: `https://github.com/iAcolyte/ModernCSharpForUnity.git#v0.1.0`. You can also add it to `Packages/manifest.json`:

```json
"com.iacolyte.modern-csharp": "https://github.com/iAcolyte/ModernCSharpForUnity.git#v0.1.0"
```

Requires Unity 6000.0 or newer.

## Contents

```
package.json
Editor/
├── ModernCSharp.Editor.asmdef
├── ModernCSharpSettingsProvider.cs # Project Settings → C# Language
├── ModernCSharpApplier.cs          # writes csc.rsp files
└── AsmdefWatcher.cs                # auto-updates when an asmdef is added
Tests/Editor/                       # EditMode tests of the Editor logic
Generators~/
└── ModernCSharp.Generators.dll     # polyfill source generator, hidden from Unity
Source~/                            # hidden from Unity
├── ModernCSharp.sln
├── build.sh                        # builds the DLL into Generators~/
├── ModernCSharp.Generators/        # generator sources
└── ModernCSharp.Generators.Tests/  # generator tests and the feature matrix
```

## Settings

Open them from **Edit → Project Settings → C# Language**. They are stored in `ProjectSettings/ModernCSharpSettings.asset`. Commit this file so the whole team shares the same settings.

| Field | What it does |
|---|---|
| Language Version | C# 9, 10, 11 or 12. Written to `csc.rsp` as `-langversion`. |
| Nullable | On: `-nullable:enable`, off: `-nullable:disable`. |
| Manage Assets/csc.rsp | Whether to manage the root `Assets/csc.rsp`, which configures `Assembly-CSharp` and `Assembly-CSharp-Editor`. |
| Auto Apply | Update the files on editor load, when a new asmdef appears and whenever the settings change. When off, changes are applied only by the **Apply to All Assemblies** button. |
| Path Filter | **Blacklist**: manage every asmdef in `Assets` except those in the listed folders. **Whitelist**: manage only asmdefs in the listed folders. |
| Folders | Folders for the filter, e.g. `Assets/Plugins`. Subfolders are included too. |

The collapsible **Assemblies** section shows which asmdefs are currently processed (`managed`) and which are skipped (`skipped`).

How the tool handles `csc.rsp`:

- only the `-langversion`, `-nullable`, `MODERN_CSHARP_*` [define symbols](#define-symbols) and polyfills generator lines are changed; all other options, including other analyzers and your own defines (`-warnaserror`, `-define`, etc.) stay as they are;
- a file is rewritten only when its content actually changes, so there are no unnecessary recompilations;
- files next to asmdefs that fall out of the filter are neither deleted nor changed;
- asmdefs in `Packages/` are never touched; the package's own assemblies ship with their own `csc.rsp`.

After changing the language version, run **Edit → Preferences → External Tools → Regenerate project files** so your IDE picks up the new version and define symbols.

## Define symbols

Every managed `csc.rsp` also gets a `-define:` line, so code can check the selected settings:

| Symbol | When it is defined |
|---|---|
| `MODERN_CSHARP_9_OR_NEWER` | Always. Marks an assembly managed by the package. |
| `MODERN_CSHARP_10_OR_NEWER` … `MODERN_CSHARP_12_OR_NEWER` | Language Version is at least that version. |
| `MODERN_CSHARP_NULLABLE` | Nullable is on. |

```csharp
#if MODERN_CSHARP_12_OR_NEWER
    int[] items = [1, 2, 3];
#else
    int[] items = { 1, 2, 3 };
#endif
```

The symbols exist only in managed assemblies: `Assets/csc.rsp` and asmdefs that pass the path filter. Assemblies skipped by the filter and code in other packages don't see them. Your own `-define` symbols in the same file are kept.

## Polyfills generator

The polyfills are produced by a Roslyn source generator. The tool adds it to every managed `csc.rsp`:

```
-a:Library/PackageCache/com.iacolyte.modern-csharp@<hash>/Generators~/ModernCSharp.Generators.dll
```

- Every assembly gets its own `internal` copies of the types from the [Polyfills](#polyfills) table, so asmdefs need no extra references.
- A type is skipped when the assembly can already see one: from the BCL, from a third-party DLL, or from another assembly through `InternalsVisibleTo`. This prevents CS0433 and CS0436 conflicts.
- Unity doesn't import folders whose names end with `~`, so the DLL doesn't appear in the Project window and isn't treated as a plugin.
- Rider and Visual Studio read `-a:` from `csc.rsp` and run the generator too, so the IDE sees the generated types. They appear under **Dependencies → Analyzers**, not as files on disk.
- The package path changes with every package update. The tool rewrites it on editor load; the package's own Editor assembly doesn't use the generator, so it compiles even with a stale path.

To change the generator, edit `Source~/` and run `Source~/build.sh`. It needs the .NET SDK and builds against `Microsoft.CodeAnalysis.CSharp` 4.8: the generator must not reference a newer Roslyn than Unity's compiler (4.10 in Unity 6000.6).

## C# 10–12 in Unity

Everything added to the language after C# 9, marked with whether it works in Unity, where to use it, and an example in game code.

| Status | Features |
|---|---|
| ✅ Works | 29 |
| 🧩 Needs a polyfill | 5 |
| ⚠️ With caveats | 2 |
| ❌ Doesn't work | 5 |

### C# 10

_November 2021 · .NET 6_

#### File-scoped namespace

**✅ Works**

**Where:** in every file. Removes one level of indentation, so a single-class file gets 4 spaces wider.

```csharp
namespace Playgrounds.Core;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _prefab = null!;
}
```

#### global using

**✅ Works**

**Where:** one `GlobalUsings.cs` file per assembly. It applies to the whole assembly, i.e. the whole asmdef it lives in.

> Each asmdef needs its own file: `global using` does not cross assembly boundaries.

```csharp
// Assets/_Project/Core/GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using UnityEngine;
global using Object = UnityEngine.Object;
global using Random = UnityEngine.Random;
```

#### record struct

**✅ Works**

**Where:** message bus events, dictionary keys, computation results. Value equality, `ToString` and `with` come for free and without allocations.

> Positional parameters become properties, while the inspector and `JsonUtility` work only with fields. Keep a plain struct for serialized data.

```csharp
public readonly record struct DamageEvent(int TargetId, float Amount, DamageType Type);

_bus.Publish(new DamageEvent(enemy.Id, 12.5f, DamageType.Fire));

// Dictionary key with correct GetHashCode/Equals
private readonly Dictionary<ChunkKey, Chunk> _chunks = new();
public readonly record struct ChunkKey(int X, int Z);
```

#### Parameterless struct constructors and field initializers

**⚠️ With caveats**

**Where:** settings structs where zeros are not sensible defaults.

> `default(T)`, `new T[n]` and Unity deserialization don't call this constructor; fields stay zero there. Only an explicit `new T()` runs it.

```csharp
public struct SpringSettings
{
    public float Stiffness = 120f;
    public float Damping = 12f;

    public SpringSettings() { }
}

var s = new SpringSettings();      // 120, 12
var d = default(SpringSettings);   // 0, 0
```

#### Extended property patterns

**✅ Works**

**Where:** AI conditions, event filters, `switch` over nested data. Instead of `{ Target: { Stats: { Hp: … } } }` you write a dotted path.

> Patterns check for real null. A destroyed `GameObject` passes `is { }`, so use a plain `!= null` for Unity objects.

```csharp
if (evt is { Target.Stats.Hp: <= 0, Source.Team: Team.Player })
    _score.AddKill();

var reaction = state switch
{
    { Enemy.Distance: < 2f } => Action.Attack,
    { Self.Stats.Hp: < 20 } => Action.Flee,
    _ => Action.Patrol,
};
```

#### Natural type of lambdas

**✅ Works**

**Where:** local helpers inside a method. A lambda can be assigned to `var`; the compiler infers `Func` or `Action`.

```csharp
var isAlive = (Enemy e) => e.Hp > 0;           // Func<Enemy, bool>
var log = (string m) => Debug.Log($"[AI] {m}"); // Action<string>

foreach (var e in _enemies)
    if (isAlive(e)) log(e.name);
```

#### Explicit lambda return type

**✅ Works**

**Where:** when the compiler can't infer the type itself, e.g. when one branch returns `null`.

```csharp
var parseLevel = int? (string s) => int.TryParse(s, out var v) ? v : null;

var level = parseLevel(PlayerPrefs.GetString("level")) ?? 1;
```

#### Attributes on lambdas

**✅ Works**

**Where:** rare in Unity. Useful to pass nullable attributes to the analyzer or to mark a handler.

```csharp
var onLegacyClick = [Obsolete("Use OnSubmit")] () => Submit();

var findTarget = [return: MaybeNull] () => _targets.Count > 0 ? _targets[0] : null;
```

#### Constant interpolated strings

**✅ Works**

**Where:** `[MenuItem]` and `[CreateAssetMenu]` paths, `PlayerPrefs` keys, Addressables names. Works when every part of the string is itself a constant.

```csharp
public static class Paths
{
    public const string Menu = "Playgrounds";
    public const string SaveKey = $"{Menu}.save.v2";
}

[CreateAssetMenu(menuName = $"{Paths.Menu}/Weapon")]
public class WeaponData : ScriptableObject { }

[MenuItem($"{Paths.Menu}/Tools/Clear Save")]
private static void ClearSave() => PlayerPrefs.DeleteKey(Paths.SaveKey);
```

#### Mixed deconstruction

**✅ Works**

**Where:** when some variables are already declared and some are new.

```csharp
Vector3 pos;
(pos, var rot) = (transform.position, transform.rotation);
```

#### sealed ToString in records

**✅ Works**

**Where:** a base record defines the log format and derived records can't override it.

```csharp
public abstract record Command(int Tick)
{
    public sealed override string ToString() => $"#{Tick} {GetType().Name}";
}

public record MoveCommand(int Tick, Vector2 Dir) : Command(Tick);
```

#### [CallerArgumentExpression]

**🧩 Needs a polyfill**

**Where:** checks in `Awake`, custom `Guard` and `Assert` helpers. The text of the expression goes into the error message automatically.

> Polyfill: `CallerArgumentExpressionAttribute`.

```csharp
public static class Guard
{
    public static T Require<T>(T? value,
        [CallerArgumentExpression("value")] string expr = "") where T : Object
    {
        if (value == null) // Unity's operator: also catches destroyed objects
            throw new MissingReferenceException($"{expr} not found");
        return value;
    }
}

private void Awake()
{
    _body = Guard.Require(GetComponent<Rigidbody>());
    // MissingReferenceException: GetComponent<Rigidbody>() not found
}
```

#### Custom interpolated string handlers

**🧩 Needs a polyfill**

**Where:** a logger that doesn't build the string at all when the level is disabled. This removes `$"..."` allocations in `Update` in release builds.

> Polyfills: `InterpolatedStringHandlerAttribute`, `InterpolatedStringHandlerArgumentAttribute`. Plain `$"..."` works without them.

```csharp
[InterpolatedStringHandler]
public ref struct VerboseHandler
{
    private StringBuilder? _sb;

    public VerboseHandler(int literalLength, int formattedCount, out bool enabled)
    {
        enabled = Log.Verbose;
        _sb = enabled ? new StringBuilder(literalLength) : null;
    }

    public void AppendLiteral(string s) => _sb!.Append(s);
    public void AppendFormatted<T>(T value) => _sb!.Append(value);
    public override string ToString() => _sb?.ToString() ?? "";
}

public static class Log
{
    public static bool Verbose;
    public static void Trace(ref VerboseHandler msg)
    {
        if (Verbose) Debug.Log(msg.ToString());
    }
}

// With Verbose == false the expressions in {} are not even evaluated
Log.Trace($"pos={transform.position} vel={_body.velocity}");
```

#### [AsyncMethodBuilder] on methods

**❌ Doesn't work**

**Would be used for:** mostly libraries. Lets a single async method use its own builder, e.g. a pooling one that avoids allocating the state machine.

> Doesn't compile: `error CS0592`. In `netstandard2.1`, `AsyncMethodBuilderAttribute` is allowed only on types; methods were added in .NET 6. A polyfill can't fix it: the type already exists, so a local copy only compiles with the CS0436 conflict warning. On types the attribute works, which is how UniTask declares its task types.

```csharp
[AsyncMethodBuilder(typeof(PooledTaskBuilder))]
private async ValueTask LoadChunkAsync(ChunkKey key)
{
    await _io.ReadAsync(key);
}
```

#### Improved definite assignment

**✅ Works**

**Where:** nothing to write. The compiler reports false CS0165 errors less often in conditions with `?.`, `is` and `== true`.

```csharp
if (_cache?.TryGetValue(id, out var item) == true)
    Use(item); // error CS0165 here in C# 9
```

#### Extended #line

**✅ Works**

**Where:** only in code generators. Maps generated code to a range of source lines so errors and debugging point to the right place.

```csharp
#line (12, 5) - (12, 30) 8 "Assets/Dialogs/intro.dlg"
Say("Greetings, traveler");
#line default
```

### C# 11

_November 2022 · .NET 7_

#### Raw string literals

**✅ Works**

**Where:** JSON in tests and save fixtures, code templates in editor scripts, regular expressions, shader snippets. Quotes and slashes need no escaping. With `$$`, curly braces stay literal and interpolation uses `{{ }}`.

```csharp
var json = $$"""
    {
        "id": "{{playerId}}",
        "hp": {{hp}},
        "tags": ["tutorial", "boss"]
    }
    """;
var save = JsonUtility.FromJson<SaveData>(json);

var pattern = """^(\w+)_(\d{2})\.png$""";
```

#### Newlines in interpolations

**✅ Works**

**Where:** long expressions inside `{ }`, e.g. a `switch` right inside a UI string.

```csharp
_label.text = $"Difficulty: {difficulty switch
{
    Difficulty.Easy => "easy",
    Difficulty.Hard => "hard",
    _ => "normal",
}}";
```

#### UTF-8 string literals

**✅ Works**

**Where:** network protocols, save file signatures, binary formats. A `"..."u8` literal gives a `ReadOnlySpan` with no allocations and no `Encoding.UTF8.GetBytes`.

```csharp
private static ReadOnlySpan<byte> Magic => "PGSV"u8;

public static bool IsSaveFile(ReadOnlySpan<byte> header) =>
    header.Length >= 4 && header[..4].SequenceEqual(Magic);
```

#### List patterns

**✅ Works**

**Where:** debug console commands, fighting game combos from an input buffer, array shape checks. `..` skips any number of elements, `.. var rest` captures them into a slice.

```csharp
switch (input.Split(' '))
{
    case ["give", var item]: Give(item, 1); break;
    case ["give", var item, var n]: Give(item, int.Parse(n)); break;
    case ["tp", var x, var y, var z]: Teleport(x, y, z); break;
    case []: break;
    default: Debug.LogWarning($"Unknown command: {input}"); break;
}

// Hadouken: the last four inputs in the buffer
if (_inputs is [.., Cmd.Down, Cmd.DownForward, Cmd.Forward, Cmd.Punch])
    Cast(Special.Fireball);
```

#### Required members

**🧩 Needs a polyfill**

**Where:** DTOs, configs, events and services created with `new`. The compiler won't let you forget a mandatory member in an object initializer.

> Polyfills: `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`, `SetsRequiredMembersAttribute`.

> Pointless on `MonoBehaviour` and `ScriptableObject`: Unity creates them itself, without an initializer, so the check never fires.

```csharp
public sealed class MatchConfig
{
    public required string MapId { get; init; }
    public required int MaxPlayers { get; init; }
    public float TimeLimit { get; init; } = 300f;

    public MatchConfig() { }

    [SetsRequiredMembers]
    public MatchConfig(string mapId) { MapId = mapId; MaxPlayers = 4; }
}

var cfg = new MatchConfig { MapId = "harbor" };
// error CS9035: required member MaxPlayers is not set
```

#### File-local types

**✅ Works**

**Where:** helper classes needed by a single file, and generator output. The type is visible only within its file, so names don't clash.

> Don't make `MonoBehaviour` or `ScriptableObject` subclasses `file`: Unity looks them up by file name.

```csharp
public class Projectile : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (Layers.IsEnemy(other.gameObject)) Explode();
    }
}

file static class Layers
{
    private static readonly int Enemy = LayerMask.NameToLayer("Enemy");
    public static bool IsEnemy(GameObject go) => go.layer == Enemy;
}
```

#### Auto-default structs

**✅ Works**

**Where:** struct constructors. You no longer have to assign every field; the rest get default values.

```csharp
public struct HitInfo
{
    public Vector3 Point;
    public float Damage;
    public bool IsCritical;

    public HitInfo(Vector3 point) { Point = point; } // error CS0171 in C# 10
}
```

#### Pattern match `Span<char>` on a constant string

**✅ Works**

**Where:** allocation-free text parsing: configs, chat commands, network messages. A string slice is compared with a constant directly.

```csharp
ReadOnlySpan<char> key = line.AsSpan(0, line.IndexOf('='));

switch (key)
{
    case "fov": _camera.fieldOfView = ParseValue(line); break;
    case "vsync": QualitySettings.vSyncCount = (int)ParseValue(line); break;
}
```

#### nameof of a parameter in a method attribute

**✅ Works**

**Where:** nullable attributes like `[NotNullIfNotNull]` without strings that break on rename.

```csharp
[return: NotNullIfNotNull(nameof(fallback))]
public static Sprite? IconOr(Item? item, Sprite? fallback) =>
    item?.Icon ?? fallback;
```

#### The `>>>` operator

**✅ Works**

**Where:** hashes, bit masks, random number generators. An unsigned shift for `int` without casting to `uint` and back.

```csharp
public static int Hash(int x, int y)
{
    var h = x * 374761393 + y * 668265263;
    h = (h ^ (h >>> 13)) * 1274126177;
    return h ^ (h >>> 16);
}
```

#### Checked user-defined operators

**✅ Works**

**Where:** custom numeric types: in-game currency, fixed-point for deterministic simulation. Inside `checked(...)` the overflow-checking version is called.

```csharp
public readonly record struct Gold(long Value)
{
    public static Gold operator +(Gold a, Gold b) => new(a.Value + b.Value);
    public static Gold operator checked +(Gold a, Gold b) => new(checked(a.Value + b.Value));
}

var total = checked(wallet + reward); // OverflowException instead of a silent overflow
```

#### Cached method group delegates

**✅ Works**

**Where:** nothing to write. Passing a static method as a delegate no longer allocates on every call, so GC garbage disappears from `Update`.

```csharp
private void Update()
{
    // before C# 11: new Predicate<Bullet>(IsExpired) every frame
    _bullets.RemoveAll(IsExpired);
}

private static bool IsExpired(Bullet b) => b.Lifetime <= 0f;
```

#### scoped and [UnscopedRef]

**🧩 Needs a polyfill**

**Where:** performance code with `Span` and `ref struct`. `scoped` promises the reference won't escape the method; `[UnscopedRef]` allows returning a reference to a struct field.

> `scoped` works on its own. `[UnscopedRef]` needs the `UnscopedRefAttribute` polyfill.

```csharp
public static int CountVisible(scoped ReadOnlySpan<Bounds> bounds, Plane[] frustum)
{
    var n = 0;
    foreach (var b in bounds)
        if (GeometryUtility.TestPlanesAABB(frustum, b)) n++;
    return n;
}

public struct Particle
{
    private Vector3 _velocity;
    [UnscopedRef] public ref Vector3 Velocity => ref _velocity;
}
```

#### Generic attributes

**⚠️ With caveats**

**Where:** an attribute that needs a type, without `typeof`.

> Compiles, but reflection over such attributes is unreliable in Mono and IL2CPP. In Unity, prefer `[Attr(typeof(T))]`.

```csharp
public sealed class RequiresService<T> : Attribute { }

[RequiresService<IAudioService>] // instead of [RequiresService(typeof(IAudioService))]
public class MusicPlayer : MonoBehaviour { }
```

#### Static abstract interface members (generic math)

**❌ Doesn't work**

**Would be used for:** generic math functions over `int`, `float` and custom types via `INumber`.

> Doesn't compile: `error CS8919`, the runtime doesn't support static abstract members. For math in Unity, use `Unity.Mathematics`.

```csharp
public interface IAdd<T> where T : IAdd<T>
{
    static abstract T operator +(T a, T b); // CS8919
}
```

#### ref fields

**❌ Doesn't work**

**Would be used for:** a `ref struct` that holds a reference to another variable.

> Doesn't compile: `error CS9064`, the runtime doesn't support ref fields. Store a `Span` of length 1 instead.

```csharp
public ref struct Cursor
{
    public ref int Index; // CS9064
}
```

### C# 12

_November 2023 · .NET 8_

#### Primary constructors

**✅ Works**

**Where:** plain C# classes: services, commands, FSM states, classes with constructor injection (VContainer, Zenject). Parameters are visible throughout the class body.

> Doesn't work on `MonoBehaviour` and `ScriptableObject`: Unity creates them without arguments. Parameters are not `readonly`. If that matters, copy into a field: `private readonly ILogger _log = log;`

```csharp
public sealed class DamageService(IHealthRegistry health, IEventBus bus)
{
    public void Apply(int targetId, float amount)
    {
        var hp = health.Get(targetId);
        hp.Current -= amount;
        bus.Publish(new DamageEvent(targetId, amount, DamageType.Physical));
    }
}

public sealed class ChaseState(Enemy owner, Transform target) : IState
{
    public void Tick() => owner.MoveTo(target.position);
}
```

#### Collection expressions

**✅ Works**

**Where:** initializing lists and arrays, empty collections, concatenation with `..`. Works for arrays, `List`, `Span`, interfaces and in serialized field initializers.

> Custom collections need the `CollectionBuilderAttribute` polyfill. Concatenation creates a new array, which is an allocation in `Update`.

```csharp
[SerializeField] private string[] _startItems = ["sword", "potion"];
private readonly List<Enemy> _alive = [];

Vector3[] corners = [min, new(max.x, min.y, 0), max, new(min.x, max.y, 0)];
int[] allLevels = [..tutorialLevels, ..mainLevels, BossLevel];

IReadOnlyList<string> tags = ["boss", "flying"];
```

#### Alias any type

**✅ Works**

**Where:** short names for tuples and long generic types: grid coordinates, loot tables.

```csharp
using Cell = (int X, int Y);
using LootTable = System.Collections.Generic.Dictionary<string, (int Weight, int Min, int Max)>;

public bool IsWalkable(Cell c) => _grid[c.X, c.Y].Walkable;
private readonly LootTable _loot = new();
```

#### Default lambda parameters

**✅ Works**

**Where:** local helpers with optional arguments.

```csharp
var shake = (float strength = 0.3f, float duration = 0.15f) =>
    _cameraShake.Play(strength, duration);

shake();
shake(1f);
```

#### params in lambdas

**✅ Works**

**Where:** local helpers with a variable number of arguments.

```csharp
var disable = (params Behaviour[] items) =>
{
    foreach (var b in items) b.enabled = false;
};

disable(_movement, _shooting, _input);
```

#### ref readonly parameters

**✅ Works**

**Where:** passing large structs (`Matrix4x4`, `Bounds`, your own data) without copying when the method must not modify them. Unlike `in`, the compiler warns if you pass a temporary value instead of a variable.

> No polyfill needed: the compiler embeds the required attribute into the assembly itself.

```csharp
public static Vector3 ToWorld(ref readonly Matrix4x4 localToWorld, Vector3 p) =>
    localToWorld.MultiplyPoint3x4(p);

var m = transform.localToWorldMatrix;
var world = ToWorld(ref m, offset);
```

#### [Experimental]

**🧩 Needs a polyfill**

**Where:** marking your own unfinished API. Any usage becomes a compile error until explicitly suppressed. Useful in shared code used by several assemblies.

> Polyfill: `ExperimentalAttribute`. To suppress: `#pragma warning disable PG0001`.

```csharp
[Experimental("PG0001")]
public static class NewPathfinder
{
    public static List<Cell> Find(Cell from, Cell to) => [];
}

#pragma warning disable PG0001
var path = NewPathfinder.Find(start, goal);
#pragma warning restore PG0001
```

#### Inline arrays

**❌ Doesn't work**

**Would be used for:** fixed-size buffers inside a struct without `unsafe fixed`.

> Requires runtime support. Don't add a polyfill for `InlineArrayAttribute`: the code will compile, but Mono won't allocate memory for the elements. Use a `fixed` buffer or `FixedList32Bytes` from Unity.Collections.

```csharp
[InlineArray(4)]
public struct WheelBuffer
{
    private WheelHit _element; // CS0234: InlineArrayAttribute not found
}
```

#### Interceptors

**❌ Doesn't work**

**Would be used for:** code generators that replace a specific method call with their own implementation.

> In C# 12 this is a preview feature for generator authors and isn't needed in regular code.

```csharp
// Written only in generated code
[InterceptsLocation("Assets/Game/Boot.cs", line: 12, character: 9)]
public static void Log_Intercepted(string msg) { }
```

## Polyfills

The generator emits them as `internal` types; the sources are in [`Source~/ModernCSharp.Generators/Polyfills.cs`](Source~/ModernCSharp.Generators/Polyfills.cs).

| Type | Namespace | Used for |
|---|---|---|
| `IsExternalInit` | `System.Runtime.CompilerServices` | `record`, `init`, `readonly record struct` |
| `CallerArgumentExpressionAttribute` | `System.Runtime.CompilerServices` | Argument text in check messages |
| `InterpolatedStringHandlerAttribute` | `System.Runtime.CompilerServices` | Custom `$"..."` handlers |
| `InterpolatedStringHandlerArgumentAttribute` | `System.Runtime.CompilerServices` | Passing method arguments to a handler |
| `RequiredMemberAttribute` | `System.Runtime.CompilerServices` | `required` members |
| `CompilerFeatureRequiredAttribute` | `System.Runtime.CompilerServices` | `required` members |
| `SetsRequiredMembersAttribute` | `System.Diagnostics.CodeAnalysis` | A constructor that sets `required` members itself |
| `UnscopedRefAttribute` | `System.Diagnostics.CodeAnalysis` | Returning a reference to a struct field |
| `CollectionBuilderAttribute` | `System.Runtime.CompilerServices` | Collection expressions for custom collections |
| `ExperimentalAttribute` | `System.Diagnostics.CodeAnalysis` | `[Experimental]` |

Rules:

- **Don't change the namespaces.** The compiler looks these types up by their full name.
- **Existing copies are respected.** If an assembly can already see a type (its own file, a `public` type in a third-party DLL, an `InternalsVisibleTo` copy), the generator doesn't emit it. Internal copies in other assemblies, like the one in Unity Test Framework, don't interfere.

Polyfills can't enable features that need runtime support, which Mono and IL2CPP lack:

- static abstract/virtual interface members: error CS8919;
- `ref` fields: error CS9064;
- inline arrays: the code compiles but won't work.

## Unity gotchas

- **Asmdefs outside the filter.** Assemblies skipped by the path filter don't get the generator, so `record` and `required` won't compile there unless they have their own polyfills.
- **Serialization.** The inspector and `JsonUtility` work only with fields. They don't see `init` properties, record parameters or primary constructors. Keep persisted data in regular fields with `[SerializeField]`.
- **Fake null.** A destroyed Unity object equals `null` only through Unity's own `==` operator. `?.`, `??`, `is null` and patterns ignore this, and so does nullable analysis.
- **Nullable and serialized fields.** Declare inspector-assigned fields with `= null!`, and optional ones as `T?`.
- **IDE.** After changing `csc.rsp`, run Edit → Preferences → External Tools → Regenerate project files.

---

Statuses were verified by compiling with Roslyn 4.10 from Unity 6000.6.3f1 against `netstandard2.1`. Runtime behavior in Mono and IL2CPP was not tested separately.

## Tests

**Generator and feature matrix** (`Source~/`, plain .NET, runs in CI on every push):

```
dotnet test "Source~/ModernCSharp.sln"
```

- the generator emits every polyfill as `internal`, skips types the assembly already sees and references Roslyn no newer than Unity's;
- every feature from [C# 10–12 in Unity](#c-1012-in-unity) has a snippet in `Source~/ModernCSharp.Generators.Tests/Features/`, compiled with Roslyn 4.10 against `netstandard2.1`, the way Unity 6000.6 compiles it. The test checks the status from this README and fails if the README and the snippets disagree.

When Unity updates its compiler, raise `Microsoft.CodeAnalysis.CSharp` in the test project and see which statuses change.

**Editor logic** (`Tests/Editor/`, Unity Test Framework, EditMode): `csc.rsp` handling, define symbols and the path filter. To run them in your project, add the package to `testables` in `Packages/manifest.json`:

```json
"testables": ["com.iacolyte.modern-csharp"]
```

## License

[MIT](LICENSE.md) © 2026 Alexander Gorozhankin
