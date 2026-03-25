using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Home******************//
//******************************//
public partial class Home
{

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
    }

    private void OpenBid(Guid bidId) => NavigationManager.NavigateTo($"/bids?selected={bidId}");
    private void OpenJob(Guid jobId) => NavigationManager.NavigateTo($"/jobs?selected={jobId}");
    private void OpenService(Guid agreementId) => NavigationManager.NavigateTo($"/service?selected={agreementId}");
}
