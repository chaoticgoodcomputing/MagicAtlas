namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

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

  // "... for each time it was kicked" (Wolfbriar Elemental) — the token count is the
  // keyword cost-paid count (CR 702.33d/f), not the article count. Reference-not-resolution
  // (ADR 0004): a KeywordCostPaidCountQuantity keyed on KeywordAbility.Kicker, the consumer
  // half of the multikicker producer on the same card.
  private static readonly System.Text.RegularExpressions.Regex _forEachTimeKicked =
    new(@"for\s+each\s+time\s+(?:it|this\s+\w+)\s+was\s+kicked",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new CreateTokenEffect {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food()}, isOptional);
      return true;
    }

    // --- Predefined artifact token: Treasure ---
    // "create a Treasure token[.]" — Rule 107.10b, Treasure subtype.
    if (_treasureTokenPattern.IsMatch(createText))
    {
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new CreateTokenEffect {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Treasure()}, isOptional);
      return true;
    }

    // --- Predefined artifact token: Clue ---
    // "create a Clue token[.]" — Rule 107.10b, Clue subtype.
    if (_clueTokenPattern.IsMatch(createText))
    {
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new CreateTokenEffect {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Clue()}, isOptional);
      return true;
    }

    // --- Predefined artifact token: Blood ---
    // "create a Blood token[.]" — Rule 107.10b, Blood subtype.
    if (_bloodTokenPattern.IsMatch(createText))
    {
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new CreateTokenEffect {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Blood()}, isOptional);
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

    Quantity tokenCount = _forEachTimeKicked.IsMatch(createText)
      ? new KeywordCostPaidCountQuantity { Keyword = KeywordAbility.Kicker }
      : LiteralQuantity.Of(count);

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new CreateTokenEffect {
      Player = ObjectReference.You(),
      Count = tokenCount,
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = tokenTypes,
        Subtypes = subtypes,
        Abilities = tokenAbilities,
      }}, isOptional);
    return true;
  }
}
