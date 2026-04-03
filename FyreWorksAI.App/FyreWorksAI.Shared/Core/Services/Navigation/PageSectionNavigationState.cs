namespace FyreWorksAI.Shared.Core.Services.Navigation;

//******************************//
//**** Page Section State ******//
//******************************//

public sealed class PageSectionNavigationState
{
    private Func<PageSectionNavigationItem, Task>? SectionNavigationRequested { get; set; }
    private string? OwnerKey { get; set; }

    public IReadOnlyList<PageSectionNavigationItem> NavigationItems { get; private set; } = [];
    public string? ActiveSectionId { get; private set; }
    public string? ContextLabel { get; private set; }
    public string? ContextValue { get; private set; }
    public bool HasItems => NavigationItems.Count > 0;
    public bool HasContext => !string.IsNullOrWhiteSpace(ContextValue);

    public event Action? StateChanged;

    public void Configure(
        string ownerKey,
        IReadOnlyList<PageSectionNavigationItem> navigationItems,
        Func<PageSectionNavigationItem, Task> sectionNavigationRequested,
        string? activeSectionId = null,
        string? contextLabel = null,
        string? contextValue = null)
    {
        OwnerKey = ownerKey;
        NavigationItems = navigationItems;
        SectionNavigationRequested = sectionNavigationRequested;
        ActiveSectionId = activeSectionId;
        ContextLabel = contextLabel;
        ContextValue = contextValue;
        NotifyStateChanged();
    }

    public async Task NavigateToSectionAsync(PageSectionNavigationItem item)
    {
        if (!NavigationItems.Contains(item))
        {
            return;
        }

        ActiveSectionId = item.SectionId;
        NotifyStateChanged();

        if (SectionNavigationRequested is not null)
        {
            await SectionNavigationRequested(item);
        }
    }

    public void SetActiveSection(string ownerKey, string? sectionId)
    {
        if (!string.Equals(OwnerKey, ownerKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ActiveSectionId = sectionId;
        NotifyStateChanged();
    }

    public void Clear(string ownerKey)
    {
        if (!string.Equals(OwnerKey, ownerKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        OwnerKey = null;
        ActiveSectionId = null;
        ContextLabel = null;
        ContextValue = null;
        NavigationItems = [];
        SectionNavigationRequested = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() =>
        StateChanged?.Invoke();
}
