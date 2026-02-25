using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp.Processing;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using ISPoint = SixLabors.ImageSharp.Point;

namespace TextureMaker.Nodes.Combine;

public class CompositeNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputBase    { get; } = new();
    public InputPin<TextureData> InputOverlay { get; } = new();

    private int _offsetX;
    public int OffsetX { get => _offsetX; set => this.RaiseAndSetIfChanged(ref _offsetX, value); }

    private int _offsetY;
    public int OffsetY { get => _offsetY; set => this.RaiseAndSetIfChanged(ref _offsetY, value); }

    private float _opacity = 1f;
    public float Opacity { get => _opacity; set => this.RaiseAndSetIfChanged(ref _opacity, value); }

    public CompositeNodeViewModel() : base("Composite")
    {
        RegisterInput(InputBase,    "Image");
        RegisterInput(InputOverlay, "Image Layer");

        Output.Value = Observable
            .CombineLatest(
                InputBase.Value, InputOverlay.Value,
                this.WhenAnyValue(x => x.OffsetX, x => x.OffsetY, x => x.Opacity),
                (b, o, v) => (b, o, v.Item1, v.Item2, v.Item3))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(a => Observable.Start(
                () => Apply(a.b, a.o, a.Item3, a.Item4, a.Item5),
                RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? baseImg, TextureData? overlay, int ox, int oy, float opacity)
    {
        if (baseImg == null) return overlay?.Clone();
        var result = baseImg.Clone();
        if (overlay == null) return result;
        try
        {
            result.Image.Mutate(ctx =>
                ctx.DrawImage(overlay.Image, new ISPoint(ox, oy), Math.Clamp(opacity, 0f, 1f)));
        }
        catch { /* overlay fully outside base bounds */ }
        return result;
    }
}
