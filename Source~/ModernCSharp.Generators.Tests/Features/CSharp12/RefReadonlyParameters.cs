// Feature: ref readonly parameters
// Status: Works

public struct Matrix { public float M00; }

public static class Math {
    public static float Trace(ref readonly Matrix matrix) => matrix.M00;

    public static float Run() {
        var matrix = new Matrix();
        return Trace(in matrix);
    }
}
