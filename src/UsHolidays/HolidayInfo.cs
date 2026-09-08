using System;

namespace UsHolidays
{
	public sealed class HolidayInfo
	{
		public HolidayInfo(DateTime date, string name, HolidayKind kind, string note = null)
		{
			Date = date.Date;
			Name = name;
			Kind = kind;
			Note = note;
		}

		public DateTime Date { get; }

		public string Name { get; }

		public HolidayKind Kind { get; }

		public string Note { get; }

		public string KindTag
		{
			get
			{
				switch (Kind)
				{
					case HolidayKind.FullClose: return "closed";
					case HolidayKind.EarlyClose: return "early close";
					case HolidayKind.Unscheduled: return "unscheduled";
					default: return "federal";
				}
			}
		}

		public override string ToString()
		{
			return Date.ToString("yyyy-MM-dd") + " " + Name + " [" + KindTag + "]";
		}
	}
}
