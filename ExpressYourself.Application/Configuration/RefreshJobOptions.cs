using System.ComponentModel.DataAnnotations;

namespace ExpressYourself.Application.Configuration
{
    public sealed class RefreshJobOptions
    {
        public const string SectionName = "RefreshJob";

        public bool Enabled { get; init; } = true;

        [Required]
        public string CronExpression { get; init; } = "0 0 * * * ?";

        [Range(1, 1_000)]
        public int BatchSize { get; init; } = 100;

        [Range(1, 20)]
        public int MaxConcurrency { get; init; } = 4;
    }
}