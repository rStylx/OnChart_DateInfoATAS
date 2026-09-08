using System;
using System.Collections.Generic;
using System.Linq;

namespace UsHolidays
{
	public static class HolidayRules
	{
		private static readonly HolidayInfo[] Unscheduled =
		{
			new HolidayInfo(new DateTime(2001, 9, 11), "9/11 attacks", HolidayKind.Unscheduled, "us markets closed 11-14 sep, reopened 17 sep"),
			new HolidayInfo(new DateTime(2001, 9, 12), "9/11 attacks", HolidayKind.Unscheduled, "us markets closed 11-14 sep, reopened 17 sep"),
			new HolidayInfo(new DateTime(2001, 9, 13), "9/11 attacks", HolidayKind.Unscheduled, "us markets closed 11-14 sep, reopened 17 sep"),
			new HolidayInfo(new DateTime(2001, 9, 14), "9/11 attacks", HolidayKind.Unscheduled, "us markets closed 11-14 sep, reopened 17 sep"),
			new HolidayInfo(new DateTime(2004, 6, 11), "Mourning - Ronald Reagan", HolidayKind.Unscheduled),
			new HolidayInfo(new DateTime(2007, 1, 2), "Mourning - Gerald Ford", HolidayKind.Unscheduled),
			new HolidayInfo(new DateTime(2012, 10, 29), "Hurricane Sandy", HolidayKind.Unscheduled, "nyse floor closed, thin globex session"),
			new HolidayInfo(new DateTime(2012, 10, 30), "Hurricane Sandy", HolidayKind.Unscheduled, "nyse floor closed, thin globex session"),
			new HolidayInfo(new DateTime(2018, 12, 5), "Mourning - George H. W. Bush", HolidayKind.Unscheduled),
			new HolidayInfo(new DateTime(2025, 1, 9), "Mourning - Jimmy Carter", HolidayKind.Unscheduled)
		};

		public static IReadOnlyList<HolidayInfo> ForYear(int year)
		{
			var list = new List<HolidayInfo>();

			// New Year's leci tylko z niedzieli na poniedzialek. Jak wypada w sobote to gielda i tak handluje
			// w piatek 31.12, kurwa, jedyny wyjatek od zwyklej reguly obserwacji.
			list.Add(new HolidayInfo(ShiftFromSunday(new DateTime(year, 1, 1)), "New Year's Day", HolidayKind.FullClose));

			if (year >= 1998)
				list.Add(new HolidayInfo(NthWeekday(year, 1, DayOfWeek.Monday, 3), "Martin Luther King Jr. Day", HolidayKind.FullClose));

			list.Add(year >= 1971
				? new HolidayInfo(NthWeekday(year, 2, DayOfWeek.Monday, 3), "Presidents' Day", HolidayKind.FullClose)
				: new HolidayInfo(Observed(new DateTime(year, 2, 22)), "Washington's Birthday", HolidayKind.FullClose));

			list.Add(new HolidayInfo(GoodFriday(year), "Good Friday", HolidayKind.FullClose,
				"equities closed, cme rates products keep a shortened session"));

			list.Add(year >= 1971
				? new HolidayInfo(LastWeekday(year, 5, DayOfWeek.Monday), "Memorial Day", HolidayKind.FullClose)
				: new HolidayInfo(Observed(new DateTime(year, 5, 30)), "Memorial Day", HolidayKind.FullClose));

			if (year >= 2022)
				list.Add(new HolidayInfo(Observed(new DateTime(year, 6, 19)), "Juneteenth", HolidayKind.FullClose));
			else if (year == 2021)
				list.Add(new HolidayInfo(new DateTime(2021, 6, 18), "Juneteenth", HolidayKind.FederalOnly,
					"federal od 2021, ale gielda zaczela dopiero w 2022"));

			list.Add(new HolidayInfo(Observed(new DateTime(year, 7, 4)), "Independence Day", HolidayKind.FullClose));
			list.Add(new HolidayInfo(NthWeekday(year, 9, DayOfWeek.Monday, 1), "Labor Day", HolidayKind.FullClose));

			if (year >= 1971)
				list.Add(new HolidayInfo(NthWeekday(year, 10, DayOfWeek.Monday, 2), "Columbus Day", HolidayKind.FederalOnly,
					"bondy zamkniete, equities i futures handluja normalnie"));

			list.Add(new HolidayInfo(Observed(new DateTime(year, 11, 11)), "Veterans Day", HolidayKind.FederalOnly,
				"bondy zamkniete, equities i futures handluja normalnie"));

			var thanksgiving = NthWeekday(year, 11, DayOfWeek.Thursday, 4);
			list.Add(new HolidayInfo(thanksgiving, "Thanksgiving", HolidayKind.FullClose));

			list.Add(new HolidayInfo(Observed(new DateTime(year, 12, 25)), "Christmas Day", HolidayKind.FullClose));

			AddEarlyClose(list, thanksgiving.AddDays(1), "Black Friday");
			AddEarlyClose(list, new DateTime(year, 7, 3), "Independence Day eve");
			AddEarlyClose(list, new DateTime(year, 12, 24), "Christmas Eve");

			list.AddRange(Unscheduled.Where(x => x.Date.Year == year));

			return list.OrderBy(x => x.Date).ToList();
		}

		public static DateTime Easter(int year)
		{
			var a = year % 19;
			var b = year / 100;
			var c = year % 100;
			var d = b / 4;
			var e = b % 4;
			var f = (b + 8) / 25;
			var g = (b - f + 1) / 3;
			var h = (19 * a + b - d - g + 15) % 30;
			var i = c / 4;
			var k = c % 4;
			var l = (32 + 2 * e + 2 * i - h - k) % 7;
			var m = (a + 11 * h + 22 * l) / 451;
			var month = (h + l - 7 * m + 114) / 31;
			var day = (h + l - 7 * m + 114) % 31 + 1;

			return new DateTime(year, month, day);
		}

		public static DateTime GoodFriday(int year)
		{
			return Easter(year).AddDays(-2);
		}

		private static void AddEarlyClose(List<HolidayInfo> list, DateTime day, string name)
		{
			if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
				return;

			if (list.Any(x => x.Date == day.Date && x.Kind == HolidayKind.FullClose))
				return;

			list.Add(new HolidayInfo(day, name, HolidayKind.EarlyClose, "skrocona sesja, zwykle 13:00 ET"));
		}

		private static DateTime Observed(DateTime date)
		{
			if (date.DayOfWeek == DayOfWeek.Saturday)
				return date.AddDays(-1);

			if (date.DayOfWeek == DayOfWeek.Sunday)
				return date.AddDays(1);

			return date;
		}

		private static DateTime ShiftFromSunday(DateTime date)
		{
			return date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(1) : date;
		}

		private static DateTime NthWeekday(int year, int month, DayOfWeek day, int n)
		{
			var first = new DateTime(year, month, 1);
			var offset = ((int)day - (int)first.DayOfWeek + 7) % 7;

			return first.AddDays(offset + 7 * (n - 1));
		}

		private static DateTime LastWeekday(int year, int month, DayOfWeek day)
		{
			var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
			var offset = ((int)last.DayOfWeek - (int)day + 7) % 7;

			return last.AddDays(-offset);
		}
	}
}
