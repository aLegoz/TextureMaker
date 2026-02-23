using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Combine;

public enum BlendMode { Normal, Multiply, Add, Screen, Overlay }

public static class BlendModeValues
{
    public static BlendMode[] All { get; } = (BlendMode[])Enum.GetValues(typeof(BlendMode));
}

public class BlendNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputA { get; } = new();
    public InputPin<TextureData> InputB { get; } = new();

    private BlendMode _mode = BlendMode.Normal;
    public BlendMode Mode { get => _mode; set => this.RaiseAndSetIfChanged(ref _mode, value); }

    private float _opacity = 1f;
    public float Opacity { get => _opacity; set => this.RaiseAndSetIfChanged(ref _opacity, value); }

    public BlendNodeViewModel() : base("Blend")
    {
        RegisterInput(InputA, "Base");
        RegisterInput(InputB, "Blend");
        Output.Value = Observable
            .CombineLatest(InputA.Value, InputB.Value,
                this.WhenAnyValue(x => x.Mode, x => x.Opacity),
                (a, b, mo) => (a, b, mo.Item1, mo.Item2))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => Apply(args.a, args.b, args.Item3, args.Item4), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? a, TextureData? b, BlendMode mode, float opacity)
    {
        if (a == null) return b?.Clone();
        if (b == null) return a.Clone();

        int w = Math.Min(a.Image.Width, b.Image.Width);
        int h = Math.Min(a.Image.Height, b.Image.Height);
        var result = new Image<Rgba32>(w, h);

        result.ProcessPixelRows(a.Image, b.Image, (rAcc, aAcc, bAcc) =>
        {
            for (int y = 0; y < h; y++)
            {
                var rRow = rAcc.GetRowSpan(y);
                var aRow = aAcc.GetRowSpan(y);
                var bRow = bAcc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    float ar = aRow[x].R / 255f, ag = aRow[x].G / 255f, ab_ = aRow[x].B / 255f;
                    float br = bRow[x].R / 255f, bg = bRow[x].G / 255f, bb = bRow[x].B / 255f;
                    float or_, og, ob;
                    switch (mode)
                    {
                        case BlendMode.Multiply: or_ = ar*br; og = ag*bg; ob = ab_*bb; break;
                        case BlendMode.Add:      or_ = MathF.Min(1f,ar+br); og = MathF.Min(1f,ag+bg); ob = MathF.Min(1f,ab_+bb); break;
                        case BlendMode.Screen:   or_ = 1-(1-ar)*(1-br); og = 1-(1-ag)*(1-bg); ob = 1-(1-ab_)*(1-bb); break;
                        case BlendMode.Overlay:  or_ = Ov(ar,br); og = Ov(ag,bg); ob = Ov(ab_,bb); break;
                        default:                 or_ = br; og = bg; ob = bb; break;
                    }
                    rRow[x] = new Rgba32(
                        (byte)((ar + (or_ - ar) * opacity) * 255),
                        (byte)((ag + (og - ag) * opacity) * 255),
                        (byte)((ab_ + (ob - ab_) * opacity) * 255),
                        aRow[x].A);
                }
            }
        });
        return new TextureData(result);
    }

    private static float Ov(float a, float b) => a < 0.5f ? 2*a*b : 1-2*(1-a)*(1-b);
}
