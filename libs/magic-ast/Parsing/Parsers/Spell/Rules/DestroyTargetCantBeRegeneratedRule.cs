namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [filter]. It can't be regenerated." — single-target destroy spell
/// with a trailing regeneration-denial clause (Dark Banishing).
///
/// <para>
/// The two-sentence form is not broken apart into sibling effects; regeneration
/// prevention is a modifier on the destruction event, not an independent effect
/// (CR 701.8a — "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard."). This mirrors <see cref="DestroyAllRule"/>'s "Destroy all [filter].
/// They can't be regenerated." handling, but for the single-target ("It") pronoun
/// rather than the mass ("They") pronoun.
/// </para>
///
/// <para>
/// GUARD: anchored to exactly "Destroy target &lt;filter&gt;. It can't be regenerated"
/// (singular "It", not "They" — the mass-destroy sibling `DestroyAllRule` owns that
/// shape on "Destroy all"). Filter resolution delegates to
/// <see cref="SpellRuleHelpers.ParseTargetFilter"/>, the same helper
/// <see cref="DestroyTargetSimpleRule"/> uses for the no-regeneration-clause form, so any
/// filter phrase already supported there (bare type, color+type, non-prefix, subtype)
/// is supported here too.
/// </para>
/// </summary>
[SpellRule]
public sealed class DestroyTargetCantBeRegeneratedRule : ISpellRule, IMultiSpellRule
{
  // Matches "Destroy target <filter>. It can't be regenerated"
  // (trailing period stripped by the dispatcher before TryMatchMulti is called).
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<filter>.+?)\.\s+It\s+can't\s+be\s+regenerated$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  /// <remarks>
  /// Always returns <c>false</c> — this shape spans two sentences, so it is only
  /// reachable via <see cref="TryMatchMulti"/> on the un-split whole-text fallback.
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

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
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
          Filter = filter,
        },
        CantBeRegenerated = true,
      },
    };
    return true;
  }
}
