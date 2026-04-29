namespace FyreWorksAI.Shared.Core.Exports;

//******************************//
//*** Proposal Branding Info ***//
//******************************//
internal sealed class ProposalBrandingProfile
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyLicenseNumber { get; init; } = string.Empty;
    public string CompanyAddress { get; init; } = string.Empty;
    public string CompanyPhoneNumber { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string ProposalLogoDataUri { get; init; } = string.Empty;
}
