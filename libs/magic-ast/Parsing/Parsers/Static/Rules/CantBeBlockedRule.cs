namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 955)]
public sealed class CantBeBlockedRule : IStaticRule
{
  private static readonly Regex _cantBeBlockedPattern = new(
    @"^\s*This\s+(?:creature|land|permanent|Vehicle)\s+can'?t\s+be\s+blocked\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeBlockedByMoreThanOnePattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+by\s+more\s+than\s+one\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeBlockedByPowerPattern = new(
    @"^\s*This\s+(?:creature|Vehicle|permanent)\s+can'?t\s+be\s+blocked\s+by\s+creatures\s+with\s+power\s+(?<value>\d+)\s+or\s+less\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeBlockedByPowerGreaterPattern = new(
    @"^\s*This\s+(?:creature|Vehicle|permanent)\s+can'?t\s+be\s+blocked\s+by\s+creatures\s+with\s+power\s+(?<value>\d+)\s+or\s+greater\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeBlockedByRelativePowerPattern = new(
    @"^\s*Creatures\s+with\s+power\s+less\s+than\s+.+?'s\s+power\s+can'?t\s+block\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeBlockedByColorPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+by\s+(?<color>white|blue|black|red|green)\s+creatures\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "This creature can't be blocked by Walls." — subtype-restricted evasion.
  // The subtype appears pluralised in oracle text (e.g., "Walls" → subtype "Wall").
  // Rule 509.1b: certain subtypes (Wall, Human, etc.) may legally appear as blocker
  // predicates; MAST records the subtype as printed, singular-normalised.
  private static readonly Regex _cantBeBlockedBySubtypePattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+by\s+(?<subtype>[A-Z][a-zA-Z]*)s\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Full unblockability: "This (creature|land|permanent|Vehicle) can't be blocked."
    if (_cantBeBlockedPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Combat.CantBeBlockedEffect()],
        },
      ];
    }

    // Blocker-count restriction: "This creature can't be blocked by more than one
    // creature." — Rule 509.1b. MaxBlockers = 1 records that at most one creature
    // may be declared as a blocker against this attacker. Placed before the power
    // and color arms because the "more than one creature" phrase is syntactically
    // unambiguous and must not fall through to the filter arms.
    if (_cantBeBlockedByMoreThanOnePattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              MaxBlockers = 1,
            },
          ],
        },
      ];
    }

    // Power-threshold variant: "This (creature|Vehicle) can't be blocked by creatures
    // with power N or less." — Rule 509.1b. The threshold N maps to a LessThanOrEqual
    // comparison on ObjectFilter.PowerComparison. Placed before the color arm because
    // the "by creatures with power" prefix is more specific than the bare color-name
    // lookup and should win first.
    var powerMatch = _cantBeBlockedByPowerPattern.Match(clause.RawText);
    if (powerMatch.Success && int.TryParse(powerMatch.Groups["value"].Value, out var threshold))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              BlockedByFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                PowerComparison = new MagicAST.AST.References.Comparison
                {
                  Operator = ComparisonOperator.LessThanOrEqual,
                  Value = threshold,
                },
              },
            },
          ],
        },
      ];
    }

    // Power-threshold variant: "This (creature|Vehicle) can't be blocked by creatures
    // with power N or greater." — Rule 509.1b. The threshold N maps to a
    // GreaterThanOrEqual comparison on ObjectFilter.PowerComparison. Placed immediately
    // after the "or less" arm; both share the same prefix and differ only in the
    // trailing comparison word.
    var powerGreaterMatch = _cantBeBlockedByPowerGreaterPattern.Match(clause.RawText);
    if (powerGreaterMatch.Success && int.TryParse(powerGreaterMatch.Groups["value"].Value, out var greaterThreshold))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              BlockedByFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                PowerComparison = new MagicAST.AST.References.Comparison
                {
                  Operator = ComparisonOperator.GreaterThanOrEqual,
                  Value = greaterThreshold,
                },
              },
            },
          ],
        },
      ];
    }

    // Relative-power-threshold variant: "Creatures with power less than [CardName]'s
    // power can't block it." — Rule 509.1b. The comparison value is self-referential
    // (the source creature's own power), not a printed integer, so it cannot be
    // expressed on ObjectFilter.PowerComparison (which requires a static int).
    // Stored as Characteristics: ["with power less than this creature's power"] to
    // preserve the oracle predicate exactly. "[CardName]'s" is the standard oracle
    // self-reference (Rule 201.4); "this creature's" is the pronoun variant — both
    // forms are matched by the non-greedy .+? prefix.
    if (_cantBeBlockedByRelativePowerPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              BlockedByFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Characteristics = [Characteristic.Other("with power less than this creature's power")],
              },
            },
          ],
        },
      ];
    }

    // Color-restricted variant: "This creature can't be blocked by [color] creatures."
    var colorMatch = _cantBeBlockedByColorPattern.Match(clause.RawText);
    if (colorMatch.Success)
    {
      var colorName = colorMatch.Groups["color"].Value.ToLowerInvariant();
      var colorCode = colorName switch
      {
        "white" => "W",
        "blue"  => "U",
        "black" => "B",
        "red"   => "R",
        "green" => "G",
        _       => null,
      };
      if (colorCode != null)
      {
        return
        [
          new StaticAbility
          {
            Effects =
            [
              new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
              {
                BlockedByFilter = new ObjectFilter
                {
                  CardTypes = ["creature"],
                  Colors = [colorCode],
                },
              },
            ],
          },
        ];
      }
    }

    // Subtype-restricted variant: "This creature can't be blocked by [Subtype]s."
    // e.g., "This creature can't be blocked by Walls." → subtype "Wall".
    // The oracle text always pluralises the subtype; we strip the trailing 's'
    // to store the canonical singular form matching the MTG subtype list.
    var subtypeMatch = _cantBeBlockedBySubtypePattern.Match(clause.RawText);
    if (subtypeMatch.Success)
    {
      var subtype = subtypeMatch.Groups["subtype"].Value; // already singular-capitalised
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              BlockedByFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Subtypes = [subtype],
              },
            },
          ],
        },
      ];
    }

    return null;
  }
}
