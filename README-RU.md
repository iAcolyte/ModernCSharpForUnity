[![Tests](https://github.com/iAcolyte/ModernCSharpForUnity/actions/workflows/tests.yml/badge.svg)](https://github.com/iAcolyte/ModernCSharpForUnity/actions/workflows/tests.yml)

# Modern C# for Unity

Unity-пакет `com.iacolyte.modern-csharp`. Он включает C# 10–12 и nullable в Unity 6:

- добавляет страницу в Project Settings, которая создаёт и обновляет `csc.rsp` рядом с каждым `.asmdef` в `Assets`;
- подключает через тот же `csc.rsp` source generator, который создаёт заглушки служебных типов, которых нет в `netstandard2.1`.

## Установка

**Window → Package Manager → + → Add package from git URL** и адрес репозитория:

```
https://github.com/iAcolyte/ModernCSharpForUnity.git
```

Конкретная версия подключается по тегу: `https://github.com/iAcolyte/ModernCSharpForUnity.git#v0.1.0`. То же самое можно прописать в `Packages/manifest.json`:

```json
"com.iacolyte.modern-csharp": "https://github.com/iAcolyte/ModernCSharpForUnity.git#v0.1.0"
```

Требуется Unity 6000.0 или новее.

## Состав

```
package.json
Editor/
├── ModernCSharp.Editor.asmdef
├── ModernCSharpSettingsProvider.cs # Project Settings → C# Language
├── ModernCSharpApplier.cs          # запись csc.rsp
└── AsmdefWatcher.cs                # автообновление при добавлении asmdef
Tests/Editor/                       # EditMode-тесты логики Editor
Generators~/
└── ModernCSharp.Generators.dll     # генератор заглушек, Unity его не видит
Source~/                            # Unity её не видит
├── ModernCSharp.sln
├── build.sh                        # собирает DLL в Generators~/
├── ModernCSharp.Generators/        # исходники генератора
└── ModernCSharp.Generators.Tests/  # тесты генератора и матрица фич
```

## Настройки

Открываются через **Edit → Project Settings → C# Language**. Хранятся в `ProjectSettings/ModernCSharpSettings.asset`. Этот файл стоит коммитить, чтобы у всей команды были одинаковые настройки.

| Поле | Что делает |
|---|---|
| Language Version | C# 9, 10, 11 или 12. Пишется в `csc.rsp` как `-langversion`. |
| Nullable | Включено: `-nullable:enable`, выключено: `-nullable:disable`. |
| Manage Assets/csc.rsp | Управлять ли корневым `Assets/csc.rsp`. Он настраивает `Assembly-CSharp` и `Assembly-CSharp-Editor`. |
| Auto Apply | Обновлять файлы при запуске редактора, при появлении нового asmdef и при любом изменении настроек. Если выключено, изменения применяются только кнопкой **Apply to All Assemblies**. |
| Path Filter | **Blacklist**: управлять всеми asmdef в `Assets`, кроме лежащих в перечисленных папках. **Whitelist**: только asmdef в перечисленных папках. |
| Folders | Папки для фильтра, например `Assets/Plugins`. Под фильтр попадают и все вложенные папки. |

В свёрнутом блоке **Assemblies** видно, какие asmdef сейчас обрабатываются (`managed`), а какие пропущены (`skipped`).

Как утилита работает с `csc.rsp`:

- в файле меняются только строки `-langversion`, `-nullable`, [define-символы](#define-символы) `MODERN_CSHARP_*` и генератор заглушек, остальные опции, включая другие анализаторы и свои define (`-warnaserror`, `-define` и т. д.) остаются как были;
- файл перезаписывается, только если содержимое действительно изменилось, поэтому лишних перекомпиляций нет;
- файлы у asmdef, которые выпали из фильтра, не удаляются и не меняются;
- asmdef из `Packages/` не трогаются, у сборок самого пакета свой `csc.rsp`.

После смены версии языка выполни **Edit → Preferences → External Tools → Regenerate project files**, чтобы IDE увидела новую версию и define-символы.

## Define-символы

В каждый управляемый `csc.rsp` добавляется ещё и строка `-define:`, чтобы код мог проверять выбранные настройки:

| Символ | Когда определён |
|---|---|
| `MODERN_CSHARP_9_OR_NEWER` | Всегда. Отмечает сборку, которой управляет пакет. |
| `MODERN_CSHARP_10_OR_NEWER` … `MODERN_CSHARP_12_OR_NEWER` | Language Version не ниже этой версии. |
| `MODERN_CSHARP_NULLABLE` | Nullable включён. |

```csharp
#if MODERN_CSHARP_12_OR_NEWER
    int[] items = [1, 2, 3];
#else
    int[] items = { 1, 2, 3 };
#endif
```

Символы есть только в управляемых сборках: в `Assets/csc.rsp` и в asmdef, прошедших фильтр путей. Пропущенные фильтром сборки и код других пакетов их не видят. Свои символы `-define` в том же файле сохраняются.

## Генератор заглушек

Заглушки создаёт source generator для Roslyn. Утилита добавляет его в каждый управляемый `csc.rsp`:

```
-a:Library/PackageCache/com.iacolyte.modern-csharp@<hash>/Generators~/ModernCSharp.Generators.dll
```

- Каждая сборка получает свои `internal`-копии типов из таблицы [Заглушки](#заглушки), поэтому в asmdef ничего добавлять не нужно.
- Тип пропускается, если сборка уже его видит: из BCL, из сторонней DLL или из другой сборки через `InternalsVisibleTo`. Поэтому конфликтов CS0433 и CS0436 нет.
- Папки с `~` в конце имени Unity не импортирует, поэтому DLL не видна в окне Project и не считается плагином.
- Rider и Visual Studio читают `-a:` из `csc.rsp` и тоже запускают генератор, так что IDE видит сгенерированные типы. Они показываются в **Dependencies → Analyzers**, файлов на диске нет.
- Путь к пакету меняется при каждом обновлении. Утилита переписывает его при запуске редактора. Editor-сборка самого пакета генератор не использует, поэтому она компилируется даже со старым путём.

Чтобы изменить генератор, правь `Source~/` и запускай `Source~/build.sh`. Нужен .NET SDK, сборка идёт с `Microsoft.CodeAnalysis.CSharp` 4.8: генератор не должен ссылаться на Roslyn новее, чем у компилятора Unity (4.10 в Unity 6000.6).

## C# 10–12 в Unity

Всё, что появилось в языке после C# 9, с пометкой, работает ли это в Unity, где это применять и пример на игровом коде.

| Статус | Фич |
|---|---|
| ✅ Работает | 30 |
| 🧩 Нужна заглушка | 5 |
| ⚠️ С оговорками | 1 |
| ❌ Не работает | 5 |

### C# 10

_ноябрь 2021 · .NET 6_

#### File-scoped namespace

**✅ Работает**

**Где:** в каждом файле. Убирает уровень отступа, в файле с одним классом код становится шире на 4 пробела.

```csharp
namespace Playgrounds.Core;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _prefab = null!;
}
```

#### global using

**✅ Работает**

**Где:** один файл `GlobalUsings.cs` на сборку. Действует на всю сборку, то есть на весь asmdef, в котором лежит.

> В каждом asmdef нужен свой файл: `global using` не переходит между сборками.

```csharp
// Assets/_Project/Core/GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using UnityEngine;
global using Object = UnityEngine.Object;
global using Random = UnityEngine.Random;
```

#### record struct

**✅ Работает**

**Где:** события для шины сообщений, ключи словарей, результаты вычислений. Равенство по значению, `ToString` и `with` даются бесплатно и без аллокаций.

> Позиционные параметры становятся свойствами, а инспектор и `JsonUtility` работают только с полями. Для сериализуемых данных оставь обычный struct.

```csharp
public readonly record struct DamageEvent(int TargetId, float Amount, DamageType Type);

_bus.Publish(new DamageEvent(enemy.Id, 12.5f, DamageType.Fire));

// Ключ словаря с корректным GetHashCode/Equals
private readonly Dictionary<ChunkKey, Chunk> _chunks = new();
public readonly record struct ChunkKey(int X, int Z);
```

#### Конструктор без параметров и инициализаторы полей в struct

**⚠️ С оговорками**

**Где:** структуры настроек, у которых нули не подходят как значения по умолчанию.

> `default(T)`, `new T[n]` и десериализация Unity этот конструктор не вызывают, там поля остаются нулями. Срабатывает только явный `new T()`.

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

#### Расширенные шаблоны свойств

**✅ Работает**

**Где:** условия в AI, фильтры событий, `switch` по вложенным данным. Вместо `{ Target: { Stats: { Hp: … } } }` пишется путь через точку.

> Шаблоны проверяют настоящий null. Уничтоженный `GameObject` пройдёт проверку `is { }`, поэтому для объектов Unity используй обычное `!= null`.

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

#### Естественный тип лямбд

**✅ Работает**

**Где:** локальные хелперы внутри метода. Лямбду можно положить в `var`, компилятор сам выведет `Func` или `Action`.

```csharp
var isAlive = (Enemy e) => e.Hp > 0;           // Func<Enemy, bool>
var log = (string m) => Debug.Log($"[AI] {m}"); // Action<string>

foreach (var e in _enemies)
    if (isAlive(e)) log(e.name);
```

#### Явный тип возврата у лямбд

**✅ Работает**

**Где:** когда компилятор не может вывести тип сам, например при возврате `null` в одной из веток.

```csharp
var parseLevel = int? (string s) => int.TryParse(s, out var v) ? v : null;

var level = parseLevel(PlayerPrefs.GetString("level")) ?? 1;
```

#### Атрибуты на лямбдах

**✅ Работает**

**Где:** в Unity редко. Полезно, чтобы передать nullable-атрибуты анализатору или пометить обработчик.

```csharp
var onLegacyClick = [Obsolete("Используй OnSubmit")] () => Submit();

var findTarget = [return: MaybeNull] () => _targets.Count > 0 ? _targets[0] : null;
```

#### Константные интерполированные строки

**✅ Работает**

**Где:** пути в `[MenuItem]` и `[CreateAssetMenu]`, ключи `PlayerPrefs`, имена Addressables. Работает, если все части строки сами константы.

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

#### Смешанная деконструкция

**✅ Работает**

**Где:** когда часть переменных уже объявлена, а часть новая.

```csharp
Vector3 pos;
(pos, var rot) = (transform.position, transform.rotation);
```

#### sealed ToString в record

**✅ Работает**

**Где:** базовый record задаёт формат для логов, и наследники не могут его переопределить.

```csharp
public abstract record Command(int Tick)
{
    public sealed override string ToString() => $"#{Tick} {GetType().Name}";
}

public record MoveCommand(int Tick, Vector2 Dir) : Command(Tick);
```

#### [CallerArgumentExpression]

**🧩 Нужна заглушка**

**Где:** проверки в `Awake`, свои `Guard` и `Assert`. В сообщение об ошибке автоматически попадает текст выражения.

> Заглушка: `CallerArgumentExpressionAttribute`.

```csharp
public static class Guard
{
    public static T Require<T>(T? value,
        [CallerArgumentExpression("value")] string expr = "") where T : Object
    {
        if (value == null) // оператор Unity: ловит и уничтоженные объекты
            throw new MissingReferenceException($"{expr} не найден");
        return value;
    }
}

private void Awake()
{
    _body = Guard.Require(GetComponent<Rigidbody>());
    // MissingReferenceException: GetComponent<Rigidbody>() не найден
}
```

#### Свои обработчики интерполированных строк

**🧩 Нужна заглушка**

**Где:** логгер, который вообще не собирает строку, если уровень отключён. Это убирает аллокации от `$"..."` в `Update` в релизной сборке.

> Заглушки: `InterpolatedStringHandlerAttribute`, `InterpolatedStringHandlerArgumentAttribute`. Обычный `$"..."` работает и без них.

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

// При Verbose == false выражения в {} даже не вычисляются
Log.Trace($"pos={transform.position} vel={_body.velocity}");
```

#### [AsyncMethodBuilder] на методе

**❌ Не работает**

**Где было бы:** в основном в библиотеках. Позволяет выбрать свой builder для одного async-метода, например пулящий, чтобы не аллоцировать state machine.

> Не компилируется: `error CS0592`. В `netstandard2.1` `AsyncMethodBuilderAttribute` разрешён только на типах, методы добавили в .NET 6. Заглушкой это не исправить: тип уже существует, и своя копия компилируется только с предупреждением о конфликте CS0436. На типах атрибут работает, так UniTask объявляет свои task-типы.

```csharp
[AsyncMethodBuilder(typeof(PooledTaskBuilder))]
private async ValueTask LoadChunkAsync(ChunkKey key)
{
    await _io.ReadAsync(key);
}
```

#### Улучшенный анализ присваивания

**✅ Работает**

**Где:** ничего писать не нужно. Компилятор реже выдаёт ложную ошибку CS0165 в условиях с `?.`, `is` и `== true`.

```csharp
if (_cache?.TryGetValue(id, out var item) == true)
    Use(item); // в C# 9 здесь была ошибка CS0165
```

#### Расширенный #line

**✅ Работает**

**Где:** только в кодогенераторах. Привязывает сгенерированный код к диапазону строк исходника, чтобы ошибки и отладка указывали на правильное место.

```csharp
#line (12, 5) - (12, 30) 8 "Assets/Dialogs/intro.dlg"
Say("Привет, путник");
#line default
```

### C# 11

_ноябрь 2022 · .NET 7_

#### Raw string literals

**✅ Работает**

**Где:** JSON в тестах и фикстурах сохранений, шаблоны кода в редакторных скриптах, регулярные выражения, фрагменты шейдеров. Кавычки и слэши не нужно экранировать. С `$$` фигурные скобки остаются текстом, а подстановка идёт через `{{ }}`.

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

#### Переносы строк внутри интерполяции

**✅ Работает**

**Где:** длинные выражения внутри `{ }`, например `switch` прямо в строке для UI.

```csharp
_label.text = $"Сложность: {difficulty switch
{
    Difficulty.Easy => "лёгкая",
    Difficulty.Hard => "высокая",
    _ => "обычная",
}}";
```

#### UTF-8 строковые литералы

**✅ Работает**

**Где:** сетевые протоколы, сигнатуры файлов сохранений, бинарные форматы. Литерал `"..."u8` даёт `ReadOnlySpan` без аллокаций и без `Encoding.UTF8.GetBytes`.

```csharp
private static ReadOnlySpan<byte> Magic => "PGSV"u8;

public static bool IsSaveFile(ReadOnlySpan<byte> header) =>
    header.Length >= 4 && header[..4].SequenceEqual(Magic);
```

#### List patterns

**✅ Работает**

**Где:** команды отладочной консоли, комбо в файтинге по буферу ввода, проверки формы массива. `..` пропускает любое количество элементов, `.. var rest` забирает их в срез.

```csharp
switch (input.Split(' '))
{
    case ["give", var item]: Give(item, 1); break;
    case ["give", var item, var n]: Give(item, int.Parse(n)); break;
    case ["tp", var x, var y, var z]: Teleport(x, y, z); break;
    case []: break;
    default: Debug.LogWarning($"Неизвестная команда: {input}"); break;
}

// Хадукен: последние четыре ввода в буфере
if (_inputs is [.., Cmd.Down, Cmd.DownForward, Cmd.Forward, Cmd.Punch])
    Cast(Special.Fireball);
```

#### required-члены

**🧩 Нужна заглушка**

**Где:** DTO, конфиги, события и сервисы, которые создаются через `new`. Компилятор не даст забыть обязательное поле в инициализаторе объекта.

> Заглушки: `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`, `SetsRequiredMembersAttribute`.

> На `MonoBehaviour` и `ScriptableObject` смысла нет: Unity создаёт их сама, без инициализатора, и проверка не срабатывает. Так сейчас сделано в `Foo.cs`.

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
// error CS9035: не задан обязательный член MaxPlayers
```

#### File-local типы

**✅ Работает**

**Где:** вспомогательные классы, которые нужны одному файлу, и код из генераторов. Тип виден только внутри своего файла, поэтому имена не конфликтуют.

> Не делай `file` классы-наследники `MonoBehaviour` и `ScriptableObject`: Unity ищет их по имени файла.

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

#### Автоинициализация struct

**✅ Работает**

**Где:** конструкторы структур. Больше не нужно присваивать каждое поле, остальные получат значения по умолчанию.

```csharp
public struct HitInfo
{
    public Vector3 Point;
    public float Damage;
    public bool IsCritical;

    public HitInfo(Vector3 point) { Point = point; } // в C# 10 здесь ошибка CS0171
}
```

#### `Span<char>` в шаблоне с константой

**✅ Работает**

**Где:** разбор текста без аллокаций: конфиги, чат-команды, сетевые сообщения. Срез строки сравнивается с константой напрямую.

```csharp
ReadOnlySpan<char> key = line.AsSpan(0, line.IndexOf('='));

switch (key)
{
    case "fov": _camera.fieldOfView = ParseValue(line); break;
    case "vsync": QualitySettings.vSyncCount = (int)ParseValue(line); break;
}
```

#### nameof параметра в атрибуте метода

**✅ Работает**

**Где:** nullable-атрибуты вроде `[NotNullIfNotNull]` без строк, которые ломаются при переименовании.

```csharp
[return: NotNullIfNotNull(nameof(fallback))]
public static Sprite? IconOr(Item? item, Sprite? fallback) =>
    item?.Icon ?? fallback;
```

#### Оператор `>>>`

**✅ Работает**

**Где:** хэши, битовые маски, генераторы случайных чисел. Беззнаковый сдвиг для `int` без приведения к `uint` и обратно.

```csharp
public static int Hash(int x, int y)
{
    var h = x * 374761393 + y * 668265263;
    h = (h ^ (h >>> 13)) * 1274126177;
    return h ^ (h >>> 16);
}
```

#### checked-операторы

**✅ Работает**

**Где:** свои числовые типы: игровая валюта, fixed-point для детерминированной симуляции. Внутри `checked(...)` вызывается версия с проверкой переполнения.

```csharp
public readonly record struct Gold(long Value)
{
    public static Gold operator +(Gold a, Gold b) => new(a.Value + b.Value);
    public static Gold operator checked +(Gold a, Gold b) => new(checked(a.Value + b.Value));
}

var total = checked(wallet + reward); // OverflowException вместо тихого переполнения
```

#### Кэширование делегатов из method group

**✅ Работает**

**Где:** ничего писать не нужно. Передача статического метода как делегата больше не аллоцирует каждый вызов, и в `Update` пропадает мусор для GC.

```csharp
private void Update()
{
    // до C# 11: new Predicate<Bullet>(IsExpired) каждый кадр
    _bullets.RemoveAll(IsExpired);
}

private static bool IsExpired(Bullet b) => b.Lifetime <= 0f;
```

#### scoped и [UnscopedRef]

**🧩 Нужна заглушка**

**Где:** производительный код на `Span` и `ref struct`. `scoped` обещает, что ссылка не утечёт из метода, `[UnscopedRef]` разрешает вернуть ссылку на поле структуры.

> `scoped` работает сам. Для `[UnscopedRef]` нужна заглушка `UnscopedRefAttribute`.

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

#### Generic-атрибуты

**✅ Работает**

**Где:** атрибут, которому нужен тип, без `typeof`.

> Чтение через рефлексию (`GetCustomAttributes(typeof(Attr<T>))`) работает и в Mono, и в IL2CPP. Проверено рантайм-тестами на Unity 6000.6.3f1.

```csharp
public sealed class RequiresService<T> : Attribute { }

[RequiresService<IAudioService>] // вместо [RequiresService(typeof(IAudioService))]
public class MusicPlayer : MonoBehaviour { }
```

#### Static abstract члены интерфейсов (generic math)

**❌ Не работает**

**Где было бы:** обобщённые математические функции для `int`, `float` и своих типов через `INumber`.

> Не собирается: `error CS8919`, среда выполнения не поддерживает статические абстрактные члены. Для математики в Unity используй `Unity.Mathematics`.

```csharp
public interface IAdd<T> where T : IAdd<T>
{
    static abstract T operator +(T a, T b); // CS8919
}
```

#### ref-поля

**❌ Не работает**

**Где было бы:** `ref struct`, который хранит ссылку на чужую переменную.

> Не собирается: `error CS9064`, среда выполнения не поддерживает ref-поля. Вместо этого храни `Span` длиной 1.

```csharp
public ref struct Cursor
{
    public ref int Index; // CS9064
}
```

### C# 12

_ноябрь 2023 · .NET 8_

#### Primary constructors

**✅ Работает**

**Где:** обычные C#-классы: сервисы, команды, состояния FSM, классы для DI через конструктор (VContainer, Zenject). Параметры видны во всём теле класса.

> На `MonoBehaviour` и `ScriptableObject` не работает: Unity создаёт их без аргументов. Параметры не `readonly`. Если это важно, скопируй в поле: `private readonly ILogger _log = log;`

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

**✅ Работает**

**Где:** инициализация списков и массивов, пустые коллекции, склейка через `..`. Работает для массивов, `List`, `Span`, интерфейсов и в инициализаторах сериализуемых полей.

> Для своих коллекций нужна заглушка `CollectionBuilderAttribute`. Склейка создаёт новый массив, в `Update` это аллокация.

```csharp
[SerializeField] private string[] _startItems = ["sword", "potion"];
private readonly List<Enemy> _alive = [];

Vector3[] corners = [min, new(max.x, min.y, 0), max, new(min.x, max.y, 0)];
int[] allLevels = [..tutorialLevels, ..mainLevels, BossLevel];

IReadOnlyList<string> tags = ["boss", "flying"];
```

#### Alias для любого типа

**✅ Работает**

**Где:** короткие имена для кортежей и длинных generic-типов: координаты сетки, таблицы лута.

```csharp
using Cell = (int X, int Y);
using LootTable = System.Collections.Generic.Dictionary<string, (int Weight, int Min, int Max)>;

public bool IsWalkable(Cell c) => _grid[c.X, c.Y].Walkable;
private readonly LootTable _loot = new();
```

#### Параметры по умолчанию в лямбдах

**✅ Работает**

**Где:** локальные хелперы с необязательными аргументами.

```csharp
var shake = (float strength = 0.3f, float duration = 0.15f) =>
    _cameraShake.Play(strength, duration);

shake();
shake(1f);
```

#### params в лямбдах

**✅ Работает**

**Где:** локальные хелперы с переменным числом аргументов.

```csharp
var disable = (params Behaviour[] items) =>
{
    foreach (var b in items) b.enabled = false;
};

disable(_movement, _shooting, _input);
```

#### ref readonly параметры

**✅ Работает**

**Где:** передача больших структур (`Matrix4x4`, `Bounds`, свои данные) без копирования, когда метод не должен их менять. Отличается от `in` тем, что компилятор предупреждает, если передать временное значение вместо переменной.

> Заглушка не нужна: компилятор сам добавляет нужный атрибут в сборку.

```csharp
public static Vector3 ToWorld(ref readonly Matrix4x4 localToWorld, Vector3 p) =>
    localToWorld.MultiplyPoint3x4(p);

var m = transform.localToWorldMatrix;
var world = ToWorld(ref m, offset);
```

#### [Experimental]

**🧩 Нужна заглушка**

**Где:** пометить своё сырое API. Любое использование станет ошибкой компиляции, пока её явно не подавят. Полезно в общем коде, который использует несколько сборок.

> Заглушка: `ExperimentalAttribute`. Подавить: `#pragma warning disable PG0001`.

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

**❌ Не работает**

**Где было бы:** буферы фиксированного размера внутри struct без `unsafe fixed`.

> Нужна поддержка среды выполнения. Заглушку для `InlineArrayAttribute` добавлять нельзя: код соберётся, но Mono не выделит память под элементы. Используй `fixed`-буфер или `FixedList32Bytes` из Unity.Collections.

```csharp
[InlineArray(4)]
public struct WheelBuffer
{
    private WheelHit _element; // CS0234: InlineArrayAttribute не найден
}
```

#### Interceptors

**❌ Не работает**

**Где было бы:** кодогенераторы, которые подменяют конкретный вызов метода своей реализацией.

> В C# 12 это preview-фича для авторов генераторов, в обычном коде не нужна.

```csharp
// Пишется только в сгенерированном коде
[InterceptsLocation("Assets/Game/Boot.cs", line: 12, character: 9)]
public static void Log_Intercepted(string msg) { }
```

## Заглушки

Генератор создаёт их как `internal`-типы, исходники лежат в [`Source~/ModernCSharp.Generators/Polyfills.cs`](Source~/ModernCSharp.Generators/Polyfills.cs).

| Тип | Пространство имён | Для чего |
|---|---|---|
| `IsExternalInit` | `System.Runtime.CompilerServices` | `record`, `init`, `readonly record struct` |
| `CallerArgumentExpressionAttribute` | `System.Runtime.CompilerServices` | Текст аргумента в сообщениях проверок |
| `InterpolatedStringHandlerAttribute` | `System.Runtime.CompilerServices` | Свои обработчики `$"..."` |
| `InterpolatedStringHandlerArgumentAttribute` | `System.Runtime.CompilerServices` | Передача аргументов метода в обработчик |
| `RequiredMemberAttribute` | `System.Runtime.CompilerServices` | `required`-члены |
| `CompilerFeatureRequiredAttribute` | `System.Runtime.CompilerServices` | `required`-члены |
| `SetsRequiredMembersAttribute` | `System.Diagnostics.CodeAnalysis` | Конструктор, который сам заполняет `required` |
| `UnscopedRefAttribute` | `System.Diagnostics.CodeAnalysis` | Возврат ссылки на поле структуры |
| `CollectionBuilderAttribute` | `System.Runtime.CompilerServices` | Collection expressions для своих коллекций |
| `ExperimentalAttribute` | `System.Diagnostics.CodeAnalysis` | `[Experimental]` |

Правила:

- **Не менять пространства имён.** Компилятор ищет эти типы по полному имени.
- **Существующие копии учитываются.** Если сборка уже видит тип (свой файл, `public`-тип в сторонней DLL, копию через `InternalsVisibleTo`), генератор его не создаёт. Internal-копии в других сборках, как в Unity Test Framework, не мешают.

Заглушками нельзя включить фичи, которым нужна поддержка среды выполнения. Её нет в Mono и IL2CPP:

- static abstract/virtual члены интерфейсов: ошибка CS8919;
- `ref`-поля: ошибка CS9064;
- inline arrays: код соберётся, но работать не будет.

## Что помнить в Unity

- **asmdef вне фильтра.** Сборки, которые пропускает фильтр путей, генератор не получают, и `record` с `required` там не соберутся без своих заглушек.
- **Сериализация.** Инспектор и `JsonUtility` работают только с полями. Свойства `init`, параметры record и primary constructors они не видят. Сохраняемые данные держи в обычных полях с `[SerializeField]`.
- **Фальшивый null.** Уничтоженный объект Unity равен `null` только через оператор `==` самой Unity. `?.`, `??`, `is null` и шаблоны это не учитывают, nullable-анализ тоже.
- **Nullable и сериализуемые поля.** Поле, которое заполняет инспектор, объявляй с `= null!`, а необязательное как `T?`.
- **IDE.** После изменения `csc.rsp` выполни Edit → Preferences → External Tools → Regenerate project files.

---

Статусы проверены компиляцией Roslyn 4.10 из Unity 6000.6.3f1 под `netstandard2.1`. Поведение во время выполнения для record, `required`, атрибутов-заглушек, своих обработчиков интерполированных строк, collection expressions, generic-атрибутов и конструкторов struct без параметров проверено в Mono (редактор) и IL2CPP (плеер macOS).

## Тесты

**Генератор и матрица фич** (`Source~/`, обычный .NET, запускается в CI на каждый пуш):

```
dotnet test "Source~/ModernCSharp.sln"
```

- генератор создаёт все заглушки как `internal`, пропускает типы, которые сборка уже видит, и ссылается на Roslyn не новее, чем в Unity;
- у каждой фичи из раздела [C# 10–12 в Unity](#c-1012-в-unity) есть сниппет в `Source~/ModernCSharp.Generators.Tests/Features/`. Он компилируется Roslyn 4.10 под `netstandard2.1`, как в Unity 6000.6. Тест проверяет статус из README и падает, если README и сниппеты расходятся.

Когда Unity обновит компилятор, подними версию `Microsoft.CodeAnalysis.CSharp` в тестовом проекте и посмотри, какие статусы изменились.

**Логика Editor** (`Tests/Editor/`, Unity Test Framework, EditMode): работа с `csc.rsp`, define-символы и фильтр путей. Чтобы запускать их в своём проекте, добавь пакет в `testables` в `Packages/manifest.json`:

```json
"testables": ["com.iacolyte.modern-csharp"]
```

## Лицензия

[MIT](LICENSE.md) © 2026 Alexander Gorozhankin
