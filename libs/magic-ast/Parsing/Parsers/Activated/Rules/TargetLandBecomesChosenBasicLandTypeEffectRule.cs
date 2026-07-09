namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target land becomes the basic land type of your choice [until end of turn]." — the
/// Reef Shaman template ("{T}: Target land becomes the basic land type of your choice until
/// end of turn."). Emits a single <see cref="ChangeSubtypeEffect"/> whose new subtype is
/// player-chosen (<see cref="ChosenCharacteristicKind.BasicLandType"/>) rather than literal.
///
/// <para>
/// This is one layer-4 type-changing continuous effect (CR 613.1d: "Layer 4: Type-changing
/// effects are applied. These include effects that change an object's card type, subtype,
/// and/or supertype."). It <em>sets</em> the land's land type, replacing its existing land
/// types (CR 205.1a: "when an effect sets one or more of an object's subtypes, the new
/// subtype(s) replaces any existing subtypes from the appropriate set (creature types, land
/// types, …)"). The chosen value is a single basic land type per CR 305.6 ("The basic land
/// types are Plains, Island, Swamp, Mountain, and Forest. If an object uses the words 'basic
/// land type,' it's referring to one of these subtypes."). Per CR 305.7 the land loses its
/// old land types and rules text and gains the appropriate mana ability for the new basic
/// land type; MAST records the subtype set and leaves that consequence to the engine.
/// </para>
///
/// <para>
/// This is the fresh-choice, land-type analogue of
/// <see cref="BecomesChosenCreatureTypeEffectRule"/> ("This creature becomes the creature
/// type of your choice"): the "of your choice" is a fresh on-resolution pick, so it lands in
/// <see cref="ChangeSubtypeEffect.ChosenSubtype"/> (NOT the back-referencing
/// <see cref="ChangeSubtypeEffect.ChosenSubtypeReference"/>, which is reserved for the
/// demonstrative "that type" — Imagecrafter). The permanent stays a land and only its land
/// type changes, so this is a subtype-set effect, not a card-type change.
/// </para>
///
/// <para>
/// Anchored on the full "Target land becomes the basic land type of your choice" phrase; the
/// optional "until end of turn" tail maps to the inherited
/// <see cref="ContinuousEffect.Duration"/>. Disjoint from
/// <c>ChangeColorEffectRule</c> ("becomes [color]"),
/// <c>BecomesChosenCreatureTypeEffectRule</c> ("This [permanent] becomes the creature type of
/// your choice"), and <c>TargetBecomesChosenSubtypeEffectRule</c> ("Target creature becomes
/// that type"), so dispatch priority relative to them is immaterial.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class TargetLandBecomesChosenBasicLandTypeEffectRule : IActivatedEffectRule
{
  // "Target land becomes the basic land type of your choice until end of turn".
  // The <until> group captures the optional trailing duration so the literal phrase is
  // consumed rather than left in free text.
  private static readonly Regex _pattern = new(
    @"^Target\s+land\s+becomes\s+the\s+basic\s+land\s+type\s+of\s+your\s+choice(?<until>\s+until\s+end\s+of\s+turn)?$",
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
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["land"] },
      },
      ChosenSubtype = ChosenCharacteristicKind.BasicLandType,
      Duration = match.Groups["until"].Success ? UntilTimeDuration.EndOfTurn : null,
    };
  }
}
