using System.Windows;
using ReactiveUI;
using WpfColor = System.Windows.Media.Color;
using System.Windows.Media;

namespace TextureMaker.Graph;

public class CommentBlockViewModel : ReactiveObject
{
    private string _title = "Comment";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string _body = "";
    public string Body
    {
        get => _body;
        set => this.RaiseAndSetIfChanged(ref _body, value);
    }

    private WpfColor _headerColor = Color.FromRgb(60, 80, 100);
    public WpfColor HeaderColor
    {
        get => _headerColor;
        set => this.RaiseAndSetIfChanged(ref _headerColor, value);
    }

    private Point _position = new(50, 50);
    public Point Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
    }

    private double _width = 300;
    public double Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private double _height = 200;
    public double Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }
}
