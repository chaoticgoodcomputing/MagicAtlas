namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Add {mana} for each tapped land your opponents control." — adds one mana of a
/// specific color per tapped land opponents control (e.g. Mana Geyser, P02/10E).
///
/// <para>
/// The amount is a <see cref="CountQuantity"/> over tapped lands opponents control:
/// <c>CountOf: { CardTypes: ["land"], Zone: Battlefield, Controller: Opponent,
/// Characteristics: [{ CharacteristicType: "tapped", Tapped: true }] }</c>.
/// The "tapped" predicate is expressed as a <see cref="TappedStateCharacteristic"/>
/// (CR 110.5a — a permanent is tapped if it's been turned sideways; the tapped/untapped
/// state is engine territory, MAST records the filter predicate descriptively).
/// </para>
/// </summary>
[SpellRule]
public sealed class AddManaForEachTappedLandOpponentsControlRule : ISpellRule
{
  // "Add {X} for each tapped land your opponents control."
  // Mana symbol group captures exactly one mana symbol (e.g. {R}).
  private static readonly Regex Pattern = new(
    @"^Add\s+(?<mana>\{[^}]+\})\s+for\s+each\s+tapped\s+land\s+your\s+opponents?\s+control\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new AddManaEffect
    {
      Mana = m.Groups["mana"].Value,
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["land"],
          Zone = Zone.Battlefield,
          Controller = ControllerFilter.Opponent,
          Characteristics = [new TappedStateCharacteristic { Tapped = true }],
        },
      },
    };
    return true;
  }
}
