using System.Windows.Controls;
using TextureMaker.Nodes.Sources;

namespace TextureMaker.Views.Nodes;

public partial class ColorNodeView : UserControl
{
    public ColorNodeView() => InitializeComponent();

    private void ColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not ColorNodeViewModel vm) return;
        var win = new ColorPickerWindow(vm.SelectedColor);
        if (win.ShowDialog() == true)
            vm.SelectedColor = win.PickedColor;
    }
}
