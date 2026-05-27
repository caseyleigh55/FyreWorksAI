using System.Net;
using System.Text;

namespace FyreWorksAI.Shared.Core.Exports;

//******************************//
//*** Service Quote Document ***//
//******************************//
internal static class ServiceQuoteHtmlDocumentBuilder
{
    //******************************//
    //******** HTML Export *********//
    //******************************//

    public static string BuildDocument(ServiceAgreement agreement, ServiceQuoteRecord quote, ClientRecord? client, DocumentBrandingProfile brandingProfile)
    {
        var siteHeading = string.IsNullOrWhiteSpace(agreement.Site.SiteName)
            ? "Service Quote"
            : $"{agreement.Site.SiteName.Trim()} Service Quote";
        var quoteTotal = EstimateMath.GetCurrency(EstimateMath.GetServiceQuoteRevenue(quote));
        var brandingMarkup = BuildBrandingMarkup(brandingProfile);
        var preparedForMarkup = BuildPreparedForMarkup(client);
        var siteDetailsMarkup = BuildSiteDetailsMarkup(agreement, quote);
        var quoteItemsMarkup = BuildQuoteItemsMarkup(quote);
        var notesMarkup = BuildOptionalNotesMarkup(quote);
        var companyFooterMarkup = BuildCompanyFooterMarkup(brandingProfile);
        var statusLabel = string.IsNullOrWhiteSpace(quote.Status) ? "Draft" : quote.Status.Trim();
        var createdLabel = quote.CreatedOn.ToString("D");
        var acceptedLabel = quote.AcceptedOn?.ToString("D") ?? "Pending";
        var agreementLabel = string.IsNullOrWhiteSpace(agreement.AgreementNumber) ? "Service" : agreement.AgreementNumber.Trim();
        var titleLabel = string.IsNullOrWhiteSpace(quote.Title) ? "Service Quote" : quote.Title.Trim();

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{{Encode(titleLabel)}}</title>
    <style>
        :root {
            color-scheme: light;
            --quote-ink: #18212b;
            --quote-muted: #596875;
            --quote-line: #d5dee7;
            --quote-page: #ffffff;
            --quote-surface: #f4f7fa;
            --quote-accent: #b64714;
            --quote-accent-soft: #fff3eb;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 24px;
            background: linear-gradient(180deg, #e8eef4 0%, #f6f8fb 100%);
            color: var(--quote-ink);
            font: 15px/1.48 "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
        }

        .quote-document {
            width: min(8.5in, 100%);
            min-height: 11in;
            margin: 0 auto;
            background: var(--quote-page);
            border: 1px solid var(--quote-line);
            box-shadow: 0 24px 60px rgba(24, 33, 43, 0.16);
        }

        .quote-body {
            min-height: 11in;
            padding: 0.62in 0.68in 0.56in;
        }

        .quote-branding-panel {
            display: grid;
            gap: 8px;
            margin-bottom: 18px;
        }

        .quote-logo-image {
            display: block;
            width: auto;
            max-width: 100%;
            max-height: 78px;
            object-fit: contain;
            object-position: left center;
        }

        .quote-branding-company-name {
            display: block;
            font-size: 0.95rem;
            font-weight: 700;
        }

        .quote-branding-details {
            margin: 0;
            color: var(--quote-muted);
            font-size: 0.82rem;
            line-height: 1.45;
        }

        .quote-branding-divider {
            padding: 0 0.34rem;
        }

        .quote-header {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 22px;
            align-items: start;
            padding-bottom: 18px;
            border-bottom: 2px solid var(--quote-ink);
        }

        .quote-eyebrow {
            margin: 0 0 6px;
            color: var(--quote-accent);
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.16em;
            text-transform: uppercase;
        }

        .quote-title {
            margin: 0;
            font-size: 1.82rem;
            line-height: 1.15;
        }

        .quote-reference {
            margin: 8px 0 0;
            color: var(--quote-muted);
            font-size: 0.88rem;
            line-height: 1.42;
        }

        .quote-total-card {
            min-width: 214px;
            padding: 15px 17px;
            background: var(--quote-accent-soft);
            border: 1px solid #f1c7b4;
        }

        .quote-total-label {
            display: block;
            margin-bottom: 6px;
            color: var(--quote-muted);
            font-size: 0.76rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .quote-total-value {
            display: block;
            color: var(--quote-accent);
            font-size: 1.8rem;
            font-weight: 700;
            line-height: 1.1;
        }

        .quote-total-date {
            margin: 8px 0 0;
            color: var(--quote-muted);
            font-size: 0.84rem;
        }

        .quote-summary-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 14px;
            margin-top: 18px;
        }

        .quote-card {
            padding: 15px 17px;
            background: var(--quote-surface);
            border: 1px solid var(--quote-line);
        }

        .quote-card-title {
            margin: 0 0 11px;
            font-size: 0.8rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .quote-meta-list {
            display: grid;
            gap: 12px;
        }

        .quote-meta-entry {
            display: grid;
            gap: 2px;
        }

        .quote-meta-label {
            color: var(--quote-muted);
            font-size: 0.76rem;
            font-weight: 600;
            letter-spacing: 0.04em;
            text-transform: uppercase;
        }

        .quote-meta-value {
            min-width: 0;
            white-space: pre-wrap;
            word-break: break-word;
            font-size: 0.9rem;
            line-height: 1.38;
        }

        .quote-section {
            margin-top: 24px;
        }

        .quote-section-title {
            margin: 0 0 12px;
            padding-bottom: 10px;
            border-bottom: 1px solid var(--quote-line);
            font-size: 1.1rem;
        }

        .quote-copy {
            color: var(--quote-ink);
            font-size: 0.96rem;
            line-height: 1.62;
        }

        .quote-copy p {
            margin: 0 0 0.85rem;
        }

        .quote-copy p:last-child {
            margin-bottom: 0;
        }

        .quote-table {
            width: 100%;
            border-collapse: collapse;
        }

        .quote-table th,
        .quote-table td {
            padding: 12px 10px;
            border-bottom: 1px solid var(--quote-line);
            text-align: left;
            vertical-align: top;
        }

        .quote-table th {
            color: var(--quote-muted);
            font-size: 0.76rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .quote-table .quote-table-number {
            text-align: right;
            white-space: nowrap;
        }

        .quote-table-total td {
            border-top: 2px solid var(--quote-ink);
            font-weight: 700;
        }

        .quote-company-block {
            margin-top: 28px;
            padding-top: 16px;
            border-top: 1px solid var(--quote-line);
        }

        @media (max-width: 880px) {
            body {
                padding: 12px;
            }

            .quote-header,
            .quote-summary-grid {
                grid-template-columns: 1fr;
            }
        }

        @media print {
            body {
                padding: 0;
                background: #ffffff;
            }

            .quote-document {
                width: 100%;
                min-height: auto;
                border: none;
                box-shadow: none;
            }

            .quote-body {
                min-height: auto;
            }
        }
    </style>
</head>
<body>
    <main class="quote-document">
        <div class="quote-body">
            {{brandingMarkup}}

            <header class="quote-header">
                <div>
                    <p class="quote-eyebrow">Service Quote</p>
                    <h1 class="quote-title">{{Encode(titleLabel)}}</h1>
                    <p class="quote-reference">Agreement {{Encode(agreementLabel)}} for {{Encode(GetDisplaySiteAddress(agreement.Site))}}</p>
                </div>
                <aside class="quote-total-card">
                    <span class="quote-total-label">Quote Amount</span>
                    <strong class="quote-total-value">{{Encode(quoteTotal)}}</strong>
                    <p class="quote-total-date">Prepared {{Encode(createdLabel)}}</p>
                </aside>
            </header>

            <section class="quote-summary-grid">
                <article class="quote-card">
                    <h2 class="quote-card-title">Prepared For</h2>
                    {{preparedForMarkup}}
                </article>
                <article class="quote-card">
                    <h2 class="quote-card-title">Service Details</h2>
                    <div class="quote-meta-list">
                        {{siteDetailsMarkup}}
                        <div class="quote-meta-entry">
                            <span class="quote-meta-label">Status</span>
                            <span class="quote-meta-value">{{Encode(statusLabel)}}</span>
                        </div>
                        <div class="quote-meta-entry">
                            <span class="quote-meta-label">Accepted</span>
                            <span class="quote-meta-value">{{Encode(acceptedLabel)}}</span>
                        </div>
                    </div>
                </article>
            </section>

            <section class="quote-section">
                <h2 class="quote-section-title">Quoted Work</h2>
                {{quoteItemsMarkup}}
            </section>

            {{notesMarkup}}

            {{companyFooterMarkup}}
        </div>
    </main>
</body>
</html>
""";
    }

    private static string BuildBrandingMarkup(DocumentBrandingProfile brandingProfile)
    {
        var companyNameMarkup = string.IsNullOrWhiteSpace(brandingProfile.CompanyName)
            ? string.Empty
            : $$"""<strong class="quote-branding-company-name">{{Encode(brandingProfile.CompanyName.Trim())}}</strong>""";
        var brandingDetailsMarkup = BuildBrandingDetailsMarkup(brandingProfile);

        if (!string.IsNullOrWhiteSpace(brandingProfile.LogoDataUri))
        {
            return $$"""
<section class="quote-branding-panel">
    <img class="quote-logo-image" src="{{brandingProfile.LogoDataUri}}" alt="Company logo" />
    {{companyNameMarkup}}
    {{brandingDetailsMarkup}}
</section>
""";
        }

        if (!string.IsNullOrWhiteSpace(companyNameMarkup) || !string.IsNullOrWhiteSpace(brandingDetailsMarkup))
        {
            return $$"""
<section class="quote-branding-panel">
    {{companyNameMarkup}}
    {{brandingDetailsMarkup}}
</section>
""";
        }

        return string.Empty;
    }

    private static string BuildPreparedForMarkup(ClientRecord? client)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<div class="quote-meta-list">""");

        AddMetaEntry(builder, "Client", client?.Name, "Unassigned");
        AddMetaEntry(builder, "Primary Contact", client?.PrimaryContact, "Not specified");
        AddMetaEntry(builder, "Email", client?.Email, "Not specified");
        AddMetaEntry(builder, "Phone", client?.Phone, "Not specified");
        AddMetaEntry(builder, "Billing Address", client?.BillingAddress, "Not specified");

        builder.AppendLine("</div>");
        return builder.ToString();
    }

    private static string BuildSiteDetailsMarkup(ServiceAgreement agreement, ServiceQuoteRecord quote)
    {
        var builder = new StringBuilder();
        AddMetaEntry(builder, "Site", agreement.Site.SiteName, "Service site");
        AddMetaEntry(builder, "Address", GetDisplaySiteAddress(agreement.Site), "Address not specified");
        AddMetaEntry(builder, "Occupancy", agreement.Site.OccupancyType, "Not specified");
        AddMetaEntry(builder, "Created", agreement.ContractStart.ToString("D"), "Not specified");
        if (EstimateMath.GetServiceQuoteLaborHours(quote) > 0m)
        {
            AddMetaEntry(builder, "Service Hours", EstimateMath.GetHours(EstimateMath.GetServiceQuoteLaborHours(quote)), "0.00");
        }

        return builder.ToString();
    }

    private static string BuildQuoteItemsMarkup(ServiceQuoteRecord quote)
    {
        var hasLaborLine = EstimateMath.GetServiceQuoteLaborHours(quote) > 0m;
        if (quote.Items.Count == 0 && !hasLaborLine)
        {
            return """<div class="quote-copy"><p>No quoted items were added.</p></div>""";
        }

        var builder = new StringBuilder();
        builder.AppendLine("""<table class="quote-table">""");
        builder.AppendLine("""
<thead>
    <tr>
        <th>Description</th>
        <th class="quote-table-number">Qty</th>
        <th class="quote-table-number">Unit Price</th>
        <th class="quote-table-number">Extended</th>
    </tr>
</thead>
<tbody>
""");

        foreach (var item in quote.Items)
        {
            builder.AppendLine($$"""
    <tr>
        <td>
            <strong>{{Encode(string.IsNullOrWhiteSpace(item.Description) ? "Quoted Item" : item.Description.Trim())}}</strong>
            {{BuildItemNotesMarkup(item.Notes)}}
        </td>
        <td class="quote-table-number">{{Encode(item.Quantity.ToString("N2"))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(item.UnitPrice))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(item.TotalPrice))}}</td>
    </tr>
""");
        }

        if (quote.LaborLines.Count > 0)
        {
            foreach (var laborLine in quote.LaborLines.Where(laborLine => laborLine.Hours > 0m || laborLine.SaleRate > 0m))
            {
                builder.AppendLine($$"""
    <tr>
        <td><strong>{{Encode(string.IsNullOrWhiteSpace(laborLine.Description) ? "Service Labor" : laborLine.Description.Trim())}}</strong></td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetHours(laborLine.Hours))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(laborLine.SaleRate))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(laborLine.TotalSale))}}</td>
    </tr>
""");
            }
        }
        else if (hasLaborLine)
        {
            builder.AppendLine($$"""
    <tr>
        <td><strong>Service Labor</strong></td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetHours(EstimateMath.GetServiceQuoteLaborHours(quote)))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(quote.ServiceLaborSaleRate))}}</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(EstimateMath.GetServiceQuoteLaborRevenue(quote)))}}</td>
    </tr>
""");
        }

        builder.AppendLine($$"""
    <tr class="quote-table-total">
        <td colspan="3">Total</td>
        <td class="quote-table-number">{{Encode(EstimateMath.GetCurrency(EstimateMath.GetServiceQuoteAdjustedRevenue(quote)))}}</td>
    </tr>
</tbody>
</table>
""");

        if (EstimateMath.GetServiceQuoteAdjustedRevenue(quote) != EstimateMath.GetServiceQuoteCalculatedRevenue(quote))
        {
            builder.AppendLine($$"""
<div class="quote-copy">
    <p>Final quoted amount reflects an adjusted sale from the calculated scope total of {{Encode(EstimateMath.GetCurrency(EstimateMath.GetServiceQuoteCalculatedRevenue(quote)))}}.</p>
</div>
""");
        }

        return builder.ToString();
    }

    private static string BuildItemNotesMarkup(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return string.Empty;
        }

        return $$"""<div class="quote-copy"><p>{{Encode(notes.Trim())}}</p></div>""";
    }

    private static string BuildOptionalNotesMarkup(ServiceQuoteRecord quote)
    {
        if (string.IsNullOrWhiteSpace(quote.Notes))
        {
            return string.Empty;
        }

        return $$"""
<section class="quote-section">
    <h2 class="quote-section-title">Service Notes</h2>
    <div class="quote-copy">
        {{BuildRichTextMarkup(quote.Notes)}}
    </div>
</section>
""";
    }

    private static string BuildCompanyFooterMarkup(DocumentBrandingProfile brandingProfile)
    {
        var companyName = string.IsNullOrWhiteSpace(brandingProfile.CompanyName) ? "FyreWorksAI" : brandingProfile.CompanyName.Trim();
        var builder = new StringBuilder();
        builder.AppendLine("""<section class="quote-company-block">""");
        builder.AppendLine($$"""<div class="quote-copy"><p>{{Encode(companyName)}}</p>""");

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyLicenseNumber))
        {
            details.Add($"License {brandingProfile.CompanyLicenseNumber.Trim()}");
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

        if (details.Count > 0)
        {
            builder.AppendLine($$"""<p>{{Encode(string.Join(" | ", details))}}</p>""");
        }

        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildBrandingDetailsMarkup(DocumentBrandingProfile brandingProfile)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyLicenseNumber))
        {
            details.Add($"License {brandingProfile.CompanyLicenseNumber.Trim()}");
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
        builder.Append("""<p class="quote-branding-details">""");
        for (var index = 0; index < details.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("""<span class="quote-branding-divider">&bull;</span>""");
            }

            builder.Append(Encode(details[index]));
        }

        builder.Append("</p>");
        return builder.ToString();
    }

    private static string BuildRichTextMarkup(string text)
    {
        var paragraphs = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (paragraphs.Length == 0)
        {
            return """<p>No additional notes were provided.</p>""";
        }

        var builder = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            builder.Append("""<p>""");
            builder.Append(Encode(paragraph));
            builder.AppendLine("</p>");
        }

        return builder.ToString();
    }

    private static void AddMetaEntry(StringBuilder builder, string label, string? value, string fallback)
    {
        var displayValue = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        builder.AppendLine($$"""
<div class="quote-meta-entry">
    <span class="quote-meta-label">{{Encode(label)}}</span>
    <span class="quote-meta-value">{{Encode(displayValue)}}</span>
</div>
""");
    }

    private static string GetDisplaySiteAddress(SiteInformation site)
    {
        var parts = new[]
        {
            site.AddressLine1,
            site.AddressLine2,
            BuildCityStatePostalCode(site)
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static string BuildCityStatePostalCode(SiteInformation site)
    {
        var parts = new[] { site.City, site.State, site.PostalCode }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());
        return string.Join(" ", parts);
    }

    private static string Encode(string value) =>
        WebUtility.HtmlEncode(value);
}
