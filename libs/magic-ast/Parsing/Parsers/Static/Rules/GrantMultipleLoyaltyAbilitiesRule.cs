namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// "[Subject] have [quoted loyalty ability 1] and [quoted loyalty ability 2]." --
/// grants two loyalty activated abilities to the named permanent type.
///
/// <para>
/// Paradigm card: Ichormoon Gauntlet -- "Planeswalkers you control have
/// [0]: Proliferate and [-12]: Take an extra turn after this one."
/// </para>
///
/// <para>
/// The quoted bodies use Scryfall bracket-notation loyalty costs: [0],
/// [+N], [-N]. Each body is parsed by stripping the bracket-notation
/// cost, setting <see cref="ClauseClassification.LoyaltyCost"/> accordingly, and
/// routing the remaining effect text through <see cref="ActivatedAbilityParser"/>.
/// </para>
///
/// <para>
/// The output is a single <see cref="StaticAbility"/> whose Effects list
/// contains one <see cref="GainAbilityEffect"/> per granted ability, all sharing
/// the same Target (the filtered subject). This mirrors the multi-effect
/// pattern established by Bloodsworn Steward ("+2/+2 and have haste").
/// </para>
///
/// <para>
/// ANCHOR: the full-line regex is anchored (^...$) to prevent matching substrings of
/// longer ability lines. Priority 997 -- fires BEFORE GrantedAbilityRule (Priority 995)
/// so the two-quote form is captured here and not partially matched by the single-quote
/// shape.
/// </para>
/// </summary>
[StaticRule(Priority = 997)]
public sealed class GrantMultipleLoyaltyAbilitiesRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // Quote character class covering:
  //   ASCII " (U+0022)
  //   left curly quotation mark " (U+201C)
  //   right curly quotation mark " (U+201D)
  // Oracle text uses curly quotes for granted-ability bodies (Scryfall convention).
  // The “ and ” escape sequences are processed by the C# compiler,
  // yielding the actual Unicode characters inside the compiled string.
  // Q    = regex fragment that matches any one quote character (open or close)
  // NotQ = regex fragment that matches any character NOT a quote character (body content)
  private static readonly string Q = "[\"“”]";
  private static readonly string NotQ = "[^\"“”]";

  // Matches: [subject] have "[body1]" and "[body2]"
  // Fully anchored (^...$) to prevent substring collisions.
  private static readonly Regex _pattern;

  // Bracket-notation loyalty cost: [0], [+N], [-N], [u2212 N] (minus sign)
  private static readonly Regex _loyaltyCostPattern = new(
    @"^\[(?<sign>[+\-−]?)(?<num>\d+)\]\s*:\s*",
    RegexOptions.Compiled
  );

  static GrantMultipleLoyaltyAbilitiesRule()
  {
    // Build pattern from parts to keep it readable.
    var p = @"^\s*(?<filter>" + NotQ + @"+?)\s+have\s+"
          + Q + @"(?<body1>" + NotQ + @"+)" + Q
          + @"\s+and\s+"
          + Q + @"(?<body2>" + NotQ + @"+)" + Q
          + @"\.?\s*$";
    _pattern = new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled);
  }

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var body1 = match.Groups["body1"].Value.Trim();
    var body2 = match.Groups["body2"].Value.Trim();

    // Guard: the filter must not contain "gets" or "and" (see GrantedAbilityRule guard --
    // compound buff+grant lines have those keywords in the subject phrase).
    if (
      filterText.Contains(" gets ", StringComparison.OrdinalIgnoreCase)
      || filterText.Contains(" and ", StringComparison.OrdinalIgnoreCase)
    )
    {
      return null;
    }

    var target = ClassifyGrantTarget(filterText);
    if (target is null)
    {
      return null;
    }

    var granted1 = TryParseGrantedLoyaltyBody(body1);
    var granted2 = TryParseGrantedLoyaltyBody(body2);

    if (granted1 is null || granted2 is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = granted1,
          },
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = granted2,
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses a quoted body that begins with a Scryfall bracket-notation loyalty cost
  /// ([0]:, [+N]:, [-N]:). Returns the parsed
  /// <see cref="ActivatedAbility"/> with <see cref="ActivatedAbility.LoyaltyCost"/> set,
  /// or null if the body's shape is not recognised.
  /// </summary>
  private Ability? TryParseGrantedLoyaltyBody(string body)
  {
    var loyaltyMatch = _loyaltyCostPattern.Match(body);
    if (!loyaltyMatch.Success)
    {
      return null;
    }

    var sign = loyaltyMatch.Groups["sign"].Value;
    var numStr = loyaltyMatch.Groups["num"].Value;
    if (!int.TryParse(numStr, out var num))
    {
      return null;
    }

    int loyaltyCost;
    if (sign == "+" )
    {
      loyaltyCost = num;
    }
    else if (sign == string.Empty)
    {
      // "[0]:" -- zero, no sign
      loyaltyCost = 0;
    }
    else
    {
      // "[-N]:" or "[u2212 N]:" -- negative (ASCII hyphen or Unicode minus)
      loyaltyCost = -num;
    }

    // Pass the FULL body (e.g. "[0]: Proliferate") to ActivatedAbilityParser.
    // The parser finds the colon after [0] and splits cost/effect from there.
    // Since LoyaltyCost is set on the classification, ParseCosts returns []
    // immediately without trying to interpret "[0]" as a cost component.
    if (body.Length == 0)
    {
      return null;
    }

    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue ? tokenResult.Value : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };

    var innerClassification = new ClauseClassification
    {
      Kind = AbilityKind.Activated,
      Confidence = 1.0,
      LoyaltyCost = loyaltyCost,
    };

    return new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
  }

  /// <summary>
  /// Maps the noun-phrase left of "have" onto an ObjectReference target.
  /// Covers the same controller-scoped card-type and subtype grant shapes as
  /// <see cref="GrantedAbilityRule.ClassifyGrantTarget"/> (duplicated here to keep
  /// this rule self-contained).
  /// </summary>
  private static ObjectReference? ClassifyGrantTarget(string filterText)
  {
    var lower = filterText.ToLowerInvariant();

    // "Enchanted creature" / "Equipped creature" -- attach-scoped grant
    if (lower.StartsWith("enchanted ", StringComparison.Ordinal)
        || lower.StartsWith("equipped ", StringComparison.Ordinal))
    {
      return new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
    }

    // "Planeswalkers you control" / "Creatures you control" / etc.
    var controlMatch = Regex.Match(
      filterText,
      @"^(?<type>Creatures|Artifacts|Enchantments|Lands|Planeswalkers|Permanents)\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$",
      RegexOptions.IgnoreCase
    );
    if (controlMatch.Success)
    {
      var plural = controlMatch.Groups["type"].Value.ToLowerInvariant();
      var singular = plural.EndsWith('s') ? plural[..^1] : plural;
      var ctrl = controlMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var controller = ctrl.StartsWith("you", StringComparison.Ordinal)
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [singular],
          Controller = controller,
        },
      };
    }

    // "All Slivers" / "All Zombies" -- global subtype grant
    var allMatch = Regex.Match(filterText, @"^All\s+(?<sub>[A-Z][a-z]+)s\b\.?$");
    if (allMatch.Success)
    {
      var subtype = allMatch.Groups["sub"].Value;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { Subtypes = [subtype] },
      };
    }

    return null;
  }
}
