using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

// One editable source-citation row in the editor.
public partial class SourceEntryViewModel : ViewModelBase
{
    private readonly Action<SourceEntryViewModel> _remove;

    public SourceEntryViewModel(Action<SourceEntryViewModel> remove) => _remove = remove;

    public IReadOnlyList<string> TypeOptions => ItemEditorViewModel.SourceTypes;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _sourceType;
    [ObservableProperty] private string _publisher = string.Empty;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _yearText = string.Empty;

    public bool IsBlank =>
        string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Publisher) &&
        string.IsNullOrWhiteSpace(Reference) && string.IsNullOrWhiteSpace(YearText) &&
        string.IsNullOrWhiteSpace(SourceType);

    [RelayCommand]
    private void Remove() => _remove(this);
}

public partial class ItemEditorViewModel : ViewModelBase
{
    // The source kinds named in the midterm: institutional, academic, digital, community.
    public static readonly IReadOnlyList<string> SourceTypes =
        new[] { "Institutional", "Academic", "Digital", "Community" };

    private readonly IServiceScopeFactory _scopes;
    private readonly Reviewer _user;
    private int? _itemId; // null until the item has been saved once

    public ItemEditorViewModel(IServiceScopeFactory scopes, Reviewer user, int? itemId)
    {
        _scopes = scopes;
        _user = user;
        _itemId = itemId;
        WindowTitle = itemId is null ? "Content Item - New" : "Content Item - Edit";

        ErrorMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
        Sources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoSources));
    }

    // The window listens to this and closes itself. true = something was submitted.
    public event Action<bool>? CloseRequested;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<SourceEntryViewModel> Sources { get; } = new();
    public ObservableCollection<string> ErrorMessages { get; } = new();

    [ObservableProperty] private string _windowTitle = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _culturalDomain = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _errorTitle = string.Empty;
    [ObservableProperty] private string? _infoMessage;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRevisionNote))]
    private string? _revisionNote;

    public bool HasRevisionNote => !string.IsNullOrEmpty(RevisionNote);
    public bool HasErrors => ErrorMessages.Count > 0;
    public bool HasNoSources => Sources.Count == 0;

    public async Task LoadAsync()
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IContentItemRepository>();

            foreach (var c in await repo.ListCategoriesAsync())
                Categories.Add(c);

            if (_itemId is int id)
            {
                var item = await repo.GetWithSourcesAsync(id);
                if (item is null)
                {
                    ShowErrors("Cannot open item", new[] { "The content item could not be found." });
                    return;
                }

                Title = item.Title;
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == item.CategoryId);
                CulturalDomain = item.CulturalDomain ?? string.Empty;
                Description = item.Description;

                // If the validator sent this item back, show their latest comment.
                if (item.Status == ContentStatus.NeedsRevision)
                {
                    var records = scope.ServiceProvider.GetRequiredService<IValidationRecordRepository>();
                    var latest = (await records.ListForItemAsync(item.ContentItemId)).FirstOrDefault();
                    if (latest is not null)
                        RevisionNote = $"{latest.Reviewer?.FullName}: {latest.Comments}";
                }

                foreach (var link in item.Sources)
                {
                    var row = NewRow();
                    row.Title = link.Source?.Title ?? string.Empty;
                    row.SourceType = link.Source?.SourceType;
                    row.Publisher = link.Source?.Publisher ?? string.Empty;
                    row.Reference = link.Source?.Reference ?? string.Empty;
                    row.YearText = link.Source?.YearPublished?.ToString() ?? string.Empty;
                    Sources.Add(row);
                }
            }

            if (Sources.Count == 0) Sources.Add(NewRow()); // start with one empty row
        }
        catch (Exception ex)
        {
            ShowErrors("Cannot open item", new[] { $"The item could not be loaded: {ex.Message}" });
        }
    }

    private SourceEntryViewModel NewRow() => new(RemoveRow);

    private void RemoveRow(SourceEntryViewModel row)
    {
        Sources.Remove(row);
        InfoMessage = null;
    }

    [RelayCommand]
    private void AddSource() => Sources.Add(NewRow());

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        if (await SaveAsync(submit: false))
            InfoMessage = "Draft saved.";
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (await SaveAsync(submit: true))
            CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private async Task<bool> SaveAsync(bool submit)
    {
        if (IsBusy) return false;
        ClearMessages();

        // Check what only the screen can check (a year typed as text).
        if (!TryBuildInput(out var input)) return false;

        IsBusy = true;
        try
        {
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IContentCurationService>();

            var result = submit
                ? await service.SubmitAsync(_user.ReviewerId, input)
                : await service.SaveDraftAsync(_user.ReviewerId, input);

            if (!result.Success)
            {
                ShowErrors(submit ? "Cannot submit for review" : "Cannot save draft", result.Errors);
                return false;
            }

            _itemId = result.Value; // later saves update this item instead of creating a duplicate
            return true;
        }
        catch (Exception)
        {
            ShowErrors("Could not save",
                new[] { "The item could not be saved because of a database problem. No changes were made. Please try again." });
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryBuildInput(out ContentItemInput input)
    {
        var errors = new List<string>();
        var sources = new List<SourceInput>();
        var number = 0;

        foreach (var row in Sources)
        {
            if (row.IsBlank) continue; // an untouched row is simply ignored
            number++;

            int? year = null;
            if (!string.IsNullOrWhiteSpace(row.YearText))
            {
                if (int.TryParse(row.YearText.Trim(), out var y) && y is >= 1000 and <= 2100)
                    year = y;
                else
                    errors.Add($"Source {number}: year must be a number such as 2023.");
            }

            sources.Add(new SourceInput(row.Title, row.SourceType, row.Publisher, row.Reference, year));
        }

        input = new ContentItemInput(
            _itemId, SelectedCategory?.CategoryId ?? 0, Title, CulturalDomain, Description, sources);

        if (errors.Count > 0)
        {
            ShowErrors("Please correct the entries", errors);
            return false;
        }
        return true;
    }

    private void ShowErrors(string title, IEnumerable<string> messages)
    {
        ErrorTitle = title;
        ErrorMessages.Clear();
        foreach (var m in messages) ErrorMessages.Add("• " + m);
    }

    private void ClearMessages()
    {
        ErrorMessages.Clear();
        ErrorTitle = string.Empty;
        InfoMessage = null;
    }
}