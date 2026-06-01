namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This [permanent] becomes the creature type of your choice [until end of turn]."
/// — the Mistform "shapeshift" template. Emits a single
/// <see cref="ChangeSubtypeEffect"/> whose new subtype is player-chosen
/// (<see cref="ChosenCharacteristicKind.CreatureType"/>) rather than literal.
///
/// <para>
/// This is one layer-4 type-changing continuous effect (CR 613.1d: "Layer 4:
/// Type-changing effects are applied. These include effects that change an object's
/// card type, subtype, and/or supertype."). It <em>sets</em> the creature's creature
/// type, replacing its existing creature types (CR 205.1a: "when an effect sets one
/// or more of an object's subtypes, the new subtype(s) replaces any existing subtypes
/// from the appropriate set (creature types, land types, …)"). The chosen value must
/// be a single creature type per CR 205.3 ("'Merfolk' or 'Wizard' is acceptable, but
/// 'Merfolk Wizard' is not. Words like 'artifact,' 'opponent,' 'Swamp,' or 'truck'
/// can't be chosen because they aren't creature types.").
/// </para>
///
/// <para>
/// The permanent stays a creature and only its creature type changes, so this is a
/// subtype-set effect and not a <c>BecomesCreatureEffect</c> (which adds the creature
/// card type). The "of your choice" is a fresh on-resolution choice, so it is modeled
/// on the effect itself, not via the back-referencing
/// <see cref="ObjectFilter.ChosenCharacteristic"/> filter.
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Mistform Stalker — "{1}: This creature becomes the creature type of your
///   choice until end of turn."</item>
///   <item>Mistform Dreamer — same activated line.</item>
/// </list>
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class BecomesChosenCreatureTypeEffectRule : IActivatedEffectRule
{
  // "This creature becomes the creature type of your choice until end of turn".
  // The <until> group captures the optional trailing duration so the literal phrase
  // is consumed rather than left in free text.
  private static readonly Regex _pattern = new(
    @"^This\s+\w+\s+becomes\s+the\s+creature\s+type\s+of\s+your\s+choice(?<until>\s+until\s+end\s+of\s+turn)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    return new ChangeSubtypeEffect
    {
      Target = ObjectReference.Self(),
      ChosenSubtype = ChosenCharacteristicKind.CreatureType,
      Duration = match.Groups["until"].Success ? UntilTimeDuration.EndOfTurn : null,
    };
  }
}
