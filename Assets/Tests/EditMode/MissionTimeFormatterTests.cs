using System.Globalization;
using NUnit.Framework;
using RocketBoost.Core;

namespace RocketBoost.Tests
{
    [TestFixture]
    public sealed class MissionTimeFormatterTests
    {
        [TestCase(0d, "00:00.00")]
        [TestCase(1.5d, "00:01.50")]
        [TestCase(9.99d, "00:09.99")]
        [TestCase(10d, "00:10.00")]
        [TestCase(59.99d, "00:59.99")]
        [TestCase(60d, "01:00.00")]
        [TestCase(125.25d, "02:05.25")]
        [TestCase(3599.99d, "59:59.99")]
        [TestCase(3600d, "60:00.00")]
        public void Format_RendersMinutesSecondsAndHundredths(double seconds, string expected)
        {
            Assert.AreEqual(expected, MissionTimeFormatter.Format(seconds));
        }

        [Test]
        public void Format_TruncatesInsteadOfRoundingUp()
        {
            // 59.999 is still inside the first minute; a rounding formatter would print 01:00.00.
            Assert.AreEqual("00:59.99", MissionTimeFormatter.Format(59.999d));
        }

        [Test]
        public void Format_KeepsHundredthWhenFloatLosesPrecision()
        {
            // 1.15f is stored as 1.14999997..., so naive flooring would print 00:01.14.
            Assert.AreEqual("00:01.15", MissionTimeFormatter.Format(1.15f));
        }

        [Test]
        public void Format_ClampsExtremelyLongDurations()
        {
            Assert.AreEqual("99:59.99", MissionTimeFormatter.Format(1_000_000d));
        }

        [TestCase(-1d)]
        [TestCase(-0.001d)]
        public void Format_TreatsNegativeDurationsAsZero(double seconds)
        {
            Assert.AreEqual("00:00.00", MissionTimeFormatter.Format(seconds));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Format_TreatsNonFiniteDurationsAsZero(double seconds)
        {
            Assert.AreEqual("00:00.00", MissionTimeFormatter.Format(seconds));
        }

        [Test]
        public void ToCentiseconds_RoundsDownToWholeHundredths()
        {
            Assert.AreEqual(150, MissionTimeFormatter.ToCentiseconds(1.5d));
            Assert.AreEqual(115, MissionTimeFormatter.ToCentiseconds(1.15f));
            Assert.AreEqual(0, MissionTimeFormatter.ToCentiseconds(0.009d));
        }

        [Test]
        public void Format_UsesInvariantCultureForDecimalSeparator()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                // A comma decimal separator would break parsing and layout on the debrief screen.
                Assert.AreEqual("00:01.50", MissionTimeFormatter.Format(1.5d));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
