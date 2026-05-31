namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Escalate effect: a modal spell's "Escalate [cost]" keyword. Paying the escalate
/// cost for each mode chosen beyond the first lets the controller choose multiple modes
/// of a modal spell as an additional cost. CR 702.120a:
/// "Escalate is a static ability of modal spells (see rule 700.2) that functions while
/// the spell with escalate is on the stack. \"Escalate [cost]\" means \"For each mode you
/// choose beyond the first as you cast this spell, you pay an additional [cost].\" Paying
/// a spell's escalate cost follows the rules for paying additional costs in rules 601.2f-h."
/// MAST records only the keyword's presence and its cost parameter; the per-mode-cost
/// multiplication is engine territory, not described by the oracle line itself.
/// </summary>
[OracleEffect("escalate")]
public sealed record EscalateEffect : Effect
{
  /// <summary>
  /// The additional cost paid per mode chosen beyond the first.
  /// </summary>
  public required Cost Cost { get; init; }
}
