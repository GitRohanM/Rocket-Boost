namespace RocketBoost.Core
{
    /// <summary>
    /// Mission grade awarded at the end of a level.
    /// Values ascend with quality so comparisons (<c>rank &gt;= MissionRank.A</c>) read naturally.
    /// </summary>
    public enum MissionRank
    {
        /// <summary>Completed, but with many crashes.</summary>
        C = 1,

        /// <summary>Completed within a handful of attempts.</summary>
        B = 2,

        /// <summary>Completed on the first attempt.</summary>
        A = 3,

        /// <summary>Completed on the first attempt, under the flawless time.</summary>
        S = 4
    }
}
