namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes an Exert paragraph into the two linked abilities defined by CR 701.43d:
/// a static ability (the optional cost to attack) and a triggered ability ("When you do").
///
/// <para>
/// CR 701.43d (verbatim): "'You may exert [this creature] as it attacks' is an optional
/// cost to attack (see rule 508.1g). Some objects with this static ability have a triggered
/// ability that triggers 'when you do' printed in the same paragraph. These abilities are
/// linked. (See rule 607.2h.)"
/// </para>
///
/// <para>
/// Combat Celebrant's oracle text (with trailing reminder stripped):
/// <c>"If this creature hasn't been exerted this turn, you may exert it as it attacks.
/// When you do, untap all other creatures you control and after this phase, there is an
/// additional combat phase."</c>
/// </para>
///
/// <para>
/// Returns two abilities in order:
/// <list type="number">
///   <item>A <see cref="StaticAbility"/> with <c>KeywordSource = "Exert"</c>,
///   a <see cref="OtherCondition"/> capturing the "hasn't been exerted this turn" gate,
///   and an <see cref="ExertEffect"/> recording the optional cost to attack.</item>
///   <item>A <see cref="TriggeredAbility"/> with <c>KeywordSource = "Exert"</c>,
///   <c>Trigger.Event = TriggerEvent.Exerted</c> ("When you do"), and the
///   combined untap + additional-combat-phase effects.</item>
/// </list>
/// </para>
/// </summary>
[StaticRule(Priority = 1050)]
public sealed class ExertStaticRule : IStaticRule
{
  /// <summary>
  /// Matches the full exert paragraph (optional leading condition + "you may exert it as
  /// it attacks. When you do, &lt;effects&gt;"), with the trailing reminder stripped.
  ///
  /// <para>Named groups:</para>
  /// <list type="bullet">
  ///   <item><c>cond</c> — the intervening-if text ("this creature hasn't been exerted
  ///   this turn"), present when the "If ..., " prefix appears.</item>
  ///   <item><c>effects</c> — the "When you do, " resolution text (everything after
  ///   the "When you do, " fragment up to the end of the trimmed oracle text).</item>
  /// </list>
  /// </summary>
  private static readonly Regex Pattern = new(
    @"^(?:If\s+(?<cond>[^,]+),\s+)?you\s+may\s+exert\s+it\s+as\s+it\s+attacks\.\s*"
    + @"When\s+you\s+do,\s*(?<effects>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
  );

  /// <summary>
  /// Matches "untap all other creatures you control and after this phase, there is an
  /// additional combat phase" — the combined effect body for the "When you do" trigger.
  /// Both halves must be present for this rule to match; partial matches fall through.
  /// </summary>
  private static readonly Regex EffectsPattern = new(
    @"^untap\s+all\s+other\s+creatures\s+you\s+control\s+and\s+"
    + @"after\s+this\s+phase,\s+there\s+is\s+an\s+additional\s+combat\s+phase\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var stripped = StaticRuleHelpers.StripReminderText(clause.RawText);
    var m = Pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var effectsText = m.Groups["effects"].Value.Trim().TrimEnd('.');
    if (!EffectsPattern.IsMatch(effectsText + "."))
    {
      return null;
    }

    // Build the optional condition if the "If ..., " prefix was present.
    Condition? condition = null;
    if (m.Groups["cond"].Success)
    {
      condition = ConditionParser.Parse(m.Groups["cond"].Value.Trim());
    }

    // Ability 1 (CR 701.43d, first part): static ability with the optional
    // exert cost-to-attack. The ExertEffect is a parameterless marker —
    // the subject is always Self (the source creature).
    var staticAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Exert,
      Condition = condition,
      Effects = [new ExertEffect()],
    };

    // Ability 2 (CR 701.43d, second part): the linked triggered ability.
    // "When you do" fires when the controller pays the optional exert cost
    // (TriggerEvent.Exerted). Effects are the combined untap + combat phase.
    var triggeredAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Exert,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Exerted,
        // Filter: Self — only triggers when THIS creature is exerted (CR 607.2h linkage).
        Filter = new ObjectFilter { IsSelf = true },
      },
      Effects =
      [
        // "untap all other creatures you control" (CR 701.26: Tap and Untap)
        new UntapEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
              ExcludeSelf = true,
            },
          },
        },
        // "after this phase, there is an additional combat phase" (CR 500.8)
        new AdditionalCombatPhaseEffect(),
      ],
    };

    return [staticAbility, triggeredAbility];
  }
}
