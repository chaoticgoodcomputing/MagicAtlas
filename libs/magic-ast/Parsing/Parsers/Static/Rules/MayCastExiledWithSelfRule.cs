namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "During your turn, you may cast cards exiled with [this object] and you may
/// cast them as though they had flash. Mana of any type can be spent to cast
/// those spells." — the persistent play-from-exile permission of a CR 406.6
/// linked pair (Azula, Cunning Usurper).
///
/// <para>
/// This is the second, separate static ability of the linked pair: it refers to
/// "cards exiled with [object]" via the <see cref="ObjectFilter.ExiledWith"/>
/// reference rather than threading a binding from the exile trigger (ADR 0004
/// "reference, not resolution"). It is a separate static permission — not a
/// one-shot bundled inside the exile ability — so a window exists (ADR 0004
/// "topology, not annotation"). A card referring to itself by name resolves to
/// the <see cref="ObjectReferenceKind.Self"/> source (CR 201.4 self-reference).
/// CR 406.6 (linked exile); CR 702.8 (flash timing).
/// </para>
/// </summary>
[StaticRule]
public sealed class MayCastExiledWithSelfRule : IStaticRule
{
  // "During your turn, you may cast cards exiled with <name> and you may cast them
  // as though they had flash. Mana of any type can be spent to cast those spells."
  private static readonly Regex Pattern = new(
    @"^\s*During\s+your\s+turn,\s+you\s+may\s+cast\s+cards\s+exiled\s+with\s+.+?\s+and\s+you\s+may\s+cast\s+them\s+as\s+though\s+they\s+had\s+flash\.\s+Mana\s+of\s+any\s+type\s+can\s+be\s+spent\s+to\s+cast\s+those\s+spells\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!Pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MayPlayFromExileEffect
          {
            Cards = new ObjectFilter
            {
              Zone = Zone.Exile,
              ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
            },
            Actions = [PlayFromExileAction.CastSpells],
            WhoseTurn = ControllerFilter.You,
            AsThoughFlash = true,
            ManaSpend = ManaSpendRelaxation.AnyType,
          },
        ],
      },
    ];
  }
}
