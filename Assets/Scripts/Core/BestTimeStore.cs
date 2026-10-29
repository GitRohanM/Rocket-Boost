using System;

namespace RocketBoost.Core
{
    /// <summary>
    /// Stores and validates per-level best times.
    /// <para>
    /// The default key prefix matches the prefix used before this class existed, so
    /// player records saved by older builds keep working.
    /// </para>
    /// </summary>
    public sealed class BestTimeStore
    {
        /// <summary>Persisted key prefix: <c>RocketBoost_BestTime_Level_</c> + level id.</summary>
        public const string DefaultKeyPrefix = "RocketBoost_BestTime_Level_";

        private readonly IKeyValueStore _store;

        public BestTimeStore(IKeyValueStore store, string keyPrefix = DefaultKeyPrefix)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            KeyPrefix = keyPrefix ?? throw new ArgumentNullException(nameof(keyPrefix));
        }

        /// <summary>Prefix applied to every level id when building a storage key.</summary>
        public string KeyPrefix { get; }

        /// <summary>Builds the storage key for a level id.</summary>
        public string GetKey(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                throw new ArgumentException("Level id must not be null or empty.", nameof(levelId));
            }

            return KeyPrefix + levelId;
        }

        /// <summary>
        /// Reads the best time for a level.
        /// Returns <c>false</c> when no record exists or the stored value is unusable.
        /// </summary>
        public bool TryGetBestTime(string levelId, out float bestTime)
        {
            if (_store.TryGetFloat(GetKey(levelId), out float stored) && IsUsableTime(stored))
            {
                bestTime = stored;
                return true;
            }

            bestTime = 0f;
            return false;
        }

        /// <summary>Reads the best time, falling back to <paramref name="fallback"/> when there is no record.</summary>
        public float GetBestTimeOr(string levelId, float fallback) =>
            TryGetBestTime(levelId, out float bestTime) ? bestTime : fallback;

        /// <summary>
        /// Submits a finished run. Writes to the store and persists only when it beats the
        /// existing record (or when no record exists yet).
        /// </summary>
        /// <param name="levelId">Level identifier, e.g. the build index as a string.</param>
        /// <param name="time">Finished run time in seconds.</param>
        /// <param name="bestTime">Resulting best time, whether or not it changed.</param>
        /// <returns><c>true</c> when this run set a new record.</returns>
        public bool Submit(string levelId, float time, out float bestTime)
        {
            string key = GetKey(levelId);

            if (!IsUsableTime(time))
            {
                bestTime = GetBestTimeOr(levelId, 0f);
                return false;
            }

            if (_store.TryGetFloat(key, out float existing) && IsUsableTime(existing) && time >= existing)
            {
                bestTime = existing;
                return false;
            }

            bestTime = time;
            _store.SetFloat(key, time);
            _store.Save();
            return true;
        }

        /// <summary>True when a value is a finite, non-negative number of seconds.</summary>
        private static bool IsUsableTime(float seconds) =>
            !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds >= 0f;
    }
}
