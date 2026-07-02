namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.Parsing;

/// <summary>
/// "[Card name] can be your commander." — printed on non-default-legal commander
/// cards (Teferi, Temporal Archmage; The Prismatic Bridge; etc.) to declare that
/// the card is usable as a commander even though it lacks the Legendary supertype
/// on its own type line or is otherwise excluded from the default commander pool.
///
/// <para>
/// Teferi, Temporal Archmage (C14) is Legendary and a Planeswalker, but carries
/// this clause because the Commander format at that time required separate printed
/// permission for planeswalkers to be commanders. Current Oracle errata still
/// preserves the text.
/// </para>
///
/// <para>
/// CR 903.3: "Each player chooses a legendary creature card or Legendary Planeswalker
/// card as their commander." The printed permission here expands (or confirms) that
/// the named card qualifies. MAST records this as a <see cref="CommanderDesignationEffect"/>
/// on a <see cref="StaticAbility"/> — the designation is a static fact about the card,
/// not a one-shot action (no zone-change, no event required).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the phrase "can be your commander" does not appear as a
/// substring of any other standard oracle-text clause. Anchoring is the defensive
/// convention.
/// </para>
/// </summary>
[StaticRule(Priority = 980)]
public sealed class CanBeYourCommanderStaticRule : IStaticRule
{
  // Matches "[Any card name] can be your commander."
  // Card names may contain commas, spaces, apostrophes, and hyphens.
  // ANCHORED (^…$) — defensive convention for all static rules.
  private static readonly Regex _pattern = new(
    @"^\s*.+\s+can\s+be\s+your\s+commander\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CommanderDesignationEffect()],
      },
    ];
  }
}
