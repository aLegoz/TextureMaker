using System.Windows;

namespace TextureMaker.Views.Controls;

public partial class PreviewWindow : Window
{
    public event Action? DockRequested;

    public PreviewWindow() => InitializeComponent();

    private void DockButton_Click(object sender, RoutedEventArgs e)
        => DockRequested?.Invoke();
}
