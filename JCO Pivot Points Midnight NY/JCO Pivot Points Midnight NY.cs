// -------------------------------------------------------------------------------------------------
//
//    JCO Pivot Points - Pivot Points and NY Midnight Indicator for cTrader
//
//    This indicator displays daily pivot points (PP, S1-S3, R1-R3) and the
//    New York midnight opening price line. Pivot points are calculated using
//    the classic formula based on the previous day's high, low, and close.
//
//    Author: J. Cornier
//    Version: 1.0
//    Last Updated: 2026-01-20
//
// -------------------------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class PivotPointsIndicator : Indicator
    {
        [Parameter("Jours à afficher", DefaultValue = 3, MinValue = 1)]
        public int DaysToShow { get; set; }

        [Parameter("Étendre lignes (si 1 jour)", DefaultValue = false)]
        public bool ExtendLines { get; set; }

        [Parameter("Afficher ligne NY minuit", DefaultValue = true)]
        public bool ShowNYMidnight { get; set; }

        [Parameter("Afficher Pivot Point", DefaultValue = true)]
        public bool ShowPivotPoint { get; set; }

        [Parameter("Afficher Supports/Résistances", DefaultValue = true)]
        public bool ShowSupportResistance { get; set; }

        // Groupe Midnight
        [Parameter("Couleur", Group = "Midnight", DefaultValue = "DodgerBlue")]
        public string MidnightColorName { get; set; }
        
        [Parameter("Épaisseur", Group = "Midnight", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int MidnightThickness { get; set; }
        
        [Parameter("Style", Group = "Midnight", DefaultValue = 2)]
        public LineStyle MidnightStyle { get; set; }

        // Groupe Pivot Point
        [Parameter("Couleur", Group = "Pivot Point", DefaultValue = "DarkOrange")]
        public string PivotColorName { get; set; }
        
        [Parameter("Épaisseur", Group = "Pivot Point", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int PivotThickness { get; set; }
        
        [Parameter("Style", Group = "Pivot Point", DefaultValue = 2)]
        public LineStyle PivotStyle { get; set; }

        // Groupe Résistances
        [Parameter("Couleur", Group = "Résistances", DefaultValue = "ForestGreen")]
        public string ResistanceColorName { get; set; }
        
        [Parameter("Épaisseur", Group = "Résistances", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int ResistanceThickness { get; set; }
        
        [Parameter("Style", Group = "Résistances", DefaultValue = 2)]
        public LineStyle ResistanceStyle { get; set; }

        // Groupe Supports
        [Parameter("Couleur", Group = "Supports", DefaultValue = "Red")]
        public string SupportColorName { get; set; }
        
        [Parameter("Épaisseur", Group = "Supports", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int SupportThickness { get; set; }
        
        [Parameter("Style", Group = "Supports", DefaultValue = 2)]
        public LineStyle SupportStyle { get; set; }

        // Groupe Labels
        [Parameter("Taille texte", Group = "Labels", DefaultValue = 9, MinValue = 6, MaxValue = 14)]
        public int LabelFontSize { get; set; }

        // Groupe General
        [Parameter("Fuseau Horaire", Group = "General", DefaultValue = "Eastern Standard Time")]
        public string TimeZoneId { get; set; }
        
        private Color resistanceColor;
        private Color supportColor;
        private Color pivotColor;
        private Color midnightColor;
        
        private Bars dailyBars;
        
        // Préfixe unique pour tous les objets de l'indicateur
        private const string INDICATOR_PREFIX = "PivotInd_";

        protected override void Initialize()
        {
            dailyBars = MarketData.GetBars(TimeFrame.Daily, SymbolName);
            
            // Parser les couleurs
            resistanceColor = ParseColor(ResistanceColorName, Color.ForestGreen);
            supportColor = ParseColor(SupportColorName, Color.Red);
            pivotColor = ParseColor(PivotColorName, Color.DarkOrange);
            midnightColor = ParseColor(MidnightColorName, Color.DodgerBlue);
        }

        public override void Calculate(int index)
        {
            if (index != Bars.Count - 1)
                return;

            ClearAllObjects();

            // Dessiner les N derniers jours
            for (int i = 1; i <= DaysToShow; i++)
            {
                if (dailyBars.Count - i < 0)
                    break;

                DrawPivotsForDay(i);
            }

            if (ShowNYMidnight)
            {
                DrawNewYorkMidnightLine();
            }
        }

        private void DrawPivotsForDay(int daysBack)
        {
            int calcIndex = dailyBars.Count - 1 - daysBack;
            int displayIndex = calcIndex + 1;
            
            if (calcIndex < 0 || displayIndex >= dailyBars.Count)
                return;

            double high = dailyBars.HighPrices[calcIndex];
            double low = dailyBars.LowPrices[calcIndex];
            double close = dailyBars.ClosePrices[calcIndex];

            if (high <= 0 || low <= 0 || close <= 0)
                return;

            double P = (high + low + close) / 3;
            double S1 = 2 * P - high;
            double S2 = P - high + low;
            double S3 = low - 2 * (high - P);
            double R1 = 2 * P - low;
            double R2 = P + high - low;
            double R3 = high + 2 * (P - low);

            DateTime normalStartTime = dailyBars.OpenTimes[displayIndex];
            DateTime endTime;
            if (displayIndex + 1 < dailyBars.Count)
            {
                endTime = dailyBars.OpenTimes[displayIndex + 1];
            }
            else
            {
                endTime = normalStartTime.AddDays(1);
            }

            DateTime actualStartTime;
            if (DaysToShow == 1 && ExtendLines)
            {
                actualStartTime = Bars.OpenTimes[0];
            }
            else
            {
                actualStartTime = normalStartTime;
            }

            DateTime labelTime = GetLabelPosition(normalStartTime, endTime, 5);

            string suffix = "_" + daysBack;

            if (ShowPivotPoint)
            {
                DrawLineByTime(INDICATOR_PREFIX + "PP" + suffix, actualStartTime, P, endTime, P, labelTime, pivotColor, PivotThickness, PivotStyle, "PP");
            }

            if (ShowSupportResistance)
            {
                DrawLineByTime(INDICATOR_PREFIX + "R1" + suffix, actualStartTime, R1, endTime, R1, labelTime, resistanceColor, ResistanceThickness, ResistanceStyle, "R1");
                DrawLineByTime(INDICATOR_PREFIX + "R2" + suffix, actualStartTime, R2, endTime, R2, labelTime, resistanceColor, ResistanceThickness, ResistanceStyle, "R2");
                DrawLineByTime(INDICATOR_PREFIX + "R3" + suffix, actualStartTime, R3, endTime, R3, labelTime, resistanceColor, ResistanceThickness, ResistanceStyle, "R3");
                DrawLineByTime(INDICATOR_PREFIX + "S1" + suffix, actualStartTime, S1, endTime, S1, labelTime, supportColor, SupportThickness, SupportStyle, "S1");
                DrawLineByTime(INDICATOR_PREFIX + "S2" + suffix, actualStartTime, S2, endTime, S2, labelTime, supportColor, SupportThickness, SupportStyle, "S2");
                DrawLineByTime(INDICATOR_PREFIX + "S3" + suffix, actualStartTime, S3, endTime, S3, labelTime, supportColor, SupportThickness, SupportStyle, "S3");
            }
        }

        private DateTime GetLabelPosition(DateTime startTime, DateTime endTime, int barsFromEnd)
        {
            int lastBarIndex = -1;
            for (int i = Bars.Count - 1; i >= 0; i--)
            {
                if (Bars.OpenTimes[i] >= startTime && Bars.OpenTimes[i] < endTime)
                {
                    lastBarIndex = i;
                    break;
                }
            }

            if (lastBarIndex >= barsFromEnd)
            {
                return Bars.OpenTimes[lastBarIndex - barsFromEnd];
            }

            return endTime.AddMinutes(-60);
        }

        private void DrawLineByTime(string name, DateTime startTime, double startY, DateTime endTime, double endY, DateTime labelTime, Color color, int thickness, LineStyle style, string label)
        {
            var line = Chart.DrawTrendLine(name, startTime, startY, endTime, endY, color, thickness, style);
            line.IsInteractive = false;

            var text = Chart.DrawText(name + "_label", label, labelTime, endY, color);
            text.IsInteractive = false;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.FontSize = LabelFontSize;
        }

        private void DrawNewYorkMidnightLine()
        {
            DateTime today = Server.Time.Date;
            DateTime midnightNY = GetNewYorkMidnight(today);
            
            int midnightIndex = -1;
            for (int i = Bars.Count - 1; i >= 0; i--)
            {
                if (Bars.OpenTimes[i] <= midnightNY)
                {
                    midnightIndex = i;
                    break;
                }
            }
            
            if (midnightIndex < 0)
                return;

            double openPrice = Bars.OpenPrices[midnightIndex];
            
            DateTime actualStartTime;
            if (DaysToShow == 1 && ExtendLines)
            {
                actualStartTime = Bars.OpenTimes[0];
            }
            else
            {
                actualStartTime = midnightNY;
            }
            
            DateTime endTime = today.AddDays(1);
            DateTime labelTime = GetLabelPosition(today, endTime, 5);

            var line = Chart.DrawTrendLine(INDICATOR_PREFIX + "MidnightNY", actualStartTime, openPrice, endTime, openPrice, midnightColor, MidnightThickness, MidnightStyle);
            line.IsInteractive = false;

            var text = Chart.DrawText(INDICATOR_PREFIX + "MidnightNY_label", "0 NY", labelTime, openPrice, midnightColor);
            text.IsInteractive = false;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.FontSize = LabelFontSize;
        }

        private void ClearAllObjects()
        {
            var toRemove = new System.Collections.Generic.List<ChartObject>();
            
            foreach (var obj in Chart.Objects)
            {
                // Ne supprimer QUE les objets créés par cet indicateur (avec le préfixe)
                if (obj.Name.StartsWith(INDICATOR_PREFIX))
                {
                    toRemove.Add(obj);
                }
            }
            
            foreach (var obj in toRemove)
            {
                Chart.RemoveObject(obj.Name);
            }
        }

        private DateTime GetNewYorkMidnight(DateTime date)
        {
            TimeZoneInfo nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime midnightNY = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(midnightNY, nyTimeZone);
        }

        private Color ParseColor(string colorString, Color defaultColor)
        {
            try
            {
                var property = typeof(Color).GetProperty(colorString);
                if (property != null)
                {
                    return (Color)property.GetValue(null);
                }
            }
            catch (Exception)
            {
            }
            return defaultColor;
        }
    }
}