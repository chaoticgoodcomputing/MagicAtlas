namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Recognises "As long as [X] is attached to a creature, you may play lands and cast spells
/// from the top of your library." — a conditional static permission that allows the controller
/// to play lands and cast spells from the top of their library while the source Equipment is
/// attached to a creature. Paradigm card: The Reality Chip (NEO).
///
/// <para>
/// CR 702.151b: "Attaching an Equipment with reconfigure to another creature causes the
/// Equipment to stop being a creature until it becomes unattached from that creature."
/// CR 401.5: "Some effects … say that a player may look at the top card of their library."
/// CR 604.2: "Static abilities create continuous effects … active as long as the permanent
/// with the ability remains on the battlefield and has the ability."
/// </para>
///
/// <para>
/// The ability is conditional ("as long as [X] is attached to a creature") — the condition
/// body is captured verbatim into an <see cref="OtherCondition"/> residual because no
/// structured <c>IsAttachedToCreature</c> arm exists in the <see cref="Condition"/> union
/// yet (debt — PB-7 structured-condition buckets). The effect itself is fully structured as
/// a <see cref="MayPlayFromTopOfLibraryEffect"/> granting both
/// <see cref="PlayFromTopAction.PlayLands"/> and <see cref="PlayFromTopAction.CastSpells"/>.
/// </para>
///
/// <para>
/// Priority 939 — fires before <see cref="MayPlayFromTopOfLibraryRule"/> (940) so the
/// conditioned form is claimed before the unconditional Bolas's Citadel form. The pattern
/// is anchored (^…$) to prevent substring collision with any sibling rule.
/// </para>
/// </summary>
[StaticRule(Priority = 939)]
public sealed class AsLongAsAttachedMayPlayFromTopOfLibraryRule : IStaticRule
{
  // Anchored pattern: "As long as <condition>, you may play lands and cast spells from the
  // top of your library." The <cond> group captures the condition body (everything between
  // "As long as " and the comma). The trailing period is optional for minor formatting variants.
  //
  // Anchored (^…$): prevents this rule from matching as a substring inside a more-specific
  // sibling. MAST anchor contract: any matcher on a shared clause must be anchored.
  private static readonly Regex _pattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>[^,]+),\s*you\s+may\s+play\s+lands\s+and\s+cast\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var conditionText = match.Groups["cond"].Value.Trim();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MayPlayFromTopOfLibraryEffect
          {
            Actions = [PlayFromTopAction.PlayLands, PlayFromTopAction.CastSpells],
          },
        ],
        Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
      },
    ];
  }
}
