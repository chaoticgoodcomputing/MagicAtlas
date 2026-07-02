namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "untap all lands you control. After this phase, there is an additional combat
/// phase. Only land creatures can attack during that combat phase." — the Bumi,
/// Unleashed combat-reset pattern.
///
/// <para>
/// Produces a <see cref="CompositeEffect"/> containing:
/// <list type="bullet">
///   <item>An <see cref="UntapEffect"/> targeting all lands the controller controls
///   (CR 701.26: untap; CR 305: lands as permanents).</item>
///   <item>An <see cref="AdditionalCombatPhaseEffect"/> with an
///   <see cref="AdditionalCombatPhaseEffect.OnlyAttackers"/> filter restricting
///   attackers in the inserted combat phase to land creatures
///   (CR 508.1c: attacker restrictions).</item>
/// </list>
/// </para>
///
/// <para>
/// This rule fires BEFORE the sentence-bundle splitter because the third sentence
/// ("Only land creatures can attack during that combat phase") qualifies the
/// <em>specific</em> additional combat phase inserted by the second sentence, making
/// all three sentences a single semantic unit. If processed independently, the third
/// sentence would fail to parse (no standalone rule covers a phase-scoped attacker
/// restriction), causing the entire bundle to fall back to <c>UnparsedAbility</c>.
/// Priority 95 places this rule above the generic sentence-bundle path (effectively
/// inline) and above other pre-bundle special-cases.
/// </para>
///
/// <para>
/// CR references: CR 500.8 (adding phases to a turn); CR 508.1c (attacker
/// restrictions); CR 701.26 (untap); CR 305 (lands).
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class UntapAllLandsAdditionalCombatLandCreaturesOnlyRule : ITriggeredRule
{
  // Anchored to the full effect text. The three sentences joined by ". " boundaries.
  // "land creatures" = creatures that are also lands (the Earthbend-created state).
  private static readonly Regex _pattern = new(
    @"^untap\s+all\s+lands\s+you\s+control\.\s*"
    + @"After\s+this\s+phase[,\s]+there\s+is\s+an\s+additional\s+combat\s+phase\.\s*"
    + @"Only\s+land\s+creatures\s+can\s+attack\s+during\s+that\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var untapTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Controller = ControllerFilter.You,
      },
    };

    // "Only land creatures can attack during that combat phase" — the attacker
    // restriction is a filter on the additional combat phase node: only permanents
    // with both "land" and "creature" types are permitted as attackers.
    // CR 508.1c: attacker restrictions constrain the declare-attackers step.
    var onlyAttackers = new ObjectFilter
    {
      CardTypes = ["land", "creature"],
    };

    effect = new CompositeEffect
    {
      Effects =
      [
        new UntapEffect { Target = untapTarget },
        new AdditionalCombatPhaseEffect { OnlyAttackers = onlyAttackers },
      ],
    };

    return true;
  }
}
