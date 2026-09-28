// Feature: Interceptors
// Status: Error
// Error: CS0246

using System.Runtime.CompilerServices;

public static class Logger {
    public static void Log(string message) { }

    public static void Run() => Log("boot");
}

public static class Interceptor {
    [InterceptsLocation("Snippet.cs", line: 6, character: 33)]
    public static void Log_Intercepted(string message) { }
}
