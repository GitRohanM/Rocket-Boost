using RocketBoost.Core;
using UnityEngine;

namespace RocketBoost
{
    /// <summary>
    /// <see cref="IKeyValueStore"/> backed by <see cref="PlayerPrefs"/>.
    /// <para>
    /// This is the only place in the project that touches PlayerPrefs, so swapping in a
    /// file or cloud save later means writing one new adapter class.
    /// </para>
    /// </summary>
    public sealed class PlayerPrefsKeyValueStore : IKeyValueStore
    {
        /// <inheritdoc />
        public bool TryGetFloat(string key, out float value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = 0f;
                return false;
            }

            value = PlayerPrefs.GetFloat(key);
            return true;
        }

        /// <inheritdoc />
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        /// <inheritdoc />
        public void Save() => PlayerPrefs.Save();
    }
}
