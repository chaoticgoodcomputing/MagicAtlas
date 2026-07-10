namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "If this creature would be destroyed, regenerate it." — an unconditional,
/// permanent self-regeneration replacement (Clergy of the Holy Nimbus).
///
/// CR 614.1 (replacement effect): "If [event] would [happen], [alternative]
/// instead." The stated event is the object's own destruction; the stated
/// alternative is the keyword action "regenerate" (CR 701.19), which itself
/// creates the standard regeneration shield (remove damage, tap, and — if
/// attacking/blocking — remove from combat) rather than letting the
/// destruction occur.
///
/// <para>
/// Modeled with the existing replacement primitives — <see cref="ReplacementEffect"/>
/// over a <see cref="DestructionEvent"/> (<c>AffectedObjects.IsSelf = true</c>) with
/// <c>OriginalEventOccurs = false</c> ("regenerate it" supplants the destruction
/// entirely, mirroring how <c>SelfWouldBePutIntoGraveyardExileInsteadRule</c> models
/// "exile it instead") and a <see cref="RegenerateEffect"/> (Target = Self) as the
/// replacement action — reusing the same node the activated/spell "Regenerate this
/// creature." rules already emit, per the descriptive-not-engine doctrine (MAST
/// records the effect's presence, not the shield-creation machinery).
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "this creature would be destroyed, regenerate it"
/// clause shape so it cannot collide with other "would be destroyed" replacement
/// phrasings elsewhere in the corpus (e.g. Umbra armor's "If enchanted creature
/// would be destroyed, instead remove all damage from it and destroy this Aura.",
/// which names a different subject and a different replacement action).
/// </para>
/// </summary>
[StaticRule]
public sealed class SelfRegenerateOnDestructionReplacementRule : IStaticRule
{
  // "If this creature/permanent would be destroyed, regenerate it."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+this\s+(?:creature|permanent)\s+would\s+be\s+destroyed,\s+regenerate\s+it\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    var replacement = new StaticAbility
    {
      Effects =
      [
        new ReplacementEffect
        {
          Event = new DestructionEvent { AffectedObjects = new ObjectFilter { IsSelf = true } },
          OriginalEventOccurs = false,
          Replacement = new RegenerateEffect { Target = ObjectReference.Self() },
        },
      ],
    };

    return [replacement];
  }
}
