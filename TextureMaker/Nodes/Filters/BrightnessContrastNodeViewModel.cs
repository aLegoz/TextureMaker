using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp.Processing;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Filters;

public class BrightnessContrastNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    private float _brightness = 0f;
    public float Brightness { get => _brightness; set => this.RaiseAndSetIfChanged(ref _brightness, value); }

    private float _contrast = 0f;
    public float Contrast { get => _contrast; set => this.RaiseAndSetIfChanged(ref _contrast, value); }

    public BrightnessContrastNodeViewModel() : base("Brightness/Contrast")
    {
        RegisterInput(InputTexture, "Image");
        Output.Value = Observable
            .CombineLatest(InputTexture.Value, this.WhenAnyValue(x => x.Brightness, x => x.Contrast),
                (tex, bc) => (tex, bc.Item1, bc.Item2))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => Apply(args.tex, args.Item2, args.Item3), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? input, float brightness, float contrast)
    {
        if (input == null) return null;
        var clone = input.Clone();
        clone.Image.Mutate(ctx => ctx.Brightness(1f + brightness).Contrast(1f + contrast));
        return clone;
    }
}
