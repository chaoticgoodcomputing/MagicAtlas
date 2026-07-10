namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Enchanted [type] is a creature with base power and toughness [P]/[T] in addition
/// to its other types." — the Ensoul Artifact "animate the enchanted permanent"
/// template: a single always-on static continuous effect (CR 613) that turns the
/// enchanted permanent into a creature with a fixed base power/toughness box
/// (CR 208.3), while keeping whatever other types it already has.
///
/// <para>
/// This is the Aura-scoped sibling of the Nature's Revolt "animate lands" template
/// (<see cref="AllLandsAreCreaturesStillLandsRule"/>): same <see cref="BecomesCreatureEffect"/>
/// node, but here the subject is the single enchanted permanent
/// (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>) rather than every land in
/// the game, and the retained type is whatever the Aura's own "Enchant [type]" line
/// restricts it to (here, "artifact") rather than a hardcoded "land". The effect has
/// no stated <c>Duration</c> — it is an always-on characteristic-setting static
/// ability (CR 604.1) that lasts as long as the source Aura remains attached
/// (CR 613 layer application while the source persists), not a fixed-length
/// continuous effect.
/// </para>
///
/// <para>
/// "in addition to its other types" retention mirrors "that are still lands" in the
/// Nature's Revolt sibling: the enchanted permanent's prior card types (here,
/// "artifact", per the Aura's own "Enchant artifact" restriction) are retained ahead
/// of the added "creature" type in <see cref="BecomesCreatureEffect.CardTypes"/>.
/// </para>
///
/// <para>
/// Canonical card: Ensoul Artifact — "Enchanted artifact is a creature with base
/// power and toughness 5/5 in addition to its other types."
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "Enchanted [type] is a creature with base power and
/// toughness P/T in addition to its other types" shape so it cannot collide with the
/// unrelated "[X] in addition to its other types" siblings elsewhere in the codebase
/// (e.g. Raven Wings' "...and is a Bird in addition to its other types",
/// Titan of Littjara's "is the chosen type in addition to its other types") — those
/// grant a single subtype/type addition via <c>AddTypeEffect</c>, not a full
/// creature-animate-with-base-P/T composite.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class EnchantedIsCreatureWithBasePTRule : IStaticRule
{
  // "Enchanted artifact is a creature with base power and toughness 5/5 in addition
  // to its other types."
  private static readonly Regex _pattern = new(
    @"^\s*Enchanted\s+(?<type>artifact|land|creature|permanent|enchantment|planeswalker)\s+is\s+a\s+creature\s+with\s+base\s+power\s+and\s+toughness\s+(?<p>\d+|X)/(?<t>\d+|X)\s+in\s+addition\s+to\s+its\s+other\s+types\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var retainedType = m.Groups["type"].Value.ToLowerInvariant();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new BecomesCreatureEffect
          {
            Subject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            Power = ParsePT(m.Groups["p"].Value),
            Toughness = ParsePT(m.Groups["t"].Value),
            Colors = [],
            CardTypes = [retainedType, "creature"],
            AddedSubtypes = [],
            GainedAbilities = [],
          },
        ],
      },
    ];
  }

  // Animate P/T is a fixed literal ("5/5") or a variable ("X/X").
  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
