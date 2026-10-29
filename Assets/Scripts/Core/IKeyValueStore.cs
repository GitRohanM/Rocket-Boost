namespace RocketBoost.Core
{
    /// <summary>
    /// Minimal key/value persistence seam.
    /// Keeps <see cref="BestTimeStore"/> free of UnityEngine.PlayerPrefs so it can be
    /// unit tested, and lets the game swap PlayerPrefs for a cloud save later without
    /// touching game logic.
    /// </summary>
    public interface IKeyValueStore
    {
        /// <summary>Reads a float. Returns <c>false</c> when the key has never been written.</summary>
        bool TryGetFloat(string key, out float value);

        /// <summary>Writes a float.</summary>
        void SetFloat(string key, float value);

        /// <summary>Flushes pending writes to durable storage.</summary>
        void Save();
    }
}
