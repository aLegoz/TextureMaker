using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;
using TextureMaker.Nodes.Sources;

namespace TextureMaker.Views.Nodes;

public partial class ImageLoadNodeView : UserControl
{
    public ImageLoadNodeView() => InitializeComponent();

    private void Browse_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ImageLoadNodeViewModel vm) return;

        var dlg = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.tiff;*.webp|All files|*.*",
            InitialDirectory = vm.CurrentRoot ?? ""
        };
        if (dlg.ShowDialog() != true) return;

        // If root is connected and the selected file is inside it, store a relative path
        if (!string.IsNullOrEmpty(vm.CurrentRoot) &&
            dlg.FileName.StartsWith(vm.CurrentRoot, System.StringComparison.OrdinalIgnoreCase))
            vm.FilePath = Path.GetRelativePath(vm.CurrentRoot, dlg.FileName);
        else
            vm.FilePath = dlg.FileName;
    }
}
