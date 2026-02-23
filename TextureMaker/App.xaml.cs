using System.Windows;
using ReactiveUI;
using Splat;

namespace TextureMaker;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Locator.CurrentMutable.InitializeReactiveUI();
        base.OnStartup(e);
    }
}
