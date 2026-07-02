namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Triggered board-sweep: "[it] deals N damage to each creature and each
/// planeswalker." — the Siege-ETB sweeper (Invasion of Karsus). The source
/// fires N damage simultaneously into both the creature population and the
/// planeswalker population.
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." Each
/// population is a distinct <see cref="DealDamageEffect"/> targeting
/// <see cref="ObjectReferenceKind.Each"/> filtered to the respective card type;
/// the two are bundled in a <see cref="CompositeEffect"/> mirroring the
/// spell-side "each creature and each player" sweeper
/// (<see cref="Spell.Rules.DealDamageToEachRule"/>).
/// </para>
///
/// <para>
/// Triggered-side companion to that spell-side rule — the ETB self-reference is
/// "it" (the permanent named by the trigger condition) rather than the
/// substituted card name, so <see cref="ObjectReference.It"/> is the source.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToEachCreatureAndPlaneswalkerRule : ITriggeredRule
{
  // "[subject] deals N damage to each creature and each planeswalker"
  private static readonly Regex Pattern = new(
    @"^(?<subject>it|this\s+\w+|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+and\s+each\s+planeswalker$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var amount = LiteralQuantity.Of(ParseAmount(m.Groups["amount"].Value));
    var source = ResolveSource(m.Groups["subject"].Value);

    effect = new CompositeEffect
    {
      Effects =
      [
        new DealDamageEffect
        {
          Amount = amount,
          Source = source,
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { CardTypes = ["creature"] },
          },
        },
        new DealDamageEffect
        {
          Amount = amount,
          Source = source,
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { CardTypes = ["planeswalker"] },
          },
        },
      ],
    };
    return true;
  }

  // "it" → the permanent named by the trigger (ObjectReferenceKind.It); a
  // "this [type]" / capitalised-name self-reference → Self.
  private static ObjectReference ResolveSource(string subject)
  {
    var trimmed = subject.Trim();
    if (trimmed.Equals("it", StringComparison.OrdinalIgnoreCase))
    {
      return ObjectReference.It();
    }
    return ObjectReference.Self();
  }

  private static int ParseAmount(string raw) => raw.ToLowerInvariant() switch
  {
    "one" => 1,
    "two" => 2,
    "three" => 3,
    "four" => 4,
    "five" => 5,
    "six" => 6,
    "seven" => 7,
    "eight" => 8,
    "nine" => 9,
    "ten" => 10,
    _ => int.Parse(raw),
  };
}
