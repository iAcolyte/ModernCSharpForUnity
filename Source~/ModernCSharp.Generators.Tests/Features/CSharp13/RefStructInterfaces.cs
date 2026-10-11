// Feature: ref struct interfaces
// Status: Caveats

using System;

public interface IReader {
    int Read();
}

public ref struct SpanReader : IReader {
    ReadOnlySpan<int> data;

    public SpanReader(ReadOnlySpan<int> data) {
        this.data = data;
    }

    public int Read() {
        var value = data[0];
        data = data.Slice(1);
        return value;
    }
}
