using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Templates*************//
//******************************//
public partial class Templates
{

    private Guid? SelectedTemplateId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
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
        StatusMessage = "New template created.";
        await Store.SaveAsync();
    }

    private void ToggleDirectoryPanel() =>
        IsDirectoryPanelExpanded = !IsDirectoryPanelExpanded;

    private void CloseDirectoryPanel() =>
        IsDirectoryPanelExpanded = false;

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = "Template saved.";
    }

    private async Task SetDefaultTemplateAsync()
    {
        if (SelectedTemplate is null) return;
        Store.Workspace.Settings.DefaultTemplateId = SelectedTemplate.Id;
        await Store.SaveAsync();
        StatusMessage = "Default template updated.";
    }

    private void AddRule() => SelectedTemplate?.Rules.Add(new LaborRule());
    private void RemoveRule(Guid ruleId) => SelectedTemplate?.Rules.RemoveAll(rule => rule.Id == ruleId);
}
