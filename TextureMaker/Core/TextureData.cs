using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TextureMaker.Core;

public sealed class TextureData : IDisposable
{
    public Image<Rgba32> Image { get; }

    public TextureData(Image<Rgba32> img) => Image = img;

    public TextureData Clone() => new(Image.Clone());

    public void Dispose() => Image.Dispose();
}
