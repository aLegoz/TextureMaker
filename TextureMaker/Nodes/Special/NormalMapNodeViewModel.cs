using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Special;

public class NormalMapNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    private float _strength = 2f;
    public float Strength { get => _strength; set => this.RaiseAndSetIfChanged(ref _strength, value); }

    public NormalMapNodeViewModel() : base("Normal Map")
    {
        RegisterInput(InputTexture, "Image");
        Output.Value = Observable
            .CombineLatest(InputTexture.Value, this.WhenAnyValue(x => x.Strength), (tex, s) => (tex, s))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => Apply(args.tex, args.s), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? input, float strength)
    {
        if (input == null) return null;
        int w = input.Image.Width, h = input.Image.Height;
        float[,] height = new float[w, h];
        input.Image.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < h; y++) { var row = acc.GetRowSpan(y); for (int x = 0; x < w; x++) height[x, y] = row[x].R / 255f; }
        });
        var result = new Image<Rgba32>(w, h);
        result.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    int xm = Math.Max(0, x-1), xp = Math.Min(w-1, x+1);
                    int ym = Math.Max(0, y-1), yp = Math.Min(h-1, y+1);
                    float gx = (height[xp,ym]+2*height[xp,y]+height[xp,yp])-(height[xm,ym]+2*height[xm,y]+height[xm,yp]);
                    float gy = (height[xm,yp]+2*height[x,yp]+height[xp,yp])-(height[xm,ym]+2*height[x,ym]+height[xp,ym]);
                    float nx=-gx*strength, ny=-gy*strength, nz=1f;
                    float len = MathF.Sqrt(nx*nx+ny*ny+nz*nz);
                    nx/=len; ny/=len; nz/=len;
                    row[x] = new Rgba32((byte)((nx+1f)*127.5f),(byte)((ny+1f)*127.5f),(byte)((nz+1f)*127.5f),255);
                }
            }
        });
        return new TextureData(result);
    }
}
