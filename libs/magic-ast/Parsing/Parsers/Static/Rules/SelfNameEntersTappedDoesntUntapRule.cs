namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "[Name] enters tapped and doesn't untap during your untap step." —
/// the compound static restriction printed on Grimgrin, Corpse-Born and
/// similar permanents that name themselves rather than using "This [type]".
///
/// <para>
/// Produces two <see cref="StaticAbility"/> nodes from one oracle sentence:
/// <list type="number">
///   <item>
///     <see cref="StaticTimingKind.AsThisEnters"/> + <see cref="TapEffect"/> targeting
///     <see cref="ObjectReferenceKind.Self"/> — the entry-tapped replacement
///     (CR 603.6d: "Such text is a static ability—not a triggered ability—whose
///     effect occurs as part of the event that puts the permanent onto the
///     battlefield.").
///   </item>
///   <item>
///     <see cref="DoesntUntapEffect"/> with <c>WhoseUntapStep = "your"</c> — the
///     continuous skip-untap effect (CR 502.3: "effects can keep one or more of
///     a player's permanents from untapping.").
///   </item>
/// </list>
/// </para>
///
/// <para>
/// Priority 963 — just above <see cref="EntersTappedRule"/> (Priority 962) so this
/// compound form is matched first; both rules handle different surface shapes
/// (self-by-name compound vs. "This [type] enters tapped" simple).
/// Pattern is anchored (^…$) so it cannot consume a plain "enters tapped" clause
/// that the <see cref="EntersTappedRule"/> already handles correctly.
/// </para>
/// </summary>
[StaticRule(Priority = 963)]
public sealed class SelfNameEntersTappedDoesntUntapRule : IStaticRule
{
  // Self-by-name compound: "[CardName] enters tapped and doesn't untap during your untap step."
  // Accepts multi-word legendary names with optional comma-epithet (e.g. "Grimgrin, Corpse-Born").
  // The name portion is one or more words (capital + letters/dashes/apostrophes), optionally
  // followed by ", <epithet>" where the epithet also consists of capitalized words.
  // Anchored to avoid substring collision with the "This [type] enters tapped" form.
  private static readonly Regex _pattern = new(
    @"^\s*[A-Z][A-Za-z'\-]+(?:,\s+[A-Z][A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*\s+enters\s+tapped\s+and\s+doesn'?t\s+untap\s+during\s+your\s+untap\s+step\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Ability 1: enters tapped (CR 603.6d — static replacement at entry time).
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

    // Ability 2: doesn't untap during your untap step (CR 502.3 — continuous effect).
    var doesntUntap = new StaticAbility
    {
      Effects =
      [
        new DoesntUntapEffect
        {
          WhoseUntapStep = "your",
        },
      ],
    };

    return [entersTapped, doesntUntap];
  }
}
