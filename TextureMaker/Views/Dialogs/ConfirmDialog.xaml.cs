using System.Windows;
using System.Windows.Input;

namespace TextureMaker.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog(Window? owner, string title, string message)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;
        TitleText.Text   = title;
        MessageText.Text = message;
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Confirmed = false; Close(); }
    }
}
