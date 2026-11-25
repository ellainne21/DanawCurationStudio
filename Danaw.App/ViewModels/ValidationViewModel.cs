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
using Danaw.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

// One source in the validator's checklist: a "verified" checkbox plus cross-check notes.
public partial class SourceCheckViewModel : ViewModelBase
{
    private readonly Action _changed;

    public SourceCheckViewModel(
        int sourceId, string title, string detail, bool isVerified, string? notes, bool canEdit, Action changed)
    {
        SourceId = sourceId;
        Title = title;
        Detail = detail;
        CanEdit = canEdit;
        _changed = changed;
        _isVerified = isVerified;
        _notes = notes ?? string.Empty;
    }

    public int SourceId { get; }
    public string Title { get; }
    public string Detail { get; }
    public bool CanEdit { get; }

    [ObservableProperty] private bool _isVerified;
    [ObservableProperty] private string _notes;

    // Lets the window update "2 of 3 sources verified" as boxes are ticked.
    partial void OnIsVerifiedChanged(bool value) => _changed();
}

// One past decision shown in the history list.
public record DecisionHistoryRow(
    string DecisionText, IBrush DecisionBrush, string Validator, string DecidedText, string Comments);

public partial class ValidationViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;
    private readonly Reviewer _user;
    private readonly int _itemId;

    public ValidationViewModel(IServiceScopeFactory scopes, Reviewer user, int itemId)
    {
        _scopes = scopes;
        _user = user;
        _itemId = itemId;

        ErrorMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
        History.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoHistory));
    }

    // The window listens to this and closes itself. true = a decision was recorded.
    public event Action<bool>? CloseRequested;

    public ObservableCollection<SourceCheckViewModel> Sources { get; } = new();
    public ObservableCollection<DecisionHistoryRow> History { get; } = new();
    public ObservableCollection<string> ErrorMessages { get; } = new();

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _culturalDomain = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _submittedBy = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private IBrush _statusBrush = Brushes.Gray;
    [ObservableProperty] private string _verificationSummary = string.Empty;
    [ObservableProperty] private string _comments = string.Empty;
    [ObservableProperty] private string _errorTitle = string.Empty;
    [ObservableProperty] private string _readOnlyNote = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    private bool _canDecide;

    public bool IsReadOnly => !CanDecide;
    public bool HasErrors => ErrorMessages.Count > 0;
    public bool HasNoHistory => History.Count == 0;

    public async Task LoadAsync()
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var items = scope.ServiceProvider.GetRequiredService<IContentItemRepository>();
            var records = scope.ServiceProvider.GetRequiredService<IValidationRecordRepository>();

            var item = await items.GetWithSourcesAsync(_itemId);
            if (item is null)
            {
                ShowErrors("Cannot open item", new[] { "The content item could not be found." });
                return;
            }

            Title = item.Title;
            CategoryName = item.Category?.Name ?? "-";
            CulturalDomain = item.CulturalDomain ?? "-";
            Description = item.Description;
            SubmittedBy = item.CreatedBy?.FullName ?? "-";

            StatusText = item.Status == ContentStatus.NeedsRevision ? "Needs revision" : item.Status.ToString();
            StatusBrush = new SolidColorBrush(Color.Parse(item.Status switch
            {
                ContentStatus.Pending => "#B85042",
                ContentStatus.NeedsRevision => "#D9A441",
                ContentStatus.Validated => "#4A7C59",
                ContentStatus.Rejected => "#A93A3F",
                _ => "#6B5D54"
            }));

            // Only a Pending item can receive a decision; anything else is view-only.
            CanDecide = item.Status == ContentStatus.Pending;
            ReadOnlyNote = CanDecide
                ? string.Empty
                : $"This item is {StatusText}. No further decision can be recorded unless it is resubmitted.";

            foreach (var link in item.Sources.OrderBy(l => l.SourceId))
            {
                Sources.Add(new SourceCheckViewModel(
                    link.SourceId,
                    link.Source?.Title ?? $"Source {link.SourceId}",
                    BuildDetail(link.Source),
                    link.IsVerified,
                    link.CrossCheckNotes,
                    CanDecide,
                    UpdateSummary));
            }
            UpdateSummary();

            foreach (var r in await records.ListForItemAsync(_itemId))
            {
                History.Add(new DecisionHistoryRow(
                    r.Decision == ValidationDecision.RevisionRequested ? "Revision requested" : r.Decision.ToString(),
                    new SolidColorBrush(Color.Parse(r.Decision switch
                    {
                        ValidationDecision.Approved => "#4A7C59",
                        ValidationDecision.RevisionRequested => "#B07D1A",
                        _ => "#A93A3F"
                    })),
                    r.Reviewer?.FullName ?? "-",
                    r.DecidedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    string.IsNullOrWhiteSpace(r.Comments) ? "(no comment)" : r.Comments));
            }
        }
        catch (Exception ex)
        {
            ShowErrors("Cannot open item", new[] { $"The item could not be loaded: {ex.Message}" });
        }
    }

    private static string BuildDetail(Source? s)
    {
        if (s is null) return string.Empty;
        var parts = new List<string> { s.Reference };
        if (!string.IsNullOrWhiteSpace(s.Publisher)) parts.Add(s.Publisher);
        if (!string.IsNullOrWhiteSpace(s.SourceType)) parts.Add(s.SourceType);
        if (s.YearPublished is int y) parts.Add(y.ToString());
        return string.Join(" | ", parts);
    }

    private void UpdateSummary()
    {
        var verified = Sources.Count(s => s.IsVerified);
        VerificationSummary = $"{verified} of {Sources.Count} sources verified";
    }

    [RelayCommand]
    private Task ApproveAsync() => DecideAsync(ValidationDecision.Approved, "Cannot approve");

    [RelayCommand]
    private Task RequestRevisionAsync() => DecideAsync(ValidationDecision.RevisionRequested, "Cannot request revision");

    [RelayCommand]
    private Task RejectAsync() => DecideAsync(ValidationDecision.Rejected, "Cannot reject");

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(false);

    private async Task DecideAsync(ValidationDecision decision, string errorTitle)
    {
        if (IsBusy || !CanDecide) return;
        ErrorMessages.Clear();
        ErrorTitle = string.Empty;
        IsBusy = true;

        try
        {
            var checks = Sources
                .Select(s => new SourceCheckInput(
                    s.SourceId, s.IsVerified, string.IsNullOrWhiteSpace(s.Notes) ? null : s.Notes))
                .ToList();
            var input = new DecisionInput(_itemId, decision, Comments, checks);

            // A fresh scope means a fresh DbContext, so a failed save never leaves stale state behind.
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IValidationWorkflowService>();
            var result = await service.RecordDecisionAsync(_user.ReviewerId, input);

            if (!result.Success)
            {
                ShowErrors(errorTitle, result.Errors);
                return;
            }

            CloseRequested?.Invoke(true);
        }
        catch (Exception)
        {
            ShowErrors("Could not save",
                new[] { "The decision could not be saved because of a database problem. No changes were made. Please try again." });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowErrors(string title, IEnumerable<string> messages)
    {
        ErrorTitle = title;
        ErrorMessages.Clear();
        foreach (var m in messages) ErrorMessages.Add("• " + m);
    }
}