namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may sacrifice another creature. If you do, draw a card." — optional
/// sacrifice-a-different-creature then draw (Sephiroth, Fabled SOLDIER ability 1).
///
/// <para>
/// Decomposes as an <see cref="OptionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="OptionalEffect.Inner"/> — <see cref="SacrificeEffect"/> targeting
///     any creature the controller controls other than the source (ExcludeSelf = true,
///     Kind = <see cref="ObjectReferenceKind.Any"/>). "Another creature" in oracle text
///     is the standard self-exclusion phrasing (CR 201.5 — the name or "this" refers to
///     the source; "another" excludes it). The sacrifice is a player choice at resolution,
///     not a target (CR 115.1 — "target" keyword only), so Kind = Any not Target.</item>
///   <item><see cref="OptionalEffect.IfYouDo"/> — <see cref="DrawCardsEffect"/> for
///     exactly one card (CR 120.1 — "draw a card" = one card from the top of the library).
///     Player = <see cref="ObjectReference.You"/>.</item>
/// </list>
/// </para>
///
/// <para>
/// Priority 95 — must run BEFORE <see cref="DrawCardsTriggeredRule"/> (priority 50),
/// which matches any text containing "draw a card" and would fire on the full
/// "you may sacrifice … If you do, draw a card" text first (producing an incorrect
/// OptionalEffect wrapping only the draw). This rule is anchored (<c>^…$</c>) so it
/// cannot fire on a sibling draw-only clause.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class SacrificeAnotherCreatureDrawCardRule : ITriggeredRule
{
  // ANCHORED: cannot match a standalone "draw a card" clause.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+sacrifice\s+another\s+creature\.\s*If\s+you\s+do,\s+draw\s+a\s+card\.?$",
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
      },
      IfYouDo = new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = ObjectReference.You(),
      },
    };
    return true;
  }
}
