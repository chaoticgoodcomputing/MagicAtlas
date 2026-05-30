namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Totem armor (oracle text: "Umbra armor"): If enchanted creature would be destroyed,
/// instead remove all damage from it and destroy this Aura.
/// Rule 702.102. The oracle text uses "Umbra armor" while the comp-rules name is
/// "totem armor". MAST records keyword presence using the comp-rules discriminator
/// "Totem armor"; the replacement-effect semantics are engine territory.
/// Multi-word keyword; mirrors LivingWeapon.
/// </summary>
[Keyword]
public sealed class UmbraArmorKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from umbra in Keyword("Umbra")
    from armor in Keyword("armor")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Totem armor",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.TotemArmor }],
      Reminder = reminder,
    }
  );
}
