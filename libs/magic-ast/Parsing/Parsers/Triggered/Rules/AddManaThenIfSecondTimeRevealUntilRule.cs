namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Nissa, Resurgent Animist Landfall effect:
/// "add one mana of any color. Then if this is the second time this ability has resolved
///  this turn, reveal cards from the top of your library until you reveal an Elf or
///  Elemental card. Put that card into your hand and the rest on the bottom of your library
///  in a random order."
///
/// <para>
/// The full effect is a single coherent resolution: unconditional mana, then a conditional
/// library-reveal. The "Then if" clause gates the reveal on an ordinal history predicate;
/// "that card" in the Put sentence back-references the card found in the reveal. This rule
/// captures all three sentences as one match and emits them as a
/// <see cref="CompositeEffect"/> containing:
/// <list type="number">
///   <item><see cref="AddManaEffect"/> — add one mana of any color (unconditional).</item>
///   <item>
///     <see cref="ConditionalEffect"/> — if this is the second time the ability has
///     resolved this turn (<see cref="OtherCondition"/> residual per ADR 0001, as no
///     structured history-ordinal condition exists yet), perform the
///     <see cref="RevealUntilEffect"/> atomic reveal-until-found action.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// The "second time this ability has resolved this turn" predicate does not yet have a
/// structured <see cref="MagicAST.AST.Abilities.Condition"/> variant; it is recorded as
/// <see cref="OtherCondition"/> — the typed IResidual residual (acceptable per ADR 0001).
/// </para>
///
/// <para>Priority 95: must beat generic sentence-bundle dispatch.</para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class AddManaThenIfSecondTimeRevealUntilRule : ITriggeredRule
{
  /// <summary>
  /// Matches the full three-sentence effect text:
  /// "add one mana of any color.
  ///  Then if this is the second time this ability has resolved this turn,
  ///  reveal cards from the top of your library until you reveal an [subtype1] or [subtype2] card.
  ///  Put that card into your hand and the rest on the bottom of your library in a random order."
  ///
  /// Groups:
  ///   <c>cond</c> — the ordinal condition phrase (between "if" and the first comma after it).
  ///   <c>sub1</c> — first subtype (e.g. "Elf").
  ///   <c>sub2</c> — second subtype (e.g. "Elemental").
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^add\s+one\s+mana\s+of\s+any\s+color\.\s*Then\s+if\s+(?<cond>[^,]+),\s*reveal\s+cards?\s+from\s+the\s+top\s+of\s+your\s+library\s+until\s+you\s+reveal\s+an?\s+(?<sub1>\w+)\s+or\s+(?<sub2>\w+)\s+card\.\s*Put\s+that\s+card\s+into\s+your\s+hand\s+and\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+a\s+random\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var condText = match.Groups["cond"].Value.Trim();
    var sub1 = TitleCase(match.Groups["sub1"].Value.Trim());
    var sub2 = TitleCase(match.Groups["sub2"].Value.Trim());

    // Effect 1: add one mana of any color (CR 106 — unconditional mana production).
    var addMana = new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = true,
    };

    // Effect 2: conditional reveal-until-found.
    //   Condition: "this is the second time this ability has resolved this turn" —
    //   an ordinal history predicate with no structured Condition variant yet;
    //   carried as OtherCondition (typed IResidual, ADR 0001).
    var condition = new OtherCondition { Text = condText };

    // The reveal target: "an Elf or Elemental card" → CardTypes: ["card"], Subtypes: ["Elf", "Elemental"]
    // Subtypes is OR-semantics in ObjectFilter (CR 205.3 — a card has zero or more subtypes).
    var revealFilter = new ObjectFilter
    {
      CardTypes = ["card"],
      Subtypes = [sub1, sub2],
    };

    var revealUntil = new RevealUntilEffect
    {
      Filter = revealFilter,
      Player = ObjectReference.You(),
    };

    var conditional = new ConditionalEffect
    {
      Condition = condition,
      Then = revealUntil,
    };

    effect = new CompositeEffect
    {
      Effects = [addMana, conditional],
    };
    return true;
  }

  private static string TitleCase(string s) =>
    s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
