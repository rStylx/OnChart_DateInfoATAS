using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using ATAS.Indicators;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;
using MediaColor = System.Windows.Media.Color;

namespace UsHolidays
{
	public enum LabelPlacement
	{
		Top,
		Bottom
	}

	[DisplayName("US Holidays")]
	[Category("Sessions")]
	[Description("Marks US market holidays, early closes and unscheduled closures on the chart.")]
	public class UsHolidaysIndicator : Indicator
	{
		private const int LabelPadding = 4;
		private const int MaxLabelRows = 4;

		private readonly HolidayProvider _provider = new HolidayProvider();
		private readonly ValueDataSeries _marker = new ValueDataSeries("holidayMarker", "Holiday");
		private readonly List<int> _labelRows = new List<int>();

		private RenderFont _font = new RenderFont("Arial", 10);
		private int _fontSize = 10;
		private int _lastYear = -1;
		private int _sessionShift;
		private bool _online = true;

		public UsHolidaysIndicator()
			: base(true)
		{
			DenyToChangePanel = true;
			EnableCustomDrawing = true;
			SubscribeToDrawingEvents(DrawingLayouts.Historical | DrawingLayouts.LatestBar);
			DrawAbovePrice = false;
			Panel = IndicatorDataProvider.CandlesPanel;

			_marker.VisualType = VisualMode.Hide;
			_marker.ShowZeroValue = false;
			_marker.ShowTooltip = false;
			DataSeries[0] = _marker;

			_provider.YearLoaded += OnYearLoaded;
		}

		[Display(Name = "Download from nager.date", GroupName = "Data", Order = 10)]
		public bool UseOnlineSource
		{
			get => _online;
			set
			{
				_online = value;
				_provider.UseOnlineSource = value;
				_provider.Reset();
				_lastYear = -1;
				RecalculateValues();
			}
		}

		[Display(Name = "Session shift (hours)", GroupName = "Data", Order = 20,
			Description = "Shift applied to bar time before it is mapped to a calendar day. Use 6 for a CME session starting at 18:00 ET.")]
		[Range(-12, 12)]
		public int SessionShiftHours
		{
			get => _sessionShift;
			set
			{
				_sessionShift = value;
				_lastYear = -1;
				RecalculateValues();
			}
		}

		[Display(Name = "Show", GroupName = "Full closure", Order = 100)]
		public bool ShowFullClose { get; set; } = true;

		[Display(Name = "Color", GroupName = "Full closure", Order = 110)]
		public MediaColor FullCloseColor { get; set; } = MediaColor.FromArgb(55, 214, 69, 69);

		[Display(Name = "Show", GroupName = "Early close", Order = 200)]
		public bool ShowEarlyClose { get; set; } = true;

		[Display(Name = "Color", GroupName = "Early close", Order = 210)]
		public MediaColor EarlyCloseColor { get; set; } = MediaColor.FromArgb(55, 226, 155, 44);

		[Display(Name = "Show", GroupName = "Unscheduled closure", Order = 300)]
		public bool ShowUnscheduled { get; set; } = true;

		[Display(Name = "Color", GroupName = "Unscheduled closure", Order = 310)]
		public MediaColor UnscheduledColor { get; set; } = MediaColor.FromArgb(70, 168, 76, 214);

		[Display(Name = "Show", GroupName = "Federal only", Order = 400)]
		public bool ShowFederalOnly { get; set; }

		[Display(Name = "Color", GroupName = "Federal only", Order = 410)]
		public MediaColor FederalColor { get; set; } = MediaColor.FromArgb(45, 90, 145, 200);

		[Display(Name = "Show labels", GroupName = "Label", Order = 500)]
		public bool ShowLabels { get; set; } = true;

		[Display(Name = "Append status", GroupName = "Label", Order = 510)]
		public bool ShowKindTag { get; set; } = true;

		[Display(Name = "Color", GroupName = "Label", Order = 520)]
		public MediaColor LabelColor { get; set; } = MediaColor.FromArgb(230, 225, 225, 225);

		[Display(Name = "Font size", GroupName = "Label", Order = 530)]
		[Range(6, 30)]
		public int FontSize
		{
			get => _fontSize;
			set
			{
				_fontSize = value;
				_font = new RenderFont("Arial", value);
			}
		}

		[Display(Name = "Placement", GroupName = "Label", Order = 540)]
		public LabelPlacement Placement { get; set; } = LabelPlacement.Top;

		[Display(Name = "Show borders", GroupName = "Border", Order = 600)]
		public bool ShowBorders { get; set; } = true;

		[Display(Name = "Color", GroupName = "Border", Order = 610)]
		public MediaColor BorderColor { get; set; } = MediaColor.FromArgb(120, 140, 140, 140);

		protected override void OnCalculate(int bar, decimal value)
		{
			var day = SessionDate(bar);

			if (day.Year != _lastYear)
			{
				_lastYear = day.Year;
				_provider.Ensure(day.Year);
			}

			var info = _provider.Find(day);
			_marker[bar] = info == null ? 0m : (int)info.Kind + 1;
		}

		protected override void OnRender(RenderContext context, DrawingLayouts layout)
		{
			if (ChartInfo == null || Container == null || CurrentBar < 1)
				return;

			var from = Math.Max(FirstVisibleBarNumber, 0);
			var to = Math.Min(LastVisibleBarNumber, CurrentBar - 1);

			if (to < from)
				return;

			var region = Container.Region;
			_labelRows.Clear();

			context.SetClip(region);

			var groupStart = from;
			var groupDay = SessionDate(from);

			for (var bar = from + 1; bar <= to; bar++)
			{
				var day = SessionDate(bar);

				if (day == groupDay)
					continue;

				DrawDay(context, region, groupDay, groupStart, bar - 1);
				groupDay = day;
				groupStart = bar;
			}

			DrawDay(context, region, groupDay, groupStart, to);

			context.ResetClip();
		}

		protected override void OnDispose()
		{
			_provider.YearLoaded -= OnYearLoaded;
			_provider.Dispose();

			base.OnDispose();
		}

		private void DrawDay(RenderContext context, Rectangle region, DateTime day, int firstBar, int lastBar)
		{
			var info = _provider.Find(day);

			if (info == null || !IsEnabled(info.Kind))
				return;

			var chart = ChartInfo.PriceChartContainer;
			var left = chart.GetXByBar(firstBar, true);
			var right = chart.GetXByBar(lastBar, true) + (int)chart.BarsWidth;

			if (right <= region.Left || left >= region.Right)
				return;

			var rect = new Rectangle(left, region.Top, Math.Max(right - left, 1), region.Height);
			context.FillRectangle(ToDrawing(ColorFor(info.Kind)), rect);

			if (ShowBorders)
			{
				var pen = new RenderPen(ToDrawing(BorderColor));
				context.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);
				context.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Bottom);
			}

			if (ShowLabels)
				DrawLabel(context, region, rect, info);
		}

		private void DrawLabel(RenderContext context, Rectangle region, Rectangle band, HolidayInfo info)
		{
			var text = ShowKindTag ? info.Name + " (" + info.KindTag + ")" : info.Name;
			var size = context.MeasureString(text, _font);

			// na intradayu jeden dzien potrafi zajac caly ekran, wiec centrujemy po widocznym
			// kawalku pasa a nie po calym, inaczej podpis ucieka za krawedz
			var visibleLeft = Math.Max(band.Left, region.Left);
			var visibleRight = Math.Min(band.Right, region.Right);
			var x = visibleLeft + (visibleRight - visibleLeft - size.Width) / 2;

			// na dziennym interwale jeden dzien to jeden slupek, wiec podpisy wchodza na siebie.
			// szukamy pierwszego wolnego wiersza, a jak nie ma to kurwa trudno, nie rysujemy
			var row = -1;

			for (var i = 0; i < MaxLabelRows; i++)
			{
				if (i == _labelRows.Count)
					_labelRows.Add(int.MinValue);

				if (x <= _labelRows[i] + LabelPadding)
					continue;

				row = i;
				_labelRows[i] = x + size.Width;

				break;
			}

			if (row < 0)
				return;

			var offset = LabelPadding + row * (size.Height + 2);

			var y = Placement == LabelPlacement.Top
				? region.Top + offset
				: region.Bottom - size.Height - offset;

			context.DrawString(text, _font, ToDrawing(LabelColor), x, y);
		}

		private DateTime SessionDate(int bar)
		{
			return GetCandle(bar).Time.AddHours(_sessionShift).Date;
		}

		private bool IsEnabled(HolidayKind kind)
		{
			switch (kind)
			{
				case HolidayKind.FullClose: return ShowFullClose;
				case HolidayKind.EarlyClose: return ShowEarlyClose;
				case HolidayKind.Unscheduled: return ShowUnscheduled;
				default: return ShowFederalOnly;
			}
		}

		private MediaColor ColorFor(HolidayKind kind)
		{
			switch (kind)
			{
				case HolidayKind.FullClose: return FullCloseColor;
				case HolidayKind.EarlyClose: return EarlyCloseColor;
				case HolidayKind.Unscheduled: return UnscheduledColor;
				default: return FederalColor;
			}
		}

		private void OnYearLoaded()
		{
			var container = ChartInfo?.ChartContainer;

			if (container == null)
				return;

			// leci z watku pobierania, seria zostaje na regulach offline do nastepnego przeliczenia
			RedrawChart(new RedrawArg(container.Region));
		}

		private static Color ToDrawing(MediaColor color)
		{
			return Color.FromArgb(color.A, color.R, color.G, color.B);
		}
	}
}
