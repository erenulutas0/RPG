using Cryptforge.Localisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Every EditMode test reads the English it was written against, even right after a play session in Turkish: English
    // is pinned over whatever the session left for the run. A test that wants Turkish sets Loc.Override itself and puts
    // back what it found (LocalisationTests, LocalisationAssetTests).
    [SetUpFixture]
    public sealed class EditModeLanguagePin
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
