using NUnit.Framework;
using RocketBoost.Core;

namespace RocketBoost.Tests
{
    [TestFixture]
    public sealed class LevelProgressionTests
    {
        [Test]
        public void TryGetNextIndex_AdvancesThroughLevels()
        {
            Assert.IsTrue(LevelProgression.TryGetNextIndex(1, 4, out int next));
            Assert.AreEqual(2, next);
        }

        [Test]
        public void TryGetNextIndex_WrapsFromLastLevelToMenu()
        {
            Assert.IsTrue(LevelProgression.TryGetNextIndex(3, 4, out int next));
            Assert.AreEqual(0, next);
        }

        [Test]
        public void TryGetNextIndex_HandlesInvalidCurrentIndexAsMenu()
        {
            // Unity reports -1 for a scene that is not in the build settings.
            Assert.IsTrue(LevelProgression.TryGetNextIndex(-1, 4, out int next));
            Assert.AreEqual(0, next);
        }

        [Test]
        public void TryGetNextIndex_FailsWhenBuildSettingsAreEmpty()
        {
            Assert.IsFalse(LevelProgression.TryGetNextIndex(0, 0, out int next));
            Assert.AreEqual(-1, next);
        }

        [TestCase(0, 4, false)]
        [TestCase(1, 4, false)]
        [TestCase(2, 4, false)]
        [TestCase(3, 4, true)]
        public void IsLastLevel_OnlyTrueForFinalIndex(int index, int count, bool expected)
        {
            Assert.AreEqual(expected, LevelProgression.IsLastLevel(index, count));
        }

        [Test]
        public void IsLastLevel_TreatsEmptyBuildSettingsAsLast()
        {
            Assert.IsTrue(LevelProgression.IsLastLevel(0, 0));
        }

        [TestCase(0, 4, true)]
        [TestCase(3, 4, true)]
        [TestCase(4, 4, false)]
        [TestCase(-1, 4, false)]
        public void IsValidIndex_MatchesBuildSettingsRange(int index, int count, bool expected)
        {
            Assert.AreEqual(expected, LevelProgression.IsValidIndex(index, count));
        }

        [Test]
        public void IsValidIndex_FalseWhenNoScenesAreConfigured()
        {
            Assert.IsFalse(LevelProgression.IsValidIndex(0, 0));
        }
    }
}
