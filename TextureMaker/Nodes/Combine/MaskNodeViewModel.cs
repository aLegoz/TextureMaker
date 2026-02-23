using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Combine;

public class MaskNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();
    public InputPin<TextureData> InputMask { get; } = new();

    public MaskNodeViewModel() : base("Mask")
    {
        RegisterInput(InputTexture, "Texture");
        RegisterInput(InputMask, "Mask (R)");
        Output.Value = Observable
            .CombineLatest(InputTexture.Value, InputMask.Value, (tex, mask) => (tex, mask))
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(args => Observable.Start(() => Apply(args.tex, args.mask), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? texture, TextureData? mask)
    {
        if (texture == null) return null;
        if (mask == null) return texture.Clone();
        int w = Math.Min(texture.Image.Width, mask.Image.Width);
        int h = Math.Min(texture.Image.Height, mask.Image.Height);
        var result = new Image<Rgba32>(w, h);
        result.ProcessPixelRows(texture.Image, mask.Image, (rAcc, tAcc, mAcc) =>
        {
            for (int y = 0; y < h; y++)
            {
                var rRow = rAcc.GetRowSpan(y); var tRow = tAcc.GetRowSpan(y); var mRow = mAcc.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                    rRow[x] = new Rgba32(tRow[x].R, tRow[x].G, tRow[x].B, (byte)(tRow[x].A * mRow[x].R / 255f));
            }
        });
        return new TextureData(result);
    }
}
