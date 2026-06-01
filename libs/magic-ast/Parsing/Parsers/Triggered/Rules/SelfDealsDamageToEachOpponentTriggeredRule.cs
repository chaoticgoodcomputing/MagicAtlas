namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Triggered self-as-source spread: "this creature deals N damage to each
/// opponent." — the cast-trigger drain on Refraction Elemental
/// ("Whenever you cast a spell, this creature deals 2 damage to each opponent.").
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." (Here the trigger is the
/// spell-cast event, parsed by the condition side.) CR 120.1: "Objects can deal
/// damage to … players. … An object that deals damage is the source of that
/// damage." The source is the permanent itself ("this creature" →
/// <see cref="ObjectReference.Self"/>); the target population is every opponent
/// (<see cref="ObjectReferenceKind.EachOpponent"/>), dealt N simultaneously.
/// </para>
///
/// <para>
/// Triggered-side companion of the activated/spell each-opponent damage rules
/// (<see cref="Activated.Rules.SelfDealsDamageToEachOpponentEffectRule"/>); the
/// effect node is identical, only the dispatch context differs.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToEachOpponentTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>this\s+creature|this\s+\w+|it|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+opponent$",
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
    var subject = m.Groups["subject"].Value.Trim();
    var source = subject.Equals("it", StringComparison.OrdinalIgnoreCase)
      ? ObjectReference.It()
      : ObjectReference.Self();

    effect = new DealDamageEffect
    {
      Amount = amount,
      Source = source,
      Target = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
    };
    return true;
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
