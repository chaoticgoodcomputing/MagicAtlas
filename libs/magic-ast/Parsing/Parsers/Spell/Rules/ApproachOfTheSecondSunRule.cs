namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the full Approach of the Second Sun spell text:
/// "If this spell was cast from your hand and you've cast another spell named
/// Approach of the Second Sun this game, you win the game. Otherwise, put
/// Approach of the Second Sun into its owner's library seventh from the top
/// and you gain 7 life."
///
/// <para>
/// This is a bespoke one-spell rule covering all three clauses atomically:
/// the compound cast-history condition, the win-game then-branch, and the
/// self-library-placement + life-gain else-branch. The compound condition
/// ("cast from your hand AND you've cast another copy this game") has no
/// general structured form yet — it lands in <see cref="OtherCondition"/>
/// as a type-honest residual (ADR 0001/0007).
/// </para>
///
/// <para>
/// CR 104.2b (you win the game), CR 401.7 (Nth from the top), CR 601.2
/// (casting from the hand). Priority 95: must beat the general sentence-bundle
/// dispatcher, which would try to split on ". " and choke on the
/// "Otherwise, …" fragment.
/// </para>
/// </summary>
[SpellRule(Priority = 95)]
public sealed class ApproachOfTheSecondSunRule : ISpellRule
{
  /// <summary>
  /// Matches the complete oracle text for Approach of the Second Sun
  /// (stripped of any reminder text; trailing period removed by caller).
  /// Anchored (^…$) to prevent matching as a substring of any future sibling rule.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^If\s+this\s+spell\s+was\s+cast\s+from\s+your\s+hand\s+and\s+you've\s+cast\s+another\s+spell\s+named\s+Approach\s+of\s+the\s+Second\s+Sun\s+this\s+game,\s*you\s+win\s+the\s+game\.\s*Otherwise,\s*put\s+Approach\s+of\s+the\s+Second\s+Sun\s+into\s+its\s+owner's\s+library\s+seventh\s+from\s+the\s+top\s+and\s+you\s+gain\s+7\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    // Condition: "this spell was cast from your hand and you've cast another spell
    // named Approach of the Second Sun this game" — compound history predicate with
    // no general structured form yet; lands in OtherCondition (ADR 0001/0007 residual).
    var condition = new OtherCondition
    {
      Text = "this spell was cast from your hand and you've cast another spell named Approach of the Second Sun this game",
    };

    // Then-branch: you win the game (CR 104.2b).
    var winEffect = new WinTheGameEffect
    {
      Player = ObjectReference.You(),
    };

    // Else-branch: put Approach into its owner's library seventh from the top (CR 401.7),
    // then you gain 7 life.
    var putLibraryEffect = new PutIntoLibraryAtPositionEffect
    {
      Card = ObjectReference.Self(),
      Position = 7,
    };

    var gainLifeEffect = new GainLifeEffect
    {
      Amount = LiteralQuantity.Of(7),
      Player = ObjectReference.You(),
    };

    var elseEffect = new CompositeEffect
    {
      Effects = new List<Effect> { putLibraryEffect, gainLifeEffect },
    };

    effect = new ConditionalEffect
    {
      Condition = condition,
      Then = winEffect,
      Else = elseEffect,
    };
    return true;
  }
}
