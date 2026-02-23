using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TextureMaker.Core;

public static class ImageConverter
{
    /// <summary>
    /// Converts an ImageSharp Image&lt;Rgba32&gt; to a frozen WPF BitmapSource (Bgra32).
    /// Safe to pass across threads after Freeze().
    /// </summary>
    public static BitmapSource ToBitmapSource(Image<Rgba32> image)
    {
        int w = image.Width;
        int h = image.Height;
        int stride = w * 4;
        byte[] pixels = new byte[h * stride];

        using var bgra = image.CloneAs<Bgra32>();
        bgra.CopyPixelDataTo(pixels);

        var bmp = BitmapSource.Create(w, h, 96, 96,
            PixelFormats.Bgra32, null, pixels, stride);
        bmp.Freeze();
        return bmp;
    }
}
