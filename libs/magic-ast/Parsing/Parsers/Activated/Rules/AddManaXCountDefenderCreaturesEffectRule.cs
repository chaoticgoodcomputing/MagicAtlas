namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Add X mana in any combination of colors, where X is the number of creatures
/// you control with defender." — Axebane Guardian's mana ability.
///
/// <para>
/// The X is NOT a cost variable (CR 107.3): it is defined inline by the
/// "where X is …" clause as the count of defender creatures the controller
/// controls. MAST records this as a <see cref="CountQuantity"/> over an
/// <see cref="ObjectFilter"/> with <c>CardTypes=["creature"]</c>,
/// <c>Controller=<see cref="ControllerFilter.You"/></c>,
/// <c>Zone=<see cref="Zone.Battlefield"/></c>, and
/// <c>Characteristics=[KeywordCharacteristic{Defender}]</c>.
/// Reference-not-resolution (ADR 0004): MAST names the quantity; the engine
/// evaluates it at resolution time.
/// </para>
///
/// <para>
/// The mana produced is X units, freely distributed across any combination of
/// the five colors (<see cref="AddManaEffect.AnyCombinationOf"/> =
/// <c>["W","U","B","R","G"]</c>). Per CR 106.4: "When an effect instructs a
/// player to add mana, that mana goes into a player's mana pool."
/// </para>
///
/// <para>
/// Defender is a static ability (CR 702.3a: "Defender is a static ability.";
/// CR 702.3b: "A creature with defender can't attack."). A creature with
/// defender in the filter is expressed as
/// <c>Characteristics=[KeywordCharacteristic{Defender}]</c> — the structured
/// <see cref="MagicAST.AST.References.KeywordCharacteristic"/> rather than an
/// <see cref="MagicAST.AST.References.OtherCharacteristic"/> residual, because
/// Defender is a first-class <see cref="MagicAST.AST.References.KeywordAbility"/>
/// enum value.
/// </para>
///
/// <para>
/// Per CR 605.1a — "An activated ability is a mana ability if it meets all of
/// the following criteria: it doesn't require a target (see rule 115.6), it
/// could add mana to a player's mana pool when it resolves, and it's not a
/// loyalty ability." — the enclosing <c>{T}:</c> ability IS a mana ability;
/// the controller filter does not introduce a target.
/// </para>
///
/// <para>
/// Runs at Priority = 1003 (above <see cref="AddManaXGreatestPowerEffectRule"/>'s
/// 1002 and <see cref="AddManaEffectRule"/>'s 1000) so the Axebane Guardian
/// "where X is the number of creatures … with defender" shape is claimed before
/// any more-general rule in the family.
/// </para>
///
/// <para>
/// The rule is anchored (<c>^…$</c>) to prevent a substring match against a
/// more-specific sibling.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1003)]
public sealed class AddManaXCountDefenderCreaturesEffectRule : IActivatedEffectRule
{
  // Anchored: matches the full add-mana sentence.
  // "Add X mana in any combination of colors, where X is the number of
  //  creatures you control with defender."
  private static readonly Regex _pattern = new(
    @"^Add\s+X\s+mana\s+in\s+any\s+combination\s+of\s+colors,\s+where\s+X\s+is\s+the\s+number\s+of\s+creatures\s+you\s+control\s+with\s+defender\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          Zone = Zone.Battlefield,
          Characteristics = [Characteristic.HasKeyword(KeywordAbility.Defender)],
        },
      },
      AnyCombinationOf = ["W", "U", "B", "R", "G"],
    };
  }
}
