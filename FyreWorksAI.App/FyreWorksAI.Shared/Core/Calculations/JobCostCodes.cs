namespace FyreWorksAI.Shared.Core.Calculations;

//******************************//
//****** Job Cost Codes ********//
//******************************//

public static class JobCostCodes
{
    public const string Admin = "Admin";
    public const string Engineering = "Engineering";
    public const string AdminEngineering = "AdminEngineering";
    public const string Components = "Components";
    public const string Wire = "Wire";
    public const string Material = "Material";
    public const string Materials = "Materials";
    public const string Install = "Install";
    public const string Demo = "Demo";
    public const string Trim = "Trim";
    public const string Test = "Test";
    public const string ChangeOrder = "ChangeOrder";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> TimeEntryCodes =
    [
        Admin,
        Engineering,
        Install,
        Demo,
        Trim,
        Test,
        ChangeOrder,
        Other
    ];

    public static readonly IReadOnlyList<string> ScheduleValueCodes =
    [
        Admin,
        Engineering,
        AdminEngineering,
        Materials,
        Install,
        Demo,
        Trim,
        Test,
        ChangeOrder,
        Other
    ];

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Other;
        }

        return code.Trim().ToLowerInvariant() switch
        {
            "admin" => Admin,
            "engineering" => Engineering,
            "adminengineering" or "admin and engineering" or "admin + engineering" => AdminEngineering,
            "components" or "component" => Components,
            "wire" => Wire,
            "material" => Material,
            "materials" or "materials / wire / components" => Materials,
            "install" => Install,
            "demo" => Demo,
            "trim" => Trim,
            "test" => Test,
            "changeorder" or "change order" => ChangeOrder,
            _ => Other
        };
    }

    public static string GetLabel(string? code) =>
        Normalize(code) switch
        {
            Admin => "Admin",
            Engineering => "Engineering",
            AdminEngineering => "Admin And Engineering",
            Components => "Components",
            Wire => "Wire",
            Material => "Material",
            Materials => "Materials / Wire / Components",
            Install => "Install",
            Demo => "Demo",
            Trim => "Trim",
            Test => "Test",
            ChangeOrder => "Change Order",
            _ => "Other"
        };

    public static bool IsMaterialReferenceCategory(string? code) =>
        Normalize(code) is Components or Wire or Material;
}
