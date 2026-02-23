using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp.Processing;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Filters;

public class BlurNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    private float _sigma = 3f;
    public float Sigma { get => _sigma; set => this.RaiseAndSetIfChanged(ref _sigma, value); }

    public BlurNodeViewModel() : base("Blur")
    {
        RegisterInput(InputTexture, "Input");
        Output.Value = Observable
            .CombineLatest(InputTexture.Value, this.WhenAnyValue(x => x.Sigma), (tex, s) => (tex, s))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => ApplyBlur(args.tex, args.s), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? ApplyBlur(TextureData? input, float sigma)
    {
        if (input == null) return null;
        var clone = input.Clone();
        // Kernel radius ≈ sigma*3; image must be larger than the kernel in both dims
        int minDim = Math.Min(clone.Image.Width, clone.Image.Height);
        float safeSigma = Math.Min(Math.Max(0.1f, sigma), (minDim / 2 - 1) / 3f);
        if (safeSigma < 0.1f) return clone;
        clone.Image.Mutate(ctx => ctx.GaussianBlur(safeSigma));
        return clone;
    }
}
