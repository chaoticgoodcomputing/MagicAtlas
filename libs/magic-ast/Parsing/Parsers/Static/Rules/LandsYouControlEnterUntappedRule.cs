namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Lands you control enter untapped." — a static continuous replacement effect
/// (CR 614.1d) that causes all lands the controller controls to enter the battlefield
/// untapped rather than tapped. Canonical card: Spelunking.
///
/// <para>
/// CR 614.1d (verbatim): "Continuous effects that read '[This permanent] enters . . .'
/// or '[Objects] enter [the battlefield] . . .' are replacement effects."
/// This oracle sentence matches the second template exactly: "[Lands you control]
/// enter [untapped]." — a replacement that suppresses any tapped-entry for
/// controller-owned lands.
/// </para>
///
/// <para>
/// Modelled as a <see cref="StaticAbility"/> with
/// <see cref="StaticTimingKind.AsObjectEnters"/> — the same timing used by the
/// "Creatures your opponents control enter tapped" arm of
/// <see cref="EntersTappedRule"/> — carrying a <see cref="UntapEffect"/> targeting
/// each land the controller controls. The parallel with <see cref="EntersTappedRule"/>
/// is intentional: the structure is "as each [object] enters, [tap/untap] it";
/// only the direction (untap instead of tap) and the filter (lands you control
/// instead of creatures opponents control) differ.
/// </para>
///
/// <para>
/// ANCHORED (^…$): fully anchored to prevent substring-matching any sibling oracle
/// line that mentions lands or untapped. Priority 972 — one above
/// <see cref="LandsAreEveryBasicLandTypeRule"/> (971) so both land-static shapes
/// are tried before the generic fall-through.
/// </para>
/// </summary>
[StaticRule(Priority = 972)]
public sealed class LandsYouControlEnterUntappedRule : IStaticRule
{
  // Anchored full-sentence match: "Lands you control enter untapped."
  private static readonly Regex _pattern = new(
    @"^\s*Lands\s+you\s+control\s+enter\s+untapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The controller-scoped land filter: "Lands you control".
  /// </summary>
  private static readonly ObjectReference _landsYouControl = new()
  {
    Kind = ObjectReferenceKind.Each,
    Filter = new ObjectFilter
    {
      CardTypes = ["land"],
      Controller = ControllerFilter.You,
    },
  };

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
        When = StaticTimingKind.AsObjectEnters,
        Effects =
        [
          new UntapEffect
          {
            Target = _landsYouControl,
          },
        ],
      },
    ];
  }
}
