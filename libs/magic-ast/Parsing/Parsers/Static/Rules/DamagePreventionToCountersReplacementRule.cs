namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Damage-prevention-plus-counters replacement effect (Vigor family):
/// "If damage would be dealt to another creature you control, prevent that
/// damage. Put a +1/+1 counter on that creature for each 1 damage prevented
/// this way."
///
/// CR 615.1: some continuous effects are prevention effects; they watch for a
/// damage event that would happen and completely prevent the damage that would
/// be dealt. CR 615.1a: effects that use the word "prevent" are prevention
/// effects. CR 615.5: some prevention effects also include an additional
/// effect, which may refer to the amount of damage that was prevented.
///
/// CR 122.1: a counter is a marker placed on an object that modifies its
/// characteristics; a +1/+1 counter (CR 122.1a) increases power and toughness.
///
/// Structure: a <c>StaticAbility</c> with a <c>ReplacementEffect</c> that:
/// <list type="bullet">
///   <item>Watches for a <c>DamageEvent</c> whose recipient is "another
///   creature you control" — <c>AffectedObjects</c> filtered to
///   <c>CardTypes=["creature"]</c>, <c>Controller=You</c>, <c>ExcludeSelf=true</c>
///   (CR 109.5 "another"). No <c>Source</c> restriction ("If damage would be
///   dealt" names no source), so <c>DamageEvent.Source</c> is null (any source).</item>
///   <item>Sets <c>OriginalEventOccurs=false</c> ("prevent that damage").</item>
///   <item>Replaces the damage with a <c>CompositeEffect</c> of: (1) a
///   <c>PreventDamageEffect</c> (All=true) targeting "that creature" — the
///   affected creature, referenced anaphorically as <c>It</c> (CR 109.2); and
///   (2) a <c>PutCountersEffect</c> placing "+1/+1" counters on that same
///   creature (<c>It</c>) with a <c>DerivedQuantity(DamagePrevented)</c> count —
///   "for each 1 damage prevented this way" = the prevented damage amount
///   (CR 615.5), mirroring how the sibling
///   <c>DamagePreventionAndMillReplacementRule</c> (The Mindskinner) records
///   "that many cards".</item>
/// </list>
///
/// ANCHORED (^...$): the surface sentence pair is matched whole so it cannot be
/// consumed as a substring of a broader clause, and cannot claim a substring of
/// a more-specific sibling. Distinct from the Phantom shield
/// (<c>PhantomDamagePreventionRule</c>: "…prevent that damage. Remove a +1/+1
/// counter from …"), which REMOVES a counter from the source itself; the two
/// regexes are disjoint (this one requires "Put a +1/+1 counter … for each 1
/// damage prevented this way").
/// </summary>
[StaticRule(Priority = 967)]
public sealed class DamagePreventionToCountersReplacementRule : IStaticRule
{
  // Matches: "If damage would be dealt to another creature you control, prevent
  // that damage. Put a +1/+1 counter on that creature for each 1 damage
  // prevented this way." ANCHORED (^...$) to prevent substring matching.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+damage\s+would\s+be\s+dealt\s+to\s+another\s+creature\s+you\s+control,\s*"
      + @"prevent\s+that\s+damage\.\s*"
      + @"Put\s+a\s+\+1/\+1\s+counter\s+on\s+that\s+creature\s+for\s+each\s+1\s+damage\s+prevented\s+this\s+way\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Replacement.ReplacementEffect
          {
            Event = new MagicAST.AST.Effects.Replacement.DamageEvent
            {
              // "another creature you control" — a permanent recipient, not a
              // player. ExcludeSelf honours "another" (CR 109.5).
              AffectedObjects = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
                ExcludeSelf = true,
              },
            },
            OriginalEventOccurs = false,
            Replacement = new MagicAST.AST.Effects.Core.CompositeEffect
            {
              Effects =
              [
                // "prevent that damage" — the prevention action (CR 615.1).
                // Target: "that creature", the affected recipient (It, CR 109.2).
                new MagicAST.AST.Effects.Damage.PreventDamageEffect
                {
                  All = true,
                  Target = new ObjectReference { Kind = ObjectReferenceKind.It },
                },
                // "Put a +1/+1 counter on that creature for each 1 damage
                // prevented this way" — additional effect (CR 615.5). Count =
                // the prevented damage amount (DerivedFrom: DamagePrevented).
                new MagicAST.AST.Effects.Counter.PutCountersEffect
                {
                  Target = new ObjectReference { Kind = ObjectReferenceKind.It },
                  CounterType = "+1/+1",
                  Count = new DerivedQuantity { DerivedFrom = DerivedKind.DamagePrevented },
                },
              ],
            },
          },
        ],
      },
    ];
  }
}
