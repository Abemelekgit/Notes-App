using Avalonia.Controls;
using Avalonia.Interactivity;
using NotesApp.Desktop.ViewModels;

namespace NotesApp.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    private async void OnSearch(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.SearchAsync();
        }
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    private async void OnAdd(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.AddAsync();
        }
    }

    private async void OnUpdate(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.UpdateAsync();
        }
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.DeleteAsync();
        }
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ClearForm();
        }
    }
}
