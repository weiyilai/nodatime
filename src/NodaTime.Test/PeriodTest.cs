// Copyright 2010 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Reflection;
using NodaTime.Calendars;
using NodaTime.Text;
using NUnit.Framework;

namespace NodaTime.Test
{
    public class PeriodTest
    {
        // June 19th 2010, 2:30:15am
        private static readonly LocalDateTime TestDateTime1 = new LocalDateTime(2010, 6, 19, 2, 30, 15);
        // June 19th 2010, 4:45:10am
        private static readonly LocalDateTime TestDateTime2 = new LocalDateTime(2010, 6, 19, 4, 45, 10);
        // June 19th 2010
        private static readonly LocalDate TestDate1 = new LocalDate(2010, 6, 19);
        // March 1st 2011
        private static readonly LocalDate TestDate2 = new LocalDate(2011, 3, 1);
        // March 1st 2012
        private static readonly LocalDate TestDate3 = new LocalDate(2012, 3, 1);

        private const PeriodUnits HoursMinutesPeriodType = PeriodUnits.Hours | PeriodUnits.Minutes;

        private static readonly PeriodUnits[] AllPeriodUnits = (PeriodUnits[])Enum.GetValues(typeof(PeriodUnits));

        [Test]
        public void BetweenLocalDateTimes_WithoutSpecifyingUnits_OmitsWeeks()
        {
            Period actual = Period.Between(new LocalDateTime(2012, 2, 21, 0, 0), new LocalDateTime(2012, 2, 28, 0, 0));
            Period expected = Period.FromDays(7);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_MovingForwardWithAllFields_GivesExactResult()
        {
            Period actual = Period.Between(TestDateTime1, TestDateTime2);
            Period expected = Period.FromHours(2) + Period.FromMinutes(14) + Period.FromSeconds(55);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_MovingBackwardWithAllFields_GivesExactResult()
        {
            Period actual = Period.Between(TestDateTime2, TestDateTime1);
            Period expected = Period.FromHours(-2) + Period.FromMinutes(-14) + Period.FromSeconds(-55);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_MovingForwardWithHoursAndMinutes_RoundsTowardsStart()
        {
            Period actual = Period.Between(TestDateTime1, TestDateTime2, HoursMinutesPeriodType);
            Period expected = Period.FromHours(2) + Period.FromMinutes(14);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_MovingBackwardWithHoursAndMinutes_RoundsTowardsStart()
        {
            Period actual = Period.Between(TestDateTime2, TestDateTime1, HoursMinutesPeriodType);
            Period expected = Period.FromHours(-2) + Period.FromMinutes(-14);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_AcrossDays()
        {
            Period expected = Period.FromHours(23) + Period.FromMinutes(59);
            Period actual = Period.Between(TestDateTime1, TestDateTime1.PlusDays(1).PlusMinutes(-1));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_AcrossDays_MinutesAndSeconds()
        {
            Period expected = Period.FromMinutes(24 * 60 - 1) + Period.FromSeconds(59);
            Period actual = Period.Between(TestDateTime1, TestDateTime1.PlusDays(1).PlusSeconds(-1), PeriodUnits.Minutes | PeriodUnits.Seconds);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDateTimes_NotInt64Representable()
        {
            LocalDateTime start = new LocalDateTime(-5000, 1, 1, 0, 1, 2, 123);
            LocalDateTime end = new LocalDateTime(   9000, 1, 1, 1, 2, 3, 456);
            Assert.False((end.ToLocalInstant().TimeSinceLocalEpoch - start.ToLocalInstant().TimeSinceLocalEpoch).IsInt64Representable);

            Period expected = new PeriodBuilder
            {
                // 365.2425 * 14000 = 5113395
                Hours = 5113395L * 24 + 1,
                Minutes = 1,
                Seconds = 1,
                Milliseconds = 333
            }.Build();
            Period actual = Period.Between(start, end, PeriodUnits.AllTimeUnits);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_InvalidUnits()
        {
            Assert.Throws<ArgumentException>(() => Period.Between(TestDate1, TestDate2, 0));
            Assert.Throws<ArgumentException>(() => Period.Between(TestDate1, TestDate2, (PeriodUnits) (-1)));
            Assert.Throws<ArgumentException>(() => Period.Between(TestDate1, TestDate2, PeriodUnits.AllTimeUnits));
            Assert.Throws<ArgumentException>(() => Period.Between(TestDate1, TestDate2, PeriodUnits.Years | PeriodUnits.Hours));
        }

        [Test]
        public void BetweenLocalDates_DifferentCalendarSystems_Throws()
        {
            LocalDate start = new LocalDate(2017, 11, 1, CalendarSystem.Coptic);
            LocalDate end = new LocalDate(2017, 11, 5, CalendarSystem.Gregorian);
            Assert.Throws<ArgumentException>(() => Period.Between(start, end));
        }

        [Test]
        [TestCase("2016-05-16", "2019-03-13", PeriodUnits.Years, 2)]
        [TestCase("2016-05-16", "2017-07-13", PeriodUnits.Months, 13)]
        [TestCase("2016-05-16", "2016-07-13", PeriodUnits.Weeks, 8)]
        [TestCase("2016-05-16", "2016-07-13", PeriodUnits.Days, 58)]
        public void BetweenLocalDates_SingleUnit(string startText, string endText, PeriodUnits units, int expectedValue)
        {
            var start = LocalDatePattern.Iso.Parse(startText).Value;
            var end = LocalDatePattern.Iso.Parse(endText).Value;
            var actual = Period.Between(start, end, units);
            var expected = new PeriodBuilder { [units] = expectedValue }.Build();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DaysBetweenLocalDates_DifferentCalendarsThrows()
        {
            var start = new LocalDate(2020, 6, 13, CalendarSystem.Iso);
            var end = new LocalDate(2020, 6, 15, CalendarSystem.Julian);
            Assert.Throws<ArgumentException>(() => Period.DaysBetween(start, end));
        }

        [Test]
        public void DaysBetweenLocalDates_SameDatesReturnsZero()
        {
            var start = new LocalDate(2020, 6, 13, CalendarSystem.Iso);
            var end = start;
            var expected = 0;
            var actual = Period.DaysBetween(start, end);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase("2016-05-16", "2016-05-17", 1)]
        [TestCase("2020-06-15", "2020-06-18", 3)]
        [TestCase("2016-05-16", "2016-05-26", 10)]
        [TestCase("2020-06-15", "2021-06-19", 369)]
        [TestCase("2020-03-23", "2020-06-12", 81)]
        public void DaysBetweenLocalDates(string startText, string endText, int expected)
        {
            var start = LocalDatePattern.Iso.Parse(startText).Value;
            var end = LocalDatePattern.Iso.Parse(endText).Value;
            var actual = Period.DaysBetween(start, end);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase("2016-05-16", "2016-05-15", -1)]
        [TestCase("2020-06-15", "2020-06-12", -3)]
        [TestCase("2016-05-16", "2016-05-06", -10)]
        [TestCase("2021-06-19", "2020-06-15", -369)]
        [TestCase("2020-05-16", "2019-05-16", -366)]
        public void DaysBetweenLocalDates_StartDateGreaterThanEndDate(string startText, string endText, int expected)
        {
            var start = LocalDatePattern.Iso.Parse(startText).Value;
            var end = LocalDatePattern.Iso.Parse(endText).Value;
            var actual = Period.DaysBetween(start, end);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingForwardNoLeapYears_WithExactResults()
        {
            Period actual = Period.Between(TestDate1, TestDate2);
            Period expected = Period.FromMonths(8) + Period.FromDays(10);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingForwardInLeapYear_WithExactResults()
        {
            Period actual = Period.Between(TestDate1, TestDate3);
            Period expected = Period.FromYears(1) + Period.FromMonths(8) + Period.FromDays(11);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingBackwardNoLeapYears_WithExactResults()
        {
            Period actual = Period.Between(TestDate2, TestDate1);
            Period expected = Period.FromMonths(-8) + Period.FromDays(-12);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingBackwardInLeapYear_WithExactResults()
        {
            // This is asymmetric with moving forward, because we first take off a whole year, which
            // takes us to March 1st 2011, then 8 months to take us to July 1st 2010, then 12 days
            // to take us back to June 19th. In this case, the fact that our start date is in a leap
            // year had no effect.
            Period actual = Period.Between(TestDate3, TestDate1);
            Period expected = Period.FromYears(-1) + Period.FromMonths(-8) + Period.FromDays(-12);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingForward_WithJustMonths()
        {
            Period actual = Period.Between(TestDate1, TestDate3, PeriodUnits.Months);
            Period expected = Period.FromMonths(20);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_MovingBackward_WithJustMonths()
        {
            Period actual = Period.Between(TestDate3, TestDate1, PeriodUnits.Months);
            Period expected = Period.FromMonths(-20);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalDates_AsymmetricForwardAndBackward()
        {
            // February 10th 2010
            LocalDate d1 = new LocalDate(2010, 2, 10);
            // March 30th 2010
            LocalDate d2 = new LocalDate(2010, 3, 30);
            // Going forward, we go to March 10th (1 month) then March 30th (20 days)
            Assert.AreEqual(Period.FromMonths(1) + Period.FromDays(20), Period.Between(d1, d2));
            // Going backward, we go to February 28th (-1 month, day is rounded) then February 10th (-18 days)
            Assert.AreEqual(Period.FromMonths(-1) + Period.FromDays(-18), Period.Between(d2, d1));
        }

        [Test]
        public void BetweenLocalDates_EndOfMonth()
        {
            LocalDate d1 = new LocalDate(2013, 3, 31);
            LocalDate d2 = new LocalDate(2013, 4, 30);
            Assert.AreEqual(Period.FromMonths(1), Period.Between(d1, d2));
            Assert.AreEqual(Period.FromDays(-30), Period.Between(d2, d1));
        }

        [Test]
        public void BetweenLocalDates_OnLeapYear()
        {
            LocalDate d1 = new LocalDate(2012, 2, 29);
            LocalDate d2 = new LocalDate(2013, 2, 28);
            Assert.AreEqual(Period.FromYears(1), Period.Between(d1, d2));
            // Go back from February 28th 2013 to March 28th 2012, then back 28 days to February 29th 2012
            Assert.AreEqual(Period.FromMonths(-11) + Period.FromDays(-28), Period.Between(d2, d1));
        }

        [Test]
        public void BetweenLocalDates_AfterLeapYear()
        {
            LocalDate d1 = new LocalDate(2012, 3, 5);
            LocalDate d2 = new LocalDate(2013, 3, 5);
            Assert.AreEqual(Period.FromYears(1), Period.Between(d1, d2));
            Assert.AreEqual(Period.FromYears(-1), Period.Between(d2, d1));
        }

        [Test]
        public void BetweenLocalDateTimes_OnLeapYear()
        {
            LocalDateTime dt1 = new LocalDateTime(2012, 2, 29, 2, 0);
            LocalDateTime dt2 = new LocalDateTime(2012, 2, 29, 4, 0);
            LocalDateTime dt3 = new LocalDateTime(2013, 2, 28, 3, 0);
            Assert.AreEqual(Parse("P1YT1H"), Period.Between(dt1, dt3));
            Assert.AreEqual(Parse("P11M29DT23H"), Period.Between(dt2, dt3));

            Assert.AreEqual(Parse("P-11M-28DT-1H"), Period.Between(dt3, dt1));
            Assert.AreEqual(Parse("P-11M-27DT-23H"), Period.Between(dt3, dt2));
        }

        [Test]
        public void BetweenLocalDateTimes_OnLeapYearIslamic()
        {
            var calendar = CalendarSystem.GetIslamicCalendar(IslamicLeapYearPattern.Base15, IslamicEpoch.Civil);
            Assert.IsTrue(calendar.IsLeapYear(2));
            Assert.IsFalse(calendar.IsLeapYear(3));

            LocalDateTime dt1 = new LocalDateTime(2, 12, 30, 2, 0, calendar);
            LocalDateTime dt2 = new LocalDateTime(2, 12, 30, 4, 0, calendar);
            LocalDateTime dt3 = new LocalDateTime(3, 12, 29, 3, 0, calendar);

            // Adding a year truncates to 0003-12-28T02:00:00, then add an hour.
            Assert.AreEqual(Parse("P1YT1H"), Period.Between(dt1, dt3));
            // Adding a year would overshoot. Adding 11 months takes us to month 03-11-30T04:00.
            // Adding another 28 days takes us to 03-12-28T04:00, then add another 23 hours to finish.
            Assert.AreEqual(Parse("P11M28DT23H"), Period.Between(dt2, dt3));

            // Subtracting 11 months takes us to 03-01-29T03:00. Subtracting another 29 days
            // takes us to 02-12-30T03:00, and another hour to get to the target.
            Assert.AreEqual(Parse("P-11M-29DT-1H"), Period.Between(dt3, dt1));
            Assert.AreEqual(Parse("P-11M-28DT-23H"), Period.Between(dt3, dt2));
        }

        [Test]
        public void BetweenLocalDateTimes_InvalidUnits()
        {
            Assert.Throws<ArgumentException>(() => Period.Between(TestDateTime1, TestDateTime2, 0));
            Assert.Throws<ArgumentException>(() => Period.Between(TestDateTime1, TestDateTime2, (PeriodUnits)(-1)));
        }

        private static TestCaseData[] NanosecondsBetweenLocalTimesTestCaseData =>
        [
            new TestCaseData(LocalTime.MinValue, LocalTime.MaxValue, LocalTime.MaxValue.NanosecondOfDay)
                .SetName($"Nanoseconds between {nameof(LocalTime.MinValue)} and {nameof(LocalTime.MaxValue)} should be maximum {nameof(LocalTime.MaxValue.NanosecondOfDay)}"),
            new TestCaseData(LocalTime.MaxValue, LocalTime.MinValue, -LocalTime.MaxValue.NanosecondOfDay)
                .SetName($"Nanoseconds between {nameof(LocalTime.MaxValue)} and {nameof(LocalTime.MinValue)} should be negative maximum {nameof(LocalTime.MaxValue.NanosecondOfDay)}"),
            new TestCaseData(LocalTime.MinValue, LocalTime.MinValue.PlusNanoseconds(1), 1L)
                .SetName($"Nanoseconds between {nameof(LocalTime.MinValue)} and {nameof(LocalTime.MinValue)}_Plus_One should be 1"),
            new TestCaseData(LocalTime.MinValue.PlusNanoseconds(1), LocalTime.MinValue, -1L).
                SetName($"Nanoseconds between {nameof(LocalTime.MinValue)}_Plus_One and {nameof(LocalTime.MinValue)} should be negative 1"),
        ];

        [TestCaseSource(nameof(NanosecondsBetweenLocalTimesTestCaseData))]
        public void NanosecondsBetweenLocalTimes(LocalTime start, LocalTime end, long expected)
        {
            var actual = Period.NanosecondsBetween(start, end);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalTimes_InvalidUnits()
        {
            LocalTime t1 = new LocalTime(10, 0);
            LocalTime t2 = LocalTime.FromHourMinuteSecondMillisecondTick(15, 30, 45, 20, 5);
            Assert.Throws<ArgumentException>(() => Period.Between(t1, t2, 0));
            Assert.Throws<ArgumentException>(() => Period.Between(t1, t2, (PeriodUnits)(-1)));
            Assert.Throws<ArgumentException>(() => Period.Between(t1, t2, PeriodUnits.YearMonthDay));
            Assert.Throws<ArgumentException>(() => Period.Between(t1, t2, PeriodUnits.Years | PeriodUnits.Hours));
        }

        [Test]
        [TestCase("01:02:03", "05:00:00", PeriodUnits.Hours, 3)]
        [TestCase("01:02:03", "03:00:00", PeriodUnits.Minutes, 117)]
        [TestCase("01:02:03", "01:05:02", PeriodUnits.Seconds, 179)]
        [TestCase("01:02:03", "01:02:04.1234", PeriodUnits.Milliseconds, 1123)]
        [TestCase("01:02:03", "01:02:04.1234", PeriodUnits.Ticks, 11234000)]
        [TestCase("01:02:03", "01:02:04.1234", PeriodUnits.Nanoseconds, 1123400000)]
        public void BetweenLocalTimes_SingleUnit(string startText, string endText, PeriodUnits units, long expectedValue)
        {
            var start = LocalTimePattern.ExtendedIso.Parse(startText).Value;
            var end = LocalTimePattern.ExtendedIso.Parse(endText).Value;
            var actual = Period.Between(start, end, units);
            var expected = new PeriodBuilder { [units] = expectedValue }.Build();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenLocalTimes_MovingForwards()
        {
            LocalTime t1 = new LocalTime(10, 0);
            LocalTime t2 = LocalTime.FromHourMinuteSecondMillisecondTick(15, 30, 45, 20, 5);
            Assert.AreEqual(Period.FromHours(5) + Period.FromMinutes(30) + Period.FromSeconds(45) +
                               Period.FromMilliseconds(20) + Period.FromTicks(5),
                               Period.Between(t1, t2));
        }

        [Test]
        public void BetweenLocalTimes_MovingBackwards()
        {
            LocalTime t1 = LocalTime.FromHourMinuteSecondMillisecondTick(15, 30, 45, 20, 5);
            LocalTime t2 = new LocalTime(10, 0);
            Assert.AreEqual(Period.FromHours(-5) + Period.FromMinutes(-30) + Period.FromSeconds(-45) +
                               Period.FromMilliseconds(-20) + Period.FromTicks(-5),
                               Period.Between(t1, t2));
        }

        [Test]
        public void BetweenLocalTimes_MovingForwards_WithJustHours()
        {
            LocalTime t1 = new LocalTime(11, 30);
            LocalTime t2 = new LocalTime(17, 15);
            Assert.AreEqual(Period.FromHours(5), Period.Between(t1, t2, PeriodUnits.Hours));
        }

        [Test]
        public void BetweenLocalTimes_MovingBackwards_WithJustHours()
        {
            LocalTime t1 = new LocalTime(17, 15);
            LocalTime t2 = new LocalTime(11, 30);
            Assert.AreEqual(Period.FromHours(-5), Period.Between(t1, t2, PeriodUnits.Hours));
        }

        [Test]
        public void Addition_WithDifferent_PeriodTypes()
        {
            Period p1 = Period.FromHours(3);
            Period p2 = Period.FromMinutes(20);
            Period sum = p1 + p2;
            Assert.AreEqual(3, sum.Hours);
            Assert.AreEqual(20, sum.Minutes);
        }

        [Test]
        public void Addition_MaxAndMinValue()
        {
            Period p1 = Period.MaxValue;
            Period p2 = Period.MinValue;
            Period sum = p1 + p2;
            Assert.AreEqual(new Period(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1), sum);
        }

        [Test]
        public void Addition_With_IdenticalPeriodTypes()
        {
            Period p1 = Period.FromHours(3);
            Period p2 = Period.FromHours(2);
            Period sum = p1 + p2;
            Assert.AreEqual(5, sum.Hours);
            Assert.AreEqual(sum, Period.Add(p1, p2));
        }

        [Test]
        public void Addition_DayCrossingMonthBoundary()
        {
            LocalDateTime start = new LocalDateTime(2010, 2, 20, 10, 0);
            LocalDateTime result = start + Period.FromDays(10);
            Assert.AreEqual(new LocalDateTime(2010, 3, 2, 10, 0), result);
        }

        [Test]
        public void Addition_OneYearOnLeapDay()
        {
            LocalDateTime start = new LocalDateTime(2012, 2, 29, 10, 0);
            LocalDateTime result = start + Period.FromYears(1);
            // Feb 29th becomes Feb 28th
            Assert.AreEqual(new LocalDateTime(2013, 2, 28, 10, 0), result);
        }

        [Test]
        public void Addition_FourYearsOnLeapDay()
        {
            LocalDateTime start = new LocalDateTime(2012, 2, 29, 10, 0);
            LocalDateTime result = start + Period.FromYears(4);
            // Feb 29th is still valid in 2016
            Assert.AreEqual(new LocalDateTime(2016, 2, 29, 10, 0), result);
        }

        [Test]
        public void Addition_YearMonthDay()
        {
            // One year, one month, two days
            Period period = Period.FromYears(1) + Period.FromMonths(1) + Period.FromDays(2);
            LocalDateTime start = new LocalDateTime(2007, 1, 30, 0, 0);
            // Periods are added in order, so this becomes...
            // Add one year: Jan 30th 2008
            // Add one month: Feb 29th 2008
            // Add two days: March 2nd 2008
            // If we added the days first, we'd end up with March 1st instead.
            LocalDateTime result = start + period;
            Assert.AreEqual(new LocalDateTime(2008, 3, 2, 0, 0), result);
        }

        [Test]
        public void Subtraction_WithDifferent_PeriodTypes()
        {
            Period p1 = Period.FromHours(3);
            Period p2 = Period.FromMinutes(20);
            Period difference = p1 - p2;
            Assert.AreEqual(3, difference.Hours);
            Assert.AreEqual(-20, difference.Minutes);
            Assert.AreEqual(difference, Period.Subtract(p1, p2));
        }

        [Test]
        public void Subtraction_With_IdenticalPeriodTypes()
        {
            Period p1 = Period.FromHours(3);
            Period p2 = Period.FromHours(2);
            Period difference = p1 - p2;
            Assert.AreEqual(1, difference.Hours);
            Assert.AreEqual(difference, Period.Subtract(p1, p2));
        }

        [Test]
        public void UnaryNegation()
        {
            Period period = new Period(2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            Period negation = -period;
            Assert.AreEqual(new Period(-2, -3, -4, -5, -6, -7, -8, -9, -10, -11), negation);
        }

        [Test]
        public void UnaryAddition()
        {
            Period period = new Period(2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            Period addition = +period;
            Assert.AreEqual(period, addition);
        }

        [Test]
        public void Equality_WhenEqual()
        {
            Assert.AreEqual(Period.FromHours(10), Period.FromHours(10));
            Assert.AreEqual(Period.FromMinutes(15), Period.FromMinutes(15));
            Assert.AreEqual(Period.FromDays(5), Period.FromDays(5));
        }

        [Test]
        public void Equality_WithDifferentPeriodTypes_OnlyConsidersValues()
        {
            Period allFields = Period.FromMinutes(1) + Period.FromHours(1) - Period.FromMinutes(1);
            Period justHours = Period.FromHours(1);
            Assert.AreEqual(allFields, justHours);
        }

        [Test]
        public void Equality_WhenUnequal()
        {
            Assert.IsFalse(Period.FromHours(10).Equals(Period.FromHours(20)));
            Assert.IsFalse(Period.FromMinutes(15).Equals(Period.FromSeconds(15)));
            Assert.IsFalse(Period.FromHours(1).Equals(Period.FromMinutes(60)));
            Assert.IsFalse(Period.FromHours(1).Equals(new object()));
            Assert.IsFalse(Period.FromHours(1).Equals(null));
            Assert.IsFalse(Period.FromHours(1).Equals((object?) null));
        }

        [Test]
        public void EqualityOperators()
        {
            Period val1 = Period.FromHours(1);
            Period val2 = Period.FromHours(1);
            Period val3 = Period.FromHours(2);
            Period? val4 = null;
            Assert.IsTrue(val1 == val2);
            Assert.IsFalse(val1 == val3);
            Assert.IsFalse(val1 == val4);
            Assert.IsFalse(val4 == val1);
            Assert.IsTrue(val4 == null);
            Assert.IsTrue(null == val4);

            Assert.IsFalse(val1 != val2);
            Assert.IsTrue(val1 != val3);
            Assert.IsTrue(val1 != val4);
            Assert.IsTrue(val4 != val1);
            Assert.IsFalse(val4 != null);
            Assert.IsFalse(null != val4);
        }

        [Test]
        [TestCase(PeriodUnits.Years, false)]
        [TestCase(PeriodUnits.Weeks, false)]
        [TestCase(PeriodUnits.Months, false)]
        [TestCase(PeriodUnits.Days, false)]
        [TestCase(PeriodUnits.Hours, true)]
        [TestCase(PeriodUnits.Minutes, true)]
        [TestCase(PeriodUnits.Seconds, true)]
        [TestCase(PeriodUnits.Milliseconds, true)]
        [TestCase(PeriodUnits.Ticks, true)]
        [TestCase(PeriodUnits.Nanoseconds, true)]
        public void HasTimeComponent_SingleValued(PeriodUnits unit, bool hasTimeComponent)
        {
            var period = new PeriodBuilder {[unit] = 1}.Build();
            Assert.AreEqual(hasTimeComponent, period.HasTimeComponent);
        }

        [Test]
        [TestCase(PeriodUnits.Years, true)]
        [TestCase(PeriodUnits.Weeks, true)]
        [TestCase(PeriodUnits.Months, true)]
        [TestCase(PeriodUnits.Days, true)]
        [TestCase(PeriodUnits.Hours, false)]
        [TestCase(PeriodUnits.Minutes, false)]
        [TestCase(PeriodUnits.Seconds, false)]
        [TestCase(PeriodUnits.Milliseconds, false)]
        [TestCase(PeriodUnits.Ticks, false)]
        [TestCase(PeriodUnits.Nanoseconds, false)]
        public void HasDateComponent_SingleValued(PeriodUnits unit, bool hasDateComponent)
        {
            var period = new PeriodBuilder {[unit] = 1 }.Build();
            Assert.AreEqual(hasDateComponent, period.HasDateComponent);
        }

        [Test]
        public void HasTimeComponent_Compound()
        {
            LocalDateTime dt1 = new LocalDateTime(2000, 1, 1, 10, 45, 00);
            LocalDateTime dt2 = new LocalDateTime(2000, 2, 4, 11, 50, 00);

            // Case 1: Entire period is date-based (no time units available)
            Assert.IsFalse(Period.Between(dt1.Date, dt2.Date).HasTimeComponent);

            // Case 2: Period contains date and time units, but time units are all zero
            Assert.IsFalse(Period.Between(dt1.Date + LocalTime.Midnight, dt2.Date + LocalTime.Midnight).HasTimeComponent);

            // Case 3: Entire period is time-based, but 0. (Same local time twice here.)
            Assert.IsFalse(Period.Between(dt1.TimeOfDay, dt1.TimeOfDay).HasTimeComponent);

            // Case 4: Period contains date and time units, and some time units are non-zero
            Assert.IsTrue(Period.Between(dt1, dt2).HasTimeComponent);

            // Case 5: Entire period is time-based, and some time units are non-zero
            Assert.IsTrue(Period.Between(dt1.TimeOfDay, dt2.TimeOfDay).HasTimeComponent);
        }

        [Test]
        public void HasDateComponent_Compound()
        {
            LocalDateTime dt1 = new LocalDateTime(2000, 1, 1, 10, 45, 00);
            LocalDateTime dt2 = new LocalDateTime(2000, 2, 4, 11, 50, 00);

            // Case 1: Entire period is time-based (no date units available)
            Assert.IsFalse(Period.Between(dt1.TimeOfDay, dt2.TimeOfDay).HasDateComponent);

            // Case 2: Period contains date and time units, but date units are all zero
            Assert.IsFalse(Period.Between(dt1, dt1.Date + dt2.TimeOfDay).HasDateComponent);

            // Case 3: Entire period is date-based, but 0. (Same local date twice here.)
            Assert.IsFalse(Period.Between(dt1.Date, dt1.Date).HasDateComponent);

            // Case 4: Period contains date and time units, and some date units are non-zero
            Assert.IsTrue(Period.Between(dt1, dt2).HasDateComponent);

            // Case 5: Entire period is date-based, and some time units are non-zero
            Assert.IsTrue(Period.Between(dt1.Date, dt2.Date).HasDateComponent);
        }

        [Test]
        public void ToString_Positive()
        {
            Period period = Period.FromDays(1) +  Period.FromHours(2);
            Assert.AreEqual("P1DT2H", period.ToString());
        }

        [Test]
        public void ToString_AllUnits()
        {
            Period period = new Period(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Assert.AreEqual("P1Y2M3W4DT5H6M7S8s9t10n", period.ToString());
        }

        [Test]
        public void ToString_Negative()
        {
            Period period = Period.FromDays(-1) + Period.FromHours(-2);
            Assert.AreEqual("P-1DT-2H", period.ToString());
        }

        [Test]
        public void ToString_Mixed()
        {
            Period period = Period.FromDays(-1) + Period.FromHours(2);
            Assert.AreEqual("P-1DT2H", period.ToString());
        }

        [Test]
        public void ToString_Zero()
        {
            Assert.AreEqual("P0D", Period.Zero.ToString());
        }

        [Test]
        public void ToBuilder_SingleUnit()
        {
            var builder = Period.FromHours(5).ToBuilder();
            var expected = new PeriodBuilder { Hours = 5 }.Build();
            Assert.AreEqual(expected, builder.Build());
        }

        [Test]
        public void ToBuilder_MultipleUnits()
        {
            var builder = (Period.FromHours(5) + Period.FromWeeks(2)).ToBuilder();
            var expected = new PeriodBuilder { Hours = 5, Weeks = 2 }.Build();
            Assert.AreEqual(expected, builder.Build());
        }

        [Test]
        public void Normalize_Weeks()
        {
            var original = new PeriodBuilder { Weeks = 2, Days = 5 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Days = 19 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_Hours()
        {
            var original = new PeriodBuilder { Hours = 25, Days = 1 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = 1, Days = 2 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_Minutes()
        {
            var original = new PeriodBuilder { Hours = 1, Minutes = 150 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = 3, Minutes = 30}.Build();
            Assert.AreEqual(expected, normalized);
        }


        [Test]
        public void Normalize_Seconds()
        {
            var original = new PeriodBuilder { Minutes = 1, Seconds = 150 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Minutes = 3, Seconds = 30 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_Milliseconds()
        {
            var original = new PeriodBuilder { Seconds = 1, Milliseconds = 1500 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Seconds = 2, Milliseconds= 500 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_Ticks()
        {
            var original = new PeriodBuilder { Milliseconds = 1, Ticks = 15000 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Milliseconds = 2, Ticks = 0, Nanoseconds = 500000 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_Nanoseconds()
        {
            var original = new PeriodBuilder { Ticks = 1, Nanoseconds = 150 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Nanoseconds = 250}.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_MultipleFields()
        {
            var original = new PeriodBuilder { Hours = 1, Minutes = 119, Seconds = 150 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = 3, Minutes = 1, Seconds = 30 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_AllNegative()
        {
            var original = new PeriodBuilder { Hours = -1, Minutes = -119, Seconds = -150 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = -3, Minutes = -1, Seconds = -30 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_MixedSigns_PositiveResult()
        {
            var original = new PeriodBuilder { Hours = 3, Minutes = -1 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = 2, Minutes = 59 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_MixedSigns_NegativeResult()
        {
            var original = new PeriodBuilder { Hours = 1, Minutes = -121 }.Build();
            var normalized = original.Normalize();
            var expected = new PeriodBuilder { Hours = -1, Minutes = -1 }.Build();
            Assert.AreEqual(expected, normalized);
        }

        [Test]
        public void Normalize_DoesntAffectMonthsAndYears()
        {
            var original = new PeriodBuilder { Years = 2, Months = 1, Days = 400 }.Build();
            Assert.AreEqual(original, original.Normalize());
        }

        [Test]
        public void Normalize_ZeroResult()
        {
            var original = new PeriodBuilder { Years = 0 }.Build();
            Assert.AreEqual(Period.Zero, original.Normalize());
        }

        [Test]
        public void Normalize_Overflow()
        {
            Period period = Period.FromHours(long.MaxValue);
            Assert.Throws<OverflowException>(() => period.Normalize());
        }

        [Test]
        public void ToString_SingleUnit()
        {
            var period = Period.FromHours(5);
            Assert.AreEqual("PT5H", period.ToString());
        }

        [Test]
        public void ToString_MultipleUnits()
        {
            var period = new PeriodBuilder { Hours = 5, Minutes = 30 }.Build();
            Assert.AreEqual("PT5H30M", period.ToString());
        }

        [Test]
        public void ToDuration_InvalidWithYears()
        {
            Period period = Period.FromYears(1);
            Assert.Throws<InvalidOperationException>(() => period.ToDuration());
        }

        [Test]
        public void ToDuration_InvalidWithMonths()
        {
            Period period = Period.FromMonths(1);
            Assert.Throws<InvalidOperationException>(() => period.ToDuration());
        }

        [Test]
        public void ToDuration_ValidAllAcceptableUnits()
        {
            Period period = new PeriodBuilder
            {
                Weeks = 1,
                Days = 2,
                Hours = 3,
                Minutes = 4,
                Seconds = 5,
                Milliseconds = 6,
                Ticks = 7
            }.Build();
            Assert.AreEqual(
                1 * NodaConstants.TicksPerWeek +
                2 * NodaConstants.TicksPerDay +
                3 * NodaConstants.TicksPerHour +
                4 * NodaConstants.TicksPerMinute +
                5 * NodaConstants.TicksPerSecond +
                6 * NodaConstants.TicksPerMillisecond + 7,
                period.ToDuration().BclCompatibleTicks);
        }

        [Test]
        public void ToDuration_ValidWithZeroValuesInMonthYearUnits()
        {
            Period period = Period.FromMonths(1) + Period.FromYears(1);
            period = period - period + Period.FromDays(1);
            Assert.IsFalse(period.HasTimeComponent);
            Assert.AreEqual(Duration.OneDay, period.ToDuration());
        }

        [Test]
        [Category("Overflow")]
        public void ToDuration_Overflow()
        {
            Period period = Period.FromSeconds(long.MaxValue);
            Assert.Throws<OverflowException>(() => period.ToDuration());
        }

        [Test]
        [Category("Overflow")]
        public void ToDuration_Overflow_WhenPossiblyValid()
        {
            // These two should pretty much cancel each other out - and would, if we had a 128-bit integer
            // representation to use.
            Period period = Period.FromSeconds(long.MaxValue) + Period.FromMinutes(long.MinValue / 60);
            Assert.Throws<OverflowException>(() => period.ToDuration());
        }

        [Test]
        public void NormalizingEqualityComparer_NullToNonNull()
        {
            Period period = Period.FromYears(1);
            Assert.IsFalse(Period.NormalizingEqualityComparer.Equals(period, null));
            Assert.IsFalse(Period.NormalizingEqualityComparer.Equals(null, period));
        }

        [Test]
        public void NormalizingEqualityComparer_NullToNull()
        {
            Assert.IsTrue(Period.NormalizingEqualityComparer.Equals(null, null));
        }

        [Test]
        public void NormalizingEqualityComparer_PeriodToItself()
        {
            Period period = Period.FromYears(1);
            Assert.IsTrue(Period.NormalizingEqualityComparer.Equals(period, period));
        }

        [Test]
        public void NormalizingEqualityComparer_NonEqualAfterNormalization()
        {
            Period period1 = Period.FromHours(2);
            Period period2 = Period.FromMinutes(150);
            Assert.IsFalse(Period.NormalizingEqualityComparer.Equals(period1, period2));
        }

        [Test]
        public void NormalizingEqualityComparer_EqualAfterNormalization()
        {
            Period period1 = Period.FromHours(2);
            Period period2 = Period.FromMinutes(120);
            Assert.IsTrue(Period.NormalizingEqualityComparer.Equals(period1, period2));
        }

        [Test]
        public void NormalizingEqualityComparer_GetHashCodeAfterNormalization()
        {
            Period period1 = Period.FromHours(2);
            Period period2 = Period.FromMinutes(120);
            Assert.AreEqual(Period.NormalizingEqualityComparer.GetHashCode(period1),
                Period.NormalizingEqualityComparer.GetHashCode(period2));
        }

        [Test]
        public void Comparer_NullWithNull()
        {
            var comparer = Period.CreateComparer(new LocalDateTime(2000, 1, 1, 0, 0));
            Assert.AreEqual(0, comparer.Compare(null, null));
        }

        [Test]
        public void Comparer_NullWithNonNull()
        {
            var comparer = Period.CreateComparer(new LocalDateTime(2000, 1, 1, 0, 0));
            Assert.That(comparer.Compare(null, Period.Zero), Is.LessThan(0));
        }

        [Test]
        public void Comparer_NonNullWithNull()
        {
            var comparer = Period.CreateComparer(new LocalDateTime(2000, 1, 1, 0, 0));
            Assert.That(comparer.Compare(Period.Zero, null), Is.GreaterThan(0));
        }

        [Test]
        public void Comparer_DurationablePeriods()
        {
            var bigger = Period.FromHours(25);
            var smaller = Period.FromDays(1);
            var comparer = Period.CreateComparer(new LocalDateTime(2000, 1, 1, 0, 0));
            Assert.That(comparer.Compare(bigger, smaller), Is.GreaterThan(0));
            Assert.That(comparer.Compare(smaller, bigger), Is.LessThan(0));
            Assert.AreEqual(0, comparer.Compare(bigger, bigger));
        }

        [Test]
        public void Comparer_NonDurationablePeriods()
        {
            var month = Period.FromMonths(1);
            var days = Period.FromDays(30);
            // At the start of January, a month is longer than 30 days
            var januaryComparer = Period.CreateComparer(new LocalDateTime(2000, 1, 1, 0, 0));
            Assert.That(januaryComparer.Compare(month, days), Is.GreaterThan(0));
            Assert.That(januaryComparer.Compare(days, month), Is.LessThan(0));
            Assert.AreEqual(0, januaryComparer.Compare(month, month));

            // At the start of February, a month is shorter than 30 days
            var februaryComparer = Period.CreateComparer(new LocalDateTime(2000, 2, 1, 0, 0));
            Assert.That(februaryComparer.Compare(month, days), Is.LessThan(0));
            Assert.That(februaryComparer.Compare(days, month), Is.GreaterThan(0));
            Assert.AreEqual(0, februaryComparer.Compare(month, month));
        }

        private static TestCaseData[] PeriodMaxAndMinValues => 
        [
            new TestCaseData(Period.MaxValue, int.MaxValue, long.MaxValue).SetName("All members should be MaxValue"), 
            new TestCaseData(Period.MinValue, int.MinValue, long.MinValue).SetName("All members should be MinValue")
        ];

        /// <summary>
        /// Ensure that Period.MaxValue and Period.MinValue contain the max/min value assignable to each public property.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(PeriodMaxAndMinValues))]
        public void Period_MaxAndMinValues_AllMembers(Period period, int expectedIntValue, long expectedLongValue)
        {
            foreach (var property in typeof(Period).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var actualValue = property.GetValue(period);
                
                // HasTimeComponent is a public property that will get included in this loop.
                // This block allows us to ignore it.
                if (actualValue is bool)
                {
                    return;
                }

                var expectedValue = actualValue is int ? expectedIntValue
                    : actualValue is long ? expectedLongValue
                    : throw new InvalidOperationException($"Property {property.Name} has unexpected type {actualValue?.GetType().Name}.");
                Assert.AreEqual(expectedValue, actualValue);
            }
        }

        [Test]
        [TestCaseSource(nameof(AllPeriodUnits))]
        public void Between_ExtremeValues(PeriodUnits units)
        {
            // We can't use None, and Nanoseconds will *correctly* overflow.
            if (units == PeriodUnits.None || units == PeriodUnits.Nanoseconds)
            {
                return;
            }
            var minValue = LocalDate.MinIsoValue.At(LocalTime.MinValue);
            var maxValue = LocalDate.MaxIsoValue.At(LocalTime.MaxValue);
            Period.Between(minValue, maxValue, units);
        }

        [Test]
        public void Between_ExtremeValues_Overflow()
        {
            var minValue = LocalDate.MinIsoValue.At(LocalTime.MinValue);
            var maxValue = LocalDate.MaxIsoValue.At(LocalTime.MaxValue);
            Assert.Throws<OverflowException>(() => Period.Between(minValue, maxValue, PeriodUnits.Nanoseconds));
        }

        [Test]
        [TestCase("2015-02-28T16:00:00", "2016-02-29T08:00:00", PeriodUnits.Years, 1, 0)]
        [TestCase("2015-02-28T16:00:00", "2016-02-29T08:00:00", PeriodUnits.Months, 12, -11)]
        [TestCase("2014-01-01T16:00:00", "2014-01-03T08:00:00", PeriodUnits.Days, 1, -1)]
        [TestCase("2014-01-01T16:00:00", "2014-01-03T08:00:00", PeriodUnits.Hours, 40, -40)]
        public void Between_LocalDateTime_AwkwardTimeOfDayWithSingleUnit(string startText, string endText, PeriodUnits units, int expectedForward, int expectedBackward)
        {
            LocalDateTime start = LocalDateTimePattern.ExtendedIso.Parse(startText).Value;
            LocalDateTime end = LocalDateTimePattern.ExtendedIso.Parse(endText).Value;
            Period forward = Period.Between(start, end, units);
            Assert.AreEqual(expectedForward, forward.ToBuilder()[units]);
            Period backward = Period.Between(end, start, units);
            Assert.AreEqual(expectedBackward, backward.ToBuilder()[units]);
        }

        [Test]
        public void Between_LocalDateTime_SameValue()
        {
            LocalDateTime start = new LocalDateTime(2014, 1, 1, 16, 0, 0);
            Assert.AreSame(Period.Zero, Period.Between(start, start));
        }

        [Test]
        public void Between_LocalDateTime_AwkwardTimeOfDayWithMultipleUnits()
        {
            LocalDateTime start = new LocalDateTime(2014, 1, 1, 16, 0, 0);
            LocalDateTime end = new LocalDateTime(2015, 2, 3, 8, 0, 0);
            Period actual = Period.Between(start, end, PeriodUnits.YearMonthDay | PeriodUnits.AllTimeUnits);
            Period expected = new PeriodBuilder { Years = 1, Months = 1, Days = 1, Hours = 16 }.Build();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void BetweenYearMonth_InvalidUnits()
        {
            YearMonth yearMonth1 = new YearMonth(2010, 1);
            YearMonth yearMonth2 = new YearMonth(2011, 3);
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, 0));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, (PeriodUnits)(-1)));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, PeriodUnits.AllTimeUnits));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, PeriodUnits.Days));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, PeriodUnits.Years | PeriodUnits.Days));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, PeriodUnits.Years | PeriodUnits.Weeks));
            Assert.Throws<ArgumentException>(() => Period.Between(yearMonth1, yearMonth2, PeriodUnits.Years | PeriodUnits.Hours));
        }

        [Test]
        public void BetweenYearMonth_DifferentCalendarSystems_Throws()
        {
            YearMonth start = new YearMonth(2017, 11, CalendarSystem.Coptic);
            YearMonth end = new YearMonth(2017, 11, CalendarSystem.Gregorian);
            Assert.Throws<ArgumentException>(() => Period.Between(start, end));
        }

        [TestCase("2016-05", "2017-03", PeriodUnits.Years, 0)]
        [TestCase("2016-05", "2016-05", PeriodUnits.Years, 0)]
        [TestCase("2016-05", "2019-03", PeriodUnits.Years, 2)]
        [TestCase("2016-05", "2019-05", PeriodUnits.Years, 3)]
        [TestCase("2016-05", "2017-07", PeriodUnits.Months, 14)]
        [TestCase("2016-05", "2017-05", PeriodUnits.Months, 12)]
        [TestCase("2016-07", "2016-07", PeriodUnits.Months, 0)]
        public void BetweenYearMonth_SingleUnit(string startText, string endText, PeriodUnits units, int expectedValue)
        {
            var start = YearMonthPattern.Iso.Parse(startText).Value;
            var end = YearMonthPattern.Iso.Parse(endText).Value;
            var forward = Period.Between(start, end, units);
            var expectedForward = new PeriodBuilder { [units] = expectedValue }.Build();
            Assert.AreEqual(expectedForward, forward);
            var backward = Period.Between(end, start, units);
            var expectedBackward = new PeriodBuilder { [units] = -expectedValue }.Build();
            Assert.AreEqual(expectedBackward, backward);
        }

        [TestCase("2017-05", "2017-05", 0, 0)]
        [TestCase("2016-05", "2017-05", 1, 0)]
        [TestCase("2016-05", "2017-06", 1, 1)]
        [TestCase("2016-05", "2018-10", 2, 5)]
        [TestCase("2016-05", "2017-04", 0, 11)]
        [TestCase("2013-05", "2017-04", 3, 11)]
        public void BetweenYearMonth_BothUnits(string startText, string endText, int expectedYears, int expectedMonths)
        {
            var start = YearMonthPattern.Iso.Parse(startText).Value;
            var end = YearMonthPattern.Iso.Parse(endText).Value;
            var forward = Period.Between(start, end, PeriodUnits.Years | PeriodUnits.Months);
            var expectedForward = new PeriodBuilder { [PeriodUnits.Years] = expectedYears, [PeriodUnits.Months] = expectedMonths }.Build();
            Assert.AreEqual(expectedForward, forward);
            var forwardNoUnits = Period.Between(start, end);
            Assert.AreEqual(expectedForward, forwardNoUnits);
            var backward = Period.Between(end, start, PeriodUnits.Years | PeriodUnits.Months);
            var expectedBackward = new PeriodBuilder { [PeriodUnits.Years] =- expectedYears, [PeriodUnits.Months] = -expectedMonths }.Build();
            Assert.AreEqual(expectedBackward, backward);
            var backwardNoUnits = Period.Between(end, start);
            Assert.AreEqual(expectedBackward, backwardNoUnits);
        }

        [Test]
        public void FromNanoseconds()
        {
            var period = Period.FromNanoseconds(1234567890L);
            Assert.AreEqual(1234567890L, period.Nanoseconds);
        }

        [Test]
        public void AddPeriodToPeriod_NoOverflow()
        {
            Period p1 = Period.FromHours(long.MaxValue);
            Period p2 = Period.FromMinutes(60);
            Assert.AreEqual(new PeriodBuilder { Hours = long.MaxValue, Minutes = 60 }.Build(), p1 + p2);
        }

        [Test]
        public void AddPeriodToPeriod_Overflow()
        {
            Period p1 = Period.FromHours(long.MaxValue);
            Period p2 = Period.FromHours(1);
            Assert.Throws<OverflowException>(() => (p1 + p2).GetHashCode());
        }

        /// <summary>
        /// Just a simple way of parsing a period string. It's a more compact period representation.
        /// </summary>
        private static Period Parse(string text)
        {
            return PeriodPattern.Roundtrip.Parse(text).Value;
        }
    }
}
