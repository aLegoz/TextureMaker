using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Nodes.Base;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace TextureMaker.Nodes.Sources;

public enum GradientDirection { Horizontal, Vertical, Diagonal, Radial }

public static class GradientDirectionValues
{
    public static GradientDirection[] All { get; } =
        (GradientDirection[])Enum.GetValues(typeof(GradientDirection));
}

public class GradientNodeViewModel : TextureNodeViewModel
{
    private WpfColor _colorA = WpfColors.Black;
    public WpfColor ColorA { get => _colorA; set => this.RaiseAndSetIfChanged(ref _colorA, value); }

    private WpfColor _colorB = WpfColors.White;
    public WpfColor ColorB { get => _colorB; set => this.RaiseAndSetIfChanged(ref _colorB, value); }

    private GradientDirection _direction = GradientDirection.Horizontal;
    public GradientDirection Direction { get => _direction; set => this.RaiseAndSetIfChanged(ref _direction, value); }

    private int _width = 512;
    public int Width { get => _width; set => this.RaiseAndSetIfChanged(ref _width, value); }

    private int _height = 512;
    public int Height { get => _height; set => this.RaiseAndSetIfChanged(ref _height, value); }

    public GradientNodeViewModel() : base("Gradient")
    {
        Output.Value = this.WhenAnyValue(x => x.ColorA, x => x.ColorB, x => x.Direction, x => x.Width, x => x.Height)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(a => Observable.Start(() => Generate(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5),
                RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData Generate(WpfColor ca, WpfColor cb, GradientDirection dir, int w, int h)
    {
        w = Math.Max(1, w); h = Math.Max(1, h);
        var img = new Image<Rgba32>(w, h);
        img.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    float t = dir switch
                    {
                        GradientDirection.Horizontal => (float)x / Math.Max(1, w - 1),
                        GradientDirection.Vertical   => (float)y / Math.Max(1, h - 1),
                        GradientDirection.Diagonal   => ((float)x / (w - 1) + (float)y / (h - 1)) * 0.5f,
                        GradientDirection.Radial     => MathF.Sqrt(
                            MathF.Pow((float)x / w - 0.5f, 2) +
                            MathF.Pow((float)y / h - 0.5f, 2)) * 2f,
                        _ => 0f
                    };
                    t = Math.Clamp(t, 0f, 1f);
                    row[x] = new Rgba32(
                        (byte)(ca.R + (cb.R - ca.R) * t),
                        (byte)(ca.G + (cb.G - ca.G) * t),
                        (byte)(ca.B + (cb.B - ca.B) * t),
                        (byte)(ca.A + (cb.A - ca.A) * t));
                }
            }
        });
        return new TextureData(img);
    }
}
