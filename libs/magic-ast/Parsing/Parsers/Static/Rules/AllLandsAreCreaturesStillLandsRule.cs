namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "All lands are [P/T] creatures that are still lands." — the Nature's Revolt
/// "animate lands" template: a single always-on static continuous effect (CR 611)
/// that turns EVERY land into a creature with the stated power/toughness box,
/// retaining the land card type.
///
/// <para>
/// This is the static (unconditional, no cost, no duration) sibling of the
/// Keyrune/manland "becomes a creature" animate family
/// (<see cref="MagicAST.Parsing.Parsers.Activated.Rules.BecomesCreatureEffectRule"/>):
/// same <see cref="BecomesCreatureEffect"/> node, but here the subject is EVERY land
/// in the game (<see cref="ObjectReferenceKind.Each"/>, CardTypes=["land"]) rather
/// than the source permanent (<see cref="ObjectReferenceKind.Self"/>), and the
/// effect has no stated <c>Duration</c> — it is an always-on characteristic-setting
/// static ability (CR 604.1) that lasts as long as the source enchantment remains on
/// the battlefield, not a fixed-length continuous effect.
/// </para>
///
/// <para>
/// <b>"still lands" retention (CR 205.1b).</b> "Some effects change an object's card
/// type, supertype, or subtype but specify that the object retains a prior card type
/// ... This rule applies to effects that ... state that something is 'still a
/// [type...]'." CR 305.7 confirms an animated land "doesn't add or remove any card
/// types ... it keeps its land types". So the retained <c>land</c> card type sits
/// ahead of the added <c>creature</c> in <see cref="BecomesCreatureEffect.CardTypes"/>
/// — mirroring the manland "It's still a land." reminder consumption, except here the
/// retention is stated inline ("that are still lands") rather than as a trailing
/// sentence.
/// </para>
///
/// <para>
/// Canonical card: Nature's Revolt — "All lands are 2/2 creatures that are still
/// lands."
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "All lands are P/T creatures that are still lands"
/// shape so it cannot collide with siblings that use different verbs/qualifiers
/// (e.g. "become" instead of "are", a "you control" controller qualifier, or an
/// "Until end of turn" prefix) — those are different static/activated shapes, not
/// this one.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class AllLandsAreCreaturesStillLandsRule : IStaticRule
{
  // "All lands are 2/2 creatures that are still lands."
  private static readonly Regex _pattern = new(
    @"^\s*All\s+lands\s+are\s+(?<p>\d+|X)/(?<t>\d+|X)\s+creatures?\s+that\s+are\s+still\s+lands\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new BecomesCreatureEffect
          {
            Subject = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["land"] },
            },
            Power = ParsePT(m.Groups["p"].Value),
            Toughness = ParsePT(m.Groups["t"].Value),
            Colors = [],
            CardTypes = ["land", "creature"],
            AddedSubtypes = [],
            GainedAbilities = [],
          },
        ],
      },
    ];
  }

  // Animate P/T is a fixed literal ("2/2") or a variable ("X/X").
  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
