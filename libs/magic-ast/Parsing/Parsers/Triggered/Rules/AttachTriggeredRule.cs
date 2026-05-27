namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "attach it to target [filter] you control" — an explicit attach instruction
/// inside a triggered ability. Covers the ETB auto-attach pattern common on
/// Equipment: "When this Equipment enters, attach it to target creature you control."
///
/// <para>
/// Rule 701.3: "To take an Aura, Equipment, or Fortification from where it currently
/// is and put it onto a specified object or player." MAST records the oracle-text
/// instruction descriptively; the legality checks, zone-change details, and
/// resulting continuous effects are engine territory.
/// </para>
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.Effects.Keyword.EquipEffect"/>, which
/// models the Equip activated-ability keyword (Rule 702.6). This rule handles
/// explicit "attach it to target" instructions in triggered ability effects.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AttachTriggeredRule : ITriggeredRule
{
  // Matches "attach it to target <filter> you control"
  // Filter can be:
  //   - a card type: "creature", "artifact", "land", etc.
  //   - a creature type / subtype: "Pirate", "Warrior", "Vehicle", etc.
  // The "you control" qualifier sets Controller = You.
  private static readonly Regex AttachPattern = new(
    @"\battach\s+it\s+to\s+target\s+(?<filter>[A-Za-z]+(?:\s+[A-Za-z]+)?)\s+you\s+control\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> CardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature",
    "artifact",
    "land",
    "enchantment",
    "planeswalker",
    "permanent",
    "battle",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var match = AttachPattern.Match(text);
    if (!match.Success)
    {
      return false;
    }

    var filterWord = match.Groups["filter"].Value.Trim();
    var isOptional = text.Contains("you may", StringComparison.OrdinalIgnoreCase);

    // Determine whether the filter word is a card type or a creature subtype.
    // Card types go into CardTypes; subtypes (creature types like "Pirate") go
    // into Subtypes. Both carry Controller = You from the "you control" qualifier.
    ObjectFilter filter;
    if (CardTypes.Contains(filterWord))
    {
      filter = new ObjectFilter
      {
        CardTypes = [filterWord.ToLowerInvariant()],
        Controller = ControllerFilter.You,
      };
    }
    else
    {
      // Treat as a creature subtype (e.g., "Pirate", "Warrior", "Vehicle").
      // Capitalise the first letter to match the Oracle convention for subtypes.
      var subtype = char.ToUpperInvariant(filterWord[0]) + filterWord[1..];
      filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      };
    }

    effect = new AttachEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      IsOptional = isOptional,
    };
    return true;
  }
}
