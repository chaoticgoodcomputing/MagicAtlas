namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Put [SelfName] onto the battlefield from the command zone." — the
/// Commander-zone recast activated ability (Derevi, Empyrial Tactician, C13).
///
/// <para>
/// This ability functions from the command zone: the card puts itself onto the
/// battlefield without being cast, bypassing the normal casting rules. The effect
/// is modelled as <see cref="ReturnToBattlefieldEffect"/> with
/// <see cref="ObjectReferenceKind.Self"/> source zone <see cref="Zone.CommandZone"/>.
/// The use of <see cref="ReturnToBattlefieldEffect"/> (rather than a dedicated node)
/// is intentional — both graveryard-reanimate and command-zone activate share the
/// "move from [zone] to battlefield" action (CR 701.7); the <c>Filter.Zone</c>
/// discriminates the source zone for clustering (command-zone put vs. graveyard
/// reanimate vs. hand put).
/// </para>
///
/// <para>
/// CR 903.9a (Commander zone ability): "Each time a commander would be put into its
/// owner's library, hand, graveyard or exile from anywhere, its owner may put it
/// into the command zone instead." Derevi's ability is an activated out-of-band
/// re-entry that avoids the normal casting-tax accumulation by bypassing the cast.
/// </para>
///
/// <para>
/// The self-name is one or more title-case name words (capitalised content words
/// and optional lowercase function words) followed by "onto the battlefield from
/// the command zone". The pattern is anchored (^…$) to prevent substring-matching
/// a broader effect sentence. Rule 201.5: "A card's text may refer to itself by its
/// own name as a shorthand for 'this object'" — the name is a self-reference
/// (IsSelf=true on the filter), not a generic card selector.
/// </para>
///
/// Rule 701.26 (Tap and Untap) is not involved here; CR 400.7 governs zone changes.
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class PutSelfFromCommandZoneOntoBattlefieldEffectRule : IActivatedEffectRule
{
  // Anchored pattern: "Put [SelfName] onto the battlefield from the command zone."
  // SelfName: one or more name words (first must be capitalised; subsequent may be
  // capitalised content words or lowercase function words; optional trailing comma
  // for legendary epithets). Tolerates a trailing period (stripped by TrimEnd below).
  private static readonly Regex _pattern = new(
    @"^Put\s+[A-Z][A-Za-z',\-]*(?:\s+(?:[A-Z][A-Za-z',\-]*|of|the|a|an|from|for|to|in|at|with|by|and|or|as),?)*\s+onto\s+the\s+battlefield\s+from\s+the\s+command\s+zone$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.CommandZone,
        },
      },
    };
  }
}
