using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Templates*************//
//******************************//
public partial class Templates
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    private Guid? SelectedTemplateId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
    private string? PendingSectionElementId { get; set; }
    private bool IsDirectoryPanelExpanded { get; set; }

    private LaborTemplate? SelectedTemplate =>
        SelectedTemplateId is null
            ? null
            : Store.Workspace.Templates.FirstOrDefault(template => template.Id == SelectedTemplateId.Value);

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
        SelectedTemplateId = Store.Workspace.Templates.FirstOrDefault()?.Id;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrWhiteSpace(PendingSectionElementId))
        {
            return;
        }

        var sectionElementId = PendingSectionElementId;
        PendingSectionElementId = null;
        await JsRuntime.InvokeVoidAsync("fyreWorksPageSectionNavigation.scrollToSection", sectionElementId);
    }

    private void SelectTemplate(Guid templateId)
    {
        SelectedTemplateId = templateId;
        CloseDirectoryPanel();
        StatusMessage = string.Empty;
    }

    private async Task CreateTemplateAsync()
    {
        var template = Store.CreateTemplate();
        SelectedTemplateId = template.Id;
        CloseDirectoryPanel();
        StatusMessage = StatusMessageFormatter.WithTimestamp("New template created.");
        await Store.SaveAsync();
    }

    private void ToggleDirectoryPanel() =>
        IsDirectoryPanelExpanded = !IsDirectoryPanelExpanded;

    private void CloseDirectoryPanel() =>
        IsDirectoryPanelExpanded = false;

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Template saved.");
    }

    private async Task SetDefaultTemplateAsync()
    {
        if (SelectedTemplate is null) return;
        Store.Workspace.Settings.DefaultTemplateId = SelectedTemplate.Id;
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Default template updated.");
    }

    private void AddRule()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        var rule = new LaborRule();
        SelectedTemplate.Rules.Add(rule);
        PendingSectionElementId = GetRuleElementId(rule.Id);
    }

    private static string GetRuleElementId(Guid ruleId) =>
        $"template-rule-{ruleId:N}";

    private void RemoveRule(Guid ruleId) => SelectedTemplate?.Rules.RemoveAll(rule => rule.Id == ruleId);
}
