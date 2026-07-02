namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "Creature spells you control can't be countered." — a static continuous
/// effect on an enchantment (or other permanent) that grants uncounterability to
/// all creature spells the controller casts. This is the Rhythm of the Wild shape.
///
/// <para>
/// CR 113.6g (verbatim): "An object's ability that states it can't be countered or
/// can't be copied functions on the stack."
/// </para>
///
/// <para>
/// The affected objects are creature spells on the stack controlled by the source's
/// controller. <c>CardTypes: ["spell", "creature"]</c> follows the established
/// MAST convention for stack objects that are both spells and creatures (parallel to
/// Maskwood Nexus "creature spells you control" — CardTypes: ["spell", "creature"],
/// Controller: You). The <see cref="CantBeCounteredEffect"/> records the restriction;
/// its presence on a <see cref="StaticAbility"/> with <c>AffectedObjects</c> scoping
/// to creature spells is the structured form of the "creature spells can't be
/// countered" lock.
/// </para>
///
/// <para>
/// Priority 972 — above <see cref="CantBeCastRestrictionRule"/> (971) and well above
/// the generic counter/spell rules (50–80 band). Anchored (^...$) to prevent substring
/// matches against other static lines.
/// </para>
/// </summary>
[StaticRule(Priority = 972)]
public sealed class CreatureSpellsCantBeCounteredRule : IStaticRule
{
  // "Creature spells you control can't be countered." — anchored at both ends.
  // Accepts the curly-apostrophe variant (can’t) as well as the ASCII form.
  private static readonly Regex _pattern = new(
    @"^\s*Creature\s+spells\s+you\s+control\s+can’?'?t\s+be\s+countered\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell", "creature"],
          Controller = ControllerFilter.You,
        },
        Effects = [new CantBeCounteredEffect()],
      },
    ];
  }
}
