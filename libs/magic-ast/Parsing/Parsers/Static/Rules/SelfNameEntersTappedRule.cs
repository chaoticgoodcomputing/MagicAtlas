namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "[Name] enters tapped." — the bare self-by-name entry-tapped static ability
/// printed on permanents that name themselves rather than using "This [type]"
/// (e.g. "Winter Soldier enters tapped.").
///
/// <para>
/// CR 603.6d: "Some permanents have text that reads ... '[This permanent] enters
/// tapped.' Such text is a static ability-not a triggered ability-whose effect
/// occurs as part of the event that puts the permanent onto the battlefield."
/// </para>
///
/// <para>
/// CR 614.1d: "Continuous effects that read '[This permanent] enters . . .' or
/// '[Objects] enter [the battlefield] . . .' are replacement effects."
/// </para>
///
/// <para>
/// Priority 960 — below the more specific enters-tapped rules
/// (<see cref="RevealOrEntersTappedRule"/> = 961, <see cref="EntersTappedRule"/> = 962,
/// <see cref="SelfNameEntersTappedDoesntUntapRule"/> = 963,
/// <see cref="EntersTappedWithCountersRule"/> = 964) so those specific shapes are
/// matched first; this general self-by-name form only fires as the fallthrough for
/// a bare "enters tapped." with no unless/counters/doesn't-untap/reveal tail.
/// The pattern is anchored (^…$) immediately after "enters tapped." so it cannot
/// steal any of those other shapes, and the required "enters" (with the s) excludes
/// the plural other-permanent "…enter tapped" form.
/// </para>
/// </summary>
[StaticRule(Priority = 960)]
public sealed class SelfNameEntersTappedRule : IStaticRule
{
  // Self-by-name bare form: "[CardName] enters tapped."
  // Accepts multi-word legendary names with optional comma-epithet (e.g. "Winter Soldier, Bucky Barnes"
  // in the type line, though the oracle text itself only uses the pre-comma short name).
  // The name portion is one or more words (capital + letters/dashes/apostrophes), optionally
  // followed by ", <epithet>" where the epithet also consists of capitalized words.
  // Anchored to avoid substring collision with the more specific enters-tapped forms.
  private static readonly Regex _pattern = new(
    @"^\s*[A-Z][A-Za-z'\-]+(?:,\s+[A-Z][A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*\s+enters\s+tapped\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Enters tapped (CR 603.6d — static replacement at entry time; CR 614.1d — replacement effect).
    var entersTapped = new StaticAbility
    {
      When = StaticTimingKind.AsThisEnters,
      Effects =
      [
        new TapEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
        },
      ],
    };

    return [entersTapped];
  }
}
