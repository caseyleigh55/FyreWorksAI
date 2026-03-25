using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Common;

//******************************//
//******** Site Details ********//
//******************************//

public sealed class SiteInformation
{
    public string SiteName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string OccupancyType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ScopeOfWork { get; set; } = string.Empty;
    public string ParcelNumber { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string BuildingArea { get; set; } = string.Empty;
    public string NumberOfStories { get; set; } = string.Empty;
    public string OccupancyGroup { get; set; } = string.Empty;
    public string OccupantLoad { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public bool IsSprinklered { get; set; }

    [JsonIgnore]
    public string SingleLineAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(AddressLine1))
            {
                parts.Add(AddressLine1.Trim());
            }

            if (!string.IsNullOrWhiteSpace(AddressLine2))
            {
                parts.Add(AddressLine2.Trim());
            }

            var cityStatePostal = string.Join(", ", new[] { City, State }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
            if (!string.IsNullOrWhiteSpace(PostalCode))
            {
                cityStatePostal = string.IsNullOrWhiteSpace(cityStatePostal)
                    ? PostalCode.Trim()
                    : $"{cityStatePostal} {PostalCode.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(cityStatePostal))
            {
                parts.Add(cityStatePostal);
            }

            return string.Join(" | ", parts);
        }
    }
}
