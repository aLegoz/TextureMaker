using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Special;

public class LevelsNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    private float _inBlack = 0f;
    public float InBlack { get => _inBlack; set => this.RaiseAndSetIfChanged(ref _inBlack, value); }

    private float _inWhite = 1f;
    public float InWhite { get => _inWhite; set => this.RaiseAndSetIfChanged(ref _inWhite, value); }

    private float _gamma = 1f;
    public float Gamma { get => _gamma; set => this.RaiseAndSetIfChanged(ref _gamma, value); }

    private float _outBlack = 0f;
    public float OutBlack { get => _outBlack; set => this.RaiseAndSetIfChanged(ref _outBlack, value); }

    private float _outWhite = 1f;
    public float OutWhite { get => _outWhite; set => this.RaiseAndSetIfChanged(ref _outWhite, value); }

    public LevelsNodeViewModel() : base("Levels")
    {
        RegisterInput(InputTexture, "Input");
        Output.Value = Observable.CombineLatest(
                InputTexture.Value,
                this.WhenAnyValue(x => x.InBlack, x => x.InWhite, x => x.Gamma, x => x.OutBlack, x => x.OutWhite),
                (tex, v) => (tex, v.Item1, v.Item2, v.Item3, v.Item4, v.Item5))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(a => Observable.Start(() => Apply(a.tex, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? input, float inBlack, float inWhite, float gamma, float outBlack, float outWhite)
    {
        if (input == null) return null;
        var clone = input.Clone();
        float inRange = Math.Max(0.001f, inWhite - inBlack);
        float outRange = outWhite - outBlack;
        float invGamma = gamma <= 0 ? 1f : 1f / gamma;

        clone.Image.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < clone.Image.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < clone.Image.Width; x++)
                {
                    float Map(float v)
                    {
                        float t = Math.Clamp((v - inBlack) / inRange, 0f, 1f);
                        t = MathF.Pow(t, invGamma);
                        return Math.Clamp(outBlack + t * outRange, 0f, 1f);
                    }
                    row[x] = new Rgba32(
                        (byte)(Map(row[x].R / 255f) * 255),
                        (byte)(Map(row[x].G / 255f) * 255),
                        (byte)(Map(row[x].B / 255f) * 255),
                        row[x].A);
                }
            }
        });
        return clone;
    }
}
