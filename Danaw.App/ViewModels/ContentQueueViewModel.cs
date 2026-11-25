using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

// One entry in the status dropdown. A null Value means "show every status".
public record StatusFilterOption(string Label, ContentStatus? Value);

// A flat, display-ready copy of a content item for one row of the list.
public record ContentItemRow(
    int Id, string Title, string Category, ContentStatus Status, string CreatedBy, DateTime CreatedAt)
{
    public string StatusText => Status switch
    {
        ContentStatus.Pending => "Pending",
        ContentStatus.NeedsRevision => "Needs revision",
        _ => Status.ToString()
    };

    // Dates are stored in UTC; show them in the user's local time.
    public string CreatedText => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public partial class ContentQueueViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;
    private int _refreshVersion;

    public ContentQueueViewModel(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        StatusOptions = new List<StatusFilterOption>
        {
            new("All statuses", null),
            new("Draft", ContentStatus.Draft),
            new("Pending", ContentStatus.Pending),
            new("Needs revision", ContentStatus.NeedsRevision),
            new("Validated", ContentStatus.Validated),
            new("Rejected", ContentStatus.Rejected)
        };
        _selectedStatus = StatusOptions[0];
    }

    public IReadOnlyList<StatusFilterOption> StatusOptions { get; }
    public ObservableCollection<ContentItemRow> Items { get; } = new();

    [ObservableProperty] private StatusFilterOption _selectedStatus;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ContentItemRow? _selectedItem;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string? _errorMessage;

    // These run automatically whenever the filter or the search box changes.
    partial void OnSelectedStatusChanged(StatusFilterOption value) => _ = RefreshAsync();
    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        // If the user types quickly, several refreshes overlap.
        // Only the newest one is allowed to update the list.
        var version = ++_refreshVersion;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IContentItemRepository>();
            var items = await repo.ListAsync(SelectedStatus.Value, SearchText);

            if (version != _refreshVersion) return;

            Items.Clear();
            foreach (var i in items)
            {
                Items.Add(new ContentItemRow(
                    i.ContentItemId,
                    i.Title,
                    i.Category?.Name ?? "-",
                    i.Status,
                    i.CreatedBy?.FullName ?? "-",
                    i.CreatedAt));
            }

            IsEmpty = Items.Count == 0;
            Summary = Items.Count == 1 ? "1 item" : $"{Items.Count} items";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"The queue could not be loaded: {ex.Message}";
        }
    }
}