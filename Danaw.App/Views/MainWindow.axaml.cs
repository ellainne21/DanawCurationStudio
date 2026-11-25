using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Danaw.App.ViewModels;

namespace Danaw.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void NewItem_Click(object? sender, RoutedEventArgs e)
        => await OpenEditorAsync(forSelected: false);

    private async void EditItem_Click(object? sender, RoutedEventArgs e)
        => await OpenEditorAsync(forSelected: true);

    private async Task OpenEditorAsync(bool forSelected)
    {
        if (DataContext is not MainViewModel main) return;

        try
        {
            ContentItemRow? row = null;
            if (forSelected)
            {
                row = main.Queue.SelectedItem;
                if (row is null)
                {
                    main.StatusMessage = "Select an item in the list first.";
                    return;
                }
            }

            var editor = await main.PrepareEditorAsync(row);
            if (editor is null) return;

            var window = new ItemEditorWindow { DataContext = editor };
            editor.CloseRequested += saved => window.Close(saved);
            await window.ShowDialog<bool?>(this);

            await main.Queue.RefreshAsync(); // show whatever was saved or submitted
        }
        catch (Exception ex)
        {
            main.StatusMessage = $"The editor could not be opened: {ex.Message}";
        }
    }
}