namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Totem armor (oracle text: "Umbra armor" on some Auras): If enchanted
/// creature would be destroyed, instead remove all damage from it and destroy
/// this Aura. Rule 702.102. The comp-rules name is "totem armor"; MAST stores
/// the keyword using that discriminator. The replacement-effect semantics are
/// engine territory.
///
/// <para>
/// Multi-word simple keyword ("Totem" "armor"). The <see cref="Definition"/>
/// is the verbatim former <c>KeywordDefinitions.TotemArmor</c>; the
/// <see cref="Combinator"/> is the verbatim former
/// <c>OracleParsers.TotemArmor</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class TotemArmorKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Totem armor",
      RuleReference = "702.102",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.TotemArmor,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.TotemArmor }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from totem in Keyword("Totem")
    from armor in Keyword("armor")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.TotemArmor,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.TotemArmor }],
      Reminder = reminder,
    }
  );
}
