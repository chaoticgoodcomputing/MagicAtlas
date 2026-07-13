namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may return another permanent you control that shares a permanent type with it to its
/// owner's hand" — Cloudstone Curio's optional bounce, keyed on a RELATIONAL "shares a permanent
/// type with [the triggering object]" predicate (CR 110.4: the six permanent types — artifact,
/// battle, creature, enchantment, land, planeswalker) rather than an absolute
/// <see cref="ObjectFilter.CardTypes"/> literal. Distinct from the generic
/// <see cref="ReturnToHandRule"/> (which has no relational "shares a type with X" axis at all) and
/// from <see cref="ObjectFilter.SharesCreatureTypeWith"/> (the narrower creature-SUBTYPE sibling,
/// CR 205.3m — Titan of Littjara's "shares a creature type with it"): this is the CARD-TYPE-level
/// relation, so it rides <see cref="ObjectFilter.SharesPermanentTypeWith"/> instead.
///
/// <para>
/// "it" is the permanent named by the trigger's "a nonartifact permanent you control enters"
/// clause — an anaphoric back-reference (Rule 109.2) to a previously-mentioned object, so it maps
/// to <see cref="ObjectReferenceKind.It"/>, not <see cref="ObjectReferenceKind.Self"/> (Cloudstone
/// Curio itself is an artifact and is never a candidate for "it", since the trigger filter already
/// excludes artifacts).
/// </para>
///
/// <para>
/// "another" (CR 109.5) excludes that same triggering permanent from the return candidates — not
/// literally Cloudstone Curio (the ability's source), but the nearest object already established in
/// the ability's own text. This generalizes the codebase's <see cref="ObjectFilter.ExcludeSelf"/>
/// convention beyond a literal "excludes the source" reading, mirroring
/// <see cref="AnotherTargetModifyPTSpellRule"/>'s precedent (there "another target creature"
/// excludes the FIRST already-targeted creature, not the spell itself).
/// </para>
///
/// <para>
/// Not a "target" reference (no "target" keyword in the oracle text), so the return candidate is
/// an indefinite controller-choice reference (<see cref="ObjectReferenceKind.Any"/>, Rule 115.1),
/// matching <see cref="ReturnToHandRule"/>'s indefinite-phrasing convention.
/// </para>
///
/// <para>
/// Anchored to the exact "shares a permanent type with it" surface (priority 95, above
/// <see cref="ReturnToHandRule"/>'s default 50) so this never shadows unrelated "return ... to its
/// owner's hand" effects, and so the generic rule never mis-handles this relational shape (it has
/// no notion of <see cref="ObjectFilter.SharesPermanentTypeWith"/>).
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class ReturnAnotherSharesPermanentTypeToHandRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+return\s+another\s+permanent\s+you\s+control\s+that\s+shares\s+a\s+permanent\s+type\s+with\s+it\s+to\s+its\s+owner'?s\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    var inner = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
          SharesPermanentTypeWith = new ObjectReference { Kind = ObjectReferenceKind.It },
        },
      },
    };

    effect = EffectWrap.Optional(inner, isOptional: true);
    return true;
  }
}
