using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TextureMaker.Views.Nodes;

public partial class ColorPickerWindow : Window
{
    // ── State ─────────────────────────────────────────────────────────
    private double _h;          // 0–360
    private double _s;          // 0–1
    private double _v;          // 0–1
    private byte   _a = 255;

    private bool _updating;

    private readonly LinearGradientBrush _svHueBrush;
    private readonly LinearGradientBrush _alphaGradBrush;

    public Color PickedColor { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────
    public ColorPickerWindow(Color initial)
    {
        InitializeComponent();

        // Build mutable brushes once
        _svHueBrush = new LinearGradientBrush(Colors.White, Colors.Red,
            new Point(0, 0), new Point(1, 0));
        SvHueBg.Fill = _svHueBrush;

        _alphaGradBrush = new LinearGradientBrush(
            Color.FromArgb(0, 255, 0, 0), Colors.Red,
            new Point(0, 0), new Point(1, 0));
        AlphaGradRect.Fill = _alphaGradBrush;

        // Set initial color
        OldColorRect.Fill = new SolidColorBrush(initial);
        RgbToHsv(initial.R, initial.G, initial.B, out _h, out _s, out _v);
        _a = initial.A;

        // Defer UpdateAll until layout pass completes (needs ActualWidth)
        Loaded += (_, _) => UpdateAll();
    }

    // ── Master update — syncs all UI from H,S,V,A ────────────────────
    private void UpdateAll()
    {
        if (_updating) return;
        _updating = true;
        try
        {
            var rgb = HsvToRgb(_h, _s, _v, _a);
            PickedColor = rgb;

            // SV canvas background (pure hue)
            var pureHue = HsvToRgb(_h, 1, 1, 255);
            _svHueBrush.GradientStops[1].Color = pureHue;

            // SV thumb
            double sx = Math.Clamp(_s * SvCanvas.Width  - 7, -7, SvCanvas.Width  - 7);
            double sy = Math.Clamp((1 - _v) * SvCanvas.Height - 7, -7, SvCanvas.Height - 7);
            System.Windows.Controls.Canvas.SetLeft(SvThumb, sx);
            System.Windows.Controls.Canvas.SetTop(SvThumb, sy);

            // SV thumb border color (invert for contrast)
            SvThumb.Stroke = _v > 0.5 ? Brushes.Black : Brushes.White;

            // Hue thumb
            PositionThumb(HueThumb, HueBar.ActualWidth, _h / 360.0);

            // Alpha gradient and thumb
            var opaqueRgb = HsvToRgb(_h, _s, _v, 255);
            _alphaGradBrush.GradientStops[0].Color =
                Color.FromArgb(0, opaqueRgb.R, opaqueRgb.G, opaqueRgb.B);
            _alphaGradBrush.GradientStops[1].Color = opaqueRgb;
            PositionThumb(AlphaThumb, AlphaBar.ActualWidth, _a / 255.0);

            // New-color preview
            NewColorRect.Fill = new SolidColorBrush(rgb);

            // Text boxes
            TxtHex.Text = $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}{rgb.A:X2}";

            TxtRgbR.Text = rgb.R.ToString();
            TxtRgbG.Text = rgb.G.ToString();
            TxtRgbB.Text = rgb.B.ToString();
            TxtRgbA.Text = rgb.A.ToString();

            TxtHsvH.Text = ((int)Math.Round(_h)).ToString();
            TxtHsvS.Text = ((int)Math.Round(_s * 100)).ToString();
            TxtHsvV.Text = ((int)Math.Round(_v * 100)).ToString();

            RgbToHsl(rgb.R, rgb.G, rgb.B, out var hl, out var sl, out var ll);
            TxtHslH.Text = ((int)Math.Round(hl)).ToString();
            TxtHslS.Text = ((int)Math.Round(sl * 100)).ToString();
            TxtHslL.Text = ((int)Math.Round(ll * 100)).ToString();
        }
        finally { _updating = false; }
    }

    private static void PositionThumb(Rectangle thumb, double barWidth, double t)
    {
        double x = Math.Clamp(t * barWidth - 2, 0, Math.Max(0, barWidth - 4));
        thumb.Margin = new Thickness(x, 0, 0, 0);
    }

    // ── SV canvas ────────────────────────────────────────────────────
    private void SvCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SvCanvas.CaptureMouse();
        ApplySvPoint(e.GetPosition(SvCanvas));
    }
    private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            ApplySvPoint(e.GetPosition(SvCanvas));
    }
    private void SvCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        => SvCanvas.ReleaseMouseCapture();

    private void ApplySvPoint(Point p)
    {
        _s = Math.Clamp(p.X / SvCanvas.Width,  0, 1);
        _v = Math.Clamp(1 - p.Y / SvCanvas.Height, 0, 1);
        UpdateAll();
    }

    // ── Hue bar ───────────────────────────────────────────────────────
    private void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        HueBar.CaptureMouse();
        ApplyHuePoint(e.GetPosition(HueBar).X);
    }
    private void HueBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            ApplyHuePoint(e.GetPosition(HueBar).X);
    }
    private void HueBar_MouseUp(object sender, MouseButtonEventArgs e)
        => HueBar.ReleaseMouseCapture();

    private void ApplyHuePoint(double x)
    {
        _h = Math.Clamp(x / HueBar.ActualWidth * 360, 0, 360);
        UpdateAll();
    }

    // ── Alpha bar ─────────────────────────────────────────────────────
    private void AlphaBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        AlphaBar.CaptureMouse();
        ApplyAlphaPoint(e.GetPosition(AlphaBar).X);
    }
    private void AlphaBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            ApplyAlphaPoint(e.GetPosition(AlphaBar).X);
    }
    private void AlphaBar_MouseUp(object sender, MouseButtonEventArgs e)
        => AlphaBar.ReleaseMouseCapture();

    private void ApplyAlphaPoint(double x)
    {
        _a = (byte)Math.Clamp(x / AlphaBar.ActualWidth * 255, 0, 255);
        UpdateAll();
    }

    // ── Text box commits ──────────────────────────────────────────────
    private void Txt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        // Find which group this box belongs to and commit
        if (sender == TxtHex)                TxtHex_Commit(sender, null!);
        else if (sender is { } tb && (tb == TxtRgbR || tb == TxtRgbG || tb == TxtRgbB || tb == TxtRgbA))
            RgbaCommit(sender, null!);
        else if (sender is { } tb2 && (tb2 == TxtHsvH || tb2 == TxtHsvS || tb2 == TxtHsvV))
            HsvCommit(sender, null!);
        else if (sender is { } tb3 && (tb3 == TxtHslH || tb3 == TxtHslS || tb3 == TxtHslL))
            HslCommit(sender, null!);
        e.Handled = true;
    }

    private void TxtHex_Commit(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        var text = TxtHex.Text.TrimStart('#').Trim();
        if (text.Length == 6 && TryParseBytes(text, 0, 3, out var b6))
        {
            RgbToHsv(b6[0], b6[1], b6[2], out _h, out _s, out _v);
            UpdateAll();
        }
        else if (text.Length == 8 && TryParseBytes(text, 0, 4, out var b8))
        {
            RgbToHsv(b8[0], b8[1], b8[2], out _h, out _s, out _v);
            _a = b8[3];
            UpdateAll();
        }
    }

    private void RgbaCommit(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        if (TryParseByte(TxtRgbR.Text, out var r) &&
            TryParseByte(TxtRgbG.Text, out var g) &&
            TryParseByte(TxtRgbB.Text, out var b) &&
            TryParseByte(TxtRgbA.Text, out var a))
        {
            RgbToHsv(r, g, b, out _h, out _s, out _v);
            _a = a;
            UpdateAll();
        }
    }

    private void HsvCommit(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        if (int.TryParse(TxtHsvH.Text, out var ih) &&
            int.TryParse(TxtHsvS.Text, out var is_) &&
            int.TryParse(TxtHsvV.Text, out var iv))
        {
            _h = Math.Clamp(ih, 0, 360);
            _s = Math.Clamp(is_, 0, 100) / 100.0;
            _v = Math.Clamp(iv, 0, 100) / 100.0;
            UpdateAll();
        }
    }

    private void HslCommit(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        if (int.TryParse(TxtHslH.Text, out var ih) &&
            int.TryParse(TxtHslS.Text, out var isl) &&
            int.TryParse(TxtHslL.Text, out var il))
        {
            var rgb = HslToRgb(
                Math.Clamp(ih, 0, 360),
                Math.Clamp(isl, 0, 100) / 100.0,
                Math.Clamp(il,  0, 100) / 100.0,
                _a);
            RgbToHsv(rgb.R, rgb.G, rgb.B, out _h, out _s, out _v);
            UpdateAll();
        }
    }

    // ── Input validation ─────────────────────────────────────────────
    private void NumBox_PreviewInput(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsAsciiDigit);

    private void HexBox_PreviewInput(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(c => char.IsAsciiHexDigit(c) || c == '#');

    // ── OK ────────────────────────────────────────────────────────────
    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    // ── Color math ───────────────────────────────────────────────────
    private static Color HsvToRgb(double h, double s, double v, byte a)
    {
        if (s == 0)
        {
            byte g = (byte)Math.Round(v * 255);
            return Color.FromArgb(a, g, g, g);
        }
        h = (h % 360) / 60.0;
        int   sector = (int)h;
        double frac  = h - sector;
        byte vi = (byte)Math.Round(v * 255);
        byte p  = (byte)Math.Round(v * (1 - s) * 255);
        byte q  = (byte)Math.Round(v * (1 - s * frac) * 255);
        byte t  = (byte)Math.Round(v * (1 - s * (1 - frac)) * 255);
        return sector switch
        {
            0 => Color.FromArgb(a, vi, t,  p),
            1 => Color.FromArgb(a, q,  vi, p),
            2 => Color.FromArgb(a, p,  vi, t),
            3 => Color.FromArgb(a, p,  q,  vi),
            4 => Color.FromArgb(a, t,  p,  vi),
            _ => Color.FromArgb(a, vi, p,  q),
        };
    }

    private static void RgbToHsv(byte r, byte g, byte b,
        out double h, out double s, out double v)
    {
        double dr = r / 255.0, dg = g / 255.0, db = b / 255.0;
        double max   = Math.Max(dr, Math.Max(dg, db));
        double min   = Math.Min(dr, Math.Min(dg, db));
        double delta = max - min;
        v = max;
        s = max == 0 ? 0 : delta / max;
        if (delta == 0) { h = 0; return; }
        if (max == dr)       h = 60 * ((dg - db) / delta % 6);
        else if (max == dg)  h = 60 * ((db - dr) / delta + 2);
        else                 h = 60 * ((dr - dg) / delta + 4);
        if (h < 0) h += 360;
    }

    private static Color HslToRgb(double h, double s, double l, byte a)
    {
        if (s == 0)
        {
            byte g = (byte)Math.Round(l * 255);
            return Color.FromArgb(a, g, g, g);
        }
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;
        double hNorm = h / 360.0;
        return Color.FromArgb(a,
            (byte)Math.Round(HueChannel(p, q, hNorm + 1.0 / 3) * 255),
            (byte)Math.Round(HueChannel(p, q, hNorm)            * 255),
            (byte)Math.Round(HueChannel(p, q, hNorm - 1.0 / 3) * 255));
    }

    private static double HueChannel(double p, double q, double t)
    {
        if (t < 0) t += 1; if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 0.5)      return q;
        if (t < 2.0 / 3)  return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    private static void RgbToHsl(byte r, byte g, byte b,
        out double h, out double s, out double l)
    {
        double dr = r / 255.0, dg = g / 255.0, db = b / 255.0;
        double max   = Math.Max(dr, Math.Max(dg, db));
        double min   = Math.Min(dr, Math.Min(dg, db));
        double delta = max - min;
        l = (max + min) / 2.0;
        if (delta == 0) { h = s = 0; return; }
        s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
        if (max == dr)       h = ((dg - db) / delta + (dg < db ? 6 : 0)) * 60;
        else if (max == dg)  h = ((db - dr) / delta + 2) * 60;
        else                 h = ((dr - dg) / delta + 4) * 60;
    }

    // ── Parse helpers ─────────────────────────────────────────────────
    private static bool TryParseByte(string s, out byte result)
    {
        result = 0;
        if (!int.TryParse(s, out var v)) return false;
        result = (byte)Math.Clamp(v, 0, 255);
        return true;
    }

    private static bool TryParseBytes(string hex, int start, int count, out byte[] result)
    {
        result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            if (!byte.TryParse(hex.AsSpan(start + i * 2, 2),
                    System.Globalization.NumberStyles.HexNumber, null, out result[i]))
                return false;
        }
        return true;
    }
}
