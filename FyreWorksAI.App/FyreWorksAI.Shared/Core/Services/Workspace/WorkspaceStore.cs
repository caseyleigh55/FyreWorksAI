using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FyreWorksAI.Shared.Core.Services.Workspace;

//******************************//
//******** Workspace ***********//
//******************************//
public sealed class WorkspaceStore(
    IWorkspaceStorage storage,
    IStoragePathResolver pathResolver,
    IAttachmentService attachmentService,
    IWorkspaceLocationService workspaceLocationService)
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        WriteIndented = false
    };
    private static readonly Regex StandardJobNumberPattern = new(
        @"^JOB-(?<year>\d{2})-(?<sequence>\d{4})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LegacyJobNumberPattern = new(
        @"^JOB-(?<date>\d{8})-(?<sequence>\d{3,4})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public FyreWorksWorkspace Workspace { get; private set; } = new();
    public bool IsInitialized { get; private set; }
    public string DataFilePath => storage.DataFilePath;
    public string RootDirectory => pathResolver.GetRootDirectory();
    public string AttachmentRootPath => Path.Combine(RootDirectory, "attachments");
    public string ExportRootPath => Path.Combine(RootDirectory, "exports");
    public string BackupRootPath => Path.Combine(RootDirectory, "backups");
    public bool CanPickAttachments => attachmentService.SupportsPicking;
    public bool CanOpenAttachments => attachmentService.SupportsOpening;
    public bool CanOpenStorageLocations => workspaceLocationService.SupportsOpeningDirectories;

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
        var template = GetTemplate(Workspace.Settings.DefaultTemplateId) ?? Workspace.Templates.First(template => !template.IsArchived);
        var bid = new BidRecord
        {
            ProjectName = $"Bid {Workspace.Bids.Count + 1}",
            CreatedOn = DateTime.Today,
            DueDate = DateTime.Today.AddDays(14),
            TemplateId = template.Id,
            Site =
            {
                SiteName = "New Project Site"
            }
        };

        ApplyTemplateValuesToBid(bid, template);
        bid.BidNumber = GenerateBidNumber(template, bid.CreatedOn);
        EnsureLaborDistributionRows(bid);
        RebalanceLaborDistributionToCalculatedHours(bid);

        Workspace.Bids.Insert(0, bid);
        return bid;
    }

    public JobRecord CreateBlankJob()
    {
        var createdOn = DateTime.Today;
        var job = new JobRecord
        {
            JobNumber = GenerateJobNumber(createdOn),
            ProjectName = $"Job {Workspace.Jobs.Count + 1}",
            CreatedOn = createdOn,
            Baseline =
            {
                ScopeSummary = "Direct job entry without a converted bid."
            }
        };

        JobFinancialBuilder.EnsureJobDerivedData(job);
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

    public LaborTemplate CreateTemplateFromBid(BidRecord bid, string? templateName = null)
    {
        var sourceTemplate = GetTemplate(bid.TemplateId) ?? GetTemplate(Workspace.Settings.DefaultTemplateId);
        var template = sourceTemplate is null
            ? CreateStarterTemplate(string.Empty)
            : Clone(sourceTemplate);

        template.Id = Guid.NewGuid();
        template.Name = string.IsNullOrWhiteSpace(templateName)
            ? $"{bid.ProjectName} Profile"
            : templateName.Trim();
        template.IsArchived = false;
        template.DefaultMarkupPercent = bid.MarkupPercent;
        template.JourneymanRegularDirectRate = bid.JourneymanRegularDirectRate;
        template.JourneymanRegularBilledRate = bid.JourneymanRegularBilledRate;
        template.JourneymanOvernightDirectRate = bid.JourneymanOvernightDirectRate;
        template.JourneymanOvernightBilledRate = bid.JourneymanOvernightBilledRate;
        template.ApprenticeRegularDirectRate = bid.ApprenticeRegularDirectRate;
        template.ApprenticeRegularBilledRate = bid.ApprenticeRegularBilledRate;
        template.ApprenticeOvernightDirectRate = bid.ApprenticeOvernightDirectRate;
        template.ApprenticeOvernightBilledRate = bid.ApprenticeOvernightBilledRate;
        template.AdminDirectRate = bid.AdminDirectRate;
        template.AdminBilledRate = bid.AdminBilledRate;
        template.EngineeringDirectRate = bid.EngineeringDirectRate;
        template.EngineeringBilledRate = bid.EngineeringBilledRate;

        Workspace.Templates.Insert(0, template);
        bid.TemplateId = template.Id;
        return template;
    }

    public JobRecord ConvertBidToJob(BidRecord bid)
    {
        var existingJob = Workspace.Jobs.FirstOrDefault(job => job.SourceBidId == bid.Id);
        if (existingJob is not null)
        {
            return existingJob;
        }

        var createdOn = DateTime.Today;
        var jobNumber = GenerateJobNumber(createdOn);
        var baseline = JobFinancialBuilder.BuildBaselineFromBid(bid, jobNumber);

        var job = new JobRecord
        {
            JobNumber = jobNumber,
            ProjectName = bid.ProjectName,
            ClientId = bid.ClientId,
            SourceBidId = bid.Id,
            Site = Clone(bid.Site),
            CreatedOn = createdOn,
            Status = "Planning",
            IsActive = true,
            Notes = $"Converted from bid {bid.BidNumber}.",
            Baseline = baseline,
            Exclusions = bid.Exclusions,
            ProposalSummary = bid.ProposalSummary,
            ProposalClosing = bid.ProposalClosing
        };

        JobFinancialBuilder.EnsureJobDerivedData(job);
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

        ApplyTemplateValuesToBid(bid, template);

        foreach (var component in bid.Components)
        {
            ApplyTemplateToComponent(component, template);
            if (component.UnitSale <= 0m)
            {
                component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, bid.MarkupPercent);
            }
        }

        foreach (var demoItem in bid.DemoItems)
        {
            ApplyTemplateToDemoItem(demoItem, template);
        }

        foreach (var material in bid.Materials)
        {
            if (material.UnitSale <= 0m)
            {
                material.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
            }
        }

        RebalanceLaborDistributionToCalculatedHours(bid);
    }

    public void RebalanceBidLaborDistribution(BidRecord bid) =>
        RebalanceLaborDistributionToCalculatedHours(bid);

    public bool ApplyTemplateToComponent(BidComponent component, LaborTemplate? template)
    {
        var rule = FindRule(template, component.LocationProfile, component.InstallType);

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

    public bool ApplyTemplateToDemoItem(BidDemoItem demoItem, LaborTemplate? template)
    {
        var rule = FindRule(template, demoItem.LocationProfile, demoItem.InstallType);

        if (rule is null)
        {
            return false;
        }

        demoItem.DemoHoursEach = rule.DemoMinutes / 60m;
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
        await File.WriteAllTextAsync(fullPath, JobFinancialBuilder.BuildJobCostReport(job, GetClient(job.ClientId)));
        return fullPath;
    }

    public async Task<string> ExportBidProposalAsync(BidRecord bid)
    {
        Directory.CreateDirectory(ExportRootPath);
        var fileName = $"{MakeSafeFileName(bid.BidNumber)}-proposal-{DateTime.Now:yyyyMMddHHmmss}.txt";
        var fullPath = Path.Combine(ExportRootPath, fileName);
        await File.WriteAllTextAsync(fullPath, JobFinancialBuilder.BuildBidProposal(bid, GetClient(bid.ClientId)));
        return fullPath;
    }

    public void SyncJobFinancials(JobRecord job) =>
        SyncLinkedJobBaseline(job);

    public async Task<string> CreateBackupAsync()
    {
        await SaveAsync();
        Directory.CreateDirectory(BackupRootPath);
        var backupPath = Path.Combine(BackupRootPath, $"fyreworks-backup-{DateTime.Now:yyyyMMddHHmmss}.txt");
        File.Copy(DataFilePath, backupPath, overwrite: true);
        return backupPath;
    }

    public bool DeleteBid(Guid bidId)
    {
        var bid = Workspace.Bids.FirstOrDefault(item => item.Id == bidId);
        if (bid is null)
        {
            return false;
        }

        Workspace.Bids.Remove(bid);
        DeleteAttachmentDirectory("bids", bid.Id);
        return true;
    }

    public bool DeleteJob(Guid jobId)
    {
        var job = Workspace.Jobs.FirstOrDefault(item => item.Id == jobId);
        if (job is null)
        {
            return false;
        }

        Workspace.Jobs.Remove(job);
        DeleteAttachmentDirectory("jobs", job.Id);
        return true;
    }

    public Task OpenDataDirectoryAsync() =>
        OpenDirectoryAsync(Path.GetDirectoryName(DataFilePath)!);

    public Task OpenAttachmentDirectoryAsync() =>
        OpenDirectoryAsync(AttachmentRootPath);

    public Task OpenExportDirectoryAsync() =>
        OpenDirectoryAsync(ExportRootPath);

    public Task OpenBackupDirectoryAsync() =>
        OpenDirectoryAsync(BackupRootPath);

    private void ApplyTemplateValuesToBid(BidRecord bid, LaborTemplate template)
    {
        bid.TemplateId = template.Id;
        bid.MarkupPercent = template.DefaultMarkupPercent;
        bid.JourneymanRegularDirectRate = template.JourneymanRegularDirectRate;
        bid.JourneymanRegularBilledRate = template.JourneymanRegularBilledRate;
        bid.JourneymanOvernightDirectRate = template.JourneymanOvernightDirectRate;
        bid.JourneymanOvernightBilledRate = template.JourneymanOvernightBilledRate;
        bid.ApprenticeRegularDirectRate = template.ApprenticeRegularDirectRate;
        bid.ApprenticeRegularBilledRate = template.ApprenticeRegularBilledRate;
        bid.ApprenticeOvernightDirectRate = template.ApprenticeOvernightDirectRate;
        bid.ApprenticeOvernightBilledRate = template.ApprenticeOvernightBilledRate;
        bid.AdminDirectRate = template.AdminDirectRate;
        bid.AdminBilledRate = template.AdminBilledRate;
        bid.EngineeringDirectRate = template.EngineeringDirectRate;
        bid.EngineeringBilledRate = template.EngineeringBilledRate;
        bid.FieldLaborRate = template.JourneymanRegularDirectRate;
        bid.AdminLaborRate = template.AdminDirectRate;
        bid.EngineeringLaborRate = template.EngineeringDirectRate;
    }

    private async Task OpenDirectoryAsync(string fullPath)
    {
        Directory.CreateDirectory(fullPath);
        await workspaceLocationService.OpenDirectoryAsync(fullPath);
    }

    private void DeleteAttachmentDirectory(string area, Guid ownerId)
    {
        var targetDirectory = Path.Combine(AttachmentRootPath, area, ownerId.ToString("N"));
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }
    }

    private int GetNextBidSequence(int year)
    {
        Workspace.Settings.BidNumberCounters ??= [];
        var counter = Workspace.Settings.BidNumberCounters.FirstOrDefault(item => item.Year == year);
        if (counter is null)
        {
            counter = new YearSequenceCounter { Year = year };
            Workspace.Settings.BidNumberCounters.Add(counter);
        }

        var minimumSequence = Workspace.Bids.Count(bid => bid.CreatedOn.Year == year) + 1;
        counter.NextSequence = Math.Max(counter.NextSequence, minimumSequence);

        var current = counter.NextSequence;
        counter.NextSequence++;
        return current;
    }

    private int GetNextJobSequence(int year)
    {
        Workspace.Settings.JobNumberCounters ??= [];
        var counter = Workspace.Settings.JobNumberCounters.FirstOrDefault(item => item.Year == year);
        if (counter is null)
        {
            counter = new YearSequenceCounter { Year = year };
            Workspace.Settings.JobNumberCounters.Add(counter);
        }

        var minimumSequence = Workspace.Jobs.Count(job => job.CreatedOn.Year == year) + 1;
        counter.NextSequence = Math.Max(counter.NextSequence, minimumSequence);

        var current = counter.NextSequence;
        counter.NextSequence++;
        return current;
    }

    private string GenerateBidNumber(LaborTemplate template, DateTime createdOn)
    {
        var year = createdOn.Year;
        var sequence = GetNextBidSequence(year);
        var format = string.IsNullOrWhiteSpace(template.BidNumberFormat) ? "BID-YY-NNNN" : template.BidNumberFormat.Trim();
        var result = format
            .Replace("YYYY", year.ToString("0000", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("YY", (year % 100).ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        var firstN = result.IndexOf('N');
        if (firstN >= 0)
        {
            var count = 0;
            for (var index = firstN; index < result.Length && result[index] == 'N'; index++)
            {
                count++;
            }

            result = result.Replace(new string('N', count), sequence.ToString(new string('0', count), CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
        else
        {
            result = $"{result}-{sequence:0000}";
        }

        return result;
    }

    private string GenerateJobNumber(DateTime createdOn)
    {
        var year = createdOn.Year;
        var sequence = GetNextJobSequence(year);
        return FormatJobNumber(year, sequence);
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
        workspace.Settings.BidNumberCounters ??= [];
        workspace.Settings.JobNumberCounters ??= [];
        workspace.Clients ??= [];
        workspace.Templates ??= [];
        workspace.Bids ??= [];
        workspace.Jobs ??= [];
        workspace.ServiceAgreements ??= [];

        NormalizeJobNumbers(workspace);

        if (workspace.Templates.Count == 0)
        {
            var starterTemplate = CreateStarterTemplate("Standard Device Labor");
            workspace.Templates.Add(starterTemplate);
            workspace.Settings.DefaultTemplateId = starterTemplate.Id;
        }

        foreach (var template in workspace.Templates)
        {
            template.Rules ??= [];
            if (string.IsNullOrWhiteSpace(template.BidNumberFormat))
            {
                template.BidNumberFormat = "BID-YY-NNNN";
            }

            if (template.DefaultMarkupPercent <= 0m)
            {
                template.DefaultMarkupPercent = workspace.Settings.DefaultMarkupPercent;
            }

            if (template.JourneymanRegularDirectRate <= 0m)
            {
                template.JourneymanRegularDirectRate = workspace.Settings.FieldLaborRate;
            }

            if (template.JourneymanRegularBilledRate <= 0m)
            {
                template.JourneymanRegularBilledRate = template.JourneymanRegularDirectRate;
            }

            if (template.JourneymanOvernightDirectRate <= 0m)
            {
                template.JourneymanOvernightDirectRate = template.JourneymanRegularDirectRate * 1.5m;
            }

            if (template.JourneymanOvernightBilledRate <= 0m)
            {
                template.JourneymanOvernightBilledRate = template.JourneymanRegularBilledRate * 1.5m;
            }

            if (template.ApprenticeRegularDirectRate <= 0m)
            {
                template.ApprenticeRegularDirectRate = Math.Max(1m, template.JourneymanRegularDirectRate * 0.7m);
            }

            if (template.ApprenticeRegularBilledRate <= 0m)
            {
                template.ApprenticeRegularBilledRate = Math.Max(1m, template.JourneymanRegularBilledRate * 0.72m);
            }

            if (template.ApprenticeOvernightDirectRate <= 0m)
            {
                template.ApprenticeOvernightDirectRate = template.ApprenticeRegularDirectRate * 1.5m;
            }

            if (template.ApprenticeOvernightBilledRate <= 0m)
            {
                template.ApprenticeOvernightBilledRate = template.ApprenticeRegularBilledRate * 1.5m;
            }

            if (template.AdminDirectRate <= 0m)
            {
                template.AdminDirectRate = workspace.Settings.AdminLaborRate;
            }

            if (template.AdminBilledRate <= 0m)
            {
                template.AdminBilledRate = template.AdminDirectRate;
            }

            if (template.EngineeringDirectRate <= 0m)
            {
                template.EngineeringDirectRate = workspace.Settings.EngineeringLaborRate;
            }

            if (template.EngineeringBilledRate <= 0m)
            {
                template.EngineeringBilledRate = template.EngineeringDirectRate;
            }
        }

        if (workspace.Settings.DefaultTemplateId is null || workspace.Templates.All(template => template.Id != workspace.Settings.DefaultTemplateId))
        {
            workspace.Settings.DefaultTemplateId = workspace.Templates.FirstOrDefault(template => !template.IsArchived)?.Id ?? workspace.Templates.First().Id;
        }

        var defaultTemplate = workspace.Templates.First(template => template.Id == workspace.Settings.DefaultTemplateId);

        foreach (var bid in workspace.Bids)
        {
            bid.Site ??= new SiteInformation();
            bid.LaborDistribution ??= [];
            bid.AdministrativeTasks ??= [];
            bid.EngineeringTasks ??= [];
            bid.Components ??= [];
            bid.DemoItems ??= [];
            bid.Materials ??= [];
            bid.Attachments ??= [];

            if (string.IsNullOrWhiteSpace(bid.Site.ScopeOfWork) && !string.IsNullOrWhiteSpace(bid.ScopeSummary))
            {
                bid.Site.ScopeOfWork = bid.ScopeSummary;
            }

            if (string.IsNullOrWhiteSpace(bid.Site.OccupancyGroup) && !string.IsNullOrWhiteSpace(bid.Site.OccupancyType))
            {
                bid.Site.OccupancyGroup = bid.Site.OccupancyType;
            }

            if (!string.IsNullOrWhiteSpace(bid.Site.Notes))
            {
                bid.Notes = string.IsNullOrWhiteSpace(bid.Notes)
                    ? bid.Site.Notes
                    : $"{bid.Notes}{Environment.NewLine}{Environment.NewLine}Site notes: {bid.Site.Notes}";
                bid.Site.Notes = string.Empty;
            }

            if (bid.TemplateId is null || workspace.Templates.All(template => template.Id != bid.TemplateId))
            {
                bid.TemplateId = workspace.Settings.DefaultTemplateId;
            }

            var template = workspace.Templates.First(template => template.Id == bid.TemplateId);

            if (bid.MarkupPercent <= 0m)
            {
                bid.MarkupPercent = template.DefaultMarkupPercent > 0m ? template.DefaultMarkupPercent : defaultTemplate.DefaultMarkupPercent;
            }

            if (bid.JourneymanRegularDirectRate <= 0m)
            {
                bid.JourneymanRegularDirectRate = bid.FieldLaborRate > 0m ? bid.FieldLaborRate : template.JourneymanRegularDirectRate;
            }

            if (bid.JourneymanRegularBilledRate <= 0m)
            {
                bid.JourneymanRegularBilledRate = bid.FieldLaborRate > 0m ? bid.FieldLaborRate : template.JourneymanRegularBilledRate;
            }

            if (bid.JourneymanOvernightDirectRate <= 0m)
            {
                bid.JourneymanOvernightDirectRate = template.JourneymanOvernightDirectRate;
            }

            if (bid.JourneymanOvernightBilledRate <= 0m)
            {
                bid.JourneymanOvernightBilledRate = template.JourneymanOvernightBilledRate;
            }

            if (bid.ApprenticeRegularDirectRate <= 0m)
            {
                bid.ApprenticeRegularDirectRate = template.ApprenticeRegularDirectRate;
            }

            if (bid.ApprenticeRegularBilledRate <= 0m)
            {
                bid.ApprenticeRegularBilledRate = template.ApprenticeRegularBilledRate;
            }

            if (bid.ApprenticeOvernightDirectRate <= 0m)
            {
                bid.ApprenticeOvernightDirectRate = template.ApprenticeOvernightDirectRate;
            }

            if (bid.ApprenticeOvernightBilledRate <= 0m)
            {
                bid.ApprenticeOvernightBilledRate = template.ApprenticeOvernightBilledRate;
            }

            if (bid.AdminDirectRate <= 0m)
            {
                bid.AdminDirectRate = bid.AdminLaborRate > 0m ? bid.AdminLaborRate : template.AdminDirectRate;
            }

            if (bid.AdminBilledRate <= 0m)
            {
                bid.AdminBilledRate = bid.AdminLaborRate > 0m ? bid.AdminLaborRate : template.AdminBilledRate;
            }

            if (bid.EngineeringDirectRate <= 0m)
            {
                bid.EngineeringDirectRate = bid.EngineeringLaborRate > 0m ? bid.EngineeringLaborRate : template.EngineeringDirectRate;
            }

            if (bid.EngineeringBilledRate <= 0m)
            {
                bid.EngineeringBilledRate = bid.EngineeringLaborRate > 0m ? bid.EngineeringLaborRate : template.EngineeringBilledRate;
            }

            bid.FieldLaborRate = bid.JourneymanRegularDirectRate;
            bid.AdminLaborRate = bid.AdminDirectRate;
            bid.EngineeringLaborRate = bid.EngineeringDirectRate;

            foreach (var task in bid.AdministrativeTasks.Concat(bid.EngineeringTasks))
            {
                if (task.PricingMode == TaskPricingMode.Fixed && task.SalePrice <= 0m && task.CostPrice > 0m)
                {
                    task.SalePrice = EstimateMath.GetDefaultSaleFromMarkup(task.CostPrice, bid.MarkupPercent);
                }
            }

            foreach (var component in bid.Components)
            {
                if (component.UnitSale <= 0m && component.UnitCost > 0m)
                {
                    component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, bid.MarkupPercent);
                }
            }

            foreach (var demoItem in bid.DemoItems)
            {
                if (string.IsNullOrWhiteSpace(demoItem.Name))
                {
                    demoItem.Name = "Demo Item";
                }
            }

            foreach (var material in bid.Materials)
            {
                if (material.Kind == BidMaterialKind.Unknown)
                {
                    material.Kind = string.Equals(material.Category, "Wire", StringComparison.OrdinalIgnoreCase)
                        ? BidMaterialKind.Wire
                        : BidMaterialKind.Material;
                }

                if (material.UnitSale <= 0m && material.UnitCost > 0m)
                {
                    material.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
                }
            }

            EnsureLaborDistributionRows(bid);
            if (EstimateMath.GetBidAllocatedFieldHours(bid) <= 0m && EstimateMath.GetBidCalculatedFieldHours(bid) > 0m)
            {
                RebalanceLaborDistributionToCalculatedHours(bid);
            }
        }

        foreach (var job in workspace.Jobs)
        {
            job.Site ??= new SiteInformation();
            job.Baseline ??= new BaselineEstimate();
            job.Baseline.AdministrativeTasks ??= [];
            job.Baseline.EngineeringTasks ??= [];
            job.Baseline.Components ??= [];
            job.Baseline.DemoItems ??= [];
            job.Baseline.Materials ??= [];
            job.Baseline.LineItems ??= [];
            job.JobDevices ??= [];
            job.Invoices ??= [];
            job.TimeEntries ??= [];
            job.MaterialPurchases ??= [];
            job.ChangeOrders ??= [];
            job.ScheduleOfValues ??= [];
            job.Commitments ??= [];
            job.Attachments ??= [];
            if (job.SourceBidId is not null)
            {
                var bid = workspace.Bids.FirstOrDefault(item => item.Id == job.SourceBidId.Value);
                if (bid is not null)
                {
                    JobFinancialBuilder.RefreshBaselineFromBid(job, bid);
                }
            }

            JobFinancialBuilder.EnsureJobDerivedData(job);
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

    private static void NormalizeJobNumbers(FyreWorksWorkspace workspace)
    {
        workspace.Settings.JobNumberCounters ??= [];

        var nextSequenceByYear = new Dictionary<int, int>();
        foreach (var counter in workspace.Settings.JobNumberCounters.Where(counter => counter.Year > 0))
        {
            if (nextSequenceByYear.TryGetValue(counter.Year, out var existing))
            {
                nextSequenceByYear[counter.Year] = Math.Max(existing, Math.Max(1, counter.NextSequence));
            }
            else
            {
                nextSequenceByYear[counter.Year] = Math.Max(1, counter.NextSequence);
            }
        }

        var usedSequencesByYear = new Dictionary<int, HashSet<int>>();
        foreach (var job in workspace.Jobs
                     .OrderBy(job => job.CreatedOn)
                     .ThenBy(job => job.JobNumber, StringComparer.OrdinalIgnoreCase))
        {
            var (year, preferredSequence) = ParseExistingJobNumber(job.JobNumber, job.CreatedOn);
            if (!usedSequencesByYear.TryGetValue(year, out var usedSequences))
            {
                usedSequences = [];
                usedSequencesByYear[year] = usedSequences;
            }

            var nextSequence = nextSequenceByYear.TryGetValue(year, out var trackedNextSequence)
                ? Math.Max(1, trackedNextSequence)
                : 1;

            var assignedSequence = preferredSequence > 0 && !usedSequences.Contains(preferredSequence)
                ? preferredSequence
                : GetNextAvailableSequence(usedSequences, nextSequence);

            usedSequences.Add(assignedSequence);
            nextSequenceByYear[year] = Math.Max(nextSequence, assignedSequence + 1);
            job.JobNumber = FormatJobNumber(year, assignedSequence);
        }

        workspace.Settings.JobNumberCounters = nextSequenceByYear
            .OrderBy(item => item.Key)
            .Select(item => new YearSequenceCounter
            {
                Year = item.Key,
                NextSequence = Math.Max(1, item.Value)
            })
            .ToList();
    }

    private static (int Year, int Sequence) ParseExistingJobNumber(string? jobNumber, DateTime createdOn)
    {
        if (!string.IsNullOrWhiteSpace(jobNumber))
        {
            var trimmed = jobNumber.Trim();
            var standardMatch = StandardJobNumberPattern.Match(trimmed);
            if (standardMatch.Success)
            {
                var shortYear = int.Parse(standardMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
                var sequence = int.Parse(standardMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
                return (2000 + shortYear, sequence);
            }

            var legacyMatch = LegacyJobNumberPattern.Match(trimmed);
            if (legacyMatch.Success)
            {
                var fullYear = int.Parse(legacyMatch.Groups["date"].Value[..4], CultureInfo.InvariantCulture);
                var sequence = int.Parse(legacyMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
                return (fullYear, sequence);
            }
        }

        return (Math.Max(2000, createdOn.Year), 0);
    }

    private static int GetNextAvailableSequence(HashSet<int> usedSequences, int startingSequence)
    {
        var candidate = Math.Max(1, startingSequence);
        while (usedSequences.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private static string FormatJobNumber(int year, int sequence) =>
        $"JOB-{(year % 100):00}-{sequence:0000}";

    private static void EnsureLaborDistributionRows(BidRecord bid)
    {
        var requiredRows = new[]
        {
            (PersonnelType.Journeyman, HourType.Regular),
            (PersonnelType.Journeyman, HourType.Overnight),
            (PersonnelType.Apprentice, HourType.Regular),
            (PersonnelType.Apprentice, HourType.Overnight)
        };

        foreach (var (personnelType, hourType) in requiredRows)
        {
            if (bid.LaborDistribution.All(line => line.PersonnelType != personnelType || line.HourType != hourType))
            {
                bid.LaborDistribution.Add(new BidLaborDistributionLine
                {
                    PersonnelType = personnelType,
                    HourType = hourType
                });
            }
        }

        bid.LaborDistribution = bid.LaborDistribution
            .OrderBy(line => line.PersonnelType)
            .ThenBy(line => line.HourType)
            .ToList();
    }

    private static void RebalanceLaborDistributionToCalculatedHours(BidRecord bid)
    {
        EnsureLaborDistributionRows(bid);
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.InstallHours, (line, value) => line.InstallHours = value, EstimateMath.GetBidInstallHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.DemoHours, (line, value) => line.DemoHours = value, EstimateMath.GetBidDemoHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.TrimHours, (line, value) => line.TrimHours = value, EstimateMath.GetBidTrimHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.TestHours, (line, value) => line.TestHours = value, EstimateMath.GetBidTestHours(bid));
    }

    private void SyncLinkedJobBaseline(JobRecord job)
    {
        if (job.SourceBidId is not null)
        {
            var bid = Workspace.Bids.FirstOrDefault(item => item.Id == job.SourceBidId.Value);
            if (bid is not null)
            {
                JobFinancialBuilder.RefreshBaselineFromBid(job, bid);
            }
        }

        JobFinancialBuilder.EnsureJobDerivedData(job);
    }

    private static void RebalanceDistributionColumn(
        List<BidLaborDistributionLine> lines,
        Func<BidLaborDistributionLine, decimal> selector,
        Action<BidLaborDistributionLine, decimal> assign,
        decimal targetTotal)
    {
        targetTotal = EstimateMath.RoundHours(targetTotal);
        var defaultLine = lines.First(line => line.PersonnelType == PersonnelType.Journeyman && line.HourType == HourType.Regular);

        if (targetTotal <= 0m)
        {
            foreach (var line in lines)
            {
                assign(line, 0m);
            }

            return;
        }

        var currentTotal = lines.Sum(selector);
        if (currentTotal <= 0m)
        {
            foreach (var line in lines)
            {
                assign(line, 0m);
            }

            assign(defaultLine, targetTotal);
            return;
        }

        var scale = targetTotal / currentTotal;
        foreach (var line in lines)
        {
            assign(line, EstimateMath.RoundHours(selector(line) * scale));
        }

        var delta = targetTotal - lines.Sum(selector);
        if (delta > 0m)
        {
            assign(defaultLine, EstimateMath.RoundHours(selector(defaultLine) + delta));
        }
        else if (delta < 0m)
        {
            var remaining = Math.Abs(delta);
            foreach (var line in lines.OrderByDescending(selector))
            {
                if (remaining <= 0m)
                {
                    break;
                }

                var currentValue = selector(line);
                if (currentValue <= 0m)
                {
                    continue;
                }

                var reduction = Math.Min(currentValue, remaining);
                reduction = EstimateMath.RoundHours(reduction);
                if (reduction <= 0m)
                {
                    continue;
                }

                assign(line, currentValue - reduction);
                remaining = EstimateMath.RoundHours(remaining - reduction);
            }
        }
    }

    private static LaborRule? FindRule(LaborTemplate? template, string? locationProfile, string? installType)
    {
        if (template is null)
        {
            return null;
        }

        var normalizedLocation = string.IsNullOrWhiteSpace(locationProfile) ? string.Empty : locationProfile.Trim();
        var normalizedInstallType = string.IsNullOrWhiteSpace(installType) ? string.Empty : installType.Trim();

        return template.Rules.FirstOrDefault(candidate =>
            string.Equals(candidate.LocationProfile.Trim(), normalizedLocation, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.InstallType.Trim(), normalizedInstallType, StringComparison.OrdinalIgnoreCase));
    }

    private static LaborTemplate CreateStarterTemplate(string name) =>
        new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Standard Device Labor" : name,
            Notes = "Starter bid profile with numbering, labor rates, markup defaults, and the install time matrix.",
            BidNumberFormat = "BID-YY-NNNN",
            DefaultMarkupPercent = 22m,
            JourneymanRegularDirectRate = 65m,
            JourneymanRegularBilledRate = 90m,
            JourneymanOvernightDirectRate = 97.5m,
            JourneymanOvernightBilledRate = 135m,
            ApprenticeRegularDirectRate = 45m,
            ApprenticeRegularBilledRate = 65m,
            ApprenticeOvernightDirectRate = 67.5m,
            ApprenticeOvernightBilledRate = 97.5m,
            AdminDirectRate = 68m,
            AdminBilledRate = 95m,
            EngineeringDirectRate = 96m,
            EngineeringBilledRate = 130m,
            Rules =
            [
                new LaborRule
                {
                    LocationProfile = "Normal Area",
                    InstallType = "Normal",
                    InstallMinutes = 15m,
                    DemoMinutes = 0m,
                    TrimMinutes = 10m,
                    TestMinutes = 5m,
                    Notes = "Typical ceiling or wall mounted device in a standard environment."
                },
                new LaborRule
                {
                    LocationProfile = "Warehouse",
                    InstallType = "Lift",
                    InstallMinutes = 60m,
                    DemoMinutes = 10m,
                    TrimMinutes = 30m,
                    TestMinutes = 15m,
                    Notes = "Use when lift staging or elevated access is required."
                },
                new LaborRule
                {
                    LocationProfile = "Control Room",
                    InstallType = "Panel",
                    InstallMinutes = 45m,
                    DemoMinutes = 10m,
                    TrimMinutes = 25m,
                    TestMinutes = 12m,
                    Notes = "Panel work, terminations, and coordination time."
                },
                new LaborRule
                {
                    LocationProfile = "Finished Office",
                    InstallType = "Pipe",
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

