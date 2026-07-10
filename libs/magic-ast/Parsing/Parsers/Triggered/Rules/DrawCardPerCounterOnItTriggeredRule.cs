namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw a card for each [counter] counter on it" — Vexing Sphinx's dies-trigger payoff
/// ("When this creature dies, draw a card for each age counter on it").
///
/// <para>
/// Modeled as a <see cref="DrawCardsEffect"/> whose <see cref="DrawCardsEffect.Count"/> is a
/// <see cref="CounterCountQuantity"/> over the named counter on
/// <see cref="ObjectReferenceKind.Self"/> (reference-not-resolution, ADR 0004 — the engine reads
/// the live counter count at the time the triggered ability resolves). The "it" pronoun
/// back-references the dying permanent named "this creature" in the trigger — mirrors
/// <see cref="LoseLifePerNamedCounterTriggeredRule"/>'s "it"/named-source → Self convention, and
/// the cumulative-upkeep cost siblings' own "for each age counter on it" → Self mapping (e.g.
/// <see cref="MagicAST.Parsing.Parsers.Static.CumulativeUpkeepDiscardCardStaticRule"/>).
/// </para>
///
/// <para>
/// ANCHORED (^…$) on the full effect clause so it cannot match as a substring of a broader
/// clause and so it doesn't collide with the unanchored generic
/// <see cref="DrawCardsTriggeredRule"/> — a higher priority than that rule's default 50 ensures
/// this more specific shape is tried first.
/// </para>
/// </summary>
[TriggeredRule(Priority = 985)]
public sealed class DrawCardPerCounterOnItTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^draw\s+a\s+card\s+for\s+each\s+(?<counter>[a-z][a-z\-]*)\s+counter\s+on\s+it$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = new CounterCountQuantity
      {
        CounterType = match.Groups["counter"].Value.ToLowerInvariant(),
        On = ObjectReference.Self(),
      },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
