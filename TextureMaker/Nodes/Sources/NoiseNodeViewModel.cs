using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Nodes.Base;
using TextureMaker.Noise;

namespace TextureMaker.Nodes.Sources;

public class NoiseNodeViewModel : TextureNodeViewModel
{
    private float _scale = 4f;
    public float Scale { get => _scale; set => this.RaiseAndSetIfChanged(ref _scale, value); }

    private int _octaves = 4;
    public int Octaves { get => _octaves; set => this.RaiseAndSetIfChanged(ref _octaves, value); }

    private int _seed = 0;
    public int Seed { get => _seed; set => this.RaiseAndSetIfChanged(ref _seed, value); }

    private int _width = 512;
    public int Width { get => _width; set => this.RaiseAndSetIfChanged(ref _width, value); }

    private int _height = 512;
    public int Height { get => _height; set => this.RaiseAndSetIfChanged(ref _height, value); }

    public NoiseNodeViewModel() : base("Noise")
    {
        Output.Value = this.WhenAnyValue(x => x.Scale, x => x.Octaves, x => x.Seed, x => x.Width, x => x.Height)
            .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
            .Select(a => Observable.Start(() => Generate(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5),
                RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData Generate(float scale, int octaves, int seed, int w, int h)
    {
        w = Math.Max(1, w); h = Math.Max(1, h);
        var perlin = new PerlinNoise(seed);
        var img = new Image<Rgba32>(w, h);
        img.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    float v = perlin.Octave((float)x / w * scale, (float)y / h * scale, Math.Max(1, octaves));
                    byte b = (byte)(v * 255f);
                    row[x] = new Rgba32(b, b, b, 255);
                }
            }
        });
        return new TextureData(img);
    }
}
