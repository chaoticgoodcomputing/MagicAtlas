namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [filterA] and target [filterB]." — a single spell that destroys
/// two independently-chosen targets (Spiteful Blow: "Destroy target creature and
/// target land.").
///
/// <para>
/// The two "target" quantifiers select two distinct objects, so the shape decomposes
/// into two sibling <see cref="DestroyEffect"/> nodes on the flat
/// <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/> list — one per target —
/// mirroring how other conjoined-sibling spells are modeled (CR 701.8a — "To destroy
/// a permanent, move it from the battlefield to its owner's graveyard."; each
/// destruction is its own effect). This is NOT the disjunction shape ("... or ...",
/// a single target with a multi-type filter) owned by
/// <see cref="DestroyTargetTypeDisjunctionRule"/>: the second literal "target"
/// keyword is what distinguishes two-target conjunction from a one-target disjunction.
/// </para>
///
/// <para>
/// GUARD: fully anchored (<c>^ ... $</c>) and requires the interior literal
/// "and target" so it cannot match a single-target destroy ("Destroy target
/// creature") or a disjunction ("Destroy target creature or land"). Each filter is
/// resolved through <see cref="SpellRuleHelpers.ParseTargetFilter"/> (the same helper
/// <see cref="DestroyTargetSimpleRule"/> uses); if either side fails to resolve the
/// rule declines so a more specific sibling can try.
/// </para>
/// </summary>
[SpellRule]
public sealed class DestroyTwoTargetsRule : ISpellRule, IMultiSpellRule
{
  // "Destroy target <f1> and target <f2>" — trailing period already stripped by the
  // dispatcher. Non-greedy filters bounded by the literal "and target" separator.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<f1>.+?)\s+and\s+target\s+(?<f2>.+?)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  /// <remarks>
  /// Always returns <c>false</c> — this shape expands to two sibling effects, so it is
  /// only reachable via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  /// <inheritdoc cref="IMultiSpellRule.TryMatchMulti"/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var firstFilter = SpellRuleHelpers.ParseTargetFilter(m.Groups["f1"].Value.Trim());
    var secondFilter = SpellRuleHelpers.ParseTargetFilter(m.Groups["f2"].Value.Trim());
    if (firstFilter is null || secondFilter is null)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = firstFilter,
        },
      },
      new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = secondFilter,
        },
      },
    };
    return true;
  }
}
