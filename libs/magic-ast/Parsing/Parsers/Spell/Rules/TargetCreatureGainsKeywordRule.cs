namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

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
  private static StaticAbility? MapKeywordToStaticAbility(string keyword) =>
    keyword switch
    {
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
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
        KeywordSource = "Haste",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = "Trample",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new LifelinkEffect()],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = "Deathtouch",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = "Hexproof",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = "Reach",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = "Indestructible",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
      "menace" => new StaticAbility
      {
        KeywordSource = "Menace",
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
        KeywordSource = "Defender",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Defender }],
      },
      "shroud" => new StaticAbility
      {
        KeywordSource = "Shroud",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }],
      },
      "shadow" => new StaticAbility
      {
        KeywordSource = "Shadow",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shadow }],
      },
      "undying" => new StaticAbility
      {
        KeywordSource = "Undying",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Undying }],
      },
      _ => null,
    };
}
