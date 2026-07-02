namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "its controller may draw a card if its power is greater than each other creature's power"
/// — Selvala, Heart of the Wilds ETB trigger pattern.
///
/// <para>
/// The entering creature's controller gets an optional draw, but only when the entering
/// creature has strictly greater power than every other creature on the battlefield.
/// CR 603.5: an optional triggered effect ("may") goes on the stack regardless of whether
/// the controller intends to exercise it; the choice is made on resolution.
/// CR 121.1: drawing a card is placing the top card of the library into hand.
/// </para>
///
/// <para>
/// AST shape: <see cref="ConditionalEffect"/> whose
/// <see cref="ConditionalEffect.Condition"/> is an <see cref="OtherCondition"/> residual
/// (the relative-maximum-power predicate "its power is greater than each other creature's
/// power" has no structured <c>GreatestPowerCondition</c> node — PB-7 debt) and whose
/// <see cref="ConditionalEffect.Then"/> is an <see cref="OptionalEffect"/> wrapping a
/// <see cref="DrawCardsEffect"/> targeting the creature's controller
/// (<see cref="ObjectReferenceKind.ThatPlayer"/>).
/// </para>
///
/// <para>
/// "Its controller" is the controller of the entering creature (the trigger's subject),
/// not the ability's controller ("you"). Encoded as
/// <see cref="ObjectReferenceKind.ThatPlayer"/> following the Smothering Tithe
/// convention (CR 109.5 — the controller of an object is the player who controls it).
/// </para>
///
/// <para>
/// The rule is fully anchored (<c>^…$</c>) so it cannot match as a substring inside a
/// more-specific sibling's text.
/// </para>
/// </summary>
[TriggeredRule(Priority = 985)]
public sealed class ControllerMayDrawIfGreatestPowerTriggeredRule : ITriggeredRule
{
  // Anchored: matches the full effect sentence.
  // "its controller may draw a card if its power is greater than each other creature's power"
  private static readonly Regex _pattern = new(
    @"^its\s+controller\s+may\s+draw\s+a\s+card\s+if\s+its\s+power\s+is\s+greater\s+than\s+each\s+other\s+creature's\s+power$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new ConditionalEffect
    {
      Condition = new OtherCondition
      {
        Text = "its power is greater than each other creature's power",
      },
      Then = new OptionalEffect
      {
        Inner = new DrawCardsEffect
        {
          Count = LiteralQuantity.Of(1),
          Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        },
      },
    };
    return true;
  }
}
