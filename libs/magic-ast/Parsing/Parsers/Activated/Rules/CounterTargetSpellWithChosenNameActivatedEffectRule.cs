namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Counter target spell with the chosen name." — the activation-cost consumer half
/// of Declaration of Naught's CR 614.12 named-card binder ("As this enchantment
/// enters, choose a card name." pairs with this activated ability's "{U}: Counter
/// target spell with the chosen name."). CR 701.6a: "To counter a spell or ability
/// means to cancel it, removing it from the stack." The chosen-name restriction is
/// modeled as <see cref="ObjectFilter.ChosenCharacteristic"/> =
/// <see cref="ChosenCharacteristicKind.CardName"/> on the countered spell's filter
/// (the structured consumer side of the CR 607 linked ability whose producer is
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseCardNameEffect"/>), rather than a
/// literal <see cref="ObjectFilter.Name"/> string — the name itself is a fresh
/// per-game choice, not a value printed on the card.
///
/// <para>
/// Anchored end-to-end (<c>^Counter\s+target\s+spell\s+with\s+the\s+chosen\s+name\.?$</c>)
/// so it is disjoint from <see cref="MagicAST.Parsing.Parsers.Spell.Rules.CounterSpellRule"/>'s
/// "with mana value N" tail and does not shadow that consolidated surface.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 65)]
public sealed class CounterTargetSpellWithChosenNameActivatedEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Counter\s+target\s+spell\s+with\s+the\s+chosen\s+name\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    if (!_pattern.IsMatch(text))
    {
      return null;
    }

    return new CounterSpellEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["spell"],
          ChosenCharacteristic = ChosenCharacteristicKind.CardName,
        },
      },
    };
  }
}
