namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "It's an enchantment." / "It's a creature." / "It's a land." — a layer-4
/// (CR 613.1d) card-type declaration sentence that sets the subject's card types.
/// Typically follows a return-to-battlefield sentence to declare the returned
/// permanent's new type (e.g. Enduring Tenacity: "… return it to the battlefield
/// under its owner's control. It's an enchantment.").
///
/// <para>
/// "It" is the generic anaphoric pronoun (CR 113.8b) back-referencing the
/// triggering object — the same object named by the preceding effect sentence.
/// MAST models it as <see cref="ObjectReferenceKind.It"/> on the
/// <see cref="SetCardTypesEffect.Subject"/> field.
/// </para>
///
/// <para>
/// CR 613.1d: "Layer 4: Type-changing effects are applied." An "It's an X" oracle
/// sentence sets the object's card types to the declared set, removing any types
/// not listed (e.g. removing the creature type from a creature/enchantment). The
/// parenthetical "(It's not a creature.)" is a clarifying reminder printed by WotC
/// for card types that lose creature status; it is not a separate effect and is
/// stripped by the reminder-extraction pass before this rule fires. This rule
/// handles the imperative declaration that remains.
/// </para>
///
/// CR 205.2 (card types list); CR 613.1d (type-changing layers); CR 113.8b ("it"
/// pronoun reference).
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class SetCardTypesTriggeredRule : ITriggeredRule
{
  // Matches: "It's an enchantment" / "It's a creature" / "It's a land" / etc.
  // The article before the type may be "a" or "an"; type is a lowercase word.
  private static readonly Regex _pattern = new(
    @"^it'?s\s+an?\s+(?<type>enchantment|creature|artifact|land|planeswalker|battle|instant|sorcery)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    var cardType = match.Groups["type"].Value.ToLowerInvariant();
    effect = new SetCardTypesEffect
    {
      Subject = new ObjectReference { Kind = ObjectReferenceKind.It },
      CardTypes = [cardType],
    };
    return true;
  }
}
