namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each player draws a card" / "each opponent draws a card" — draw effect applied
/// to all players simultaneously or to all opponents simultaneously.
/// Rule 121 (Drawing a Card). MAST records the draw event and the player scope
/// (EachPlayer or EachOpponent); the simultaneous draw order for multiplayer is
/// engine territory, not described by the oracle text.
///
/// <para>
/// This rule covers the bare single-card form only. The composite
/// "each player draws a card and loses N life" (Stormfist Crusader shape) is
/// handled by the <c>TryParseEachPlayerDrawAndLoseLife</c> orchestrator in
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/>, which runs
/// before this rule and takes priority.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachPlayerDrawsTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value.ToLowerInvariant().Trim();
    var kind = scope == "opponent" ? ObjectReferenceKind.EachOpponent : ObjectReferenceKind.EachPlayer;

    var countRaw = m.Groups["count"].Value.ToLowerInvariant().Trim();
    var count = countRaw switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.TryParse(countRaw, out var n) ? n : 1,
    };

    effect = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = new ObjectReference { Kind = kind },
      IsOptional = false,
    };
    return true;
  }

  // Matches "each player draws a card" and "each opponent draws a card"
  // (and plurals: "two cards", "three cards", etc.).
  // The composite "and loses N life" form is handled upstream; this rule
  // should never see that text, but the negative lookahead guards it anyway.
  private static readonly Regex _pattern = new(
    @"^each\s+(?<scope>player|opponent)\s+draws\s+(?<count>a|one|two|three|\d+)\s+cards?(?!\s+and\s+loses)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
