using System.Net;
using System.Text;

namespace FyreWorksAI.Shared.Core.Exports;

//******************************//
//*** Bid Proposal Document ****//
//******************************//
internal static class BidProposalHtmlDocumentBuilder
{
    //******************************//
    //******** HTML Export *********//
    //******************************//
    public static string BuildDocument(BidRecord bid, ClientRecord? client, ProposalBrandingProfile brandingProfile)
    {
        var projectHeading = string.IsNullOrWhiteSpace(bid.ProjectName)
            ? "Fire Alarm Proposal"
            : bid.ProjectName.Trim();
        var proposalTitle = string.IsNullOrWhiteSpace(bid.ProjectName)
            ? "Fire Alarm Proposal"
            : $"{bid.ProjectName.Trim()} Proposal";
        var proposalAmount = EstimateMath.GetCurrency(EstimateMath.GetBidAdjustedRevenue(bid));
        var coverBrandingMarkup = BuildCoverBrandingMarkup(brandingProfile);
        var preparedForMarkup = BuildPreparedForMarkup(client);
        var projectDetailsMarkup = BuildProjectDetailsMarkup(bid);
        var scopeReferenceMarkup = BuildScopeReferenceMarkup(bid);
        var scopeMarkup = BuildRichTextMarkup(GetProposalSummary(bid));
        var exclusionsMarkup = BuildOptionalSectionMarkup("Exclusions", bid.Exclusions);
        var closingMarkup = BuildRichTextMarkup(GetProposalClosing(bid));
        var closingCompanyInfoMarkup = BuildClosingCompanyInfoMarkup(brandingProfile);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{{Encode(proposalTitle)}}</title>
    <style>
        :root {
            color-scheme: light;
            --proposal-ink: #18212b;
            --proposal-muted: #5e6b78;
            --proposal-line: #d7dee7;
            --proposal-surface: #f4f7fa;
            --proposal-page: #ffffff;
            --proposal-accent: #b64714;
            --proposal-accent-soft: #fff3eb;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 24px;
            background: linear-gradient(180deg, #e8eef4 0%, #f6f8fb 100%);
            color: var(--proposal-ink);
            font: 15px/1.48 "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
        }

        .proposal-document {
            display: grid;
            gap: 24px;
        }

        .proposal-page {
            width: min(8.5in, 100%);
            min-height: 11in;
            margin: 0 auto;
            background: var(--proposal-page);
            border: 1px solid var(--proposal-line);
            box-shadow: 0 24px 60px rgba(24, 33, 43, 0.16);
            display: flex;
        }

        .proposal-body {
            display: flex;
            flex-direction: column;
            flex: 1 1 auto;
            min-height: 11in;
            padding: 0.62in 0.68in 0.56in;
        }

        .proposal-page-content {
            flex: 1 1 auto;
        }

        .proposal-cover-content {
            display: grid;
            align-content: start;
        }

        .proposal-branding-panel {
            display: grid;
            gap: 8px;
            margin-bottom: 18px;
        }

        .proposal-logo-image {
            display: block;
            width: auto;
            max-width: 100%;
            max-height: 78px;
            object-fit: contain;
            object-position: left center;
        }

        .proposal-branding-company-name {
            display: block;
            font-size: 0.95rem;
            font-weight: 700;
        }

        .proposal-branding-details {
            margin: 0;
            color: var(--proposal-muted);
            font-size: 0.82rem;
            line-height: 1.45;
        }

        .proposal-branding-divider {
            padding: 0 0.34rem;
        }

        .proposal-header {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 22px;
            align-items: start;
            padding-bottom: 18px;
            border-bottom: 2px solid var(--proposal-ink);
        }

        .proposal-eyebrow {
            margin: 0 0 6px;
            color: var(--proposal-accent);
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.16em;
            text-transform: uppercase;
        }

        .proposal-title {
            margin: 0;
            font-size: 1.82rem;
            line-height: 1.15;
        }

        .proposal-reference {
            margin: 8px 0 0;
            color: var(--proposal-muted);
            font-size: 0.88rem;
            line-height: 1.42;
        }

        .proposal-amount-card {
            min-width: 214px;
            padding: 15px 17px;
            background: var(--proposal-accent-soft);
            border: 1px solid #f1c7b4;
        }

        .proposal-amount-label {
            display: block;
            margin-bottom: 6px;
            color: var(--proposal-muted);
            font-size: 0.76rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .proposal-amount-value {
            display: block;
            color: var(--proposal-accent);
            font-size: 1.8rem;
            font-weight: 700;
            line-height: 1.1;
        }

        .proposal-amount-date {
            margin: 8px 0 0;
            color: var(--proposal-muted);
            font-size: 0.84rem;
        }

        .proposal-summary-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 14px;
            margin-top: 18px;
        }

        .proposal-card {
            padding: 15px 17px;
            background: var(--proposal-surface);
            border: 1px solid var(--proposal-line);
        }

        .proposal-card-title {
            margin: 0 0 11px;
            font-size: 0.8rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .proposal-meta-list {
            display: grid;
            gap: 12px;
        }

        .proposal-meta-entry {
            display: grid;
            gap: 2px;
            align-items: start;
        }

        .proposal-meta-label {
            color: var(--proposal-muted);
            font-size: 0.76rem;
            font-weight: 600;
            letter-spacing: 0.04em;
            text-transform: uppercase;
        }

        .proposal-meta-value {
            min-width: 0;
            white-space: pre-wrap;
            word-break: break-word;
            font-size: 0.9rem;
            line-height: 1.38;
        }

        .proposal-detail-page-heading {
            margin-bottom: 16px;
            padding-bottom: 14px;
            border-bottom: 2px solid var(--proposal-ink);
        }

        .proposal-page-title {
            margin: 0;
            font-size: 1.42rem;
            line-height: 1.18;
        }

        .proposal-page-reference {
            margin: 8px 0 0;
            color: var(--proposal-muted);
            font-size: 0.88rem;
            line-height: 1.42;
        }

        .proposal-section {
            margin-top: 22px;
        }

        .proposal-section-first {
            margin-top: 0;
        }

        .proposal-section-title {
            margin: 0 0 11px;
            padding-bottom: 8px;
            border-bottom: 1px solid var(--proposal-line);
            font-size: 0.82rem;
            font-weight: 700;
            letter-spacing: 0.1em;
            text-transform: uppercase;
        }

        .proposal-copy {
            color: #23303a;
            font-size: 0.82rem;
            line-height: 1.45;
        }

        .proposal-copy p {
            margin: 0 0 10px;
        }

        .proposal-copy p:last-child {
            margin-bottom: 0;
        }

        .proposal-copy ul {
            margin: 0;
            padding-left: 18px;
        }

        .proposal-copy li + li {
            margin-top: 5px;
        }

        .proposal-company-block {
            margin-top: 28px;
            padding-top: 14px;
            border-top: 1px solid var(--proposal-line);
        }

        @media (max-width: 760px) {
            body {
                padding: 12px;
            }

            .proposal-page {
                min-height: auto;
            }

            .proposal-body {
                min-height: auto;
                padding: 24px;
            }

            .proposal-header,
            .proposal-summary-grid {
                grid-template-columns: minmax(0, 1fr);
            }

            .proposal-amount-card {
                min-width: 0;
            }
        }

        @media print {
            @page {
                size: letter;
                margin: 0;
            }

            html,
            body {
                margin: 0;
                padding: 0;
                background: #ffffff;
            }

            .proposal-document {
                display: block;
            }

            .proposal-page {
                display: block;
                width: 8.5in;
                min-height: 11in;
                overflow: hidden;
                margin: 0;
                border: none;
                box-shadow: none;
            }

            .proposal-cover-page {
                break-before: auto;
                page-break-before: auto;
            }

            .proposal-detail-page {
                break-before: page;
                page-break-before: always;
            }

            .proposal-body,
            .proposal-page-content,
            .proposal-cover-content {
                display: block;
            }

            .proposal-body {
                min-height: 11in;
                padding: 0.42in 0.5in 0.38in;
            }

            .proposal-page-content {
                flex: none;
            }

            .proposal-page,
            .proposal-page:not(:last-child) {
                break-after: auto;
                page-break-after: auto;
            }

            .proposal-header,
            .proposal-summary-grid {
                break-inside: avoid;
                page-break-inside: avoid;
            }

            .proposal-section,
            .proposal-company-block {
                break-inside: avoid;
                page-break-inside: avoid;
            }
        }
    </style>
</head>
<body>
    <div class="proposal-document">
        <main class="proposal-page proposal-cover-page">
            <div class="proposal-body">
                <div class="proposal-page-content proposal-cover-content">
                    {{coverBrandingMarkup}}

                    <header class="proposal-header">
                        <div>
                            <p class="proposal-eyebrow">Fire Alarm Proposal</p>
                            <h1 class="proposal-title">{{Encode(projectHeading)}}</h1>
                            <p class="proposal-reference">Proposal {{Encode(bid.BidNumber)}} for {{Encode(GetDisplayProjectAddress(bid.Site))}}</p>
                        </div>
                        <aside class="proposal-amount-card">
                            <span class="proposal-amount-label">Proposal Amount</span>
                            <strong class="proposal-amount-value">{{Encode(proposalAmount)}}</strong>
                            <p class="proposal-amount-date">Prepared {{Encode(DateTime.Now.ToString("D"))}}</p>
                        </aside>
                    </header>

                    <section class="proposal-summary-grid">
                        <article class="proposal-card">
                            <h2 class="proposal-card-title">Prepared For</h2>
                            {{preparedForMarkup}}
                        </article>
                        <article class="proposal-card">
                            <h2 class="proposal-card-title">Project Details</h2>
                            {{projectDetailsMarkup}}
                        </article>
                    </section>
                </div>
            </div>
        </main>

        <main class="proposal-page proposal-detail-page">
            <div class="proposal-body">
                <div class="proposal-page-content">
                    <header class="proposal-detail-page-heading">
                        <p class="proposal-eyebrow">Project Scope</p>
                        <h2 class="proposal-page-title">Scope of Work</h2>
                        <p class="proposal-page-reference">{{scopeReferenceMarkup}}</p>
                    </header>

                    <section class="proposal-section proposal-section-first">
                        <div class="proposal-copy">
                            {{scopeMarkup}}
                        </div>
                    </section>

                    {{exclusionsMarkup}}

                    <section class="proposal-section">
                        <h2 class="proposal-section-title">Proposal Closing</h2>
                        <div class="proposal-copy">
                            {{closingMarkup}}
                        </div>
                    </section>

                    {{closingCompanyInfoMarkup}}
                </div>
            </div>
        </main>
    </div>
</body>
</html>
""";
    }

    //******************************//
    //****** Section Builders ******//
    //******************************//
    private static string BuildPreparedForMarkup(ClientRecord? client)
    {
        var details = new List<(string Label, string Value)>();
        var clientName = client?.Name;
        var primaryContact = client?.PrimaryContact;
        var clientEmail = client?.Email;
        var clientPhone = client?.Phone;

        details.Add(("Client", string.IsNullOrWhiteSpace(clientName) ? "Client to be confirmed" : clientName.Trim()));

        if (!string.IsNullOrWhiteSpace(primaryContact))
        {
            details.Add(("Attention", primaryContact.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(clientEmail))
        {
            details.Add(("Email", clientEmail.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(clientPhone))
        {
            details.Add(("Phone", clientPhone.Trim()));
        }

        return BuildMetaListMarkup(details);
    }

    private static string BuildProjectDetailsMarkup(BidRecord bid)
    {
        var details = new List<(string Label, string Value)>
        {
            ("Proposal Number", string.IsNullOrWhiteSpace(bid.BidNumber) ? "Pending" : bid.BidNumber.Trim()),
            ("Project", string.IsNullOrWhiteSpace(bid.ProjectName) ? "Project to be confirmed" : bid.ProjectName.Trim()),
            ("Site", GetDisplaySiteName(bid.Site)),
            ("Address", GetDisplayProjectAddress(bid.Site))
        };

        return BuildMetaListMarkup(details);
    }

    private static string BuildMetaListMarkup(IEnumerable<(string Label, string Value)> details)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<div class="proposal-meta-list">""");

        foreach (var (label, value) in details)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.AppendLine($$"""
    <div class="proposal-meta-entry">
        <span class="proposal-meta-label">{{Encode(label)}}</span>
        <span class="proposal-meta-value">{{Encode(value)}}</span>
    </div>
""");
        }

        builder.Append("""</div>""");
        return builder.ToString();
    }

    private static string BuildOptionalSectionMarkup(string sectionTitle, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return $$"""
<section class="proposal-section">
    <h2 class="proposal-section-title">{{Encode(sectionTitle)}}</h2>
    <div class="proposal-copy">
        {{BuildRichTextMarkup(content)}}
    </div>
</section>
""";
    }

    private static string BuildScopeReferenceMarkup(BidRecord bid)
    {
        var siteName = GetDisplaySiteName(bid.Site);
        var projectAddress = GetDisplayProjectAddress(bid.Site);
        return string.IsNullOrWhiteSpace(projectAddress)
            ? Encode(siteName)
            : $"{Encode(siteName)} | {Encode(projectAddress)}";
    }

    private static string BuildCoverBrandingMarkup(ProposalBrandingProfile brandingProfile)
    {
        var hasLogo = !string.IsNullOrWhiteSpace(brandingProfile.ProposalLogoDataUri);
        var detailsMarkup = BuildBrandingDetailsMarkup(brandingProfile);

        if (hasLogo)
        {
            if (string.IsNullOrWhiteSpace(detailsMarkup))
            {
                return $$"""
<section class="proposal-branding-panel">
    <img class="proposal-logo-image" src="{{brandingProfile.ProposalLogoDataUri}}" alt="Company logo" />
</section>
""";
            }

            return $$"""
<section class="proposal-branding-panel">
    <img class="proposal-logo-image" src="{{brandingProfile.ProposalLogoDataUri}}" alt="Company logo" />
    {{detailsMarkup}}
</section>
""";
        }

        var companyName = string.IsNullOrWhiteSpace(brandingProfile.CompanyName)
            ? string.Empty
            : brandingProfile.CompanyName.Trim();

        if (string.IsNullOrWhiteSpace(companyName) && string.IsNullOrWhiteSpace(detailsMarkup))
        {
            return string.Empty;
        }

        return $$"""
<section class="proposal-branding-panel">
    {{BuildBrandingCompanyNameMarkup(companyName)}}
    {{detailsMarkup}}
</section>
""";
    }

    private static string BuildClosingCompanyInfoMarkup(ProposalBrandingProfile brandingProfile)
    {
        var companyName = string.IsNullOrWhiteSpace(brandingProfile.CompanyName)
            ? string.Empty
            : brandingProfile.CompanyName.Trim();
        var detailsMarkup = BuildBrandingDetailsMarkup(brandingProfile);
        var includeCompanyName = true;

        if ((!includeCompanyName || string.IsNullOrWhiteSpace(companyName)) && string.IsNullOrWhiteSpace(detailsMarkup))
        {
            return string.Empty;
        }

        return $$"""
<section class="proposal-company-block">
    {{(includeCompanyName ? BuildBrandingCompanyNameMarkup(companyName) : string.Empty)}}
    {{detailsMarkup}}
</section>
""";
    }

    private static string BuildBrandingCompanyNameMarkup(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return string.Empty;
        }

        return $$"""<strong class="proposal-branding-company-name">{{Encode(companyName.Trim())}}</strong>""";
    }

    private static string BuildBrandingDetailsMarkup(ProposalBrandingProfile brandingProfile)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyLicenseNumber))
        {
            details.Add($"License #{brandingProfile.CompanyLicenseNumber.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyAddress))
        {
            details.Add(brandingProfile.CompanyAddress.Trim());
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyPhoneNumber))
        {
            details.Add(brandingProfile.CompanyPhoneNumber.Trim());
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyEmail))
        {
            details.Add(brandingProfile.CompanyEmail.Trim());
        }

        if (details.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("""<p class="proposal-branding-details">""");

        for (var index = 0; index < details.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("""<span class="proposal-branding-divider">&bull;</span>""");
            }

            builder.Append(Encode(details[index]));
        }

        builder.Append("</p>");
        return builder.ToString();
    }

    //******************************//
    //******* Text Helpers *********//
    //******************************//
    private static string BuildRichTextMarkup(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return """<p>Content to be confirmed.</p>""";
        }

        var normalizedContent = content.Replace("\r\n", "\n").Trim();
        var paragraphs = normalizedContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                continue;
            }

            if (lines.All(IsBulletLine))
            {
                builder.AppendLine("<ul>");

                foreach (var line in lines)
                {
                    builder.AppendLine($"    <li>{Encode(RemoveBulletPrefix(line))}</li>");
                }

                builder.AppendLine("</ul>");
                continue;
            }

            builder.AppendLine($"""<p>{string.Join("<br />", lines.Select(Encode))}</p>""");
        }

        return builder.Length == 0
            ? """<p>Content to be confirmed.</p>"""
            : builder.ToString().Trim();
    }

    private static bool IsBulletLine(string line) =>
        line.StartsWith("- ", StringComparison.Ordinal) ||
        line.StartsWith("* ", StringComparison.Ordinal);

    private static string RemoveBulletPrefix(string line) =>
        line.Length > 2 ? line[2..].Trim() : line.Trim();

    private static string GetProposalSummary(BidRecord bid) =>
        string.IsNullOrWhiteSpace(bid.ProposalSummary)
            ? (!string.IsNullOrWhiteSpace(bid.Site.ScopeOfWork) ? bid.Site.ScopeOfWork.Trim() : "Scope to be finalized.")
            : bid.ProposalSummary.Trim();

    private static string GetProposalClosing(BidRecord bid) =>
        string.IsNullOrWhiteSpace(bid.ProposalClosing)
            ? "We appreciate the opportunity to provide this proposal and are ready to proceed upon approval."
            : bid.ProposalClosing.Trim();

    private static string GetDisplaySiteName(SiteInformation site) =>
        string.IsNullOrWhiteSpace(site.SiteName)
            ? "Site to be confirmed"
            : site.SiteName.Trim();

    private static string GetDisplayProjectAddress(SiteInformation site)
    {
        var addressLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(site.AddressLine1))
        {
            addressLines.Add(site.AddressLine1.Trim());
        }

        if (!string.IsNullOrWhiteSpace(site.AddressLine2))
        {
            addressLines.Add(site.AddressLine2.Trim());
        }

        var cityStatePostalParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(site.City))
        {
            cityStatePostalParts.Add(site.City.Trim());
        }

        if (!string.IsNullOrWhiteSpace(site.State))
        {
            cityStatePostalParts.Add(site.State.Trim());
        }

        var cityStatePostal = string.Join(", ", cityStatePostalParts);
        if (!string.IsNullOrWhiteSpace(site.PostalCode))
        {
            cityStatePostal = string.IsNullOrWhiteSpace(cityStatePostal)
                ? site.PostalCode.Trim()
                : $"{cityStatePostal} {site.PostalCode.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(cityStatePostal))
        {
            addressLines.Add(cityStatePostal);
        }

        return addressLines.Count == 0
            ? "Project address to be confirmed"
            : string.Join(", ", addressLines);
    }

    private static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}
