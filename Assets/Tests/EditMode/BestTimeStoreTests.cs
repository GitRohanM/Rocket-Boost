using System;
using System.Collections.Generic;
using NUnit.Framework;
using RocketBoost.Core;

namespace RocketBoost.Tests
{
    [TestFixture]
    public sealed class BestTimeStoreTests
    {
        private FakeKeyValueStore _store;
        private BestTimeStore _bestTimes;

        [SetUp]
        public void SetUp()
        {
            _store = new FakeKeyValueStore();
            _bestTimes = new BestTimeStore(_store);
        }

        [Test]
        public void Submit_FirstRun_SetsRecordAndPersists()
        {
            bool isRecord = _bestTimes.Submit("1", 32.5f, out float bestTime);

            Assert.IsTrue(isRecord);
            Assert.AreEqual(32.5f, bestTime);
            Assert.AreEqual(32.5f, _store.GetRaw("RocketBoost_BestTime_Level_1"));
            Assert.AreEqual(1, _store.SaveCount, "A new record must be flushed to durable storage.");
        }

        [Test]
        public void Submit_SlowerRun_KeepsExistingRecord()
        {
            _bestTimes.Submit("1", 30f, out _);

            bool isRecord = _bestTimes.Submit("1", 45f, out float bestTime);

            Assert.IsFalse(isRecord);
            Assert.AreEqual(30f, bestTime);
            Assert.AreEqual(30f, _store.GetRaw("RocketBoost_BestTime_Level_1"));
        }

        [Test]
        public void Submit_FasterRun_ReplacesRecord()
        {
            _bestTimes.Submit("1", 30f, out _);

            bool isRecord = _bestTimes.Submit("1", 20f, out float bestTime);

            Assert.IsTrue(isRecord);
            Assert.AreEqual(20f, bestTime);
            Assert.AreEqual(20f, _store.GetRaw("RocketBoost_BestTime_Level_1"));
            Assert.AreEqual(2, _store.SaveCount);
        }

        [Test]
        public void Submit_EqualTime_IsNotANewRecord()
        {
            _bestTimes.Submit("1", 25f, out _);

            bool isRecord = _bestTimes.Submit("1", 25f, out float bestTime);

            Assert.IsFalse(isRecord);
            Assert.AreEqual(25f, bestTime);
            Assert.AreEqual(1, _store.SaveCount);
        }

        [Test]
        public void Submit_KeepsRecordsPerLevel()
        {
            _bestTimes.Submit("1", 30f, out _);
            _bestTimes.Submit("2", 60f, out _);

            Assert.IsTrue(_bestTimes.TryGetBestTime("1", out float first));
            Assert.IsTrue(_bestTimes.TryGetBestTime("2", out float second));
            Assert.AreEqual(30f, first);
            Assert.AreEqual(60f, second);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-5f)]
        public void Submit_IgnoresUnusableTimes(float time)
        {
            bool isRecord = _bestTimes.Submit("1", time, out float bestTime);

            Assert.IsFalse(isRecord);
            Assert.AreEqual(0f, bestTime);
            Assert.AreEqual(0, _store.SaveCount);
            Assert.IsFalse(_store.ContainsKey("RocketBoost_BestTime_Level_1"));
        }

        [Test]
        public void TryGetBestTime_ReturnsFalseWhenNoRecordExists()
        {
            Assert.IsFalse(_bestTimes.TryGetBestTime("3", out float bestTime));
            Assert.AreEqual(0f, bestTime);
        }

        [Test]
        public void TryGetBestTime_TreatsCorruptedValueAsNoRecord()
        {
            // A negative value can never be a legitimate flight time.
            _store.SetRaw("RocketBoost_BestTime_Level_4", -12f);

            Assert.IsFalse(_bestTimes.TryGetBestTime("4", out _));
        }

        [Test]
        public void GetBestTimeOr_UsesFallbackWhenNoRecordExists()
        {
            Assert.AreEqual(99f, _bestTimes.GetBestTimeOr("7", 99f));
        }

        [Test]
        public void DefaultKeyPrefix_MatchesLegacyPlayerPrefsKey()
        {
            // Changing this string silently wipes every player's saved records.
            Assert.AreEqual("RocketBoost_BestTime_Level_", BestTimeStore.DefaultKeyPrefix);
            Assert.AreEqual("RocketBoost_BestTime_Level_2", _bestTimes.GetKey("2"));
        }

        [Test]
        public void Constructor_AcceptsCustomKeyPrefix()
        {
            var custom = new BestTimeStore(_store, "Test_");

            custom.Submit("5", 10f, out _);

            Assert.AreEqual(10f, _store.GetRaw("Test_5"));
        }

        [Test]
        public void Constructor_RejectsNullStore()
        {
            Assert.Throws<ArgumentNullException>(() => new BestTimeStore(null));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetKey_RejectsMissingLevelId(string levelId)
        {
            Assert.Throws<ArgumentException>(() => _bestTimes.GetKey(levelId));
        }

        /// <summary>In-memory <see cref="IKeyValueStore"/> that records writes.</summary>
        private sealed class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

            public int SaveCount { get; private set; }

            public bool TryGetFloat(string key, out float value) => _values.TryGetValue(key, out value);

            public void SetFloat(string key, float value) => _values[key] = value;

            public void Save() => SaveCount++;

            public bool ContainsKey(string key) => _values.ContainsKey(key);

            public float GetRaw(string key) => _values[key];

            public void SetRaw(string key, float value) => _values[key] = value;
        }
    }
}
