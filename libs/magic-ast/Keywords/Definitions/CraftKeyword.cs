namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Craft with [materials] [cost]: an activated ability on double-faced cards.
///
/// <para>
/// CR 702.167a (verbatim): "Craft represents an activated ability. It is written as
/// \"Craft with [materials] [cost],\" where [materials] is a description of one or more
/// objects. It means \"[Cost], Exile this permanent, Exile [materials] from among
/// permanents you control and/or cards in your graveyard: Return this card to the
/// battlefield transformed under its owner's control. Activate only as a sorcery.\""
/// </para>
///
/// <para>
/// MAST models Craft as a single <see cref="ActivatedAbility"/> decomposed into its
/// three cost components and one effect (the Buyback / Embalm multi-part keyword
/// decomposition is the structural precedent):
/// </para>
/// <list type="bullet">
///   <item><description><b>Costs</b> = the printed mana <see cref="ManaCost"/>, then
///   an <see cref="ExileCost"/> for "Exile this permanent" (<c>IsSelf</c>, from the
///   battlefield), then an <see cref="ExileCost"/> for the [materials]
///   (here "another artifact you control or an artifact card from your graveyard").</description></item>
///   <item><description><b>Effect</b> = a <see cref="ReturnToBattlefieldEffect"/> of
///   <see cref="ObjectReference.Self"/> with <c>Transformed = true</c> and
///   <c>UnderControl = Owner</c> — "Return this card to the battlefield transformed
///   under its owner's control."</description></item>
///   <item><description><b>Restriction</b> = <see cref="ActivationRestriction.OnlyAsSorcery"/>
///   ("Activate only as a sorcery").</description></item>
/// </list>
///
/// <para>
/// The [materials] pool spans two zones — CR 702.167a "from among permanents you control
/// and/or cards in your graveyard". <see cref="ObjectFilter"/> carries a single
/// <see cref="Zone"/> and <see cref="ExileCost.FromZone"/> a single value with no
/// cross-zone disjunction axis, so the keyword-inherent dual pool is represented by
/// <c>Owner = You</c> ("yours") + <c>FromZone = Anywhere</c> (any Craft-permitted zone).
/// <c>ExcludeSelf = true</c> encodes "another"; <c>CardTypes</c> is the [materials] type.
/// The verbatim reminder is preserved in <see cref="Ability.Reminder"/>.
/// </para>
///
/// <para>
/// The combinator recognises the single-card-type-word material shape ("Craft with
/// artifact/creature/enchantment/land/planeswalker/battle [cost]"). Materials it cannot
/// faithfully structure (tokens, "historic", counted "three creatures", etc.) fall
/// through unparsed rather than mis-parsing into a wrong filter (per the lossy-parse
/// avoidance doctrine). The leading <c>Craft</c> + <c>with</c> anchor is unique to the
/// Craft keyword, so no sibling keyword shares its prefix.
/// </para>
/// </summary>
[Keyword]
public sealed class CraftKeyword : IKeyword
{
  /// <summary>Single-word [materials] descriptors this combinator maps to a card type.</summary>
  private static readonly HashSet<string> MaterialCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "artifact",
    "creature",
    "enchantment",
    "land",
    "planeswalker",
    "battle",
  };

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Craft")
    from with in Keyword("with")
    from material in Token.EqualTo(OracleToken.Word)
      .Where(t => MaterialCardTypes.Contains(t.ToStringValue()))
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Craft,
      Costs =
      [
        cost,
        new ExileCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["permanent"],
            IsSelf = true,
          },
          FromZone = Zone.Battlefield,
          Quantity = LiteralQuantity.Of(1),
        },
        new ExileCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = [material.ToStringValue().ToLowerInvariant()],
            Owner = ControllerFilter.You,
            ExcludeSelf = true,
          },
          FromZone = Zone.Anywhere,
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new ReturnToBattlefieldEffect
        {
          Target = ObjectReference.Self(),
          UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
          Transformed = true,
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );
}
