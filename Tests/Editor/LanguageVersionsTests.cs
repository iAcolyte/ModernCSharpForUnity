using ModernCSharp.Editor;

using NUnit.Framework;

public sealed class LanguageVersionsTests {
    [Test]
    public void EveryEditorSupportsCSharp9And10() {
        Assert.That(LanguageVersions.IsSupported(LanguageVersion.CSharp9), Is.True);
        Assert.That(LanguageVersions.IsSupported(LanguageVersion.CSharp10), Is.True);
    }

    [Test]
    public void CSharp11And12NeedTheNewCompiler() {
#if UNITY_6000_5_OR_NEWER
        Assert.That(LanguageVersions.Max, Is.EqualTo(LanguageVersion.CSharp12));
#else
        Assert.That(LanguageVersions.Max, Is.EqualTo(LanguageVersion.CSharp10));
#endif
        Assert.That(LanguageVersions.IsSupported(LanguageVersion.CSharp11), Is.EqualTo(LanguageVersions.Max >= LanguageVersion.CSharp11));
        Assert.That(LanguageVersions.IsSupported(LanguageVersion.CSharp12), Is.EqualTo(LanguageVersions.Max >= LanguageVersion.CSharp12));
    }

    [Test]
    public void ClampKeepsSupportedVersions() {
        for (var version = LanguageVersions.Min; version <= LanguageVersions.Max; version++) {
            Assert.That(LanguageVersions.Clamp(version), Is.EqualTo(version));
        }
    }

    [Test]
    public void ClampCutsToTheSupportedRange() {
        Assert.That(LanguageVersions.Clamp(LanguageVersions.Max + 1), Is.EqualTo(LanguageVersions.Max));
        Assert.That(LanguageVersions.Clamp(LanguageVersions.Min - 1), Is.EqualTo(LanguageVersions.Min));
    }
}
