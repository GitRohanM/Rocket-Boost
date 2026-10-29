using System;
using NUnit.Framework;
using RocketBoost.Core;

namespace RocketBoost.Tests
{
    [TestFixture]
    public sealed class MissionRankCalculatorTests
    {
        [Test]
        public void FirstAttemptUnderFlawlessTime_EarnsRankS()
        {
            Assert.AreEqual(MissionRank.S, MissionRankCalculator.Evaluate(1, 12.5d));
        }

        [Test]
        public void FirstAttemptExactlyAtFlawlessTime_EarnsRankA()
        {
            // The threshold is exclusive: 20.00s is fast but not "perfect".
            Assert.AreEqual(
                MissionRank.A,
                MissionRankCalculator.Evaluate(1, MissionRankCalculator.FlawlessTimeSeconds));
        }

        [Test]
        public void FirstAttemptOverFlawlessTime_EarnsRankA()
        {
            Assert.AreEqual(MissionRank.A, MissionRankCalculator.Evaluate(1, 45d));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void FewAttempts_EarnRankB(int attempts)
        {
            Assert.AreEqual(MissionRank.B, MissionRankCalculator.Evaluate(attempts, 10d));
        }

        [TestCase(4)]
        [TestCase(25)]
        public void ManyAttempts_EarnRankC(int attempts)
        {
            Assert.AreEqual(MissionRank.C, MissionRankCalculator.Evaluate(attempts, 10d));
        }

        [Test]
        public void FastTimeCannotUpgradeRankWhenAttemptsExceedOne()
        {
            Assert.AreEqual(MissionRank.B, MissionRankCalculator.Evaluate(2, 5d));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void NonPositiveAttempts_Throw(int attempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MissionRankCalculator.Evaluate(attempts, 10d));
        }

        [Test]
        public void GetLabel_CombinesRankAndTitle()
        {
            Assert.AreEqual("RANK S // PERFECT INSERTION", MissionRankCalculator.GetLabel(MissionRank.S));
            Assert.AreEqual("RANK A // MISSION ACCOMPLISHED", MissionRankCalculator.GetLabel(MissionRank.A));
            Assert.AreEqual("RANK B // FLIGHT QUALIFIED", MissionRankCalculator.GetLabel(MissionRank.B));
            Assert.AreEqual("RANK C // CRAFT RECOVERED", MissionRankCalculator.GetLabel(MissionRank.C));
        }

        [Test]
        public void GetTitle_UnknownRankThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MissionRankCalculator.GetTitle((MissionRank)99));
        }

        [Test]
        public void RanksAreOrderedFromWorstToBest()
        {
            Assert.Less((int)MissionRank.C, (int)MissionRank.B);
            Assert.Less((int)MissionRank.B, (int)MissionRank.A);
            Assert.Less((int)MissionRank.A, (int)MissionRank.S);
        }
    }
}
