namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This [permanent] becomes a copy of target [filter] until end of turn." —
/// the Shifting Woodland delirium template. Emits a single <see cref="BecomesCopyEffect"/>
/// describing the source permanent becoming a copy of the targeted object.
///
/// <para>
/// This is a layer-1 copy effect (CR 707.6: "Some effects cause a permanent that's
/// copying a permanent to copy a different object while remaining on the battlefield")
/// — the source stays on the battlefield and takes on the copiable values of the
/// target. Distinct from <see cref="CreateCopyTokenWithModificationsEffectRule"/>,
/// which <em>creates a new token</em> that is a copy.
/// </para>
///
/// <para>
/// CR 207.2c (verbatim): "Delirium" is listed as an ability word — the em-dash
/// prefix is mechanically inert and is stripped by
/// <see cref="MagicAST.Parsing.Parsers.ActivatedAbilityParser"/> before this rule
/// fires. The <c>AbilityWord</c> field on the emitted
/// <see cref="MagicAST.AST.Abilities.ActivatedAbility"/> is populated by the
/// classifier, not here.
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching inside a more-specific sibling such as
/// <see cref="BecomesCreatureEffectRule"/> — the surface phrase "becomes a copy of"
/// cannot appear inside a "becomes a [P/T] … creature" animate spec. The guard is
/// belt-and-suspenders: the two patterns are structurally disjoint ("copy of target"
/// vs. a P/T box), but explicit anchoring is the project standard.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class BecomesCopyEffectRule : IActivatedEffectRule
{
  // "This [permanent] becomes a copy of target [filter noun phrase] until end of turn."
  // The <target> group captures everything between "copy of target " and " until end of turn"
  // (case-insensitive). Anchored ^…$ — no substring match across sibling rules.
  private static readonly Regex _pattern = new(
    @"^This\s+\w+\s+becomes\s+a\s+copy\s+of\s+target\s+(?<target>.+?)\s+until\s+end\s+of\s+turn$",
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

    var targetPhrase = match.Groups["target"].Value.Trim();
    var targetFilter = ParseTargetFilter(targetPhrase);

    return new BecomesCopyEffect
    {
      Subject = ObjectReference.Self(),
      CopyTarget = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = targetFilter,
      },
      Duration = MagicAST.AST.Effects.UntilTimeDuration.EndOfTurn,
    };
  }

  /// <summary>
  /// Parses the target noun phrase from "becomes a copy of target [phrase] until end of turn".
  /// Recognises "permanent card in your graveyard" and falls back to an unzoned
  /// permanent filter for other forms.
  /// </summary>
  private static ObjectFilter ParseTargetFilter(string phrase)
  {
    // "permanent card in your graveyard" — Shifting Woodland delirium
    if (Regex.IsMatch(phrase,
        @"^permanent\s+card\s+in\s+your\s+graveyard$",
        RegexOptions.IgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["permanent"],
        Zone = Zone.Graveyard,
        Controller = ControllerFilter.You,
      };
    }

    // Generic fallback: use bare "permanent" card type with no zone restriction.
    return new ObjectFilter
    {
      CardTypes = ["permanent"],
    };
  }
}
