namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you get a[n]/N [type] counter(s)" — the controller receives a named counter
/// (Kalemne, Disciple of Iroas: "Whenever you cast a creature spell with mana
/// value 5 or greater, you get an experience counter."; the same "you get {E}"
/// shape generalised beyond energy to any named counter, e.g. experience,
/// CR 122.2i).
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies
/// its characteristics and/or interacts with a rule or effect." Counters can be
/// placed on players (experience, energy, poison) as well as permanents. This is
/// the "you" (the ability's controller) sibling of the existing "that player gets
/// N [type] counters" shape already recognised by <see cref="PutCountersTriggeredRule"/>
/// — that rule's guard requires "put"/"counter" in the text and its named-player
/// branch is anchored to the literal "that player", so "you get a[n] [type]
/// counter" is not covered there. Modelled identically via <c>putCounters</c>
/// (<see cref="PutCountersEffect"/>) targeting <see cref="ObjectReferenceKind.You"/>.
/// </para>
///
/// <para>
/// Priority 60 — above the default-priority (50) reflection-discovered pool so
/// this anchored, more specific "you get" surface is tried before any broader
/// fallback. Fully anchored (^…$) so it cannot substring-match inside a longer
/// or differently-shaped effect sentence.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class YouGetNamedCounterTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+get\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[\w\-]+)\s+counters?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var countRaw = match.Groups["count"].Value.ToLowerInvariant();
    var count = countRaw switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.TryParse(countRaw, out var n) ? n : 1,
    };

    var counterType = match.Groups["type"].Value.ToLowerInvariant();

    effect = new PutCountersEffect
    {
      Target = ObjectReference.You(),
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
    return true;
  }
}
