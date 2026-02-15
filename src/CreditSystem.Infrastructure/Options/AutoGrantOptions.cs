namespace CreditSystem.Infrastructure.Options;

public class AutoGrantOptions
{
    public const string SectionName = "AutoGrant";

    public int GrantAmount { get; set; } = 100;
    public int GrantFrequencyDays { get; set; } = 3;
    public int CheckIntervalMinutes { get; set; } = 60;
}
