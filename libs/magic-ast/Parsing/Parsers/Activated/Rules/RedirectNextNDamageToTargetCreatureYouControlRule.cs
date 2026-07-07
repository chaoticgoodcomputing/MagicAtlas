namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "The next N damage that would be dealt to this creature this turn is dealt to
/// target creature you control instead." — the {0} en-Kor damage-redirection family
/// (Warrior en-Kor, Nomads en-Kor, …).
///
/// <para>Maps to <see cref="RedirectDamageEffect"/> with a literal
/// <see cref="RedirectDamageEffect.Amount"/>, <see cref="ObjectReference.Self()"/> as
/// <see cref="RedirectDamageEffect.From"/> ("this creature"), a "target creature you
/// control" <see cref="ObjectReferenceKind.Target"/> reference as
/// <see cref="RedirectDamageEffect.To"/>, and an end-of-turn
/// <see cref="MagicAST.AST.Effects.Duration"/> ("this turn").</para>
///
/// <para>CR 602.1 (verbatim): "Activated abilities have a cost and an effect. They are
/// written as \"[Cost]: [Effect.] [Activation instructions (if any).]\" …" — this is
/// the post-colon effect fragment of the {0} activated ability.</para>
///
/// <para>CR 614.1 (verbatim): "Some continuous effects are replacement effects … Such
/// effects watch for a particular event that would happen and completely or partially
/// replace it …" — the redirection is a one-shot, turn-scoped replacement shield.</para>
///
/// The regex is fully anchored (<c>^…$</c>) so it cannot match a substring of another
/// card's effect text.
/// </summary>
[ActivatedEffectRule(Priority = 979)]
public sealed class RedirectNextNDamageToTargetCreatureYouControlRule : IActivatedEffectRule
{
  // "The next N damage that would be dealt to this creature this turn is dealt to
  // target creature you control instead." N is a digit string or a spelled-out word.
  private static readonly Regex Pattern = new(
    @"^[Tt]he\s+next\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+this\s+creature\s+this\s+turn\s+is\s+dealt\s+to\s+target\s+creature\s+you\s+control\s+instead$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
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

    return new RedirectDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      From = ObjectReference.Self(),
      To = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
