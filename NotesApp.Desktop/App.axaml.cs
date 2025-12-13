using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NotesApp.Desktop.Services;
using NotesApp.Desktop.ViewModels;

namespace NotesApp.Desktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var store = new JsonNoteStore(JsonNoteStore.ResolveFilePath());
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(store)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
