namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents the Class superstructure on an "Enchantment — Class" card
/// (Comprehensive Rules 716, "Class Cards"; AFR). A Class card has a base
/// ability printed in its top text-box section that is active at all times
/// (CR 716.3), followed by an ordered series of <em>class level bars</em>.
///
/// <para>
/// Each class level bar is a keyword ability that represents BOTH an activated
/// ability and a static ability (CR 716.2 / 107.16): paying the bar's activation
/// cost raises the Class to that level (the activated half — gained "as a sorcery"
/// per the reminder text), and the abilities printed in the same text-box section
/// as the bar become active while the Class is at that level or higher (the static
/// half). MAST models the bar as a <see cref="ClassLevel"/>: the level number, the
/// level-up cost, and the abilities that section grants.
/// </para>
///
/// <para>
/// MAST is descriptive, not an engine — the "becomes level N" gating of each level's
/// abilities and the sorcery-speed activation timing are derived from this structure
/// by consumers rather than encoded as runtime triggers/restrictions here. The
/// reminder line "(Gain the next level as a sorcery to add its ability.)" carries no
/// game function (CR 207.2) and is dropped at clause-split time.
/// </para>
/// </summary>
[OracleAbility("class")]
public sealed record ClassAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Class;

  /// <summary>
  /// The base abilities printed in the top text-box section (CR 716.3). These are
  /// active at all times — for Barbarian Class, the dice-advantage replacement
  /// ("If you would roll one or more dice, instead roll that many dice plus one and
  /// ignore the lowest roll."). Dispatched through the registry like any body
  /// abilities, so they land as whatever shape their text resolves to.
  /// Empty only on the (non-existent) Class with no top-section ability.
  /// </summary>
  public IReadOnlyList<Ability> BaseAbilities { get; init; } = [];

  /// <summary>The class level bars, in oracle-text order (level 2, level 3, …).</summary>
  public required IReadOnlyList<ClassLevel> Levels { get; init; }
}

/// <summary>
/// One class level bar on a Class card (CR 716.2). The bar names the level number
/// and the activation cost paid to raise the Class to that level; the abilities in
/// the same text-box section become active at that level.
/// </summary>
public sealed record ClassLevel
{
  /// <summary>The level number this bar advances the Class to (2, 3, …).</summary>
  public required int Level { get; init; }

  /// <summary>
  /// The activation cost of the level bar's activated ability — the mana paid to
  /// raise the Class to this level (e.g. <c>{1}{R}</c> for Barbarian Class level 2).
  /// CR 716.2 / 602.1a: the activation cost is everything before the level number.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>
  /// The abilities this level grants — the abilities printed in the same text-box
  /// section as the bar, which become part of the bar's static ability (CR 716.2).
  /// Dispatched through the registry, so they land as <see cref="TriggeredAbility"/>,
  /// <see cref="StaticAbility"/>, or whatever each body's classification resolves to.
  /// </summary>
  public IReadOnlyList<Ability> Abilities { get; init; } = [];
}
