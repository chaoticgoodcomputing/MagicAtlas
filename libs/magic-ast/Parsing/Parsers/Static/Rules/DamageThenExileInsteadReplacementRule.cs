namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to any target. If a creature dealt damage this way
/// would die this turn, exile it instead." — the Anger-of-the-Gods / Incendiary
/// Flow damage-exile family: a burn spell whose second sentence installs a
/// linked one-shot replacement effect.
///
/// <para>
/// CR 614.1: replacement effects watch for a particular event that would
/// happen and completely or partially replace it with a different event.
/// CR 614.6: if an event is replaced, it never happens — the modified event
/// occurs instead. CR 700.4: "dies" means put into a graveyard from the
/// battlefield. CR 607.1: "this way" links the replacement's scope back to
/// objects affected by this spell's own damage (a linked ability).
/// </para>
///
/// <para>
/// Both sentences live in a single un-split oracle-text clause — no printed
/// line break separates them — and <see cref="MagicAST.Parsing.AbilityClassifier"/>
/// routes the whole clause to Static because it contains both "would" and
/// "instead" tokens (<c>ContainsReplacementPattern</c>) regardless of which
/// sentence they appear in, so the compound two-sentence text arrives here as
/// one <see cref="OracleClause"/>. This rule recognises that compound shape as
/// a unit and emits the two abilities it actually describes: the
/// damage-dealing <see cref="SpellAbility"/> (sentence 1) and the linked
/// replacement <see cref="StaticAbility"/> (sentence 2) — mirroring
/// <c>DrawReplacementRule</c>'s replacement-effect shape for the second half.
/// </para>
/// </summary>
[StaticRule(Priority = 980)]
public sealed class DamageThenExileInsteadReplacementRule : IStaticRule
{
  private static readonly Regex _damageExilePattern = new(
    @"^\s*(?<subject>[A-Z][^.]*?)\s+deals\s+(?<amount>\d+)\s+damage\s+to\s+any\s+target\."
      + @"\s+If\s+a\s+creature\s+dealt\s+damage\s+this\s+way\s+would\s+die\s+this\s+turn,?\s+exile\s+it\s+instead\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _damageExilePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!int.TryParse(match.Groups["amount"].Value, out var amount))
    {
      return null;
    }

    return
    [
      new SpellAbility
      {
        Effects =
        [
          new DealDamageEffect
          {
            Amount = LiteralQuantity.Of(amount),
            Source = ObjectReference.Self(),
            Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
          },
        ],
      },
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.DeathEvent
          {
            AffectedObjects = new ObjectFilter
            {
              CardTypes = ["creature"],
              History = new DealtDamageByPredicate
              {
                Source = ObjectReference.Self(),
                Timeframe = "this way",
              },
            },
          },
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.ZoneChange.ExileEffect
          {
            Target = ObjectReference.It(),
          },
        }],
      },
    ];
  }
}
