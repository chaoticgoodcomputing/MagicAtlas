namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Thassa's Oracle ETB effect:
/// "Look at the top X cards of your library, where X is your devotion to blue.
///  Put up to one of them on top of your library and the rest on the bottom of
///  your library in a random order.
///  If X is greater than or equal to the number of cards in your library, you win the game."
///
/// <para>
/// The whole three-sentence fragment is a single coherent game action: the variable X is
/// defined in the first sentence and referenced by both the disposition sentence and the
/// win-condition. Splitting on sentences would break the X-binding, so this rule captures
/// all three sentences as one match and emits them as two structured effects:
/// <list type="number">
///   <item><see cref="OracleTopLookEffect"/> — the look-and-reorder action.</item>
///   <item><see cref="ConditionalEffect"/> — the QuantityComparisonCondition → WinTheGameEffect gate.</item>
/// </list>
/// CR 700.5 (devotion); CR 701.12 (look); CR 104.3a (you win the game).
/// </para>
///
/// <para>
/// Priority 95: must beat generic sentence-bundle dispatch and the plain LookAtTopCards rules.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class ThassasOracleRule : ITriggeredRule
{
  /// <summary>
  /// Matches the full three-sentence effect text (with optional trailing punctuation stripped by caller):
  /// "look at the top X cards of your library, where X is your devotion to {color}.
  ///  Put up to one of them on top of your library and the rest on the bottom of your library in a random order.
  ///  If X is greater than or equal to the number of cards in your library, you win the game"
  ///
  /// The color group captures one or more color names joined by " and " (e.g. "blue", "green and blue").
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^look\s+at\s+the\s+top\s+X\s+cards?\s+of\s+your\s+library,\s*where\s+X\s+is\s+your\s+devotion\s+to\s+(?<colors>(?:white|blue|black|red|green)(?:\s+and\s+(?:white|blue|black|red|green))*)\.\s*Put\s+up\s+to\s+one\s+of\s+them\s+on\s+top\s+of\s+your\s+library\s+and\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+a\s+random\s+order\.\s*If\s+X\s+is\s+greater\s+than\s+or\s+equal\s+to\s+the\s+number\s+of\s+cards\s+in\s+your\s+library,\s*you\s+win\s+the\s+game$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    // Parse the devotion color(s), e.g. "blue" → ["U"], "green and blue" → ["G", "U"]
    var colorNames = Regex.Split(
      match.Groups["colors"].Value,
      @"\s+and\s+",
      RegexOptions.IgnoreCase
    );

    var colorCodes = new List<string>(colorNames.Length);
    foreach (var name in colorNames)
    {
      if (!_colorNameToCode.TryGetValue(name.Trim(), out var code))
      {
        return false;
      }
      colorCodes.Add(code);
    }

    // Effect 1: look at top X (= devotion) cards, put up to one on top, rest on bottom random.
    var devotionQuantity = new DevotionQuantity { Colors = colorCodes };
    var lookEffect = new OracleTopLookEffect
    {
      Count = devotionQuantity,
      Player = ObjectReference.You(),
    };

    // Effect 2: if X (devotion) >= cards in library, you win.
    // X in the condition refers to the same devotion quantity used above.
    var winCondition = new QuantityComparisonCondition
    {
      Left = new DevotionQuantity { Colors = colorCodes },
      Operator = ComparisonOperator.GreaterThanOrEqual,
      Right = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInLibrary },
    };
    var winEffect = new WinTheGameEffect
    {
      Player = ObjectReference.You(),
    };
    var conditionalWin = new ConditionalEffect
    {
      Condition = winCondition,
      Then = winEffect,
    };

    effect = new CompositeEffect
    {
      Effects = new List<Effect> { lookEffect, conditionalWin },
    };
    return true;
  }
}
