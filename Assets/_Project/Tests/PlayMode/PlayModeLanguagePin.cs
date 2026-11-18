using Cryptforge.Localisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Every PlayMode test reads the English it was written against, whatever language a developer last chose in the
    // Editor's play mode (the choice lives in PlayerPrefs and is left untouched): English is pinned over it for the run.
    // A test that wants Turkish sets Loc.Override itself and puts back what it found (TurkishScreensTests).
    [SetUpFixture]
    public sealed class PlayModeLanguagePin
    {
        private Language? _before;

        [OneTimeSetUp]
        public void PinEnglish()
        {
            _before = Loc.Override;
            Loc.Override = Language.English;
        }

        [OneTimeTearDown]
        public void Unpin() => Loc.Override = _before;
    }
}
