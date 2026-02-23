using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using TextureMaker.Nodes.Output;

namespace TextureMaker.Views.Nodes;

public partial class SaveNodeView : UserControl
{
    public SaveNodeView() => InitializeComponent();

    // Save directly to root + filename (no dialog)
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SaveNodeViewModel vm || vm.LastTexture == null)
        {
            MessageBox.Show("No texture to save.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var fullPath = vm.ResolveFullPath();
        if (fullPath == null)
        {
            MessageBox.Show(
                "Set a filename (and connect a Root Folder for relative paths).",
                "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DoSave(vm, fullPath);
    }

    // Save via dialog, defaulting to root folder if connected
    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SaveNodeViewModel vm || vm.LastTexture == null)
        {
            MessageBox.Show("No texture to save.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp|TIFF|*.tiff",
            DefaultExt = ".png",
            FileName = Path.GetFileName(vm.FileName) is { Length: > 0 } fn ? fn : "texture",
            InitialDirectory = vm.CurrentRoot ?? ""
        };
        if (dlg.ShowDialog() != true) return;

        // If saved inside root, update FileName to relative path
        if (!string.IsNullOrEmpty(vm.CurrentRoot) &&
            dlg.FileName.StartsWith(vm.CurrentRoot, StringComparison.OrdinalIgnoreCase))
            vm.FileName = Path.GetRelativePath(vm.CurrentRoot, dlg.FileName);
        else
            vm.FileName = dlg.FileName;

        DoSave(vm, dlg.FileName);
    }

    private static void DoSave(SaveNodeViewModel vm, string fullPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            IImageEncoder encoder = ext switch
            {
                ".jpg" or ".jpeg" => new JpegEncoder(),
                ".bmp"            => new BmpEncoder(),
                ".tiff" or ".tif" => new TiffEncoder(),
                _                 => new PngEncoder()
            };
            using var stream = File.OpenWrite(fullPath);
            vm.LastTexture!.Image.Save(stream, encoder);
            MessageBox.Show($"Saved:\n{fullPath}", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
