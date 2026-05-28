namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Reconfigure [cost]: Attach to target creature you control; or unattach
/// from a creature. Reconfigure only as a sorcery. While attached, this
/// isn't a creature.
/// Rule 702.173. Scope: mana-cost parameter (all known printings).
/// The attach/unattach mechanics, sorcery-speed restriction, and
/// creature-status switching are engine territory — MAST records the
/// keyword's presence and cost only, mirroring the EquipEffect pattern.
/// </summary>
[Keyword]
public sealed class ReconfigureKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Reconfigure",
      RuleReference = "702.173",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Reconfigure",
        Effects = [new ReconfigureEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Reconfigure")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Reconfigure",
      Effects = [new ReconfigureEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Mana-cost-parameter parser, inlined from the former
  /// <c>KeywordDefinitions.ParseManaCost</c>.
  /// </summary>
  private static ManaCost ParseManaCost(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Reconfigure requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
