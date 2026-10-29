using System;
using System.Globalization;

namespace RocketBoost.Core
{
    /// <summary>
    /// Formats mission durations for the HUD and the debrief screen.
    /// <para>
    /// Pure, deterministic, and free of UnityEngine types so it can be unit tested
    /// without entering play mode. Formatting is culture-invariant on purpose:
    /// interpolation with the current culture would render "00:01,50" on a machine
    /// using a comma decimal separator.
    /// </para>
    /// </summary>
    public static class MissionTimeFormatter
    {
        /// <summary>Largest value the formatter will display (99:59.99).</summary>
        public const double MaxDisplaySeconds = 5999.99;

        /// <summary>
        /// Tolerance added before flooring, so values like 1.15f (stored as 1.1499999)
        /// still render as "01.15" instead of dropping a hundredth.
        /// </summary>
        private const double RoundingTolerance = 1e-4;

        /// <summary>Formats a duration as <c>mm:ss.ff</c>, truncating to hundredths.</summary>
        public static string Format(double seconds)
        {
            int centiseconds = ToCentiseconds(seconds);
            int minutes = centiseconds / 6000;
            int remainingSeconds = centiseconds / 100 % 60;
            int hundredths = centiseconds % 100;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}.{2:00}",
                minutes,
                remainingSeconds,
                hundredths);
        }

        /// <summary>Formats a duration as <c>mm:ss.ff</c>, truncating to hundredths.</summary>
        public static string Format(float seconds) => Format((double)seconds);

        /// <summary>
        /// Converts a duration to whole hundredths of a second, clamped to
        /// <see cref="MaxDisplaySeconds"/> and treating NaN, infinity, and negative
        /// values as zero.
        /// </summary>
        public static int ToCentiseconds(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d)
            {
                return 0;
            }

            double clamped = Math.Min(seconds, MaxDisplaySeconds);
            return (int)Math.Floor(clamped * 100d + RoundingTolerance);
        }
    }
}
