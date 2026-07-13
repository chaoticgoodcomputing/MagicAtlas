namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;

/// <summary>
/// "This creature can't have counters put on it." — a static counter-prohibition
/// (CR 122: counters) scoped to the ability's own controlling object (Self), e.g.
/// Melira's Keepers.
/// </summary>
/// <remarks>
/// Anchored full-line match: only the reflexive "This creature/permanent" subject
/// is recognized — a board-wide or typed-filter subject ("Creatures you control
/// can't have counters put on them.", "Other creatures you control…") is a
/// DIFFERENT shape (a filtered/plural <c>Target</c>) and must not be swallowed by
/// this Self-only rule (the batch-1 over-capture lesson: null-ing <c>Target</c>
/// here would silently drop that filter). No self-by-name arm is needed for this
/// card family — the oracle text always leads with the reflexive "This creature".
/// </remarks>
[StaticRule(Priority = 960)]
public sealed class CantHaveCountersRule : IStaticRule
{
  private static readonly Regex _cantHaveCountersPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can['’]?t\s+have\s+counters\s+put\s+on\s+it\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_cantHaveCountersPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CantHaveCountersEffect()],
      },
    ];
  }
}
