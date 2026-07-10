namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gets -N/-M until end of turn. If that creature would die this
/// turn, exile it instead." — the Ob Nixilis's Cruelty family: a P/T-shrinking
/// removal spell whose second sentence installs a linked one-shot death-replacement
/// so the shrunk creature is exiled (not merely killed) if it would die this turn.
///
/// <para>
/// CR 614.1: replacement effects watch for a particular event that would happen and
/// completely or partially replace it. CR 614.6: if an event is replaced, it never
/// happens — the modified event (exile) occurs instead. CR 700.4: "dies" means "is
/// put into a graveyard from the battlefield." "That creature" and the trailing "it"
/// are anaphoric back-references to the "target creature" established earlier in the
/// SAME spell's resolution, so both map to <see cref="ObjectReferenceKind.It"/> — the
/// identical treatment <c>UntapThatCreatureRule</c> gives the "that creature"
/// back-reference (same pronoun semantics as "it").
/// </para>
///
/// <para>
/// Both sentences live in a single un-split oracle-text clause (no printed line
/// break), and <see cref="MagicAST.Parsing.AbilityClassifier"/> routes the whole
/// clause to Static because it contains both "would" and "instead"
/// (<c>ContainsReplacementPattern</c>) regardless of which sentence they appear in.
/// So the compound two-sentence text arrives here as one <see cref="OracleClause"/>.
/// This rule recognises that compound shape as a unit and emits the two abilities it
/// describes: the P/T-modifying <see cref="SpellAbility"/> (sentence 1) and the linked
/// death-replacement <see cref="StaticAbility"/> (sentence 2) — mirroring
/// <c>DamageThenExileInsteadReplacementRule</c> (Incendiary Flow), which differs only
/// in that its first sentence deals damage and its replacement scope is a
/// filter/history predicate rather than an anaphoric reference.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class ModifyPTThenExileInsteadReplacementRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Target\s+creature\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn\."
      + @"\s+If\s+that\s+creature\s+would\s+die\s+this\s+turn,?\s+exile\s+it\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new SpellAbility
      {
        Effects =
        [
          new ModifyPTEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Target,
              Filter = new ObjectFilter { CardTypes = ["creature"] },
            },
            PowerModifier = LiteralQuantity.Of(power),
            ToughnessModifier = LiteralQuantity.Of(toughness),
            Duration = UntilTimeDuration.EndOfTurn,
          },
        ],
      },
      new StaticAbility
      {
        Effects =
        [
          new ReplacementEffect
          {
            Event = new DeathEvent { DyingObject = ObjectReference.It() },
            OriginalEventOccurs = false,
            Replacement = new ExileEffect { Target = ObjectReference.It() },
          },
        ],
      },
    ];
  }
}
