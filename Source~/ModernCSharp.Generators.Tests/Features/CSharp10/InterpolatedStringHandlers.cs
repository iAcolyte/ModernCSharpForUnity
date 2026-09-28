// Feature: Custom interpolated string handlers
// Status: Polyfill

using System.Runtime.CompilerServices;
using System.Text;

[InterpolatedStringHandler]
public ref struct LogHandler {
    readonly StringBuilder builder;

    public LogHandler(int literalLength, int formattedCount, Logger logger, out bool enabled) {
        enabled = logger.Enabled;
        builder = new StringBuilder(literalLength);
    }

    public void AppendLiteral(string value) => builder.Append(value);

    public void AppendFormatted<T>(T value) => builder.Append(value);

    public override string ToString() => builder.ToString();
}

public sealed class Logger {
    public bool Enabled;

    public string Log([InterpolatedStringHandlerArgument("")] LogHandler handler) => handler.ToString();
}

public static class Usage {
    public static string Run(Logger logger, int hp) => logger.Log($"HP: {hp}");
}
