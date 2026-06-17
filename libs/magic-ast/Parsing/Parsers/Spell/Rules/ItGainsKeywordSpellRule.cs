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
/// "It gains [keyword] until end of turn." — a standalone sentence within a
/// multi-sentence spell ability where "it" is a back-reference to an object named
/// in the preceding sentence (e.g. the token created by a copy effect).
///
/// <para>
/// Emits a <see cref="GainAbilityEffect"/> targeting <see cref="ObjectReferenceKind.It"/>
/// with an <c>untilEndOfTurn</c> duration. The <c>It</c> reference is the standard MAST
/// pronoun-back-reference (Rule 109.2 — game objects referenced by "it") and connects
/// to the immediately preceding object in the same ability's effect list.
/// </para>
///
/// <para>
/// This rule handles the trailing "It gains haste until end of turn." sentence that
/// appears in copy-token patterns (Molten Duplication) and similar spells. It is
/// intentionally limited to a single keyword per sentence so the match is unambiguous
/// and does not collide with the multi-effect <see cref="ThreatenRule"/> (which
/// handles the full three-sentence Threaten / Act of Treason pattern as a unit).
/// </para>
///
/// <para>
/// Priority 55: above the generic <see cref="TargetCreatureGainsKeywordRule"/> (no
/// priority = default 50) so this "it gains" shape is claimed before the "target …
/// gains" shape could incorrectly fire.
/// </para>
///
/// Rule citations: CR 109.2 (pronoun reference), CR 611 (continuous effects with
/// duration), CR 702.10 (haste).
/// </summary>
[SpellRule(Priority = 55)]
public sealed class ItGainsKeywordSpellRule : ISpellRule
{
  // Matches "It gains [keyword] until end of turn" — single keyword only.
  private static readonly Regex _pattern = new(
    @"^It\s+gains\s+(?<kw>[a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var keyword = m.Groups["kw"].Value.ToLowerInvariant().Trim();
    var ability = MapKeywordToStaticAbility(keyword);
    if (ability is null)
    {
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      GainedAbility = ability,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  // -------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors the mapping in TargetCreatureGainsKeywordRule; kept local so each
  // rule is self-contained and independently evolvable.
  // -------------------------------------------------------------------------
  private static Ability? MapKeywordToStaticAbility(string keyword) =>
    keyword switch
    {
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects =
        [
          new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste },
        ],
      },
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
              Characteristics =
              [
                Characteristic.HasKeyword(KeywordAbility.Flying),
                Characteristic.HasKeyword(KeywordAbility.Reach),
              ],
            },
          },
        ],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new LifelinkEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Indestructible,
        Effects = [new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Hexproof,
        Effects = [new KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      },
      _ => null,
    };
}
