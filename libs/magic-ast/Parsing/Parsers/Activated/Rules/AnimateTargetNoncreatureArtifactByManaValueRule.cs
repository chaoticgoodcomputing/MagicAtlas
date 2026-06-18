namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Until your next turn, up to one target noncreature artifact becomes an
/// artifact creature with power and toughness each equal to its mana value." —
/// Karn, the Great Creator's +1 loyalty ability. A duration-bounded continuous
/// effect (CR 611.1) that makes the chosen artifact into an artifact creature
/// whose P/T box equals its own mana value (CR 202.3 / CR 208).
///
/// <para>
/// Modelled as a <see cref="BecomesCreatureEffect"/> with:
/// <list type="bullet">
///   <item><c>Subject</c> — <c>Target</c> reference to "up to one … noncreature
///   artifact" (quantity <c>UpTo 1</c>, CardTypes ["artifact"],
///   ExcludedCardTypes ["creature"]).</item>
///   <item><c>Power</c> / <c>Toughness</c> — <see cref="DerivedQuantity"/>
///   (<see cref="DerivedKind.ManaValue"/>) for "each equal to its mana value".</item>
///   <item><c>Colors</c> — empty (no color change stated).</item>
///   <item><c>CardTypes</c> — <c>["artifact","creature"]</c> (additive, per CR 205.1b).</item>
///   <item><c>AddedSubtypes</c> — empty (no creature subtype stated).</item>
///   <item><c>GainedAbilities</c> — empty (no keyword granted).</item>
///   <item><c>Duration</c> — <see cref="UntilTimeDuration.YourNextTurn"/>.</item>
/// </list>
/// </para>
///
/// <para>
/// "Up to one" is modelled as <c>Quantity = UpToQuantity { Maximum = 1 }</c> on the
/// <see cref="ObjectReference"/>, not an <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>
/// wrapper (CR 117.3a: "up to X" targets means 0–X are chosen at targeting time,
/// which is a cardinality constraint, not an optionality on the one-shot action).
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) to prevent substring collision with other
/// "becomes … creature" animate rules.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class AnimateTargetNoncreatureArtifactByManaValueRule : IActivatedEffectRule
{
  // Anchored. Accepts optional trailing period.
  // Leading "Until your next turn," is mandatory — distinguishes this from
  // the trailing-duration self-animate (BecomesCreatureEffectRule).
  private static readonly Regex _pattern = new(
    @"^\s*Until\s+your\s+next\s+turn,\s+up\s+to\s+one\s+target\s+noncreature\s+artifact\s+becomes\s+an\s+artifact\s+creature\s+with\s+power\s+and\s+toughness\s+each\s+equal\s+to\s+its\s+mana\s+value\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
    {
      return null;
    }

    // CR 202.3: mana value is the total numeric value of the mana cost.
    // CR 208: power and toughness are set by this continuous effect.
    var manaValueQuantity = new DerivedQuantity
    {
      DerivedFrom = DerivedKind.ManaValue,
    };

    return new BecomesCreatureEffect
    {
      Subject = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = new UpToQuantity { Maximum = 1, Minimum = 0 },
        Filter = new ObjectFilter
        {
          CardTypes = ["artifact"],
          ExcludedCardTypes = ["creature"],
        },
      },
      Power = manaValueQuantity,
      Toughness = manaValueQuantity,
      Colors = [],
      CardTypes = ["artifact", "creature"],
      AddedSubtypes = [],
      GainedAbilities = [],
      Duration = UntilTimeDuration.YourNextTurn,
    };
  }
}
