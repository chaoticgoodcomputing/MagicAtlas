namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Regenerate [SelfName]." — the keyword action of CR 701.19a, where the
/// regenerated permanent is referred to by its own printed name rather than
/// "this creature"/"this permanent" (Silvos, Rogue Elemental: "{G}: Regenerate
/// Silvos.").
///
/// <para>
/// CR 701.19a: "If the effect of a resolving spell or ability regenerates a
/// permanent, it creates a replacement effect that protects the permanent the
/// next time it would be destroyed this turn. In this case, "Regenerate
/// [permanent]" means "The next time [permanent] would be destroyed this turn,
/// instead remove all damage marked on it and its controller taps it. If it's
/// an attacking or blocking creature, remove it from combat."" MAST records the
/// effect's presence and target only; the shield / destruction-replacement
/// semantics are engine territory (see <see cref="RegenerateEffect"/>).
/// </para>
///
/// <para>
/// CR 201.5: "Text that refers to the object it's on by name means just that
/// particular object and not any other objects with that name, regardless of
/// any name changes caused by game effects." Its own on-point example: "{BB}:
/// Regenerate Skithiryx" resolves to the object the ability is printed on.
/// <c>TryMatch</c> has no card-name parameter, so — per the established
/// self-by-name doctrine also used by
/// <see cref="PutSelfFromCommandZoneOntoBattlefieldEffectRule"/> and the
/// "deals damage" self rule — a capitalised proper-noun in the regenerated-
/// object slot is treated as a self-reference (<see cref="ObjectReference.Self"/>).
/// </para>
///
/// <para>
/// The pattern requires the first word after "Regenerate" to be capitalised,
/// which keeps it disjoint from every branch of the generic
/// <see cref="RegenerateEffectRule"/> (all of which begin with a lowercase
/// token: "this", "enchanted", "equipped", "target"), so the two rules never
/// collide regardless of relative priority.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class RegenerateSelfByNameEffectRule : IActivatedEffectRule
{
  // Anchored pattern: "Regenerate [SelfName]" where SelfName is one or more
  // name words (first must be capitalised; subsequent may be capitalised
  // content words or lowercase function words; optional trailing comma for
  // legendary epithets). Mirrors the self-name convention in
  // PutSelfFromCommandZoneOntoBattlefieldEffectRule.
  private static readonly Regex _pattern = new(
    @"^Regenerate\s+[A-Z][A-Za-z',\-]*(?:\s+(?:[A-Z][A-Za-z',\-]*|of|the|a|an|from|for|to|in|at|with|by|and|or|as),?)*$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new RegenerateEffect { Target = ObjectReference.Self() };
  }
}
