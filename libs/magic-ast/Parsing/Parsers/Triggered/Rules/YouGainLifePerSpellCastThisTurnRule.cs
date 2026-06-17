namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain 1 life for each spell you've cast this turn" — triggered effect on
/// Aetherflux Reservoir. The life amount equals the count of spells you have cast
/// in the current turn (CR 601.2 — casting a spell puts it on the stack; the count
/// includes the spell currently being cast when the trigger resolves). The
/// <see cref="CountQuantity"/> carries an <see cref="ObjectFilter"/> with
/// <see cref="CastThisTurnPredicate"/> to express the backward-looking "cast this
/// turn" restriction.
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class YouGainLifePerSpellCastThisTurnRule : ITriggeredRule
{
  // "you gain 1 life for each spell you've cast this turn"
  private static readonly Regex Pattern = new(
    @"^you\s+gain\s+(?<amount>\d+|one)\s+life\s+for\s+each\s+spell\s+you'?ve\s+cast\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    // The gain-per-spell amount (always 1 for this pattern, but captured for completeness).
    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    var perSpellAmount = rawAmount == "one" ? 1 : int.Parse(rawAmount);

    // Build the count quantity: number of spells you've cast this turn.
    // The ObjectFilter uses CastThisTurnPredicate (Caster=You) to express
    // the "you've cast this turn" backward-looking condition.
    var countQuantity = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["spell"],
        Controller = ControllerFilter.You,
        History = new CastThisTurnPredicate { Caster = ControllerFilter.You },
      },
    };

    // If the per-spell gain is 1, the total gain equals the count directly.
    // If it were N, we'd wrap in a CalculatedQuantity (multiply by N). For now
    // per-spell amount is always 1 on the known card, so emit the count directly.
    Quantity amount = perSpellAmount == 1
      ? countQuantity
      : new CalculatedQuantity
        {
          BaseQuantity = countQuantity,
          Operation = "multiply",
          Operand = perSpellAmount,
        };

    effect = new GainLifeEffect
    {
      Amount = amount,
      Player = ObjectReference.You(),
    };
    return true;
  }
}
