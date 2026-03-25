using Microsoft.AspNetCore.Components;

namespace FyreWorksAI.Shared.Layout;

//******************************//
//****** Main Layout ***********//
//******************************//

public partial class MainLayout : IDisposable
{
    [Inject]
    private PageSectionNavigationState PageSectionNavigationState { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageSectionNavigationState.StateChanged += OnPageSectionNavigationStateChanged;
    }

    private Task NavigateToSectionAsync(PageSectionNavigationItem item) =>
        PageSectionNavigationState.NavigateToSectionAsync(item);

    private void OnPageSectionNavigationStateChanged() =>
        _ = InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        PageSectionNavigationState.StateChanged -= OnPageSectionNavigationStateChanged;
    }
}
