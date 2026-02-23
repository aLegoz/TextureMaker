using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Filters;

public class SharpenNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    private float _amount = 1f;
    public float Amount { get => _amount; set => this.RaiseAndSetIfChanged(ref _amount, value); }

    private float _radius = 1.5f;
    public float Radius { get => _radius; set => this.RaiseAndSetIfChanged(ref _radius, value); }

    public SharpenNodeViewModel() : base("Sharpen")
    {
        RegisterInput(InputTexture, "Input");
        Output.Value = Observable
            .CombineLatest(InputTexture.Value, this.WhenAnyValue(x => x.Amount, x => x.Radius),
                (tex, ar) => (tex, ar.Item1, ar.Item2))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => ApplySharpen(args.tex, args.Item2, args.Item3), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? ApplySharpen(TextureData? input, float amount, float radius)
    {
        if (input == null) return null;
        var original = input.Clone();
        using var blurred = input.Clone();
        int minDim = original.Image.Width < original.Image.Height ? original.Image.Width : original.Image.Height;
        float safeRadius = Math.Min(Math.Max(0.1f, radius), (minDim / 2 - 1) / 3f);
        if (safeRadius < 0.1f) return original;
        blurred.Image.Mutate(ctx => ctx.GaussianBlur(safeRadius));

        int w = original.Image.Width, h = original.Image.Height;
        original.Image.ProcessPixelRows(blurred.Image, (origAcc, blurAcc) =>
        {
            for (int y = 0; y < h; y++)
            {
                var origRow = origAcc.GetRowSpan(y);
                var blurRow = blurAcc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                    origRow[x] = new Rgba32(
                        ClampByte(origRow[x].R + amount * (origRow[x].R - blurRow[x].R)),
                        ClampByte(origRow[x].G + amount * (origRow[x].G - blurRow[x].G)),
                        ClampByte(origRow[x].B + amount * (origRow[x].B - blurRow[x].B)),
                        origRow[x].A);
            }
        });
        return original;
    }

    private static byte ClampByte(float v) => (byte)Math.Clamp((int)v, 0, 255);
}
