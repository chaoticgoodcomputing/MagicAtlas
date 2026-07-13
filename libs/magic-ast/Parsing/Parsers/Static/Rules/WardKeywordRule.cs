namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// Parses Ward keyword abilities in five cost forms:
/// <list type="bullet">
/// <item>Mana cost: "Ward {N}" — e.g. "Ward {2}"</item>
/// <item>Life cost: "Ward—Pay N life." — e.g. "Ward—Pay 7 life."</item>
/// <item>Sacrifice cost: "Ward—Sacrifice a [filter]." — e.g. "Ward—Sacrifice a Food."</item>
/// <item>Discard cost: "Ward—Discard a card." — e.g. "Ward—Discard a card." (Sunset Saboteur)</item>
/// <item>Compound mana+life cost: "Ward—{N}, Pay N life." — e.g. Ovika, Enigma Goliath's
/// "Ward—{3}, Pay 3 life." A single Ward cost slot (CR 702.21a) that is itself a comma-joined
/// pair of a mana cost and a life payment, mirrored on <see cref="CompositeCost"/> the same way
/// activated-ability cost lists bundle multiple cost kinds (CR 601.2f — "the total cost is the
/// mana cost plus all additional costs").</item>
/// </list>
/// CR 702.21a: "Ward is a triggered ability. Ward [cost] means 'Whenever this permanent
/// becomes the target of a spell or ability an opponent controls, counter that spell or
/// ability unless that player pays [cost].'"
/// </summary>
[StaticRule(Priority = 989)]
public sealed class WardKeywordRule : IStaticRule
{
  // Matches: "Ward {2}" or "Ward {2}{G}" (space-separated mana cost)
  private static readonly Regex ManaCostPattern = new(
    @"^\s*Ward\s+(?<cost>(?:\{[^}]+\})+)\s*(?<rest>.*)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "Ward—Pay N life." (em-dash separated life cost, CR 702.21a)
  private static readonly Regex LifeCostPattern = new(
    @"^\s*Ward—Pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life[.\s]*(?<rest>.*)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "Ward—{N}, Pay N life." (em-dash separated COMPOUND cost: a mana cost followed by
  // a comma-joined life payment, CR 702.21a). Anchored at the "Ward—" head and requires the
  // mana-cost group to be immediately followed by a comma so it cannot match the bare mana-cost
  // form (which uses a space, not an em-dash, before the cost — see ManaCostPattern) or the bare
  // life-cost form (which has no leading mana-cost group — see LifeCostPattern).
  private static readonly Regex CompoundManaLifeCostPattern = new(
    @"^\s*Ward—(?<manacost>(?:\{[^}]+\})+)\s*,\s*Pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life[.\s]*(?<rest>(?:\([^)]+\))?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches ONLY the SINGULAR "Ward—Sacrifice a/an [filter]." form (Ygra: "Ward—Sacrifice a Food.").
  // The plural/count form ("Ward—Sacrifice two permanents." — Ulamog, +6 siblings) is deliberately
  // excluded: ParseSacrificePattern emits the count-word as a bogus Subtype ({Subtypes:["two"]}), so
  // those fall through to the base (Ward-cost unparsed) rather than being mislabeled. CR 702.21a.
  // Anchored at both ends so it cannot match as a substring of a more-specific pattern.
  private static readonly Regex SacrificeCostPattern = new(
    @"^\s*Ward—(?<saccost>Sacrifice\s+an?\s+.+?)[.\s]*(?<rest>(?:\([^)]+\))?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "Ward—Discard a card." (em-dash separated NON-mana discard cost, CR 702.21a).
  // Anchored at both ends so it cannot match as a substring of a more-specific sibling.
  private static readonly Regex DiscardCostPattern = new(
    @"^\s*Ward—(?<disccost>Discard\s+.+?)[.\s]*(?<rest>(?:\([^)]+\))?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    Cost? wardCost = null;
    Parenthetical? reminder = null;

    // Try the compound mana+life form first: "Ward—{N}, Pay N life." Must be tried before
    // the bare mana-cost form below, since that pattern's permissive "\s*(?<rest>.*)$" tail
    // would otherwise swallow the ", Pay N life." remainder as unrecognised trailing text.
    var compoundMatch = CompoundManaLifeCostPattern.Match(clause.RawText);
    if (compoundMatch.Success)
    {
      var manaCostStr = compoundMatch.Groups["manacost"].Value;
      ManaCost parsedManaCost;
      try
      {
        var parsedMana = new MagicAST.Parsing.ManaCostParser().Parse(manaCostStr);
        if (parsedMana.Symbols.Count == 0)
        {
          return null;
        }
        parsedManaCost = new ManaCost { Symbols = parsedMana.Symbols };
      }
      catch
      {
        return null;
      }

      var rawAmount = compoundMatch.Groups["amount"].Value;
      var amount = ParseNumberWord(rawAmount);
      if (amount is null)
      {
        return null;
      }

      wardCost = new CompositeCost
      {
        Costs = [parsedManaCost, new PayLifeCost { Amount = LiteralQuantity.Of(amount.Value) }],
      };

      var compoundRest = compoundMatch.Groups["rest"].Value.Trim();
      if (compoundRest.StartsWith('(') && compoundRest.EndsWith(')'))
      {
        reminder = new Parenthetical { Text = compoundRest };
      }
    }
    else
    {
      // Try mana cost form: "Ward {N}"
      var manaMatch = ManaCostPattern.Match(clause.RawText);
      if (manaMatch.Success)
      {
        var costStr = manaMatch.Groups["cost"].Value;
        try
        {
          var parsed = new MagicAST.Parsing.ManaCostParser().Parse(costStr);
          if (parsed.Symbols.Count == 0)
          {
            return null;
          }
          wardCost = new ManaCost { Symbols = parsed.Symbols };
        }
        catch
        {
          return null;
        }

        var rest = manaMatch.Groups["rest"].Value.Trim();
        if (rest.StartsWith('(') && rest.EndsWith(')'))
        {
          reminder = new Parenthetical { Text = rest };
        }
      }
      else
      {
        // Try life cost form: "Ward—Pay N life."
        var lifeMatch = LifeCostPattern.Match(clause.RawText);
        if (lifeMatch.Success)
        {
          var rawAmount = lifeMatch.Groups["amount"].Value;
          var amount = ParseNumberWord(rawAmount);
          if (amount is null)
          {
            return null;
          }

          wardCost = new PayLifeCost { Amount = LiteralQuantity.Of(amount.Value) };

          var rest = lifeMatch.Groups["rest"].Value.Trim();
          if (rest.StartsWith('(') && rest.EndsWith(')'))
          {
            reminder = new Parenthetical { Text = rest };
          }
        }
        else
        {
          // Try sacrifice cost form: "Ward—Sacrifice a [filter]."
          var sacMatch = SacrificeCostPattern.Match(clause.RawText);
          if (sacMatch.Success)
          {
            var sacCostText = sacMatch.Groups["saccost"].Value.Trim();
            var (quantity, filter) = ActivatedRuleHelpers.ParseSacrificePattern(sacCostText);
            if (filter is null)
            {
              return null;
            }

            wardCost = new SacrificeCost { Filter = filter, Quantity = quantity };

            var rest = sacMatch.Groups["rest"].Value.Trim();
            if (rest.StartsWith('(') && rest.EndsWith(')'))
            {
              reminder = new Parenthetical { Text = rest };
            }
          }
          else
          {
            // Try discard cost form: "Ward—Discard a card." (non-mana cost, CR 702.21a)
            var discMatch = DiscardCostPattern.Match(clause.RawText);
            if (!discMatch.Success)
            {
              return null;
            }

            var discCostText = discMatch.Groups["disccost"].Value.Trim();
            var (discQuantity, discFilter) = ActivatedRuleHelpers.ParseDiscardPattern(discCostText);

            wardCost = new DiscardCost { Filter = discFilter, Quantity = discQuantity };

            var rest = discMatch.Groups["rest"].Value.Trim();
            if (rest.StartsWith('(') && rest.EndsWith(')'))
            {
              reminder = new Parenthetical { Text = rest };
            }
          }
        }
      }
    }

    var trigger = new MagicAST.AST.Triggers.TriggerCondition
    {
      Timing = MagicAST.AST.Triggers.TriggerTiming.Whenever,
      Event = MagicAST.AST.Triggers.TriggerEvent.BecomesTarget,
      Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
    };

    var counterSpell = new MagicAST.AST.Effects.Core.PreventableEffect
    {
      Inner = new MagicAST.AST.Effects.Control.CounterSpellEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      },
      Unless = new MagicAST.AST.Effects.UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        Cost = wardCost,
      },
    };

    return
    [
      new MagicAST.AST.Abilities.TriggeredAbility
      {
        KeywordSource = KeywordAbility.Ward,
        Trigger = trigger,
        Effects = [counterSpell],
        Reminder = reminder,
      },
    ];
  }

  private static int? ParseNumberWord(string text)
  {
    if (int.TryParse(text, out var n))
    {
      return n;
    }

    return text.ToLowerInvariant() switch
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
      _ => null,
    };
  }
}
