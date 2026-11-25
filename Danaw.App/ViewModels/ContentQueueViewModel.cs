using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

// One entry in the sidebar. A null Value means "show every status".
public record StatusFilterOption(string Label, ContentStatus? Value);

// A flat, display-ready copy of a content item for one row of the list.
public record ContentItemRow(
    int Id, string Title, string Category, ContentStatus Status,
    int CreatedById, string CreatedBy, DateTime CreatedAt)
{
    public string StatusText => Status switch
    {
        ContentStatus.Pending => "Pending",
        ContentStatus.NeedsRevision => "Needs rev.",
        _ => Status.ToString()
    };

    // Badge colors follow the midterm mockup (slides 5 and 6).
    public IBrush BadgeBrush => new SolidColorBrush(Color.Parse(Status switch
    {
        ContentStatus.Draft => "#6B5D54",
        ContentStatus.Pending => "#B85042",
        ContentStatus.NeedsRevision => "#D9A441",
        ContentStatus.Validated => "#4A7C59",
        ContentStatus.Rejected => "#A93A3F",
        _ => "#6B5D54"
    }));

    // Dates are stored in UTC; show them in the user's local time.
    public string CreatedText => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public partial class ContentQueueViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ReviewerRole _role;
    private int _refreshVersion;

    public ContentQueueViewModel(IServiceScopeFactory scopes, ReviewerRole role)
    {
        _scopes = scopes;
        _role = role;

        var options = new List<StatusFilterOption> { new("All Items", null) };
        if (role == ReviewerRole.Researcher)
            options.Add(new("Drafts", ContentStatus.Draft)); // validators never review unsubmitted work
        options.Add(new("Pending Check", ContentStatus.Pending));
        options.Add(new("Needs Revision", ContentStatus.NeedsRevision));
        options.Add(new("Validated", ContentStatus.Validated));
        options.Add(new("Rejected", ContentStatus.Rejected));
        StatusOptions = options;

        // A validator starts on the work that is waiting for them.
        _selectedStatus = role == ReviewerRole.Validator
            ? options.First(o => o.Value == ContentStatus.Pending)
            : options[0];
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
                if (_role == ReviewerRole.Validator && i.Status == ContentStatus.Draft)
                    continue;

                Items.Add(new ContentItemRow(
                    i.ContentItemId,
                    i.Title,
                    i.Category?.Name ?? "-",
                    i.Status,
                    i.CreatedByReviewerId,
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