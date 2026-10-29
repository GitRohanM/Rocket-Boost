using System;

namespace RocketBoost.Core
{
    /// <summary>
    /// Turns a level outcome (attempt count and flight time) into a <see cref="MissionRank"/>.
    /// Pure and side-effect free; the UI layer is responsible for colors and animation.
    /// </summary>
    public static class MissionRankCalculator
    {
        /// <summary>Flight time (seconds) required for rank S when the level is cleared first try.</summary>
        public const double FlawlessTimeSeconds = 20.0;

        /// <summary>Highest attempt count that still earns rank B.</summary>
        public const int VeteranAttemptLimit = 3;

        /// <summary>
        /// Evaluates the rank for a completed level.
        /// </summary>
        /// <param name="attempts">Number of attempts used, including the successful one. Must be at least 1.</param>
        /// <param name="elapsedSeconds">Final flight time in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="attempts"/> is less than 1.</exception>
        public static MissionRank Evaluate(int attempts, double elapsedSeconds)
        {
            if (attempts < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attempts),
                    attempts,
                    "A completed level must have at least one attempt.");
            }

            if (attempts == 1)
            {
                return elapsedSeconds < FlawlessTimeSeconds ? MissionRank.S : MissionRank.A;
            }

            return attempts <= VeteranAttemptLimit ? MissionRank.B : MissionRank.C;
        }

        /// <summary>Short flavour text for a rank.</summary>
        public static string GetTitle(MissionRank rank)
        {
            switch (rank)
            {
                case MissionRank.S: return "PERFECT INSERTION";
                case MissionRank.A: return "MISSION ACCOMPLISHED";
                case MissionRank.B: return "FLIGHT QUALIFIED";
                case MissionRank.C: return "CRAFT RECOVERED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown mission rank.");
            }
        }

        /// <summary>Full label shown on the debrief screen, e.g. <c>RANK S // PERFECT INSERTION</c>.</summary>
        public static string GetLabel(MissionRank rank) => $"RANK {rank} // {GetTitle(rank)}";
    }
}
