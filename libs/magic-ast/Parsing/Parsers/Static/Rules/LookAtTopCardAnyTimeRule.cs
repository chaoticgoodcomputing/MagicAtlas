namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Recognises "You may look at the top card of your library any time." and
/// produces a <see cref="StaticAbility"/> with a <see cref="LookAtTopCardAnyTimeEffect"/>.
///
/// <para>
/// This line appears on Sphinx of Jwar Isle, Bolas's Citadel, and similar cards
/// that grant the controller a continuous permission to inspect their library's
/// top card. It was previously misrouted to the spell parser; this rule intercepts
/// it in the static pipeline where it belongs (Rule 604.3 — continuous effects).
/// </para>
/// </summary>
[StaticRule(Priority = 941)]
public sealed class LookAtTopCardAnyTimeRule : IStaticRule
{
  // Matches "You may look at the top card of your library any time."
  // The trailing period is optional for minor formatting variants.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+look\s+at\s+the\s+top\s+card\s+of\s+your\s+library\s+any\s+time\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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
        Effects = [new MagicAST.AST.Effects.Core.OptionalEffect { Inner = new LookAtTopCardAnyTimeEffect {} }],
      },
    ];
  }
}
