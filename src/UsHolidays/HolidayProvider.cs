using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UsHolidays
{
	public sealed class HolidayProvider : IDisposable
	{
		private readonly object _sync = new object();
		private readonly Dictionary<int, Dictionary<DateTime, HolidayInfo>> _years = new Dictionary<int, Dictionary<DateTime, HolidayInfo>>();
		private readonly HashSet<int> _requested = new HashSet<int>();

		private CancellationTokenSource _cts = new CancellationTokenSource();

		public bool UseOnlineSource { get; set; } = true;

		public event Action YearLoaded;

		public void Ensure(int year)
		{
			if (year < 1900 || year > 2200)
				return;

			var fetch = false;

			lock (_sync)
			{
				if (!_years.ContainsKey(year))
					_years[year] = BuildOffline(year);

				if (_requested.Add(year))
					fetch = UseOnlineSource;
			}

			if (fetch)
			{
				var token = _cts.Token;
				Task.Run(() => LoadOnlineAsync(year, token), token);
			}
		}

		public HolidayInfo Find(DateTime day)
		{
			lock (_sync)
			{
				return _years.TryGetValue(day.Year, out var map) && map.TryGetValue(day.Date, out var info)
					? info
					: null;
			}
		}

		public void Reset()
		{
			CancellationTokenSource old;

			lock (_sync)
			{
				_years.Clear();
				_requested.Clear();
				old = _cts;
				_cts = new CancellationTokenSource();
			}

			old.Cancel();
			old.Dispose();
		}

		public void Dispose()
		{
			_cts.Cancel();
			_cts.Dispose();
		}

		private async Task LoadOnlineAsync(int year, CancellationToken token)
		{
			try
			{
				if (!HolidayCache.TryRead(year, out var json))
				{
					json = await NagerClient.DownloadAsync(year, token).ConfigureAwait(false);
					HolidayCache.Write(year, json);
				}

				if (token.IsCancellationRequested)
					return;

				var downloaded = NagerClient.Parse(json);

				lock (_sync)
				{
					if (!_years.TryGetValue(year, out var map))
						return;

					foreach (var item in downloaded)
						Merge(map, item);
				}

				YearLoaded?.Invoke();
			}
			catch (Exception)
			{
				// brak neta albo api padlo - trudno, zostajemy na wyliczonym kalendarzu
			}
		}

		private static Dictionary<DateTime, HolidayInfo> BuildOffline(int year)
		{
			var map = new Dictionary<DateTime, HolidayInfo>();

			foreach (var item in HolidayRules.ForYear(year))
				Merge(map, item);

			return map;
		}

		private static void Merge(Dictionary<DateTime, HolidayInfo> map, HolidayInfo item)
		{
			if (!map.TryGetValue(item.Date, out var existing) || item.Kind > existing.Kind)
				map[item.Date] = item;
		}
	}
}
