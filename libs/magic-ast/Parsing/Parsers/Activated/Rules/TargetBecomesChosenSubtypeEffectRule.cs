namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature becomes that type[ until end of turn]." — the consumer half of the
/// Imagecrafter template ("{T}: Choose a creature type other than Wall. Target creature
/// becomes that type until end of turn."). Sets the target creature's creature type to
/// the creature type chosen by the preceding
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseCreatureTypeEffect"/> sentence.
///
/// <para>
/// Emits a <see cref="ChangeSubtypeEffect"/> — a layer-4 subtype-changing continuous
/// effect (CR 613.1: continuous effects apply in layers). The permanent stays a
/// creature and only its creature type is replaced (CR 205.3: creature types are
/// subtypes), so this is a subtype set, not a <c>BecomesCreatureEffect</c> (which adds
/// the creature card type). The new value is the demonstrative "that type", a
/// back-reference to the earlier choice, so it lands in
/// <see cref="ChangeSubtypeEffect.ChosenSubtypeReference"/>
/// (<see cref="ChosenCharacteristicKind.CreatureType"/>) — NOT the fresh-choice
/// <see cref="ChangeSubtypeEffect.ChosenSubtype"/>, which is reserved for "the creature
/// type of your choice" (Mistform). The producer/consumer pairing is a CR 607.1
/// linked-ability relationship; MAST records both effects but not the link edge.
/// </para>
///
/// <para>
/// Anchored on "Target creature becomes that type"; the optional "until end of turn"
/// tail maps to the inherited <see cref="ContinuousEffect.Duration"/>. Disjoint from
/// <c>ChangeColorEffectRule</c> ("becomes [color]") and
/// <c>BecomesChosenCreatureTypeEffectRule</c> ("This creature becomes the creature type
/// of your choice"), so dispatch priority relative to them is immaterial.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class TargetBecomesChosenSubtypeEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+becomes\s+that\s+type(?<until>\s+until\s+end\s+of\s+turn)?$",
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
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      ChosenSubtypeReference = ChosenCharacteristicKind.CreatureType,
      Duration = match.Groups["until"].Success ? UntilTimeDuration.EndOfTurn : null,
    };
  }
}
