namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Equip [cost]: Attach to target creature you control. Activate only as a sorcery.
/// Rule 702.6. An activated ability that attaches this Equipment to a creature
/// you control. MAST records the keyword and its activation cost; the attach
/// mechanics and sorcery-speed restriction are derived from the rules
/// (per the descriptive-not-engine doctrine).
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EquipKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Equip")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Equip",
      Effects = [new EquipEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
