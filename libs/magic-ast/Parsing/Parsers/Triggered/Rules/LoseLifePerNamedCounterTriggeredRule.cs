namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you lose 1 life for each [counter] counter on [self]" — The One Ring's upkeep drain
/// ("you lose 1 life for each burden counter on The One Ring").
///
/// <para>
/// You lose life equal to the number of named counters on the source (1 per counter). Modeled as a
/// <see cref="LoseLifeEffect"/> whose <see cref="LoseLifeEffect.Amount"/> is a
/// <see cref="CounterCountQuantity"/> over the named counter on <see cref="ObjectReferenceKind.Self"/>
/// (reference-not-resolution, ADR 0004 — the engine reads the live counter count). CR 122 (counters);
/// the "1 … for each" is the unit per-counter multiplier, i.e. the count itself.
/// </para>
///
/// <para>The self target is "it" or the card naming itself (a capitalized name → self-reference,
/// CR 201.5); a reference to ANOTHER permanent's counters uses "that"/"target"/lowercase and is not
/// matched here. ANCHORED on the full clause.</para>
/// </summary>
[TriggeredRule(Priority = 50)]
public sealed class LoseLifePerNamedCounterTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+lose\s+1\s+life\s+for\s+each\s+(?<counter>[a-z][a-z\-]*)\s+counter\s+on\s+(?:it|[A-Z][A-Za-z',]*(?:\s+[A-Za-z',]+)*)$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new LoseLifeEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.You },
      Amount = new CounterCountQuantity
      {
        CounterType = m.Groups["counter"].Value.ToLowerInvariant(),
        On = new ObjectReference { Kind = ObjectReferenceKind.Self },
      },
    };
    return true;
  }
}
