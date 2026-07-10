namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "If [Name] would be put into a graveyard from anywhere, exile it instead." — the
/// standard self-by-name replacement clause printed on the back face of every Disturb
/// double-faced card (CR 702.146a family; e.g. Luminous Phantom), preventing the
/// transformed permanent from ever returning to the graveyard.
///
/// <para>
/// CR 614.1 (replacement effect): "If [event] would [happen], [alternative] instead."
/// Modeled with the existing replacement primitives — <see cref="ReplacementEffect"/>
/// over a <see cref="ZoneChangeEvent"/> (destination graveyard, origin left unset since
/// "from anywhere" names no specific origin zone) with <c>OriginalEventOccurs = false</c>
/// ("instead") and an <see cref="ExileEffect"/> as the replacement action. The subject is
/// the card's own printed name, resolved to <c>IsSelf</c>/<see cref="ObjectReferenceKind.Self"/>
/// — the same self-by-name resolution used by <see cref="SelfNameEntersTappedRule"/> for
/// "[Name] enters tapped.".
/// </para>
///
/// <para>
/// Anchored (^…$) on the exact clause shape so it cannot collide with the differently-
/// worded exile-instead replacements elsewhere in the corpus ("would die this turn,
/// exile it instead" — <see cref="ModifyPTThenExileInsteadReplacementRule"/> /
/// <see cref="DamageThenExileInsteadReplacementRule"/>; "is countered this way, exile it
/// instead of putting it into its owner's graveyard" — the counter-replacement rules):
/// those require a preceding "this way"/"this turn" antecedent clause this pattern does
/// not have, and this pattern requires the "from anywhere" zone-origin phrase they don't
/// have.
/// </para>
/// </summary>
[StaticRule]
public sealed class SelfWouldBePutIntoGraveyardExileInsteadRule : IStaticRule
{
  // "If <Name> would be put into a graveyard from anywhere, exile it instead."
  // <Name> is the card's own printed name (self-by-name reference, mirroring
  // SelfNameEntersTappedRule): one or more capitalized words, optionally with a
  // comma-epithet for legendary names.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+[A-Z][A-Za-z'\-]+(?:,\s+[A-Z][A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*\s+would\s+be\s+put\s+into\s+a\s+graveyard\s+from\s+anywhere,\s+exile\s+it\s+instead\.?\s*$",
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
          Event = new ZoneChangeEvent
          {
            AffectedObjects = new ObjectFilter { IsSelf = true },
            DestinationZone = Zone.Graveyard,
          },
          OriginalEventOccurs = false,
          Replacement = new ExileEffect { Target = ObjectReference.Self() },
        },
      ],
    };

    return [replacement];
  }
}
