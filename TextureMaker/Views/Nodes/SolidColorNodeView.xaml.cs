using System.Windows.Controls;
using System.Windows.Media;
using TextureMaker.Nodes.Sources;

namespace TextureMaker.Views.Nodes;

public partial class SolidColorNodeView : UserControl
{
    public SolidColorNodeView() => InitializeComponent();

    private void ColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not SolidColorNodeViewModel vm) return;

        // Simple hex input dialog
        var win = new ColorPickerWindow(vm.SelectedColor);
        if (win.ShowDialog() == true)
            vm.SelectedColor = win.PickedColor;
    }
}
