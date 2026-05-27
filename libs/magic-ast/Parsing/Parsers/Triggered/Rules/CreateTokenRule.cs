namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;

/// <summary>
/// "create [article] [P/T] [colors] [subtypes] creature token [with ...]" — Rule 111.
/// Also handles predefined artifact tokens (Food, Treasure, Clue, Blood) which
/// have no P/T and whose activated ability is reminder text only (Rule 107.10b).
/// </summary>
[TriggeredRule]
public sealed class CreateTokenRule : ITriggeredRule
{
  private static readonly System.Text.RegularExpressions.Regex _foodTokenPattern =
    new(@"^create a Food token\.?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!text.Contains("create", System.StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    // --- Predefined artifact token: Food ---
    // "create a Food token[.]" — Rule 111.10a, Food subtype.
    // No P/T; reminder text describes the activated ability (engine territory, not modelled here).
    if (_foodTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food(),
      };
      return true;
    }

    var (_, count) = TriggeredRuleHelpers.ParseArticle(text);
    var powerToughness = TriggeredRuleHelpers.ParsePowerToughness(text);
    if (powerToughness == null)
    {
      return false;
    }
    var colors = TriggeredRuleHelpers.ParseColors(text);
    var subtypes = TriggeredRuleHelpers.ParseCreatureSubtypes(text);
    if (subtypes.Count == 0)
    {
      return false;
    }

    var abilityNames = TriggeredRuleHelpers.ParseTokenAbilities(text);
    IReadOnlyList<Ability>? tokenAbilities = null;
    if (abilityNames.Count > 0)
    {
      var abilities = new List<Ability>();
      foreach (var name in abilityNames)
      {
        var sa = TriggeredRuleHelpers.BuildKeywordStaticAbility(name);
        if (sa is not null)
        {
          abilities.Add(sa);
        }
      }
      if (abilities.Count > 0)
      {
        tokenAbilities = abilities;
      }
    }

    effect = new CreateTokenEffect
    {
      Count = LiteralQuantity.Of(count),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = ["creature"],
        Subtypes = subtypes,
        Abilities = tokenAbilities,
      },
    };
    return true;
  }
}
