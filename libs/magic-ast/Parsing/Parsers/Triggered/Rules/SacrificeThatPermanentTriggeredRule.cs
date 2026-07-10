namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice that permanent" — the resolution half of the becomes-unattached trigger,
/// paired with <see cref="BecomesUnattachedConditionRule"/> (Stitcher's Graft: "Whenever
/// this Equipment becomes unattached from a permanent, sacrifice that permanent.").
///
/// <para>
/// Rule 701.21a: "To sacrifice a permanent, its controller moves it from the battlefield
/// directly to its owner's graveyard." The pronoun "that permanent" back-references the
/// object named by the trigger condition's Filter (the permanent the Equipment detached
/// from — CR 701.3d), maps to <see cref="ObjectReferenceKind.ThatPermanent"/>, mirroring
/// the "that creature"/"that player" back-reference convention used elsewhere (e.g.
/// Flanking's <c>ThatCreature</c>).
/// </para>
///
/// <para>
/// Distinct from <see cref="SacrificeTriggeredRule"/> ("sacrifice it"/"sacrifice this
/// permanent" — the trigger SUBJECT) and <see cref="SacrificeSelfTriggeredRule"/>
/// ("sacrifice this [type]" — the ability's own source): here the sacrificed object is
/// neither the subject nor the source, but a third object named earlier in the trigger
/// condition.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeThatPermanentTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+that\s+permanent\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new SacrificeEffect { Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPermanent } };
    return true;
  }
}
