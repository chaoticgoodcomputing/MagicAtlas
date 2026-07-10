namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// "As this [permanent] enters, choose an opponent." (The Rack) — recognizes the
/// as-enters player-choice declaration RESTRICTED to opponents and emits a
/// composite <see cref="StaticAbility"/> carrying <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 614.1c) plus a <see cref="MagicAST.AST.Effects.Keyword.ChoosePlayerEffect"/>
/// with <see cref="MagicAST.AST.Effects.Keyword.ChoosePlayerEffect.Scope"/> set to
/// <see cref="ControllerFilter.Opponent"/> (CR 614.12 — the as-enters chosen-value
/// binding, here bound over the restricted "an opponent" pool rather than "a
/// player"). Downstream abilities reference the binding via
/// <see cref="ControllerFilter.ChosenPlayer"/>.
///
/// <para>Sibling of <see cref="ChoosePlayerOnEntryRule"/>: the regexes for "choose a
/// player" and "choose an opponent" are disjoint (the latter requires the "an
/// opponent" noun phrase, never present when the former matches), so dispatch
/// priority relative to that rule is immaterial. Kept as a SEPARATE effect
/// instance (not a shared TryParse helper) because the two surface phrases bind
/// different <see cref="MagicAST.AST.Effects.Keyword.ChoosePlayerEffect.Scope"/>
/// values — collapsing them into one regex/branch would risk silently dropping
/// the opponent restriction for one of the two surface forms.</para>
/// </summary>
[StaticRule(Priority = 942)]
public sealed class ChooseOpponentOnEntryRule : IStaticRule
{
  private static readonly Regex _chooseOpponentOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+an\s+opponent\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseOpponentOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects =
        [
          new MagicAST.AST.Effects.Keyword.ChoosePlayerEffect { Scope = ControllerFilter.Opponent },
        ],
      },
    ];
  }
}
