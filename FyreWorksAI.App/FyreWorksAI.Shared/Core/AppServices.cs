using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace FyreWorksAI.Shared;

public sealed record PickedFile(string SourcePath, string FileName, string ContentType);

public interface IAttachmentService
{
    bool SupportsPicking { get; }
    bool SupportsOpening { get; }
    Task<IReadOnlyList<PickedFile>> PickFilesAsync();
    Task OpenAsync(string fullPath);
}

public interface IStoragePathResolver
{
    string GetRootDirectory();
}

public interface IWorkspaceStorage
{
    string DataFilePath { get; }
    Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFyreWorksCore(this IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceStorage, TextFileWorkspaceStorage>();
        services.AddSingleton<WorkspaceStore>();
        return services;
    }
}

public sealed class TextFileWorkspaceStorage(IStoragePathResolver pathResolver) : IWorkspaceStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DataFilePath => Path.Combine(pathResolver.GetRootDirectory(), "data", "fyreworks-data.txt");

    public async Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DataFilePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(DataFilePath);
        return await JsonSerializer.DeserializeAsync<FyreWorksWorkspace>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath)!);
        await using var stream = File.Create(DataFilePath);
        await JsonSerializer.SerializeAsync(stream, workspace, JsonOptions, cancellationToken);
    }
}

public sealed class WorkspaceStore(
    IWorkspaceStorage storage,
    IStoragePathResolver pathResolver,
    IAttachmentService attachmentService)
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        WriteIndented = false
    };

    public FyreWorksWorkspace Workspace { get; private set; } = new();
    public bool IsInitialized { get; private set; }
    public string DataFilePath => storage.DataFilePath;
    public string RootDirectory => pathResolver.GetRootDirectory();
    public string AttachmentRootPath => Path.Combine(RootDirectory, "attachments");
    public string ExportRootPath => Path.Combine(RootDirectory, "exports");
    public string BackupRootPath => Path.Combine(RootDirectory, "backups");
    public bool CanPickAttachments => attachmentService.SupportsPicking;
    public bool CanOpenAttachments => attachmentService.SupportsOpening;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Workspace = await storage.LoadAsync() ?? CreateDefaultWorkspace();
        EnsureDefaults(Workspace);
        IsInitialized = true;
        await SaveAsync();
    }

    public async Task SaveAsync()
    {
        EnsureDefaults(Workspace);
        await storage.SaveAsync(Workspace);
    }

    public ClientRecord? GetClient(Guid? clientId) =>
        clientId is null
            ? null
            : Workspace.Clients.FirstOrDefault(client => client.Id == clientId.Value);

    public LaborTemplate? GetTemplate(Guid? templateId) =>
        templateId is null
            ? null
            : Workspace.Templates.FirstOrDefault(template => template.Id == templateId.Value);

    public ClientRecord CreateClient(string? name = null)
    {
        var client = new ClientRecord
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Client {Workspace.Clients.Count + 1}" : name.Trim()
        };

        Workspace.Clients.Insert(0, client);
        return client;
    }

    public BidRecord CreateBid()
    {
        var bid = new BidRecord
        {
            BidNumber = GenerateNumber("BID", Workspace.Bids.Count + 1),
            ProjectName = $"Bid {Workspace.Bids.Count + 1}",
            CreatedOn = DateTime.Today,
            DueDate = DateTime.Today.AddDays(14),
            TemplateId = Workspace.Settings.DefaultTemplateId,
            FieldLaborRate = Workspace.Settings.FieldLaborRate,
            AdminLaborRate = Workspace.Settings.AdminLaborRate,
            EngineeringLaborRate = Workspace.Settings.EngineeringLaborRate,
            MarkupPercent = Workspace.Settings.DefaultMarkupPercent,
            Site =
            {
                SiteName = "New Project Site"
            }
        };

        Workspace.Bids.Insert(0, bid);
        return bid;
    }

    public JobRecord CreateBlankJob()
    {
        var job = new JobRecord
        {
            JobNumber = GenerateNumber("JOB", Workspace.Jobs.Count + 1),
            ProjectName = $"Job {Workspace.Jobs.Count + 1}",
            CreatedOn = DateTime.Today,
            Baseline =
            {
                ScopeSummary = "Direct job entry without a converted bid."
            }
        };

        Workspace.Jobs.Insert(0, job);
        return job;
    }

    public ServiceAgreement CreateServiceAgreement()
    {
        var agreement = new ServiceAgreement
        {
            AgreementNumber = GenerateNumber("SVC", Workspace.ServiceAgreements.Count + 1),
            AgreementName = $"Monitoring Agreement {Workspace.ServiceAgreements.Count + 1}",
            ContractStart = DateTime.Today,
            ContractMonths = Workspace.Settings.DefaultServiceContractMonths,
            MonthlyMonitoringAmount = Workspace.Settings.DefaultMonthlyMonitoringAmount,
            InspectionIntervalMonths = Workspace.Settings.DefaultInspectionIntervalMonths,
            NextInspectionDate = DateTime.Today.AddMonths(Workspace.Settings.DefaultInspectionIntervalMonths),
            Site =
            {
                SiteName = "Protected Premises"
            }
        };

        RegenerateMonitoringSchedule(agreement);
        Workspace.ServiceAgreements.Insert(0, agreement);
        return agreement;
    }

    public LaborTemplate CreateTemplate()
    {
        var template = CreateStarterTemplate($"Template {Workspace.Templates.Count + 1}");
        Workspace.Templates.Insert(0, template);
        return template;
    }

    public JobRecord ConvertBidToJob(BidRecord bid)
    {
        var existingJob = Workspace.Jobs.FirstOrDefault(job => job.SourceBidId == bid.Id);
        if (existingJob is not null)
        {
            return existingJob;
        }

        var baseline = new BaselineEstimate
        {
            SourceBidNumber = bid.BidNumber,
            ScopeSummary = bid.ScopeSummary,
            OriginalRevenue = EstimateMath.GetBidRevenue(bid),
            EstimatedLaborCost = EstimateMath.GetBidLaborCost(bid),
            EstimatedMaterialCost = EstimateMath.GetBidMaterialCost(bid),
            EstimatedTotalCost = EstimateMath.GetBidEstimatedCost(bid),
            EstimatedFieldHours = EstimateMath.GetBidFieldHours(bid),
            EstimatedAdminHours = EstimateMath.GetBidAdminHours(bid),
            EstimatedEngineeringHours = EstimateMath.GetBidEngineeringHours(bid),
            AdministrativeTasks = Clone(bid.AdministrativeTasks),
            EngineeringTasks = Clone(bid.EngineeringTasks),
            Components = Clone(bid.Components),
            Materials = Clone(bid.Materials)
        };

        var job = new JobRecord
        {
            JobNumber = GenerateNumber("JOB", Workspace.Jobs.Count + 1),
            ProjectName = bid.ProjectName,
            ClientId = bid.ClientId,
            SourceBidId = bid.Id,
            Site = Clone(bid.Site),
            CreatedOn = DateTime.Today,
            Status = "Planning",
            IsActive = true,
            Notes = $"Converted from bid {bid.BidNumber}.",
            Baseline = baseline
        };

        Workspace.Jobs.Insert(0, job);
        bid.Status = "Awarded";
        return job;
    }

    public void ApplyTemplateToBid(BidRecord bid)
    {
        var template = GetTemplate(bid.TemplateId) ?? GetTemplate(Workspace.Settings.DefaultTemplateId);
        if (template is null)
        {
            return;
        }

        foreach (var component in bid.Components)
        {
            ApplyTemplateToComponent(component, template);
        }
    }

    public bool ApplyTemplateToComponent(BidComponent component, LaborTemplate? template)
    {
        if (template is null)
        {
            return false;
        }

        var rule = template.Rules.FirstOrDefault(candidate =>
            string.Equals(candidate.LocationProfile.Trim(), component.LocationProfile.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.InstallType.Trim(), component.InstallType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (rule is null)
        {
            return false;
        }

        component.InstallMinutes = rule.InstallMinutes;
        component.DemoMinutes = rule.DemoMinutes;
        component.TrimMinutes = rule.TrimMinutes;
        component.TestMinutes = rule.TestMinutes;
        return true;
    }

    public void RegenerateMonitoringSchedule(ServiceAgreement agreement)
    {
        agreement.ContractMonths = Math.Max(1, agreement.ContractMonths);
        agreement.InspectionIntervalMonths = Math.Max(1, agreement.InspectionIntervalMonths);

        var existingPayments = agreement.MonitoringPayments
            .GroupBy(payment => payment.DueDate.Date)
            .ToDictionary(group => group.Key, group => group.First());

        var rebuilt = new List<MonitoringPayment>();
        for (var monthIndex = 0; monthIndex < agreement.ContractMonths; monthIndex++)
        {
            var dueDate = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1)
                .AddMonths(monthIndex);
            var payment = existingPayments.TryGetValue(dueDate.Date, out var existing)
                ? existing
                : new MonitoringPayment
                {
                    DueDate = dueDate,
                    Amount = agreement.MonthlyMonitoringAmount,
                    ReceivedOn = dueDate
                };

            payment.DueDate = dueDate;
            payment.Amount = agreement.MonthlyMonitoringAmount;
            rebuilt.Add(payment);
        }

        agreement.MonitoringPayments = rebuilt;
        agreement.NextInspectionDate = agreement.ContractStart.AddMonths(agreement.InspectionIntervalMonths);
    }

    public async Task<int> AddAttachmentsAsync(List<AttachmentRecord> attachments, string area, Guid ownerId)
    {
        if (!attachmentService.SupportsPicking)
        {
            return 0;
        }

        var pickedFiles = await attachmentService.PickFilesAsync();
        if (pickedFiles.Count == 0)
        {
            return 0;
        }

        var targetDirectory = Path.Combine(AttachmentRootPath, area, ownerId.ToString("N"));
        Directory.CreateDirectory(targetDirectory);

        foreach (var pickedFile in pickedFiles)
        {
            if (string.IsNullOrWhiteSpace(pickedFile.SourcePath) || !File.Exists(pickedFile.SourcePath))
            {
                continue;
            }

            var safeFileName = MakeSafeFileName(pickedFile.FileName);
            var storedFileName = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeFileName}";
            var fullDestinationPath = Path.Combine(targetDirectory, storedFileName);
            File.Copy(pickedFile.SourcePath, fullDestinationPath, overwrite: true);

            attachments.Add(new AttachmentRecord
            {
                OriginalFileName = pickedFile.FileName,
                StoredFileName = storedFileName,
                RelativePath = Path.Combine(area, ownerId.ToString("N"), storedFileName),
                ContentType = pickedFile.ContentType,
                UploadedOn = DateTime.Now
            });
        }

        return pickedFiles.Count;
    }

    public async Task OpenAttachmentAsync(AttachmentRecord attachment)
    {
        if (!attachmentService.SupportsOpening)
        {
            return;
        }

        var fullPath = Path.Combine(AttachmentRootPath, attachment.RelativePath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        await attachmentService.OpenAsync(fullPath);
    }

    public void RemoveAttachment(List<AttachmentRecord> attachments, AttachmentRecord attachment)
    {
        var fullPath = Path.Combine(AttachmentRootPath, attachment.RelativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        attachments.RemoveAll(existing => existing.Id == attachment.Id);
    }

    public async Task<string> ExportJobCostReportAsync(JobRecord job)
    {
        Directory.CreateDirectory(ExportRootPath);
        var fileName = $"{MakeSafeFileName(job.JobNumber)}-{DateTime.Now:yyyyMMddHHmmss}.txt";
        var fullPath = Path.Combine(ExportRootPath, fileName);
        await File.WriteAllTextAsync(fullPath, BuildJobCostReport(job));
        return fullPath;
    }

    public async Task<string> CreateBackupAsync()
    {
        await SaveAsync();
        Directory.CreateDirectory(BackupRootPath);
        var backupPath = Path.Combine(BackupRootPath, $"fyreworks-backup-{DateTime.Now:yyyyMMddHHmmss}.txt");
        File.Copy(DataFilePath, backupPath, overwrite: true);
        return backupPath;
    }

    private static string BuildJobCostReport(JobRecord job)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Job Cost Export - {job.JobNumber}");
        builder.AppendLine($"Project: {job.ProjectName}");
        builder.AppendLine($"Generated: {DateTime.Now:G}");
        builder.AppendLine();
        builder.AppendLine("Baseline");
        builder.AppendLine($"  Original revenue: {EstimateMath.GetCurrency(job.Baseline.OriginalRevenue)}");
        builder.AppendLine($"  Estimated total cost: {EstimateMath.GetCurrency(job.Baseline.EstimatedTotalCost)}");
        builder.AppendLine($"  Estimated field hours: {job.Baseline.EstimatedFieldHours:N2}");
        builder.AppendLine();
        builder.AppendLine("Actuals");
        builder.AppendLine($"  Labor cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualLaborCost(job))}");
        builder.AppendLine($"  Material cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualMaterialCost(job))}");
        builder.AppendLine($"  Commitment billings: {EstimateMath.GetCurrency(EstimateMath.GetJobBilledCommitments(job))}");
        builder.AppendLine($"  Contract revenue incl. approved COs: {EstimateMath.GetCurrency(EstimateMath.GetJobRevenue(job))}");
        builder.AppendLine($"  Actual cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualCost(job))}");
        builder.AppendLine($"  Exposure including commitments: {EstimateMath.GetCurrency(EstimateMath.GetJobCommittedExposure(job))}");
        builder.AppendLine($"  Profit to date: {EstimateMath.GetCurrency(EstimateMath.GetJobProfit(job))}");
        builder.AppendLine($"  Margin to date: {EstimateMath.GetPercent(EstimateMath.GetJobMarginPercent(job))}");
        builder.AppendLine();
        builder.AppendLine("Schedule of Values");
        foreach (var item in job.ScheduleOfValues)
        {
            builder.AppendLine($"  {item.Description}: scheduled {EstimateMath.GetCurrency(item.ScheduledValue)}, billed {EstimateMath.GetCurrency(item.BilledToDate)}, paid {EstimateMath.GetCurrency(item.PaidToDate)}");
        }

        return builder.ToString();
    }

    private static string GenerateNumber(string prefix, int sequence) =>
        $"{prefix}-{DateTime.Today:yyyyMMdd}-{sequence:000}";

    private static string MakeSafeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var character in fileName)
        {
            builder.Append(invalidCharacters.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, CloneOptions);
        return JsonSerializer.Deserialize<T>(json, CloneOptions)!;
    }

    private static FyreWorksWorkspace CreateDefaultWorkspace()
    {
        var workspace = new FyreWorksWorkspace();
        EnsureDefaults(workspace);
        return workspace;
    }

    private static void EnsureDefaults(FyreWorksWorkspace workspace)
    {
        workspace.Settings ??= new AppSettings();
        workspace.Clients ??= [];
        workspace.Templates ??= [];
        workspace.Bids ??= [];
        workspace.Jobs ??= [];
        workspace.ServiceAgreements ??= [];

        if (workspace.Templates.Count == 0)
        {
            var starterTemplate = CreateStarterTemplate("Standard Device Labor");
            workspace.Templates.Add(starterTemplate);
            workspace.Settings.DefaultTemplateId = starterTemplate.Id;
        }

        if (workspace.Settings.DefaultTemplateId is null || workspace.Templates.All(template => template.Id != workspace.Settings.DefaultTemplateId))
        {
            workspace.Settings.DefaultTemplateId = workspace.Templates.FirstOrDefault(template => !template.IsArchived)?.Id ?? workspace.Templates.First().Id;
        }

        foreach (var bid in workspace.Bids)
        {
            bid.Site ??= new SiteInformation();
            bid.AdministrativeTasks ??= [];
            bid.EngineeringTasks ??= [];
            bid.Components ??= [];
            bid.Materials ??= [];
            bid.Attachments ??= [];
            if (bid.FieldLaborRate <= 0m)
            {
                bid.FieldLaborRate = workspace.Settings.FieldLaborRate;
            }

            if (bid.AdminLaborRate <= 0m)
            {
                bid.AdminLaborRate = workspace.Settings.AdminLaborRate;
            }

            if (bid.EngineeringLaborRate <= 0m)
            {
                bid.EngineeringLaborRate = workspace.Settings.EngineeringLaborRate;
            }

            if (bid.MarkupPercent <= 0m)
            {
                bid.MarkupPercent = workspace.Settings.DefaultMarkupPercent;
            }

            if (bid.TemplateId is null)
            {
                bid.TemplateId = workspace.Settings.DefaultTemplateId;
            }
        }

        foreach (var job in workspace.Jobs)
        {
            job.Site ??= new SiteInformation();
            job.Baseline ??= new BaselineEstimate();
            job.Baseline.AdministrativeTasks ??= [];
            job.Baseline.EngineeringTasks ??= [];
            job.Baseline.Components ??= [];
            job.Baseline.Materials ??= [];
            job.TimeEntries ??= [];
            job.MaterialPurchases ??= [];
            job.ChangeOrders ??= [];
            job.ScheduleOfValues ??= [];
            job.Commitments ??= [];
            job.Attachments ??= [];
        }

        foreach (var agreement in workspace.ServiceAgreements)
        {
            agreement.Site ??= new SiteInformation();
            agreement.MonitoringPayments ??= [];
            agreement.ServiceCalls ??= [];
            agreement.Quotes ??= [];
            agreement.Attachments ??= [];
            if (agreement.ContractMonths <= 0)
            {
                agreement.ContractMonths = workspace.Settings.DefaultServiceContractMonths;
            }

            if (agreement.MonthlyMonitoringAmount <= 0m)
            {
                agreement.MonthlyMonitoringAmount = workspace.Settings.DefaultMonthlyMonitoringAmount;
            }

            if (agreement.InspectionIntervalMonths <= 0)
            {
                agreement.InspectionIntervalMonths = workspace.Settings.DefaultInspectionIntervalMonths;
            }

            if (agreement.MonitoringPayments.Count == 0)
            {
                var store = new WorkspaceStoreBootstrapper();
                store.RegenerateMonitoringSchedule(agreement);
            }
        }
    }

    private static LaborTemplate CreateStarterTemplate(string name) =>
        new()
        {
            Name = name,
            Notes = "Starter template matching the estimating rules described in the project brief.",
            Rules =
            [
                new LaborRule
                {
                    LocationProfile = "Normal Area",
                    InstallType = "No Pipe",
                    InstallMinutes = 15m,
                    DemoMinutes = 0m,
                    TrimMinutes = 10m,
                    TestMinutes = 5m,
                    Notes = "Typical ceiling or wall mounted device in a standard environment."
                },
                new LaborRule
                {
                    LocationProfile = "Warehouse",
                    InstallType = "Lift Required",
                    InstallMinutes = 60m,
                    DemoMinutes = 10m,
                    TrimMinutes = 30m,
                    TestMinutes = 15m,
                    Notes = "Use when lift staging or elevated access is required."
                },
                new LaborRule
                {
                    LocationProfile = "Finished Office",
                    InstallType = "Surface Raceway",
                    InstallMinutes = 35m,
                    DemoMinutes = 5m,
                    TrimMinutes = 20m,
                    TestMinutes = 8m,
                    Notes = "Finished spaces with added coordination and cleanup."
                }
            ]
        };

    private sealed class WorkspaceStoreBootstrapper
    {
        public void RegenerateMonitoringSchedule(ServiceAgreement agreement)
        {
            agreement.ContractMonths = Math.Max(1, agreement.ContractMonths);
            agreement.InspectionIntervalMonths = Math.Max(1, agreement.InspectionIntervalMonths);
            agreement.MonitoringPayments =
            [
                ..Enumerable.Range(0, agreement.ContractMonths).Select(monthIndex =>
                    new MonitoringPayment
                    {
                        DueDate = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1).AddMonths(monthIndex),
                        Amount = agreement.MonthlyMonitoringAmount,
                        ReceivedOn = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1).AddMonths(monthIndex)
                    })
            ];
            agreement.NextInspectionDate = agreement.ContractStart.AddMonths(agreement.InspectionIntervalMonths);
        }
    }
}

public static class EstimateMath
{
    public static decimal GetBidFieldHours(BidRecord bid) =>
        bid.Components.Sum(component => component.TotalMinutes) / 60m;

    public static decimal GetBidAdminHours(BidRecord bid) =>
        bid.AdministrativeTasks.Sum(task => task.EstimatedHours);

    public static decimal GetBidEngineeringHours(BidRecord bid) =>
        bid.EngineeringTasks.Sum(task => task.EstimatedHours);

    public static decimal GetBidLaborCost(BidRecord bid)
    {
        var fieldLaborCost = GetBidFieldHours(bid) * bid.FieldLaborRate;
        var adminLaborCost = GetBidAdminHours(bid) * bid.AdminLaborRate;
        var engineeringLaborCost = GetBidEngineeringHours(bid) * bid.EngineeringLaborRate;
        return fieldLaborCost + adminLaborCost + engineeringLaborCost;
    }

    public static decimal GetBidMaterialCost(BidRecord bid) =>
        bid.Components.Sum(component => component.TotalMaterialCost) +
        bid.Materials.Sum(material => material.ExtendedCost);

    public static decimal GetBidEstimatedCost(BidRecord bid) =>
        GetBidLaborCost(bid) + GetBidMaterialCost(bid);

    public static decimal GetBidSuggestedRevenue(BidRecord bid) =>
        GetBidEstimatedCost(bid) * (1m + (Math.Max(0m, bid.MarkupPercent) / 100m));

    public static decimal GetBidRevenue(BidRecord bid) =>
        bid.ProposedRevenue > 0m ? bid.ProposedRevenue : GetBidSuggestedRevenue(bid);

    public static decimal GetBidMargin(BidRecord bid) =>
        GetBidRevenue(bid) - GetBidEstimatedCost(bid);

    public static decimal GetBidMarginPercent(BidRecord bid)
    {
        var revenue = GetBidRevenue(bid);
        return revenue <= 0m ? 0m : GetBidMargin(bid) / revenue;
    }

    public static decimal GetJobActualLaborHours(JobRecord job) =>
        job.TimeEntries.Sum(entry => entry.Hours);

    public static decimal GetJobActualLaborCost(JobRecord job) =>
        job.TimeEntries.Sum(entry => entry.TotalCost);

    public static decimal GetJobActualMaterialCost(JobRecord job) =>
        job.MaterialPurchases.Sum(purchase => purchase.ActualCost);

    public static decimal GetJobApprovedChangeOrderRevenue(JobRecord job) =>
        job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.RevenueAmount);

    public static decimal GetJobApprovedChangeOrderCost(JobRecord job) =>
        job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.EstimatedCostImpact);

    public static decimal GetJobRevenue(JobRecord job) =>
        job.Baseline.OriginalRevenue + GetJobApprovedChangeOrderRevenue(job);

    public static decimal GetJobBilledCommitments(JobRecord job) =>
        job.Commitments.Sum(commitment => commitment.BilledAmount);

    public static decimal GetJobCommittedExposure(JobRecord job) =>
        GetJobActualLaborCost(job) +
        GetJobActualMaterialCost(job) +
        GetJobApprovedChangeOrderCost(job) +
        job.Commitments.Sum(commitment => Math.Max(commitment.CommittedAmount, commitment.BilledAmount));

    public static decimal GetJobActualCost(JobRecord job) =>
        GetJobActualLaborCost(job) +
        GetJobActualMaterialCost(job) +
        GetJobApprovedChangeOrderCost(job) +
        GetJobBilledCommitments(job);

    public static decimal GetJobProfit(JobRecord job) =>
        GetJobRevenue(job) - GetJobActualCost(job);

    public static decimal GetJobMarginPercent(JobRecord job)
    {
        var revenue = GetJobRevenue(job);
        return revenue <= 0m ? 0m : GetJobProfit(job) / revenue;
    }

    public static decimal GetJobBilledRevenue(JobRecord job) =>
        job.ScheduleOfValues.Sum(item => item.BilledToDate);

    public static decimal GetJobCollectedRevenue(JobRecord job) =>
        job.ScheduleOfValues.Sum(item => item.PaidToDate);

    public static decimal GetServiceContractValue(ServiceAgreement agreement) =>
        agreement.MonthlyMonitoringAmount * agreement.ContractMonths;

    public static decimal GetServicePaid(ServiceAgreement agreement) =>
        agreement.MonitoringPayments.Where(payment => payment.IsPaid).Sum(payment => payment.Amount);

    public static decimal GetServiceOutstanding(ServiceAgreement agreement) =>
        GetServiceContractValue(agreement) - GetServicePaid(agreement);

    public static DateTime GetNextBillingDate(ServiceAgreement agreement)
    {
        var nextPayment = agreement.MonitoringPayments
            .Where(payment => !payment.IsPaid)
            .OrderBy(payment => payment.DueDate)
            .FirstOrDefault();

        return nextPayment?.DueDate ?? agreement.ContractStart.AddMonths(agreement.ContractMonths);
    }

    public static decimal GetServiceQuoteRevenue(ServiceQuoteRecord quote) =>
        quote.Items.Sum(item => item.TotalPrice);

    public static decimal GetServiceQuoteCost(ServiceQuoteRecord quote) =>
        quote.Items.Sum(item => item.TotalCost);

    public static string GetCurrency(decimal value) =>
        value.ToString("C", CultureInfo.CurrentCulture);

    public static string GetPercent(decimal value) =>
        value.ToString("P1", CultureInfo.CurrentCulture);
}
