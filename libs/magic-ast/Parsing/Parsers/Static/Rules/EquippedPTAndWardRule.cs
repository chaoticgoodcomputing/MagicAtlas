namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Equipped creature gets +N/+M and has ward {cost}." — the P/T buff plus a
/// Ward keyword grant (CR 702.21a) on the equipped creature. Sibling of
/// <see cref="EnchantedPTAndKeywordRule"/> (bare bodyless keywords, e.g.
/// trample/flying via <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/>):
/// that rule's keyword capture group is letters-only and cannot match a
/// parameterized "ward {N}" phrase, so Ward needs its own rule that builds the
/// full Ward <see cref="TriggeredAbility"/> shape (mirroring
/// <see cref="WardKeywordRule"/>'s mana-cost form) as the granted ability.
///
/// <para>
/// CR 702.21a (verbatim): "Ward is a triggered ability. Ward [cost] means
/// 'Whenever this permanent becomes the target of a spell or ability an
/// opponent controls, counter that spell or ability unless that player pays
/// [cost].'" CR 613.4c (P/T layer 7c) and CR 613.1f (keyword-ability-grant
/// layer 6) both apply to the equipped creature (CR 702.6, Equipment); MAST
/// records the oracle text descriptively — layer ordering is engine territory.
/// </para>
///
/// <para>
/// Priority 969 — just above <see cref="EnchantedPTAndKeywordRule"/> (965) so a
/// future generalisation of that sibling's keyword capture cannot shadow this
/// more specific parameterized-keyword shape. Trailing reminder text (e.g. the
/// Ward reminder parenthetical) is stripped before matching and dropped from
/// the gold AST — Rule 207.2, and matches the convention on sibling bare-grant
/// rules (<see cref="StaticRuleHelpers.StripReminderText"/>).
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class EquippedPTAndWardRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+ward\s+(?<cost>(?:\{[^}]+\})+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ManaCostParser _manaCostParser = new();

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    ManaCost wardCost;
    try
    {
      var parsed = _manaCostParser.Parse(match.Groups["cost"].Value);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      wardCost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    var wardAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Ward,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BecomesTarget,
        Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
      },
      Effects =
      [
        new PreventableEffect
        {
          Inner = new CounterSpellEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.It },
          },
          Unless = new MagicAST.AST.Effects.UnlessClause
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
            Cost = wardCost,
          },
        },
      ],
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CompositeEffect
          {
            Effects =
            [
              new ModifyPTEffect
              {
                Target = target,
                PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
                ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
              },
              new GainAbilityEffect
              {
                Target = target,
                GainedAbility = wardAbility,
              },
            ],
          },
        ],
      },
    ];
  }
}
