using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Host.Budgeting;

internal sealed class CostBudgetOptions
{
    public const string SectionName = "CostBudget";

    public bool Enabled { get; init; }

    [Range(0, long.MaxValue)]
    public long DailyTokenLimitPerKey { get; init; } = 1_000_000;
}
