using System;
using System.Globalization;
using System.IO;

namespace UsHolidays
{
	public static class HolidayCache
	{
		private static readonly TimeSpan LiveYearTtl = TimeSpan.FromDays(7);

		public static string Directory
		{
			get
			{
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"ATAS", "UsHolidays");
			}
		}

		public static bool TryRead(int year, out string json)
		{
			json = null;

			try
			{
				var file = PathFor(year);

				if (!File.Exists(file))
					return false;

				// stare lata sie nie zmieniaja, wiec trzymamy je w nieskonczonosc
				if (year >= DateTime.UtcNow.Year && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > LiveYearTtl)
					return false;

				json = File.ReadAllText(file);

				return !string.IsNullOrWhiteSpace(json);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void Write(int year, string json)
		{
			try
			{
				System.IO.Directory.CreateDirectory(Directory);

				var file = PathFor(year);
				var temp = file + ".tmp";

				File.WriteAllText(temp, json);
				File.Move(temp, file, true);
			}
			catch (Exception)
			{
			}
		}

		private static string PathFor(int year)
		{
			return Path.Combine(Directory, year.ToString(CultureInfo.InvariantCulture) + ".json");
		}
	}
}
