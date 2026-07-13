namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Board-wide, permanent (not "this turn") attacker-side restriction filtered by
/// the ABSENCE of a keyword ability: "Creatures without flying can't attack."
/// (Moat). CR 508.1c (declare-attackers step; attacking restrictions constrain
/// the set of legal attacker declarations the active player can make). CR 702.9
/// (flying, the evasion keyword this restriction is scoped around on Moat; the
/// pattern is general over any single-word keyword name, not flying-specific).
///
/// <para>
/// Unlike <c>CreaturesCantBlockThisTurnRule</c>'s "without &lt;keyword&gt;" arm
/// (Falter, Cosmotronic Wave), which is a temporary "this turn" restriction and
/// still relies on the typed <see cref="OtherCharacteristic"/> residual for the
/// keyword-absence predicate, this rule is a permanent static ability with no
/// stated duration and uses the first-class <see cref="ObjectFilter.LacksKeywords"/>
/// axis — the structured "without [keyword]" predicate that residual was a
/// deliberate scope deferral for (ADR 0001).
/// </para>
///
/// <para>
/// Maps to a <see cref="CantAttackEffect"/> whose <see cref="CantAttackEffect.Target"/>
/// is <see cref="ObjectReferenceKind.Each"/> creature filtered by
/// <c>CardTypes=["creature"]</c> + <see cref="ObjectFilter.LacksKeywords"/>. No
/// <see cref="MagicAST.AST.Effects.Duration"/> — Moat's restriction is always-on,
/// not scoped to the current turn.
/// </para>
/// </summary>
[StaticRule(Priority = 960)]
public sealed class CreaturesWithoutKeywordCantAttackRule : IStaticRule
{
  // "Creatures without <keyword> can't attack." — anchored on the literal leading
  // "Creatures" token so this cannot collide with the self-referencing
  // ("This creature"/named-self) or Aura-body ("Enchanted"/"Equipped") "can't
  // attack" shapes handled by sibling rules, and cannot collide with the "this
  // turn" temporary variant (which requires a trailing "this turn" this pattern's
  // end anchor excludes).
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+without\s+(?<keyword>[A-Za-z]+)\s+can'?t\s+attack\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!Enum.TryParse<KeywordAbility>(match.Groups["keyword"].Value, ignoreCase: true, out var keyword))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantAttackEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["creature"], LacksKeywords = [keyword] },
            },
          },
        ],
      },
    ];
  }
}
