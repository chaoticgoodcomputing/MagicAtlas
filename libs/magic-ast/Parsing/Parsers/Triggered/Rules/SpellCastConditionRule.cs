namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you cast a spell" / "Whenever an opponent casts a spell" /
/// "Whenever you cast a spell that's white, blue, black, or red" / etc.
/// Encodes the caster and any inline spell-color/type qualifiers on the filter.
/// </summary>
[TriggerConditionRule(Priority = 998)]
public sealed class SpellCastConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("cast") || !lower.Contains("spell"))
    {
      return null;
    }

    // Recognize "[subject] cast(s) [some] spell ..."
    if (!Regex.IsMatch(triggerText, @"\bcasts?\b", RegexOptions.IgnoreCase))
    {
      return null;
    }
    if (!Regex.IsMatch(triggerText, @"\bspell\b", RegexOptions.IgnoreCase))
    {
      return null;
    }

    // Caster (controller filter)
    ControllerFilter? controller = null;
    if (Regex.IsMatch(lower, @"\b(you|an?\s+opponent|an?\s+player)\b"))
    {
      controller = lower.Contains("opponent")
        ? ControllerFilter.Opponent
        : lower.Contains("you")
          ? ControllerFilter.You
          // "a player" / "any player" — no restriction to one side; Any encodes this. Rule 102.1.
          : Regex.IsMatch(lower, @"\ba\s+player\b") ? ControllerFilter.Any : null;
    }

    // Card-type qualifiers on the cast spell ("creature spell", "noncreature spell", etc.)
    var characteristics = new List<string>();

    // "instant or sorcery spell" — combined disjunction (Rule 700.4). Must be
    // detected before the per-word loop so both halves are captured.
    if (Regex.IsMatch(lower, @"\binstant\s+or\s+sorcery\s+spell\b"))
    {
      characteristics.Add("instant");
      characteristics.Add("sorcery");
    }
    else
    {
      foreach (var word in new[] { "creature", "noncreature", "instant", "sorcery", "artifact", "enchantment" })
      {
        if (Regex.IsMatch(lower, $@"\b{Regex.Escape(word)}\s+spell\b"))
        {
          characteristics.Add(word);
        }
      }
    }

    // Color qualifiers: "that's white" / "that's white, blue, black, or red" /
    // "white spell" etc. Look for any colour word in the trigger fragment.
    var colors = new List<string>();
    var colorMap = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMap)
    {
      if (Regex.IsMatch(lower, $@"\b{name}\b"))
      {
        colors.Add(code);
      }
    }

    // "this spell from anywhere other than exile" — Rory Williams shape.
    if (lower.Contains("this spell from anywhere other than exile"))
    {
      characteristics.Add("this spell from anywhere other than exile");
    }

    // Heroic ability-word (Rule 702.108): "...a spell that targets this creature".
    if (Regex.IsMatch(lower, @"\bthat\s+targets?\s+this\s+(creature|permanent|card)\b"))
    {
      characteristics.Add("targeting this creature");
    }

    // Multicolored qualifier: "a multicolored spell" (Rule 105.5). Encoded on
    // IsMulticolored rather than Colors to preserve the two-or-more constraint.
    bool? isMulticolored = null;
    if (Regex.IsMatch(lower, @"\bmulticolored\b"))
    {
      isMulticolored = true;
    }

    // Build filter. Suppress CardTypes=["spell"] when no qualifiers were detected
    // and the controller is non-You (matches RhysticStudy's gold).
    var hasAnyQualifier = characteristics.Count > 0 || colors.Count > 0 || isMulticolored == true;
    IReadOnlyList<string>? cardTypes = hasAnyQualifier ? new List<string> { "spell" } : null;

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors.Count > 0 ? colors : null,
      IsMulticolored = isMulticolored,
      Controller = controller,
    };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = filter,
    };
  }
}
