namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Umbra armor (oracle text: "Umbra armor"): If enchanted creature would be destroyed,
/// instead remove all damage from it and destroy this Aura.
/// Rule 702.89. "Umbra armor" is the current Oracle name; "totem armor" is the obsolete
/// prior name (CR 702.89: the older text was renamed). MAST records keyword presence
/// using the discriminator that matches the card's current Oracle wording —
/// <see cref="KeywordAbility.UmbraArmor"/> — distinct from the obsolete
/// <see cref="KeywordAbility.TotemArmor"/> the literal "totem armor" combinator emits.
/// The replacement-effect semantics are engine territory. Multi-word keyword; mirrors
/// LivingWeapon.
/// </summary>
[Keyword]
public sealed class UmbraArmorKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from umbra in Keyword("Umbra")
    from armor in Keyword("armor")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.UmbraArmor,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.UmbraArmor }],
      Reminder = reminder,
    }
  );
}
