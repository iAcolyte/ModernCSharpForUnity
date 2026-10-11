// Feature: The field keyword
// Status: Works

public sealed class Profile {
    public string Name {
        get;
        set => field = value.Trim();
    } = "";

    public int Level {
        get;
        set => field = value < 1 ? 1 : value;
    } = 1;
}
