using System.Windows;
using System.Windows.Input;

namespace TextureMaker.Views.Controls;

public partial class PreviewWindow : Window
{
    public event Action? DockRequested;

    public PreviewWindow() => InitializeComponent();

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void DockButton_Click(object sender, RoutedEventArgs e)
        => DockRequested?.Invoke();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
