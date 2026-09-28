// Feature: [AsyncMethodBuilder] on methods
// Status: Error
// Error: CS0592

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class Loader {
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<int> Load() {
        await Task.Yield();
        return 1;
    }
}
