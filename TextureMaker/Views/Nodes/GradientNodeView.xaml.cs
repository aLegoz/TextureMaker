using System.Windows.Controls;
using System.Windows.Media;
using TextureMaker.Nodes.Sources;

namespace TextureMaker.Views.Nodes;

public partial class GradientNodeView : UserControl
{
    public GradientNodeView() => InitializeComponent();

    private void ColorA_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not GradientNodeViewModel vm) return;
        var win = new ColorPickerWindow(vm.ColorA);
        if (win.ShowDialog() == true) vm.ColorA = win.PickedColor;
    }

    private void ColorB_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not GradientNodeViewModel vm) return;
        var win = new ColorPickerWindow(vm.ColorB);
        if (win.ShowDialog() == true) vm.ColorB = win.PickedColor;
    }
}
