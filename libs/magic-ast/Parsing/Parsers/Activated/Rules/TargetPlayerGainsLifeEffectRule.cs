namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player gains N life." — the activated-ability counterpart of
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.GainLifeSpellRule"/>'s
/// targeted-player clause. N is a literal number, number word, or variable
/// (X/Y/Z). Representative card: Mournful Zombie ("{W}, {T}: Target player
/// gains 1 life.").
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." Unlike
/// <see cref="GainLifeEffectRule"/> (subject is the controller, "You gain …"),
/// this rule's subject is a targeted player (<see cref="ObjectReferenceKind.Target"/>
/// over an <see cref="ObjectFilter.Player()"/> filter) — any player, not just an
/// opponent, may be chosen.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "Target player gains N life" is not a substring of any
/// sibling activated rule (in particular it is disjoint from "You gain N life" and
/// from "Target player loses N life and you gain N life"), and the anchor prevents
/// a future broader pattern from consuming this sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 992)]
public sealed class TargetPlayerGainsLifeEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+player\s+gains\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.');
    var match = Pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    var amountText = match.Groups["amount"].Value;
    Quantity amount;
    if (amountText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.X;
    }
    else if (amountText.Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Y;
    }
    else if (amountText.Equals("Z", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Z;
    }
    else
    {
      var count = ActivatedRuleHelpers.ParseNumberWord(amountText) ?? 1;
      amount = LiteralQuantity.Of(count);
    }

    return new GainLifeEffect
    {
      Amount = amount,
      Player = ObjectReference.Target(ObjectFilter.Player()),
    };
  }
}
