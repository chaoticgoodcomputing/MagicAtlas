namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Destroy {N} target {type}[s]." — exact-count targeted destroy spell.
/// Examples:
/// <list type="bullet">
///   <item>"Destroy two target lands." (Rain of Salt)</item>
///   <item>"Destroy three target artifacts."</item>
/// </list>
/// The count lives on <see cref="ObjectReference.Quantity"/> as a
/// <see cref="LiteralQuantity"/>, not on the effect itself, because the oracle
/// phrase is "{N} target {type}", not "destroy {type}, repeat N times".
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its
/// owner's graveyard."
/// CR 115.1: "Some spells and abilities require their controller to choose one
/// or more targets for them. The targets are object(s) and/or player(s) the
/// spell or ability will affect. These targets are declared as part of casting..."
/// </summary>
[SpellRule]
public sealed class DestroyNTargetTypeRule : ISpellRule
{
  // Period is already stripped by SpellAbilityParser.TryParseEffect before TryMatch is called.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+(?<n>\w+)\s+target\s+(?<type>\w+?)s?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["n"].Value, out var count))
    {
      return false;
    }

    // Reject count == 1: the singular case is handled by DestroyTargetSimpleRule.
    if (count <= 1)
    {
      return false;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant();

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [cardType] },
        Quantity = LiteralQuantity.Of(count),
      },
    };
    return true;
  }
}
