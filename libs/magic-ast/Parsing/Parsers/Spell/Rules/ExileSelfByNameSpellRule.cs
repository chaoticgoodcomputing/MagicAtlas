namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile [this card's name]." as a standalone spell sentence — the self-exile clause many instants/
/// sorceries use to remove themselves instead of going to the graveyard (older self-referential
/// templating, e.g. Pair o' Dice Lost: "Exile Pair o' Dice Lost."). The exiled object is the spell
/// itself, so this emits <see cref="ExileEffect"/> targeting <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>Matches "Exile [Proper-Noun phrase]$" — a CAPITALISED name with no filter words ("target",
/// "all", "each", "a/an/the" are lower-case and excluded by the capital-letter anchor), so it cannot
/// swallow a real targeted/mass exile. Anchored; spell context only (an ISpellRule).</para>
/// </summary>
[SpellRule(Priority = 55)]
public sealed class ExileSelfByNameSpellRule : ISpellRule
{
  // "Exile <Capitalised name>" — the name may contain letters, spaces, apostrophes, commas, hyphens,
  // and periods (e.g. "Mr. House"). The leading [A-Z] excludes filter words (all lower-case).
  private static readonly Regex Pattern = new(
    @"^Exile\s+[A-Z][A-Za-z'’.,\- ]+$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ExileEffect { Target = ObjectReference.Self() };
    return true;
  }
}
