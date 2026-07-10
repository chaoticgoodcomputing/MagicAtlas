namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "create a token that's a copy of Council of Reeds" — a self-copy effect where
/// the card names ITSELF rather than saying "this creature" (current Oracle
/// templating convention for self-reference on many post-2020 cards). Sibling of
/// <see cref="CreateCopyOnCombatDamageTriggeredRule"/> (which handles the "of this
/// creature" phrasing); here the copy source is the card's own printed name.
///
/// <para>
/// CR 201.5: text that refers to the object it's printed on by name means just
/// that particular object — a self-reference exactly like "this creature" (CR
/// 109). CR 707.1: "Some effects create a token that's a copy of another object."
/// CR 707.2: the copy acquires the copiable values of the original object's
/// characteristics. CR 111.2: the player who creates a token is its owner (and,
/// absent a stated alternative, its controller). The copy source is modelled as
/// <see cref="ObjectReferenceKind.Self"/>, mirroring
/// <see cref="CreateCopyOnCombatDamageTriggeredRule"/> and
/// <see cref="MagicAST.Parsing.Parsers.Triggered.TriggeredRuleHelpers.IsSelfByNameTrigger"/>'s
/// established self-by-name convention for trigger conditions, extended here to
/// the effect side.
/// </para>
///
/// <para>
/// The parser does not have access to the card's own name at this layer (mirroring
/// <see cref="TriggeredRuleHelpers.IsSelfByNameTrigger"/>'s documented limitation),
/// so this is a STRUCTURAL match, not a name-equality check: the captured name must
/// begin with a capitalised word and may continue with capitalised words or the
/// lowercase function words that legally appear inside MTG card names ("of", "the",
/// "a", "an", "from", "for", "to", "in", "at", "with", "by", "and", "or", "as").
/// This case-sensitivity is exactly what keeps this rule disjoint from the
/// lowercase-pronoun siblings — "of it" (<c>YouMayCreateTokenCopyOfItTriggeredRule</c>),
/// "of that creature" (<c>CreateCopyOfThatCreatureNotLegendaryWithTypeAndColorTriggeredRule</c>),
/// "of equipped creature" (<c>CreateCopyOfEquippedCreatureTriggeredRule</c>), and
/// "of target creature" (<c>CreateTokenRule</c>/spell-side copy rules) never begin
/// with an uppercase letter, so none of those surfaces can match here.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the whole effect fragment must be exactly "create a token
/// that's a copy of [Name]" so this rule cannot claim a substring of a longer
/// "except …" clause (those stay with the sibling rules that model modifications).
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class CreateTokenCopyOfSelfByNameTriggeredRule : ITriggeredRule
{
  private const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";

  // "create a token that's a copy of [SelfName][.]"
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^(?i:create\s+a\s+token\s+that's\s+a\s+copy\s+of)\s+"
      + @"(?<name>[A-Z][A-Za-z'\-]*,?(?:\s+(?:[A-Z][A-Za-z'\-]*|"
      + FunctionWords
      + @"),?)*)\.?$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // Rule 201.5: a card naming itself refers to THAT object — the source permanent.
    effect = new CopyEffect
    {
      Target = ObjectReference.Self(),
    };
    return true;
  }
}
