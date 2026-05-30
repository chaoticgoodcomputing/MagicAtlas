namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Handles two global "can't block this turn" shapes on spell oracle lines:
/// <list type="bullet">
///   <item>
///     "Creatures without flying can't block this turn." (Falter, Cosmotronic Wave) —
///     emits a <see cref="CantBlockEffect"/> whose <see cref="CantBlockEffect.Target"/>
///     references all creatures with <c>Characteristics = [Characteristic.Other("withoutFlying")]</c>.
///   </item>
///   <item>
///     "Creatures can't block this turn." — emits a bare <see cref="CantBlockEffect"/>
///     covering all creatures with no keyword filter.
///   </item>
/// </list>
/// Both shapes use <see cref="ObjectReferenceKind.Each"/> (the restriction is not
/// targeted; it applies to the whole population of creatures on the battlefield)
/// and attach an <c>untilEndOfTurn</c> duration (Rule 509.1c — blocking restrictions
/// resolve at the declare-blockers step of the current turn).
/// </summary>
[SpellRule]
public sealed class CreaturesCantBlockThisTurnRule : ISpellRule
{
  // "Creatures without <keyword> can't block this turn"
  private static readonly Regex WithoutKeywordPattern = new(
    @"^Creatures\s+without\s+(?<keyword>[A-Za-z]+)\s+can't\s+block\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "Creatures can't block this turn"
  private static readonly Regex BarePattern = new(
    @"^Creatures\s+can't\s+block\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // --- "Creatures without <keyword> can't block this turn" ---
    var m = WithoutKeywordPattern.Match(text);
    if (m.Success)
    {
      var keyword = m.Groups["keyword"].Value.ToLowerInvariant();
      effect = new CantBlockEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.FromLabel("without" + char.ToUpperInvariant(keyword[0]) + keyword[1..])],
          },
        },
        Duration = UntilTimeDuration.EndOfTurn,
      };
      return true;
    }

    // --- "Creatures can't block this turn" ---
    if (BarePattern.IsMatch(text))
    {
      effect = new CantBlockEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
          },
        },
        Duration = UntilTimeDuration.EndOfTurn,
      };
      return true;
    }

    return false;
  }
}
