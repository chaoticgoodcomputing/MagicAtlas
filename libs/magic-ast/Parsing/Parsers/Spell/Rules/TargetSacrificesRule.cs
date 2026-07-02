namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Target [player|opponent] sacrifices a [type][.]" — targeted sacrifice.
/// The named player chooses which qualifying permanent to sacrifice (Rule 701.21a).
/// <list type="bullet">
///   <item>"Target opponent sacrifices a creature." (Diabolic Edict, Cruel Edict)</item>
///   <item>"Target player sacrifices a creature." (generic)</item>
///   <item>"Target player sacrifices a creature of their choice." (older errata phrasing)</item>
/// </list>
/// Emits a <see cref="SacrificeEffect"/> where:
/// <list type="bullet">
///   <item><see cref="SacrificeEffect.Target"/> carries <see cref="ObjectReferenceKind.Target"/>
///   with <c>Filter.CardTypes = ["opponent"]</c> or <c>["player"]</c> identifying the actor.</item>
///   <item><see cref="SacrificeEffect.Filter"/> restricts the sacrificed permanent type.</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class TargetSacrificesRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+(?<subject>player|opponent)\s+sacrifices\s+a\s+(?<type>[a-zA-Z]+?)(?:\s+of\s+their\s+choice)?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subject = m.Groups["subject"].Value;
    var typeWord = m.Groups["type"].Value;

    var cardType = typeWord.ToLowerInvariant() switch
    {
      "creature" => "creature",
      "artifact" => "artifact",
      "enchantment" => "enchantment",
      "permanent" => "permanent",
      "planeswalker" => "planeswalker",
      "land" => "land",
      _ => null,
    };

    if (cardType is null)
    {
      return false;
    }

    var isOpponent = subject.Equals("opponent", StringComparison.OrdinalIgnoreCase);

    effect = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [isOpponent ? "opponent" : "player"] },
      },
      Filter = new ObjectFilter { CardTypes = [cardType] },
    };

    return true;
  }
}
