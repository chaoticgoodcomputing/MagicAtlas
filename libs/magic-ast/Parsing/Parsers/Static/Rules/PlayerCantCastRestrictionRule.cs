namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "You can't cast [class] spells." — a CASTER-scoped sibling of
/// <see cref="CantBeCastRestrictionRule"/>'s unscoped "[class] spells can't be
/// cast." form. CR 601.3a: "If an effect prohibits a player from casting a
/// spell with certain qualities..." — this rule recognizes the shape where the
/// prohibited player is named as "you" (the controller of this static
/// ability), e.g. Steel Golem's "You can't cast creature spells."
/// </summary>
/// <remarks>
/// The spell class is generalized over the single-card-type token immediately
/// preceding "spells" (creature, artifact, enchantment, instant, sorcery,
/// planeswalker, battle) with an optional "non" prefix, mirroring the
/// CardTypes/ExcludedCardTypes negation axis used throughout
/// <see cref="MagicAST.AST.References.ObjectFilter"/>. "creature spells" →
/// <c>CardTypes=["spell","creature"]</c> (both a spell AND of the named type,
/// per the Nullify "creature or Aura spell" convention); "noncreature spells"
/// → <c>CardTypes=["spell"]</c> + <c>ExcludedCardTypes=["creature"]</c>.
/// </remarks>
[StaticRule(Priority = 971)]
public sealed class PlayerCantCastRestrictionRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+can'?t\s+cast\s+(?<neg>non)?(?<type>artifact|creature|enchantment|instant|sorcery|planeswalker|battle)\s+spells\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var type = match.Groups["type"].Value.ToLowerInvariant();
    var isNegated = match.Groups["neg"].Success;

    return
    [
      new StaticAbility
      {
        Effects = [new CantBeCastEffect { Caster = ObjectReference.You() }],
        AffectedObjects = isNegated
          ? new ObjectFilter { CardTypes = ["spell"], ExcludedCardTypes = [type] }
          : new ObjectFilter { CardTypes = ["spell", type] },
      },
    ];
  }
}
