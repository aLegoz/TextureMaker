using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using WpfColor  = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace TextureMaker.Nodes.Sources;

public class SolidColorNodeViewModel : TextureNodeViewModel
{
    // Optional colour override from a connected Color node
    public InputPin<WpfColor> ColorInput { get; } = new();

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
        ColorInput.ViewModel.Name    = "Color";
        ColorInput.ViewModel.PinType = "color";
        AllPins.Insert(0, ColorInput.ViewModel); // input before output visually

        // Use pin colour when connected, own colour otherwise
        var effectiveColor = Observable.CombineLatest(
            ColorInput.Value,
            this.WhenAnyValue(x => x.SelectedColor),
            ColorInput.ViewModel.WhenAnyValue(x => x.IsConnected),
            (pin, own, connected) => connected ? pin : own);

        Output.Value = effectiveColor
            .CombineLatest(
                this.WhenAnyValue(x => x.Width, x => x.Height),
                (color, size) => (color, size.Item1, size.Item2))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(
                () => Generate(args.color, args.Item2, args.Item3),
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
