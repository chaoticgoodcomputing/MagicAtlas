namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Recognises the pure keyword-grant shape (no P/T modification):
///   "Target creature gains &lt;keyword&gt; until end of turn."
///   "Target permanent gains &lt;keyword&gt; until end of turn."
///
/// This covers single-keyword combat tricks and protection spells whose entire
/// oracle effect is a UEOT ability grant on a single target. Examples:
/// <list type="bullet">
///   <item>"Target creature gains flying until end of turn."  (Jump)</item>
///   <item>"Target creature gains haste until end of turn."  (Unnatural Speed)</item>
///   <item>"Target creature gains deathtouch until end of turn."  (Lace with Moonglove)</item>
///   <item>"Target creature gains shroud until end of turn."  (Mage's Guile)</item>
///   <item>"Target creature gains shadow until end of turn."  (Shadow Rift)</item>
///   <item>"Target creature gains undying until end of turn."  (Undying Evil)</item>
/// </list>
///
/// Multi-keyword forms ("gains flying and lifelink") and composite forms
/// ("gets +N/+M and gains …") are handled by
/// <see cref="ModifyPTAndGainKeywordSpellRule"/>; this rule is intentionally
/// restricted to exactly one keyword so the regex is unambiguous.
///
/// Rule citations: 613.1c (Layer 6 — ability-granting effects), 611 (continuous
/// effects with duration).
/// </summary>
[SpellRule]
public sealed class TargetCreatureGainsKeywordRule : ISpellRule
{
  // Matches single-keyword UEOT grants on creature or permanent targets.
  // Named group "tgt" captures the target noun; "kw" captures the keyword (one or two words).
  private static readonly Regex _pattern = new(
    @"^Target\s+(?<tgt>creature|permanent)\s+gains\s+(?<kw>[a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var targetNoun = m.Groups["tgt"].Value.ToLowerInvariant();
    var keyword = m.Groups["kw"].Value.ToLowerInvariant().Trim();

    var ability = MapKeywordToStaticAbility(keyword);
    if (ability is null)
    {
      // Unrecognised keyword — let fallback handle it.
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [targetNoun] },
      },
      GainedAbility = ability,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  // -------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors the mapping in ModifyPTAndGainKeywordSpellRule.BuildKeywordAbility;
  // kept local so each rule is self-contained and independently evolvable.
  // -------------------------------------------------------------------------
  private static Ability? MapKeywordToStaticAbility(string keyword) =>
    keyword switch
    {
      "flying" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Flying,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
            },
          },
        ],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new LifelinkEffect()],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.FirstStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.DoubleStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Hexproof,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Reach,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Indestructible,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
      "menace" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Menace,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
            MinimumBlockers = 2,
          },
        ],
      },
      "defender" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Defender,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Defender }],
      },
      "shroud" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shroud,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }],
      },
      "shadow" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shadow,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shadow }],
      },
      // Undying (CR 702.93): decomposed triggered ability — when this creature dies,
      // if it had no +1/+1 counters on it, return it to the battlefield under its
      // owner's control with a +1/+1 counter on it. See UndyingKeyword.cs.
      "undying" => new TriggeredAbility
      {
        KeywordSource = KeywordAbility.Undying,
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.When,
          Event = TriggerEvent.Dies,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        InterveningIf = new OtherCondition { Text = "it had no +1/+1 counters on it" },
        Effects =
        [
          new ReturnToBattlefieldEffect
          {
            Target = ObjectReference.It(),
            UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
            WithCounters = new CounterPlacement
            {
              CounterType = "+1/+1",
              Count = LiteralQuantity.Of(1),
            },
          },
        ],
      },
      _ => null,
    };
}
