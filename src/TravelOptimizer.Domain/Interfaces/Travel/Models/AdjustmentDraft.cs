namespace TravelOptimizer.Domain.Interfaces.Travel.Models;

/// <summary>
/// Structured proposal as returned by the LLM (spec §9). Never free-form actions — only these four
/// fields, each of which is validated and shadow-evaluated before it can reach Active.
/// </summary>
public record AdjustmentDraft(string Kind, string Target, string Change, string Rationale);
