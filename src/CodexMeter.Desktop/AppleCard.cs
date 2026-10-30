using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CodexMeter.Desktop.Services;

namespace CodexMeter.Desktop;

public sealed class AppleCard : FrameworkElement
{
    private MainWindowViewModel? _model;
    private AppearanceSettings _settings = new();
    public AppleCard()
    {
        IsHitTestVisible = false;
        DataContextChanged += (_, _) => { if (_model != null) _model.PropertyChanged -= OnModelChanged; _model = DataContext as MainWindowViewModel; if (_model != null) _model.PropertyChanged += OnModelChanged; InvalidateVisual(); };
    }
    public void Apply(AppearanceSettings settings) { _settings = settings; InvalidateVisual(); }
    private void OnModelChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    private static Brush B(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
    private void Label(DrawingContext dc, string text, double x, double y, double size, Brush brush, double maxWidth, bool bold = false, TextAlignment align = TextAlignment.Left)
    {
        if (maxWidth <= 0) return;
        var ft = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = maxWidth, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis, TextAlignment = align };
        dc.DrawText(ft, new Point(x, y));
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var w = ActualWidth; var h = ActualHeight; if (w < 30 || h < 30) return;
        var t = _settings.Theme; var light = t == "ember-light";
        var main = B(light ? "#101010" : "#F8F8F8"); var muted = B(light ? "#5C5C57" : "#B9BAC0");
        var pct = _model?.PrimaryRemainingText ?? "--%";
        var caption = _model?.PrimaryCaption ?? "JANELA DE 5 HORAS";
        var week = _model?.WeeklyText ?? "Semana · aguardando dados";
        var reset = _model?.ResetDisplay ?? "Renovação indisponível";
        var value = _model?.PrimaryRemainingPercent;
        double pad = Math.Clamp(w * .065, 12, 20);
        if (t is "ember-dark" or "ember-light")
        {
            dc.DrawRectangle(B(light ? "#EEEDE6" : "#1B1B1D"), null, new Rect(0, 0, w, h));
            var gradient = new LinearGradientBrush(); gradient.StartPoint = new Point(0, 0); gradient.EndPoint = new Point(1, 1);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#6E100B"), 0)); gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F54B16"), .46)); gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#591B19"), 1));
            dc.DrawRectangle(gradient, null, new Rect(0, 0, w, h * .48));
            dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(160,255,224,180), Colors.Transparent) { Center = new Point(.5,.5), GradientOrigin = new Point(.5,.5) }, null, new Point(w*.7,h*.08), w*.32,h*.24);
            var body = new StreamGeometry(); using (var c = body.Open()) { double y=h*.36; c.BeginFigure(new Point(0,y+8),true,true); c.QuadraticBezierTo(new Point(0,y),new Point(8,y),true,false); c.LineTo(new Point(w*.37,y),true,false); c.QuadraticBezierTo(new Point(w*.4,y),new Point(w*.44,y+8),true,false); c.LineTo(new Point(w*.49,y+15),true,false); c.LineTo(new Point(w,h*.36+15),true,false); c.LineTo(new Point(w,h),true,false); c.LineTo(new Point(0,h),true,false); } dc.DrawGeometry(B(light ? "#EEEDE6" : "#1B1B1D"),null,body);
            Label(dc,"CODEX",w*.58,pad,11,Brushes.White,w*.36,true,TextAlignment.Right);
            Label(dc,"Codex Meter",pad,h*.39,Math.Clamp(h*.075,12,16),main,w*.55,true);
            Label(dc,caption,pad,h*.50,10,muted,w-pad*2);
            Label(dc,pct,pad,h*.64,Math.Clamp(h*.16,20,34),main,w*.55,true);
            if (_settings.ShowWeekly) Label(dc,week,pad,h-24,10,muted,w*.50);
            if (_settings.ShowReset) Label(dc,reset,w*.50,h-24,9,muted,w*.50-pad,false,TextAlignment.Right);
            return;
        }
        if (t == "bar")
        {
            dc.DrawRectangle(B("#0B0B0D"), null, new Rect(0,0,w,h));
            Label(dc,value is null ? "CODEX INDISPONÍVEL" : "CODEX  ·  5 HORAS",pad,pad,10,muted,w*.8,true);
            Label(dc,pct,pad,h*.28,Math.Clamp(h*.19,22,36),main,w-pad*2,true);
            double y=h*.59; dc.DrawRoundedRectangle(B("#343438"),null,new Rect(pad,y,w-pad*2,8),4,4);
            if (value is double remaining && double.IsFinite(remaining)) { var width = (w-pad*2) * Math.Clamp(remaining/100,0,1); if(width>0) dc.DrawRoundedRectangle(B("#F54B50"),null,new Rect(pad,y,width,8),4,4); }
            if (_settings.ShowWeekly) Label(dc,week,pad,h-24,10,muted,w*.48);
            if (_settings.ShowReset) Label(dc,reset,w*.50,h-24,9,muted,w*.50-pad,false,TextAlignment.Right);
            return;
        }
    }
}
