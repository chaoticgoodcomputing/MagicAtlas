namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;

/// <summary>
/// "create [article] [P/T] [colors] [subtypes] creature token [with ...]" — Rule 111.
/// Also handles predefined artifact tokens (Food, Treasure, Clue, Blood) which
/// have no P/T and whose activated ability is reminder text only (Rule 107.10b).
/// Handles the optional "you may" prefix (Rule 116.1b) by setting IsOptional = true
/// on the resulting CreateTokenEffect.
/// </summary>
[TriggeredRule]
public sealed class CreateTokenRule : ITriggeredRule
{
  // Matches optional "you may" prefix before "create ...".
  // Rule 116.1b: a player "may" perform an action as an optional choice.
  private static readonly System.Text.RegularExpressions.Regex _youMayPrefix =
    new(@"^you\s+may\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  private static readonly System.Text.RegularExpressions.Regex _foodTokenPattern =
    new(@"^create a Food token\.?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  private static readonly System.Text.RegularExpressions.Regex _treasureTokenPattern =
    new(@"^create a Treasure token\.?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  private static readonly System.Text.RegularExpressions.Regex _clueTokenPattern =
    new(@"^create a Clue token\.?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  private static readonly System.Text.RegularExpressions.Regex _bloodTokenPattern =
    new(@"^create a Blood token\.?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!text.Contains("create", System.StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    // Strip optional "you may" prefix (Rule 116.1b). Track it so the produced
    // effect carries IsOptional = true to signal the player has a choice.
    var isOptional = false;
    var createText = text;
    var youMayMatch = _youMayPrefix.Match(text);
    if (youMayMatch.Success)
    {
      isOptional = true;
      createText = text[youMayMatch.Length..];
    }

    // --- Predefined artifact token: Food ---
    // "create a Food token[.]" — Rule 107.10b, Food subtype.
    // No P/T; reminder text describes the activated ability (engine territory, not modelled here).
    if (_foodTokenPattern.IsMatch(createText))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food(),
        IsOptional = isOptional,
      };
      return true;
    }

    // --- Predefined artifact token: Treasure ---
    // "create a Treasure token[.]" — Rule 107.10b, Treasure subtype.
    if (_treasureTokenPattern.IsMatch(createText))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Treasure(),
        IsOptional = isOptional,
      };
      return true;
    }

    // --- Predefined artifact token: Clue ---
    // "create a Clue token[.]" — Rule 107.10b, Clue subtype.
    if (_clueTokenPattern.IsMatch(createText))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Clue(),
        IsOptional = isOptional,
      };
      return true;
    }

    // --- Predefined artifact token: Blood ---
    // "create a Blood token[.]" — Rule 107.10b, Blood subtype.
    if (_bloodTokenPattern.IsMatch(createText))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Blood(),
        IsOptional = isOptional,
      };
      return true;
    }

    var (_, count) = TriggeredRuleHelpers.ParseArticle(createText);
    var powerToughness = TriggeredRuleHelpers.ParsePowerToughness(createText);
    if (powerToughness == null)
    {
      return false;
    }
    var colors = TriggeredRuleHelpers.ParseColors(createText);
    var subtypes = TriggeredRuleHelpers.ParseCreatureSubtypes(createText);
    if (subtypes.Count == 0)
    {
      return false;
    }

    var abilityNames = TriggeredRuleHelpers.ParseTokenAbilities(createText);
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

    var tokenTypes = TriggeredRuleHelpers.ParseTokenTypes(createText);

    effect = new CreateTokenEffect
    {
      Count = LiteralQuantity.Of(count),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = tokenTypes,
        Subtypes = subtypes,
        Abilities = tokenAbilities,
      },
      IsOptional = isOptional,
    };
    return true;
  }
}
