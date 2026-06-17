namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Add {mana}" — e.g. "Add {G}", "Add {C}{C}{C}", "Add {W}{U}{B}{R}{G}". Also
/// handles "Add one mana of any color" (Crystal Grotto / Chromatic Lantern shape)
/// where the produced mana is a single choice across all five colors, and
/// "Add one mana of the chosen color" (Coldsteel Heart / Shimmerdrift Vale /
/// Thriving lands) where the produced mana's color is the color chosen as this
/// permanent entered — the consumer side of a CR 607 linked "choose a color".
///
/// <para>Per CR 605.1a — "An activated ability is a mana ability if it meets all of
/// the following criteria: it doesn't require a target (see rule 115.6), it could add
/// mana to a player's mana pool when it resolves, and it's not a loyalty ability." —
/// the enclosing "{T}: Add ..." ability is a mana ability; the chosen-color back-
/// reference does not introduce a target.</para>
/// </summary>
[ActivatedEffectRule(Priority = 1000)]
public sealed class AddManaEffectRule : IActivatedEffectRule
{
  // SCALING / SEQUENTIAL / RESTRICTED mana clauses this rule does NOT model.
  // The Mana scalar is a mana EXPRESSION (symbols + "or"/commas), not prose; left
  // unguarded the trailing clause was silently swallowed into Mana with no unparsed
  // node and no residual — false coverage that inflates triage directYield. Match →
  // return null so the line falls to UnparsedEffect and surfaces as an honest,
  // pickable family (scaling-mana "for each X" / restricted-mana "Spend only to Y").
  private static readonly Regex UnmodeledManaClause = new(
    @"\bfor\s+each\b|\bfor\s+every\b|,?\s*then\s+add\b|\bspend\s+this\s+mana\s+only\b|\buntil\s+end\s+of\s+turn\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    // Normalize whitespace
    effectText = effectText.Trim();

    if (!effectText.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Extract the mana portion (everything after "Add" and before optional ".")
    var manaText = effectText[4..].Trim();
    if (manaText.EndsWith('.'))
    {
      manaText = manaText[..^1].Trim();
    }

    // "one mana of any color" optionally followed by a spend restriction —
    // "Spend this mana only to <X>." (Unclaimed Territory shape). The activated
    // ParseEffects flow runs a multi-sentence pre-pass first; when the second
    // sentence has no rule of its own it falls back to matching the WHOLE
    // combined string against each rule, so this rule must accept the joined
    // "Add one mana of any color. Spend this mana only to <X>." text. MAST
    // describes rather than executes, so the restriction is captured verbatim.
    var anyColorMatch = Regex.Match(
      manaText,
      @"^one\s+mana\s+of\s+any\s+color(?:\.\s+Spend\s+this\s+mana\s+only\s+to\s+(?<restriction>.+?))?$",
      RegexOptions.IgnoreCase
    );
    if (anyColorMatch.Success)
    {
      var restrictionGroup = anyColorMatch.Groups["restriction"];
      return new AddManaEffect
      {
        Mana = string.Empty,
        AnyColor = true,
        SpendRestriction = restrictionGroup.Success
          ? restrictionGroup.Value.Trim()
          : null,
      };
    }

    // "N mana of any one color" / "N mana of any color" where N is a word-number
    // (e.g. "three mana of any one color" — Lion's Eye Diamond). The count is a
    // fixed literal (not a variable), AnyColor=true encodes the free choice of color.
    // Rule 106.4: "When an effect instructs a player to add mana, that mana goes into
    // a player's mana pool." "Any one color" means a single free choice of W/U/B/R/G.
    var nAnyColorMatch = Regex.Match(
      manaText,
      @"^(?<word>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+mana\s+of\s+any(?:\s+one)?\s+color$",
      RegexOptions.IgnoreCase
    );
    if (nAnyColorMatch.Success)
    {
      var wordRaw = nAnyColorMatch.Groups["word"].Value;
      int count = wordRaw.ToLowerInvariant() switch
      {
        "one" => 1,
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ when int.TryParse(wordRaw, out var n) => n,
        _ => 0,
      };
      if (count > 0)
      {
        return new AddManaEffect
        {
          Mana = string.Empty,
          AnyColor = true,
          Amount = LiteralQuantity.Of(count),
        };
      }
    }

    // "one mana of the chosen color" — the produced color is the color chosen as
    // this permanent entered (CR 607 linked consumer; producer is the "As this
    // enters, choose a color" ChooseColorEffect). Captured STRUCTURALLY via the
    // OfChosenColor marker, never free-texted into Mana, mirroring the AnyColor
    // branch above.
    if (Regex.IsMatch(
          manaText,
          @"^one\s+mana\s+of\s+the\s+chosen\s+color$",
          RegexOptions.IgnoreCase))
    {
      return new AddManaEffect
      {
        Mana = string.Empty,
        OfChosenColor = true,
      };
    }

    // The mana text should be a sequence of mana symbols like "{G}" or "{C}{C}{C}",
    // optionally a colour CHOICE ("{R} or {G}", "{W}, {B}, or {G}").
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    // Counter-driven scaling mana (ADR 0009, shapes S1–S5). Parsed structurally
    // here ahead of the UnmodeledManaClause bail; the bail stays as the backstop
    // for genuinely-unmodeled shapes (spend-restriction, "until end of turn").
    var scaling = TryParseScaling(manaText);
    if (scaling is not null)
    {
      return scaling;
    }

    // Bail on scaling / sequential / restricted mana (see UnmodeledManaClause).
    if (UnmodeledManaClause.IsMatch(manaText))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
  }

  // ADR 0009: "for each [type] counter on this [noun]" — counters currently ON
  // the object (S1, Gyre Sage / Everflowing Chalice).
  private static readonly Regex ForEachCounterOn = new(
    @"^(?<mana>(?:\{[^}]+\})+)\s+for\s+each\s+(?<type>[\w\-/+]+)\s+counter\s+on\s+this\s+\w+$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // ADR 0009: "for each [type] counter removed this way" — the cost-linked count
  // (S2, Hollow Trees / Fountain of Cho).
  private static readonly Regex ForEachCounterRemovedThisWay = new(
    @"^(?<mana>(?:\{[^}]+\})+)\s+for\s+each\s+(?<type>[\w\-/+]+)\s+counter\s+removed\s+this\s+way$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // ADR 0009: "X mana in any combination of {W} and/or {U}" (or "…of colors")
  // (S3, Calciform Pools / Crucible of the Spirit Dragon).
  private static readonly Regex AnyCombination = new(
    @"^X\s+mana\s+in\s+any\s+combination\s+of\s+(?<colors>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // ADR 0009: "{B}, then add an additional {B} for each charge counter removed
  // this way" (S4, the Mana Batteries) — a base unit then one-per-removed.
  private static readonly Regex BaseThenAdditionalPerRemoved = new(
    @"^(?<base>(?:\{[^}]+\})+),\s*then\s+add\s+an\s+additional\s+(?<each>(?:\{[^}]+\})+)\s+for\s+each\s+(?<type>[\w\-/+]+)\s+counter\s+removed\s+this\s+way$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // ADR 0009: "an amount of {C} equal to X plus one" (S5, Kyren Toy).
  private static readonly Regex AmountEqualToXPlusOne = new(
    @"^an\s+amount\s+of\s+(?<mana>(?:\{[^}]+\})+)\s+equal\s+to\s+X\s+plus\s+one$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // S6 — "{G} for each [Subtype] on the battlefield" (Priest of Titania and similar).
  // Counts ALL permanents of the named subtype on the battlefield, regardless of controller.
  // CR 605.1a: the enclosing {T}: Add ... ability is a mana ability (no target, could add mana).
  private static readonly Regex ForEachSubtypeOnBattlefield = new(
    @"^(?<mana>(?:\{[^}]+\})+)\s+for\s+each\s+(?<subtype>[A-Z][a-zA-Z'-]+)\s+on\s+the\s+battlefield$",
    RegexOptions.Compiled
  );

  /// <summary>
  /// Parses the counter-driven scaling-mana shapes (ADR 0009 S1–S5) from the
  /// post-"Add " mana text. Returns null for non-scaling text so the caller keeps
  /// its existing flat-mana / bail behaviour. Every shape's enclosing ability is a
  /// mana ability (CR 605.1a); the "removed this way" link is
  /// reference-not-resolution (ADR 0004) — no variable threaded from the cost.
  /// </summary>
  private static Effect? TryParseScaling(string manaText)
  {
    // S1 — "{G} for each +1/+1 counter on this creature".
    var onMatch = ForEachCounterOn.Match(manaText);
    if (onMatch.Success)
    {
      return new AddManaEffect
      {
        Mana = onMatch.Groups["mana"].Value,
        AnyColor = false,
        Amount = new CounterCountQuantity
        {
          CounterType = onMatch.Groups["type"].Value.ToLowerInvariant(),
          On = ObjectReference.Self(),
        },
      };
    }

    // S2 — "{G} for each storage counter removed this way".
    var removedMatch = ForEachCounterRemovedThisWay.Match(manaText);
    if (removedMatch.Success)
    {
      return new AddManaEffect
      {
        Mana = removedMatch.Groups["mana"].Value,
        AnyColor = false,
        Amount = new CountersRemovedThisWayQuantity
        {
          CounterType = removedMatch.Groups["type"].Value.ToLowerInvariant(),
        },
      };
    }

    // S3 — "X mana in any combination of {W} and/or {U}" / "…of colors".
    var combMatch = AnyCombination.Match(manaText);
    if (combMatch.Success)
    {
      var colors = ParseAnyCombinationColors(combMatch.Groups["colors"].Value);
      if (colors is not null)
      {
        return new AddManaEffect
        {
          Mana = string.Empty,
          AnyColor = false,
          Amount = VariableQuantity.X,
          AnyCombinationOf = colors,
        };
      }
    }

    // S4 — "{B}, then add an additional {B} for each charge counter removed this way".
    var s4 = BaseThenAdditionalPerRemoved.Match(manaText);
    if (s4.Success)
    {
      var counterType = s4.Groups["type"].Value.ToLowerInvariant();
      return new CompositeEffect
      {
        Effects =
        [
          new AddManaEffect
          {
            Mana = s4.Groups["base"].Value,
            AnyColor = false,
            Amount = LiteralQuantity.Of(1),
          },
          new AddManaEffect
          {
            Mana = s4.Groups["each"].Value,
            AnyColor = false,
            Amount = new CountersRemovedThisWayQuantity { CounterType = counterType },
          },
        ],
      };
    }

    // S5 — "an amount of {C} equal to X plus one".
    var s5 = AmountEqualToXPlusOne.Match(manaText);
    if (s5.Success)
    {
      return new AddManaEffect
      {
        Mana = s5.Groups["mana"].Value,
        AnyColor = false,
        Amount = new CalculatedQuantity
        {
          BaseQuantity = VariableQuantity.X,
          Operation = "add",
          Operand = 1,
        },
      };
    }

    // S6 — "{G} for each [Subtype] on the battlefield" (Priest of Titania).
    // Counts ALL permanents of the named creature subtype on the battlefield across
    // all players — no Controller filter because "on the battlefield" is unrestricted.
    // CR 605.1a: mana ability — no target, could add mana, not a loyalty ability.
    var s6 = ForEachSubtypeOnBattlefield.Match(manaText);
    if (s6.Success)
    {
      return new AddManaEffect
      {
        Mana = s6.Groups["mana"].Value,
        AnyColor = false,
        Amount = new CountQuantity
        {
          CountOf = new ObjectFilter
          {
            Subtypes = [s6.Groups["subtype"].Value],
            Zone = Zone.Battlefield,
          },
        },
      };
    }

    return null;
  }

  /// <summary>
  /// Parses the colour set of "in any combination of …": "{W} and/or {U}" →
  /// ["W","U"]; "colors" → the five colours. Returns null if no colour symbols
  /// are found so the caller can fall through.
  /// </summary>
  private static IReadOnlyList<string>? ParseAnyCombinationColors(string colorsText)
  {
    var trimmed = colorsText.Trim();
    if (Regex.IsMatch(trimmed, @"^colors$", RegexOptions.IgnoreCase))
    {
      return ["W", "U", "B", "R", "G"];
    }

    var symbols = Regex.Matches(trimmed, @"\{(?<c>[WUBRG])\}");
    if (symbols.Count == 0)
    {
      return null;
    }
    return symbols.Select(s => s.Groups["c"].Value.ToUpperInvariant()).ToList();
  }
}
