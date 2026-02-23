using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Nodes.Base;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace TextureMaker.Nodes.Sources;

public class SolidColorNodeViewModel : TextureNodeViewModel
{
    private WpfColor _color = WpfColors.Gray;
    public WpfColor SelectedColor
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    private int _width = 512;
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private int _height = 512;
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    public SolidColorNodeViewModel() : base("Solid Color")
    {
        Output.Value = this.WhenAnyValue(x => x.SelectedColor, x => x.Width, x => x.Height)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => Generate(args.Item1, args.Item2, args.Item3),
                RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Generate(WpfColor c, int w, int h)
    {
        w = Math.Max(1, w); h = Math.Max(1, h);
        var img = new Image<Rgba32>(w, h, new Rgba32(c.R, c.G, c.B, c.A));
        return new TextureData(img);
    }
}
