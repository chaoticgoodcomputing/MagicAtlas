namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Until end of turn, enchanted [BasicLandType] becomes a [P/T] [colors] [subtype]
/// creature with "[quoted activated ability]." It's still a land." — the Kamigawa
/// "Genju" cycle's animate line. A variant of the Keyrune/manland "becomes a
/// creature" template (<see cref="BecomesCreatureEffectRule"/>) that differs in two
/// ways the shared rule doesn't cover:
/// <list type="bullet">
///   <item>The subject is "enchanted [BasicLandType]" (the Aura's attached
///   permanent, CR 303.4c / 702.5) rather than "This [type]" (the source itself).</item>
///   <item>The granted ability is a full quoted ACTIVATED ability
///   ("{B}: This creature gets +1/+1 until end of turn.") rather than a single
///   keyword word (flying, trample, reach) — CR 113.6 / 607.</item>
/// </list>
///
/// <para>
/// The quoted ability is parsed into a real <see cref="ActivatedAbility"/> node,
/// mirroring how a top-level "{cost}: [effect]" ability parses (e.g. Gateway
/// Shade's "{B}: This creature gets +1/+1 until end of turn." → Costs=[mana B],
/// Effects=[modifyPT Self +1/+1 untilEndOfTurn]) — the same structured shape, just
/// nested under <see cref="BecomesCreatureEffect.GainedAbilities"/> instead of at
/// the ability list's top level. This keeps the granted ability fully structured
/// (no free text) rather than a residual string.
/// </para>
///
/// <para>
/// Canonical card: Genju of the Fens (DIS) — "{2}: Until end of turn, enchanted
/// Swamp becomes a 2/2 black Spirit creature with "{B}: This creature gets +1/+1
/// until end of turn." It's still a land." Quote characters are accepted as either
/// straight (") or curly (“ ”) — oracle text sources vary — and the apostrophe in
/// "It's" as either straight (') or curly (’).
/// </para>
///
/// <para>
/// Priority 990 — above <see cref="BecomesCreatureEffectRule"/> (985), so this more
/// specific "enchanted [BasicLandType] ... with "[quoted ability]"" shape is tried
/// first; the shared rule's "This [type] becomes..." patterns don't match this
/// subject/granted-ability shape anyway (no collision).
/// </para>
///
/// Rule 205.1b (retained card type on a "still a [type]" animate); Rule 305.7 (an
/// animated land keeps its land types); Rule 208 (power/toughness); Rule 113.6 /
/// 607 (granting an ability); Rule 611 (continuous effects).
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class AnimateEnchantedBasicLandWithGrantedActivatedAbilityRule : IActivatedEffectRule
{
  private static readonly ManaCostParser _manaCostParser = new();

  // "Until end of turn, enchanted Swamp becomes a 2/2 black Spirit creature with
  // "{B}: This creature gets +1/+1 until end of turn." It's still a land."
  private static readonly Regex _pattern = new(
    @"^Until\s+end\s+of\s+turn,\s+enchanted\s+(?<subtype>Plains|Island|Swamp|Mountain|Forest)\s+becomes\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature\s+with\s+[""“](?<granted>[^""”]+)[""”]\.?\s*It['’]s\s+still\s+a\s+(?<retain>\w+)\.?\s*$",
    RegexOptions.Compiled
  );

  // "{B}: This creature gets +1/+1 until end of turn." — the granted quoted ability.
  private static readonly Regex _grantedAbilityPattern = new(
    @"^\{(?<mana>[^}]+)\}:\s*This\s+creature\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s+until\s+end\s+of\s+turn\.?\s*$",
    RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
      ["colorless"] = "C",
    };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var grantedAbility = ParseGrantedAbility(match.Groups["granted"].Value.Trim());
    if (grantedAbility is null)
    {
      // Granted ability shape not recognised — bail so the clause surfaces as
      // unparsed rather than silently dropping the granted ability (no free text).
      return null;
    }

    // The enchanted basic land type (e.g. "Swamp") is captured by the pattern to
    // scope the match (it's a real subject noun, not a free variable), but is not
    // repeated on this effect — it is already stated by the Aura's Enchant
    // restriction (EnchantBasicLandTypeRule) and by the trigger filter that
    // watches this same permanent leaving the battlefield; recording it a third
    // time here would duplicate, not add, information.
    var retain = match.Groups["retain"].Value.Trim().ToLowerInvariant();

    var cardTypes = new List<string>();
    // CR 205.1b / 305.7: the animate is additive — the enchanted land keeps its
    // "land" card type and gains "creature". The retained type is the "It's still
    // a [type]" confirmation, consumed here rather than emitted separately.
    if (retain == "land")
    {
      cardTypes.Add("land");
    }

    var colors = new List<string>();
    var subtypes = new List<string>();
    foreach (var rawWord in match.Groups["spec"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
      var word = rawWord.Trim();
      if (word.Length == 0 || string.Equals(word, "and", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }
      if (_colorCodes.TryGetValue(word, out var code))
      {
        colors.Add(code);
      }
      else
      {
        subtypes.Add(char.ToUpperInvariant(word[0]) + word[1..]);
      }
    }
    cardTypes.Add("creature");

    return new BecomesCreatureEffect
    {
      // "enchanted [BasicLandType]" — the Aura's attached permanent (CR 303.4c / 702.5),
      // not the Aura itself.
      Subject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      Power = ParsePT(match.Groups["p"].Value),
      Toughness = ParsePT(match.Groups["t"].Value),
      Colors = colors,
      CardTypes = cardTypes,
      AddedSubtypes = subtypes,
      GainedAbilities = [grantedAbility],
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }

  private static Ability? ParseGrantedAbility(string quotedText)
  {
    var match = _grantedAbilityPattern.Match(quotedText.Trim());
    if (!match.Success)
    {
      return null;
    }

    ManaCost manaCost;
    try
    {
      var parsed = _manaCostParser.Parse("{" + match.Groups["mana"].Value + "}");
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      manaCost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    return new ActivatedAbility
    {
      Costs = [manaCost],
      Effects =
      [
        new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = LiteralQuantity.Of(int.Parse(match.Groups["p"].Value)),
          ToughnessModifier = LiteralQuantity.Of(int.Parse(match.Groups["t"].Value)),
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      IsManaAbility = false,
    };
  }

  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
