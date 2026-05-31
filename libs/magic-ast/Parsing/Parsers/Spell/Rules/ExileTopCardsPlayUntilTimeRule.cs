namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top N cards of your library. Until [time], you may play those
/// cards." — a duration-bounded impulse where all N exiled cards stay playable
/// from exile for a stated window (The Legend of Roku, chapter I).
///
/// <para>
/// Modeled as a single <see cref="ImpulseEffect"/> with
/// <see cref="ImpulseRestDestination.RemainExiled"/> (none kept in hand; all N
/// remain exiled) and an inherited <see cref="ContinuousEffect.Duration"/> bounding
/// the play window. The two oracle sentences are one game concept — "those cards"
/// back-references the exiled pile — so they collapse into one node rather than a
/// separate exile + permission (ADR 0004: impulse is a cohesive one-shot; the
/// "cards playable from exile" cluster is recovered by consumer projection over
/// {ImpulseEffect, MayPlayFromExile}). CR 406 (exile zone); CR 701.13 (exile).
/// </para>
/// </summary>
[SpellRule]
public sealed class ExileTopCardsPlayUntilTimeRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+the\s+top\s+(?<count>\w+)\s+cards?\s+of\s+your\s+library\.\s+Until\s+the\s+end\s+of\s+your\s+next\s+turn,\s+you\s+may\s+play\s+those\s+cards\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["count"].Value, out var count))
    {
      return false;
    }

    effect = new ImpulseEffect
    {
      Count = LiteralQuantity.Of(count),
      RestDestination = ImpulseRestDestination.RemainExiled,
      Duration = new UntilTimeDuration
      {
        Until = new MagicAST.AST.References.GameTime
        {
          Part = MagicAST.AST.References.TurnPart.Turn,
          Edge = MagicAST.AST.References.TimeBoundary.End,
          When = MagicAST.AST.References.TimeRelation.Next,
          Whose = MagicAST.AST.References.ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
