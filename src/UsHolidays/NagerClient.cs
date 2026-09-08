using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UsHolidays
{
	public static class NagerClient
	{
		private const string UrlFormat = "https://date.nager.at/api/v3/PublicHolidays/{0}/US";

		private static readonly HttpClient Http = CreateClient();

		public static async Task<string> DownloadAsync(int year, CancellationToken token)
		{
			var url = string.Format(CultureInfo.InvariantCulture, UrlFormat, year);

			using (var response = await Http.GetAsync(url, token).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();

				return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
			}
		}

		public static IReadOnlyList<HolidayInfo> Parse(string json)
		{
			var result = new List<HolidayInfo>();

			if (string.IsNullOrWhiteSpace(json))
				return result;

			using (var doc = JsonDocument.Parse(json))
			{
				if (doc.RootElement.ValueKind != JsonValueKind.Array)
					return result;

				foreach (var item in doc.RootElement.EnumerateArray())
				{
					if (!item.TryGetProperty("date", out var dateNode))
						continue;

					if (!DateTime.TryParseExact(dateNode.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
						    DateTimeStyles.None, out var date))
						continue;

					// counties != null oznacza swieto stanowe, gielda ma to w dupie
					if (item.TryGetProperty("counties", out var counties) && counties.ValueKind == JsonValueKind.Array)
						continue;

					if (item.TryGetProperty("global", out var global) && global.ValueKind == JsonValueKind.False)
						continue;

					var name = item.TryGetProperty("name", out var nameNode) ? nameNode.GetString() : null;

					if (string.IsNullOrWhiteSpace(name))
						name = item.TryGetProperty("localName", out var local) ? local.GetString() : "US holiday";

					result.Add(new HolidayInfo(date, name, HolidayKind.FederalOnly, "source: nager.date"));
				}
			}

			return result;
		}

		private static HttpClient CreateClient()
		{
			var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
			client.DefaultRequestHeaders.Add("User-Agent", "ATAS-UsHolidays/1.0");

			return client;
		}
	}
}
