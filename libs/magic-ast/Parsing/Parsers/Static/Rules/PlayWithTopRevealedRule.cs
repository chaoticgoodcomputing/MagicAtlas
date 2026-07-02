namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Parses "Play with the top card of your library revealed." and produces a
/// <see cref="StaticAbility"/> with a <see cref="PlayWithTopRevealedEffect"/>.
///
/// <para>
/// CR 701.18c: "Some effects instruct a player to 'play' with a certain aspect
/// of the game changed, such as 'Play with the top card of your library
/// revealed.' 'Play' in this sense means to play the Magic game."
/// CR 401.5 governs how the revealed top-card rule interacts with spells being
/// cast; CR 401.6 governs card-object identity when the top card changes.
/// </para>
///
/// <para>
/// This is a continuous, non-optional static effect: the controller must keep
/// the top card of their library face-up at all times while the source permanent
/// is on the battlefield (CR 604.2). The effect is NOT wrapped in
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>. The pattern is
/// fully anchored (^…$) to prevent matching "Play with …" substrings inside
/// longer ability lines.
/// </para>
/// </summary>
[StaticRule(Priority = 942)]
public sealed class PlayWithTopRevealedRule : IStaticRule
{
  // Fully anchored to avoid matching "Play with …" as a substring of a
  // more complex ability. Trailing period is optional for formatting variants.
  private static readonly Regex _pattern = new(
    @"^\s*Play\s+with\s+the\s+top\s+card\s+of\s+your\s+library\s+revealed\.?\s*$",
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
        Effects = [new PlayWithTopRevealedEffect()],
      },
    ];
  }
}
