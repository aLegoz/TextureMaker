using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TextureMaker.Core;

namespace TextureMaker.Views.Controls;

public partial class PreviewPanel : UserControl
{
    private IDisposable? _subscription;

    public PreviewPanel() => InitializeComponent();

    public void BindToOutput(IObservable<TextureData?> source)
    {
        _subscription?.Dispose();
        _subscription = source
            .ObserveOn(System.Reactive.Concurrency.TaskPoolScheduler.Default)
            .Select(tex =>
            {
                if (tex?.Image == null) return ((BitmapSource?)null, string.Empty);
                var bmp = ImageConverter.ToBitmapSource(tex.Image);
                var info = $"{tex.Image.Width} × {tex.Image.Height}";
                return (bmp, info);
            })
            .ObserveOn(ReactiveUI.RxApp.MainThreadScheduler)
            .Subscribe(result =>
            {
                PreviewImage.Source = result.Item1;
                InfoText.Text = string.IsNullOrEmpty(result.Item2) ? "–" : result.Item2;
                NoInputText.Visibility = result.Item1 == null ? Visibility.Visible : Visibility.Collapsed;
                UpdateScalingMode();
            });
    }

    public void ClearPreview()
    {
        _subscription?.Dispose();
        _subscription = null;
        PreviewImage.Source = null;
        InfoText.Text = "–";
        NoInputText.Visibility = Visibility.Visible;
    }

    // Re-evaluate when the panel is resized
    private void PreviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateScalingMode();

    private void UpdateScalingMode()
    {
        if (PreviewImage.Source is not BitmapSource bmp) return;

        // Pixel-perfect (NearestNeighbor) when image fits inside the panel without downscaling;
        // smooth interpolation (HighQuality) when the image is larger and must be shrunk.
        var mode = bmp.PixelWidth  <= PreviewImage.ActualWidth &&
                   bmp.PixelHeight <= PreviewImage.ActualHeight
            ? BitmapScalingMode.NearestNeighbor
            : BitmapScalingMode.HighQuality;

        RenderOptions.SetBitmapScalingMode(PreviewImage, mode);
    }
}
