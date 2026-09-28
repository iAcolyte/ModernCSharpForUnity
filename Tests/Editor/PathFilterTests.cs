using ModernCSharp.Editor;

using NUnit.Framework;


public sealed class PathFilterTests {
    const string Asmdef = "Assets/Game/Combat/Game.Combat.asmdef";

    [TestCase(new string[0], false, TestName = "Empty list")]
    [TestCase(new[] { "Assets/Game/Combat" }, true, TestName = "Same folder")]
    [TestCase(new[] { "Assets/Game" }, true, TestName = "Parent folder")]
    [TestCase(new[] { "Assets" }, true, TestName = "Assets matches everything")]
    [TestCase(new[] { "Assets/Game/Com" }, false, TestName = "Folder name prefix is not a parent")]
    [TestCase(new[] { "Assets/Game/Combat/AI" }, false, TestName = "Child folder")]
    [TestCase(new[] { "assets/GAME" }, true, TestName = "Case insensitive")]
    [TestCase(new[] { @"Assets\Game\" }, true, TestName = "Backslashes and trailing slash")]
    [TestCase(new[] { " Assets/Game/ " }, true, TestName = "Surrounding whitespace")]
    [TestCase(new[] { "", "   " }, false, TestName = "Blank entries are ignored")]
    [TestCase(new[] { "Assets/Plugins", "Assets/Game" }, true, TestName = "Any entry matches")]
    public void MatchesFolder(string[] paths, bool matched) {
        Assert.That(PathFilter.IsIncluded(Asmdef, PathFilterMode.Whitelist, paths), Is.EqualTo(matched), "whitelist");
        Assert.That(PathFilter.IsIncluded(Asmdef, PathFilterMode.Blacklist, paths), Is.EqualTo(!matched), "blacklist");
    }

    [Test]
    public void FolderPrefixDoesNotMatchSibling() {
        Assert.That(PathFilter.IsIncluded("Assets/FooBar/X.asmdef", PathFilterMode.Whitelist, new[] { "Assets/Foo" }), Is.False);
    }

    [Test]
    public void AsmdefInAssetsRoot() {
        Assert.That(PathFilter.IsIncluded("Assets/Root.asmdef", PathFilterMode.Whitelist, new[] { "Assets" }), Is.True);
        Assert.That(PathFilter.IsIncluded("Assets/Root.asmdef", PathFilterMode.Whitelist, new[] { "Assets/Game" }), Is.False);
    }

    [TestCase(@"Assets\Game\", "Assets/Game")]
    [TestCase(" Assets/Game/ ", "Assets/Game")]
    [TestCase("Assets", "Assets")]
    public void Normalize(string path, string expected) {
        Assert.That(PathFilter.Normalize(path), Is.EqualTo(expected));
    }
}
