namespace FyreWorksAI.Shared.Core.Exports;

//******************************//
//*** Document Branding Info ***//
//******************************//
internal sealed class DocumentBrandingProfile
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyLicenseNumber { get; init; } = string.Empty;
    public string CompanyAddress { get; init; } = string.Empty;
    public string CompanyPhoneNumber { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string LogoDataUri { get; init; } = string.Empty;
}
