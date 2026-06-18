namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may sacrifice any number of other creatures. If you do, draw that many cards." —
/// optional mass-sacrifice-then-draw-that-many (Sephiroth, One-Winged Angel ability 3).
///
/// <para>
/// Decomposes as an <see cref="OptionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="OptionalEffect.Inner"/> — <see cref="SacrificeEffect"/> targeting
///     any number of other creatures. "Any number of other creatures" → Kind =
///     <see cref="ObjectReferenceKind.Any"/>, Filter.ExcludeSelf = true (the card's
///     "other" clause, CR 201.5 self-exclusion), Count = <see cref="AnyAmountQuantity"/>
///     (the "any number of" upper-unbounded player choice, CR 107.3). The sacrifice is a
///     player choice, not a target (CR 115.1 — no "target" keyword).</item>
///   <item><see cref="OptionalEffect.IfYouDo"/> — <see cref="DrawCardsEffect"/> for
///     "that many" cards. "That many" is an anaphoric reference to the number of creatures
///     sacrificed in the Inner — backed by <see cref="DerivedKind.Other"/> (the sacrificed-
///     creature count is not a named <see cref="DerivedKind"/> variant; Other is the
///     structured residual per ADR 0001). Player = <see cref="ObjectReference.You"/>.</item>
/// </list>
/// </para>
///
/// <para>
/// Priority 95 — must run BEFORE <see cref="YouDrawThatManyCardsTriggeredRule"/> (default
/// priority 50) and <see cref="DrawCardsTriggeredRule"/> (50), which would fire on the
/// "draw that many cards" tail and produce an incorrect shape. ANCHORED (<c>^…$</c>) so it
/// cannot fire on a standalone "draw that many cards" clause.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class SacrificeAnyNumberOtherCreaturesDrawThatManyRule : ITriggeredRule
{
  // ANCHORED: cannot match a standalone "draw that many cards" clause or a sacrifice-only clause.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+sacrifice\s+any\s+number\s+of\s+other\s+creatures\.\s*If\s+you\s+do,\s+draw\s+that\s+many\s+cards\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new SacrificeEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Any,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            ExcludeSelf = true,
          },
        },
        Count = new AnyAmountQuantity(),
      },
      IfYouDo = new DrawCardsEffect
      {
        Count = new DerivedQuantity { DerivedFrom = DerivedKind.Other },
        Player = ObjectReference.You(),
      },
    };
    return true;
  }
}
