namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the two-sentence conditional-token-creation shape:
/// "create a P1/T1 color1 Subtype1 creature token. If [condition], create a P2/T2 color2 Subtype2
/// creature token instead."
///
/// <para>
/// Rule 111 (tokens), CR 603 (triggered abilities). The word "instead" signals that the second
/// creation REPLACES the first (not in addition to it). Modelled as a <see cref="ConditionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="ConditionalEffect.Condition"/> — the mid-resolution board-state predicate;</item>
///   <item><see cref="ConditionalEffect.Then"/> — the token created when the condition is true (the
///   "instead" token from the second sentence);</item>
///   <item><see cref="ConditionalEffect.Else"/> — the token created when the condition is false (the
///   default token from the first sentence).</item>
/// </list>
/// </para>
///
/// <para>
/// The Necrobloom (BIG) is the canonical example:
/// "create a 0/1 green Plant creature token. If you control seven or more lands with different names,
/// create a 2/2 black Zombie creature token instead."
/// CR 207.2c: "Landfall" is a CR ability word with no special rules meaning. CR 603: triggered
/// abilities fire "Whenever a land you control enters". The condition "you control seven or more lands
/// with different names" is an <see cref="OtherCondition"/> residual (ADR 0001) because
/// <see cref="ObjectFilter"/> does not yet have a "different names" axis.
/// </para>
///
/// <para>
/// Called as a composite path from
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> BEFORE the sentence bundle
/// splitter, so the two-sentence shape is not incorrectly split into two independent token
/// creation effects.
/// </para>
///
/// <para>
/// A sibling shape handled by the same composite path: "create a P/T color Subtype creature
/// token [with "&lt;ability&gt;"]. If the creature had power N or greater, create &lt;count&gt;
/// of those tokens instead." — the SAME token definition, with the "instead" branch creating
/// MULTIPLE copies of it rather than a distinct replacement token (Anax, Hardened in the Forge:
/// "create a 1/1 red Satyr creature token with "This token can't block." If the creature had
/// power 4 or greater, create two of those tokens instead."). "The creature" is the creature
/// named by the trigger's dies event — recorded as <see cref="DerivedQuantity.Source"/> = "the
/// creature" (the established short-antecedent convention, e.g. "it"/"the card you exiled"),
/// not a free-text condition. CR 604.3 does not apply here (this is an ordinary triggered
/// effect, not a CDA); CR 111.2 (token creation) and CR 603 (triggered abilities) do.
/// </para>
/// </summary>
public static class CreateTokenOrInsteadIfConditionRule
{
  // "create a P/T color Subtype creature token[ with "<ability>"]. If the creature had power N
  // or greater, create <count word> of those tokens instead." Anchored (^…$); disjoint from
  // _pattern above because that shape's second sentence always describes a DIFFERENT token
  // (its own P/T/color/subtype), never "those tokens".
  //
  // The two alternatives after "creature token" are mutually exclusive: a bare token
  // description ends in its own sentence period ("...token."), while a quoted with-clause
  // ends in the QUOTED ability's own internal period ("...with \"This token can't block.\"")
  // — the closing quote is NOT followed by a second sentence-terminating period.
  private static readonly Regex _doubledCountPattern = new(
    @"^create\s+a\s+(?<p>\d+)/(?<t>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<sub>[A-Z][a-z]+)"
      + @"\s+creature\s+token(?:\.|\s+with\s+""(?<ability>[^""]+)"")\s+"
      + @"If\s+the\s+creature\s+had\s+power\s+(?<threshold>\d+)\s+or\s+greater,\s+"
      + @"create\s+(?<count>two|three|four|five|six|seven|eight|nine|ten)\s+of\s+those\s+tokens\s+instead\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, int> _countWords =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };
  // Matches the full two-sentence pattern (without trailing period stripped by caller):
  //   "create a P/T color Subtype creature token. If [condition], create a P/T color Subtype
  //    creature token instead"
  // Group "cond" captures the condition phrase between "If " and ",".
  // Groups "p1"/"t1"/"col1"/"sub1" capture the default token (first sentence).
  // Groups "p2"/"t2"/"col2"/"sub2" capture the replacement token (second sentence, "instead").
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+(?<p1>\d+)/(?<t1>\d+)\s+(?<col1>white|blue|black|red|green)\s+(?<sub1>[A-Z][a-z]+)"
      + @"\s+creature\s+token\.\s+"
      + @"If\s+(?<cond>[^,]+),\s+create\s+a\s+(?<p2>\d+)/(?<t2>\d+)\s+(?<col2>white|blue|black|red|green)"
      + @"\s+(?<sub2>[A-Z][a-z]+)\s+creature\s+token\s+instead\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorMap =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
    };

  /// <summary>
  /// Attempts to match <paramref name="text"/> as either two-sentence conditional-token-creation
  /// shape: the distinct-replacement-token pattern, or the sibling same-token-doubled-count
  /// pattern (see class remarks). Returns the <see cref="ConditionalEffect"/> on success; null on
  /// no-match.
  /// </summary>
  public static Effect? TryMatch(string text)
  {
    var doubled = TryMatchDoubledCount(text);
    if (doubled is not null)
    {
      return doubled;
    }

    var match = _pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    if (!_colorMap.TryGetValue(match.Groups["col1"].Value, out var colorCode1) ||
        !_colorMap.TryGetValue(match.Groups["col2"].Value, out var colorCode2))
    {
      return null;
    }

    var sub1 = NormalizeSubtype(match.Groups["sub1"].Value);
    var sub2 = NormalizeSubtype(match.Groups["sub2"].Value);
    var condText = match.Groups["cond"].Value.Trim();

    // Default token (created when condition is FALSE — the "Else" branch).
    var defaultToken = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = match.Groups["p1"].Value,
        Toughness = match.Groups["t1"].Value,
        Colors = [colorCode1],
        Types = ["creature"],
        Subtypes = [sub1],
      },
    };

    // Replacement token (created when condition is TRUE — the "Then" branch).
    var replacementToken = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = match.Groups["p2"].Value,
        Toughness = match.Groups["t2"].Value,
        Colors = [colorCode2],
        Types = ["creature"],
        Subtypes = [sub2],
      },
    };

    return new ConditionalEffect
    {
      Condition = MagicAST.Parsing.ConditionParser.Parse(condText),
      Then = replacementToken,
      Else = defaultToken,
    };
  }

  private static string NormalizeSubtype(string raw)
  {
    if (raw.Length == 0) return raw;
    return char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();
  }

  /// <summary>
  /// Attempts to match <paramref name="text"/> as the same-token-doubled-count shape: "create a
  /// P/T color Subtype creature token [with "&lt;ability&gt;"]. If the creature had power N or
  /// greater, create &lt;count&gt; of those tokens instead." Returns null on no-match.
  /// </summary>
  private static Effect? TryMatchDoubledCount(string text)
  {
    var match = _doubledCountPattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    if (!_colorMap.TryGetValue(match.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var subtype = NormalizeSubtype(match.Groups["sub"].Value);
    var count = match.Groups["count"].Success && _countWords.TryGetValue(match.Groups["count"].Value, out var cw)
      ? cw
      : 2;
    var threshold = int.Parse(match.Groups["threshold"].Value);

    IReadOnlyList<Ability>? tokenAbilities = null;
    if (match.Groups["ability"].Success)
    {
      var granted = ParseQuotedTokenAbility(match.Groups["ability"].Value.Trim());
      if (granted is not null)
      {
        tokenAbilities = [granted];
      }
    }

    var token = new TokenDefinition
    {
      Power = match.Groups["p"].Value,
      Toughness = match.Groups["t"].Value,
      Colors = [colorCode],
      Types = ["creature"],
      Subtypes = [subtype],
      Abilities = tokenAbilities,
    };

    // "the creature" — the creature named by the trigger's dies event (CR 603). Recorded via
    // the established short-antecedent Source convention (DerivedQuantity.Source = "it"/"the
    // card you exiled"), not a free-text condition (ADR 0004: reference-not-resolution).
    var condition = new MagicAST.AST.Abilities.QuantityComparisonCondition
    {
      Left = new DerivedQuantity { DerivedFrom = DerivedKind.Power, Source = "the creature" },
      Operator = ComparisonOperator.GreaterThanOrEqual,
      Right = LiteralQuantity.Of(threshold),
    };

    return new ConditionalEffect
    {
      Condition = condition,
      Then = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(count),
        Token = token,
      },
      Else = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = token,
      },
    };
  }

  /// <summary>
  /// Maps a quoted token-ability sentence to a structured <see cref="Ability"/>. Narrowly scoped
  /// to the recognised shape ("This token can't block.") rather than a general sentence parser;
  /// returns null for unrecognised text so the caller can fall back gracefully.
  /// </summary>
  private static Ability? ParseQuotedTokenAbility(string quoted)
  {
    if (Regex.IsMatch(quoted, @"^this\s+token\s+can'?t\s+block\.?$", RegexOptions.IgnoreCase))
    {
      return new StaticAbility { Effects = [new CantBlockEffect()] };
    }
    return null;
  }
}
