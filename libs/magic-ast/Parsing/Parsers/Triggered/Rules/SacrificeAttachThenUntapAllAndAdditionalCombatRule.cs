namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it and attach this Aura to a creature you control. If you do, untap all
/// creatures you control and after this phase, there is an additional combat phase." —
/// the Breath of Fury extra-combat-engine pattern (the combat-damage Aura that hops to a
/// fresh creature and grants another combat phase).
///
/// <para>
/// Produces a <see cref="CompositeEffect"/> of three effects:
/// <list type="bullet">
///   <item>A <see cref="SacrificeEffect"/> on "it" — the enchanted creature named by the
///   trigger condition (CR 701.21: sacrifice; the "it" pronoun is the trigger subject,
///   modelled as <see cref="ObjectReferenceKind.It"/>).</item>
///   <item>An <see cref="AttachEffect"/> moving this Aura onto "a creature you control"
///   — an indefinite, non-targeted controller choice (CR 701.3: attach;
///   <see cref="ObjectReferenceKind.Any"/> per the "a [filter] you control" convention,
///   not a target since the oracle omits the word "target").</item>
///   <item>A <see cref="ConditionalEffect"/> gating the payoff on "If you do" — i.e.
///   the attach actually happened (CR 101.3: any part of an instruction that's impossible
///   to perform is ignored; the attach can fail when no legal creature is available, in
///   which case the consequent is skipped). Its <c>Then</c> is a nested
///   <see cref="CompositeEffect"/> of an <see cref="UntapEffect"/> on every creature the
///   controller controls (CR 701.26: untap) and an <see cref="AdditionalCombatPhaseEffect"/>
///   inserting one extra combat phase (CR 500.8: adding phases to a turn; CR 506.1: the
///   combat phase).</item>
/// </list>
/// The "If you do" antecedent is structured as a <see cref="PrecedingActionPerformedCondition"/>
/// — the within-resolution gate on whether the preceding attach happened (CR 101.3: an
/// impossible instruction is ignored). MAST records the printed gate without pre-evaluating
/// it (ADR 0004); this idiom is fixed and parameter-free, so it is de-stringed rather than
/// left as an <see cref="OtherCondition"/> residual.
/// </para>
///
/// <para>
/// Pattern is anchored (^...$) to the entire effect text so it cannot match as a substring
/// of a longer phrase. The clause is one semantic unit — the second sentence ("If you do, …")
/// is conditional on the first — so this full-text rule wins over the sentence-bundle splitter:
/// the splitter bails when its fragments ("sacrifice it and attach …" / "If you do, …") fail
/// to parse independently, after which the dispatcher's single-rule loop reaches this rule on
/// the whole text (the same fall-through that lets
/// <see cref="MayPayUntapAttackingAndAdditionalCombatRule"/> handle Hellkite Charger).
/// </para>
///
/// <para>
/// CR references: CR 500.8 (adding a phase to a turn); CR 506.1 (the combat phase);
/// CR 701.3 (attach); CR 701.21 (sacrifice); CR 701.26 (tap/untap); CR 101.3 (an impossible
/// instruction is ignored — the "if you do" gate).
/// </para>
/// </summary>
[TriggeredRule(Priority = 85)]
public sealed class SacrificeAttachThenUntapAllAndAdditionalCombatRule : ITriggeredRule
{
  // Anchored to the full effect text. "this Aura" is the source permanent (Self);
  // "a creature you control" is the indefinite controller choice; "all creatures you
  // control" is every creature the controller controls. All phrasing is fixed.
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+and\s+attach\s+this\s+Aura\s+to\s+a\s+creature\s+you\s+control\.\s*"
    + @"If\s+you\s+do,\s*untap\s+all\s+creatures\s+you\s+control\s+and\s+after\s+this\s+phase[,\s]+"
    + @"there\s+is\s+an\s+additional\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // "sacrifice it" — the enchanted creature the trigger condition refers to (the
    // pronoun back-reference). CR 701.21 (sacrifice).
    var sacrifice = new SacrificeEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
    };

    // "attach this Aura to a creature you control" — indefinite, non-targeted controller
    // choice (no "target" keyword in the oracle, CR 115.1). CR 701.3 (attach).
    var attach = new AttachEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
    };

    // "If you do, untap all creatures you control and after this phase, there is an
    // additional combat phase." — the payoff is gated on the attach succeeding (CR 101.3).
    var payoff = new CompositeEffect
    {
      Effects =
      [
        new UntapEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
        new AdditionalCombatPhaseEffect(),
      ],
    };

    var conditional = new ConditionalEffect
    {
      Condition = new PrecedingActionPerformedCondition(),
      Then = payoff,
    };

    effect = new CompositeEffect
    {
      Effects = [sacrifice, attach, conditional],
    };

    return true;
  }
}
