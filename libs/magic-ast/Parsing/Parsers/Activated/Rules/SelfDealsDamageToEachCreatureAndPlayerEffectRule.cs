namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "This creature deals N damage to each creature and each player." — a Pestilence-style
/// symmetric ping (CR 602.1: "Activated abilities have a cost and an effect."; the cost — e.g.
/// "{B}" — is parsed separately by the activated cost rules, this rule recognises only the
/// post-colon effect fragment). The source deals non-combat damage (CR 120.1) simultaneously to
/// every creature (including itself — the ability is symmetric, unlike a one-sided sweeper) and
/// every player, the latter causing life loss (CR 119.2).
///
/// <para>
/// Emits the same <see cref="CompositeEffect"/> shape as the spell-side
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.DealDamageToEachRule"/> "each creature and each
/// player" branch, but reachable from an activated ability: <see cref="ActivatedAbilityParser"/>
/// dispatches the post-colon effect ONLY through the <see cref="IActivatedEffectRule"/> registry,
/// which is distinct from the spell/triggered registries (see the note in
/// <see cref="RollDieActivatedRule"/>), so the spell rule can never fire here.
/// </para>
///
/// <para>
/// Guard: anchors on "each creature and each player" specifically; does NOT match "each opponent"
/// (handled by <see cref="SelfDealsDamageToEachOpponentEffectRule"/>) or "each player equal to
/// half life total" style clauses (<see cref="SelfDealsHalfLifeTotalDamageToEachPlayerEffectRule"/>).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class SelfDealsDamageToEachCreatureAndPlayerEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+and\s+each\s+player$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    // Accept "This creature" (pronoun self-ref) or a capitalised proper-noun subject.
    var subject = m.Groups["subject"].Value;
    var isThisCreature = subject.Equals("This creature", StringComparison.OrdinalIgnoreCase);
    var isNamedSelf = subject.Length > 0 && char.IsUpper(subject[0]) && !isThisCreature;
    if (!isThisCreature && !isNamedSelf)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
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
      _ => int.Parse(rawAmount),
    };

    return new CompositeEffect
    {
      Effects =
      [
        new DealDamageEffect
        {
          Amount = LiteralQuantity.Of(amount),
          Source = ObjectReference.Self(),
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { CardTypes = ["creature"] },
          },
        },
        new DealDamageEffect
        {
          Amount = LiteralQuantity.Of(amount),
          Source = ObjectReference.Self(),
          Target = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
        },
      ],
    };
  }
}
