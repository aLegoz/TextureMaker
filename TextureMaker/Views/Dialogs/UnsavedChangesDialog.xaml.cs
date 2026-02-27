using System.Windows;
using System.Windows.Input;

namespace TextureMaker.Views.Dialogs;

public enum UnsavedResult { Save, Discard, Cancel }

public partial class UnsavedChangesDialog : Window
{
    public UnsavedResult Result { get; private set; } = UnsavedResult.Cancel;

    public UnsavedChangesDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedResult.Save;
        Close();
    }

    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedResult.Discard;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedResult.Cancel;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)  { Result = UnsavedResult.Cancel;  Close(); }
        if (e.Key == Key.Enter)   { Result = UnsavedResult.Save;     Close(); }
    }
}
