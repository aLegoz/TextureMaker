using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TextureMaker.Nodes.Sources;

namespace TextureMaker.Views.Nodes;

public partial class RootFolderNodeView : UserControl
{
    public RootFolderNodeView() => InitializeComponent();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Root Folder" };
        if (dlg.ShowDialog() == true && DataContext is RootFolderNodeViewModel vm)
            vm.FolderPath = dlg.FolderName;
    }
}
