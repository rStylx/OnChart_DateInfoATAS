# US Holidays — ATAS indicator

Marks US market holidays directly on the chart so historical futures sessions can be
identified at a glance. Built for research on historical data, not for live.

Every holiday day is drawn as a coloured vertical band spanning the full height of the
price panel with a label naming the day and its market status:

## What?:

| Category | Meaning | Default |
|---|---|---|
| **Full closure** | US equity and equity-index futures markets closed for the day | shown, red |
| **Early close** | Shortened session, normally 13:00 ET (July 3rd, Black Friday, Christmas Eve) | shown, orange |
| **Unscheduled closure** | 9/11, Hurricane Sandy, national days of mourning | shown, purple |
| **Federal only** | Federal holiday where futures keep trading (Columbus Day, Veterans Day) | hidden, blue |

The distinction matters for futures research: Good Friday is a market closure but *not* a
federal holiday, while Columbus Day is a federal holiday where the futures session runs
normally. A plain federal-holiday list gets both of these wrong.

Unscheduled closures currently covered: 11–14 Sep 2001, 11 Jun 2004 (Reagan),
2 Jan 2007 (Ford), 29–30 Oct 2012 (Sandy), 5 Dec 2018 (Bush), 9 Jan 2025 (Carter).


## Settings

The indicator also exposes a hidden `Holiday` data series: `0` = normal day, `1` =
federal only, `2` = early close, `3` = unscheduled, `4` = full closure. Switch its
visual type away from *Hide* to plot it, or read it from another indicator.

## Build and install

Requires the .NET 10 SDK and ATAS. Then just put Indicator in APPDATA where Indicators are.
