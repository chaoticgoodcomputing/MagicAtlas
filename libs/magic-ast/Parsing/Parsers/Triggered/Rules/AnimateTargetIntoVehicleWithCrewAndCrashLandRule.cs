namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Dice;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Captain Rex Nebula's combat trigger resolution:
/// "choose target nonland permanent you control. Until end of turn, it becomes a
/// Vehicle artifact with base power and toughness each equal to its mana value, and
/// it gains crew 2 and 'Crash Land — Whenever this Vehicle deals damage, roll a
/// six-sided die. If the result is equal to this Vehicle's mana value, sacrifice this
/// Vehicle, then it deals that much damage to any target.'"
///
/// <para>
/// A duration-bounded continuous effect (CR 611) that animates the chosen permanent
/// into a <b>noncreature</b> Vehicle artifact (CR 301.7: a Vehicle isn't a creature
/// unless an effect says it is — this one does not), whose base power/toughness each
/// equal its mana value (CR 202.3, CR 208), and that gains two abilities: the Crew 2
/// keyword (CR 702.122) and a nested "Crash Land" triggered ability (CR 207.2c — the
/// em-dash ability word names the granted ability).
/// </para>
///
/// <para>
/// Modelled as a single <see cref="BecomesPermanentEffect"/> — NOT
/// <see cref="BecomesCreatureEffect"/>, whose discriminator asserts "becomes a
/// creature", which would be a fidelity error for a Vehicle (CR 301.7). The Subject is
/// the inline <see cref="ObjectReferenceKind.Target"/> reference to "target nonland
/// permanent you control" (ExcludedCardTypes ["land"], Controller You); the
/// cross-sentence "it" in sentence two is bound back to that target. The granted
/// abilities are fully structured:
/// <list type="bullet">
///   <item>Crew 2 — a <see cref="StaticAbility"/> with KeywordSource Crew and a
///   <see cref="CrewEffect"/> (Power 2), mirroring the Bomat Bazaar Barge gold.</item>
///   <item>Crash Land — a <see cref="TriggeredAbility"/> with AbilityWord "Crash Land",
///   triggering on this Vehicle dealing damage (<see cref="TriggerEvent.DealsDamage"/>,
///   Filter IsSelf), whose effects are [rollDie d6, conditional(roll == this Vehicle's
///   mana value → sacrifice this Vehicle, then it deals that much damage to any
///   target)]. The damage carries Source Self (the Vehicle) and Target AnyTarget.</item>
/// </list>
/// </para>
///
/// <para>
/// Anchored (^…$) and highly specific so it cannot collide with any other "becomes …"
/// or combat-trigger sibling. The terminal period and the embedded sentence boundary
/// ("die. If …") inside the quoted clause are tolerated explicitly by the regex; this
/// rule is tried in the single-rule dispatch loop AFTER the sentence-bundle splitter
/// has failed to parse the two-sentence body.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class AnimateTargetIntoVehicleWithCrewAndCrashLandRule : ITriggeredRule
{
  // Whole-body match. Straight quotes/apostrophe per the gold's ASCII normalisation;
  // the "Crash Land —" em-dash sits inside the quoted clause.
  private static readonly Regex _pattern = new(
    @"^choose\s+target\s+nonland\s+permanent\s+you\s+control\.\s+" +
    @"Until\s+end\s+of\s+turn,\s+it\s+becomes\s+a\s+Vehicle\s+artifact\s+with\s+base\s+power\s+and\s+toughness\s+each\s+equal\s+to\s+its\s+mana\s+value,\s+" +
    @"and\s+it\s+gains\s+crew\s+2\s+and\s+" +
    "\"" + @"Crash\s+Land\s+—\s+" +
    @"Whenever\s+this\s+Vehicle\s+deals\s+damage,\s+roll\s+a\s+six-sided\s+die\.\s+" +
    @"If\s+the\s+result\s+is\s+equal\s+to\s+this\s+Vehicle's\s+mana\s+value,\s+" +
    @"sacrifice\s+this\s+Vehicle,\s+then\s+it\s+deals\s+that\s+much\s+damage\s+to\s+any\s+target\." +
    "\"" + @"\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    // CR 202.3: mana value; CR 208: P/T set by the continuous effect.
    var manaValue = new DerivedQuantity { DerivedFrom = DerivedKind.ManaValue };

    // Crew 2 — static keyword ability (CR 702.122), modelled exactly like the
    // Bomat Bazaar Barge gold: a static ability with KeywordSource Crew carrying a
    // single CrewEffect with literal Power 2.
    var crew2 = new StaticAbility
    {
      KeywordSource = KeywordAbility.Crew,
      Effects =
      [
        new CrewEffect { Power = LiteralQuantity.Of(2) },
      ],
    };

    // "Crash Land —" — the granted ability's name (CR 207.2c ability word).
    // Trigger: "Whenever this Vehicle deals damage" → DealsDamage, Filter IsSelf.
    var crashLand = new TriggeredAbility
    {
      AbilityWord = "Crash Land",
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.DealsDamage,
        Filter = new ObjectFilter { IsSelf = true },
      },
      Effects =
      [
        // "roll a six-sided die" (CR 706.1).
        new RollDieEffect { Sides = 6 },
        // "If the result is equal to this Vehicle's mana value, sacrifice this
        // Vehicle, then it deals that much damage to any target." (CR 706 result;
        // CR 701.16 sacrifice; CR 119 damage). The "that much" damage is the roll
        // result (CR 706.2). The Vehicle is the damage source (Self).
        new ConditionalEffect
        {
          Condition = new QuantityComparisonCondition
          {
            Left = new DieRollResultQuantity(),
            Operator = ComparisonOperator.Equal,
            Right = new DerivedQuantity
            {
              DerivedFrom = DerivedKind.ManaValue,
              Source = "this Vehicle",
            },
          },
          Then = new CompositeEffect
          {
            Effects =
            [
              new SacrificeEffect { Target = ObjectReference.Self() },
              new DealDamageEffect
              {
                Amount = new DieRollResultQuantity(),
                Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
                Source = ObjectReference.Self(),
              },
            ],
          },
        },
      ],
    };

    // "choose target nonland permanent you control" — the Subject. "it" in sentence
    // two is bound back to this target (CR 611.2c: the continuous effect locks onto
    // the chosen object).
    effect = new BecomesPermanentEffect
    {
      Subject = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          ExcludedCardTypes = ["land"],
          Controller = ControllerFilter.You,
        },
      },
      Power = manaValue,
      Toughness = manaValue,
      Colors = [],
      CardTypes = ["artifact"],
      AddedSubtypes = ["Vehicle"],
      GainedAbilities = [crew2, crashLand],
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
