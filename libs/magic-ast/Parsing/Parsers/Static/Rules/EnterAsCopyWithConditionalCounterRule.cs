namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You may have this creature enter as a copy of a creature or planeswalker you
/// control, except [comma-separated except-clauses]." — the Spark Double template:
/// a clone-on-enter replacement (CR 707.2/614.12, sibling of
/// <see cref="EnterAsCopyRule"/>'s Clone/Glasspool Mimic shape) whose "except" rider
/// is a LIST of clauses rather than the single "in addition to its other types" form
/// <see cref="EnterAsCopyRule"/> already covers. Declines (and lets
/// <see cref="EnterAsCopyRule"/> — which itself also declines here, since its
/// copy-target sub-pattern only recognises the single-type "a/any [type]" form —
/// or any other rule try) whenever any except-clause doesn't match a recognised
/// shape, rather than emitting a lossy parse.
///
/// <para>
/// Decomposition (mirrors <see cref="EnterAsCopyRule"/>): the "as it enters" timing
/// lives on <see cref="StaticAbility.When"/> = <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 603.6d/614.1c); the "may" optionality is an <see cref="OptionalEffect"/> wrapper
/// (CR 117.7); the copy relationship is <see cref="BecomesCopyEffect"/> (Subject: Self
/// becomes a copy of the chosen object, CR 707.2 — modifies the existing permanent in
/// place, no new object is created).
/// </para>
///
/// <para>
/// Copy target — "a creature or planeswalker you control" — is an indefinite
/// controller choice (<see cref="ObjectReferenceKind.Any"/>, CR 115.1) whose card-type
/// disjunction is recorded as a multi-element <see cref="ObjectFilter.CardTypes"/>
/// list (OR semantics — established pattern, see
/// <c>TargetOpponentExilesGreatestManaValuePermanentRule</c>).
/// </para>
///
/// <para>
/// Each except-clause is classified independently:
/// <list type="bullet">
///   <item>"it enters with an additional [count] [counter type] counter(s) on it if
///   it's a [card type]" → a <see cref="ConditionalModification"/> gating a
///   <see cref="CounterAdder"/> on an <see cref="ObjectHasCardTypeCondition"/>
///   (Subject: Self — the entering permanent's own resulting type, CR 707.2/614.12);</item>
///   <item>"it isn't legendary" → <see cref="SupertypeRemover"/> (CR 704.5j legend
///   rule; the negation analogue of <see cref="TypeAdder"/>).</item>
/// </list>
/// Spark Double's three clauses become two <see cref="ConditionalModification"/>s
/// (creature → +1/+1 counter, planeswalker → loyalty counter) plus one
/// <see cref="SupertypeRemover"/>.
/// </para>
/// </summary>
[StaticRule(Priority = 965)]
public sealed class EnterAsCopyWithConditionalCounterRule : IStaticRule
{
  // "You may have this <noun> enter as a copy of <copyTarget>, except <exceptions>."
  // Anchored ^…$; the trailing period is optional. <copyTarget> is captured
  // non-greedily and resolved by ParseCopyTarget; an unrecognised copyTarget or
  // any unrecognised sub-clause of <exceptions> declines the whole rule.
  private static readonly Regex Pattern = new(
    @"^You\s+may\s+have\s+this\s+(?:permanent|creature|artifact|land|enchantment|planeswalker)"
    + @"\s+enter\s+as\s+a\s+copy\s+of\s+(?<copyTarget>.+?)"
    + @",\s*except\s+(?<exceptions>.+?)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "a creature or planeswalker you control" (disjunction of card types).
  private static readonly Regex CopyTargetPattern = new(
    @"^a\s+(?<types>[a-z]+(?:\s+or\s+[a-z]+)*)\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "it enters with an additional <count> <counterType> counter(s) on it if it's a[n]
  // <cardType>".
  private static readonly Regex ConditionalCounterClause = new(
    @"^it\s+enters\s+with\s+an?\s+additional\s+(?<counterType>[a-z0-9+/\-]+)\s+counters?\s+on\s+it"
    + @"\s+if\s+it's\s+an?\s+(?<cardType>[a-z]+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "it isn't legendary".
  private static readonly Regex NotLegendaryClause = new(
    @"^it\s+isn't\s+legendary$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> CardTypeTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "planeswalker", "permanent", "land", "battle",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Pattern.Match(clause.RawText.Trim());
    if (!match.Success)
    {
      return null;
    }

    var copyFilter = ParseCopyTarget(match.Groups["copyTarget"].Value.Trim());
    if (copyFilter is null)
    {
      return null;
    }

    var modifications = ParseExceptions(match.Groups["exceptions"].Value.Trim());
    if (modifications is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects =
        [
          new OptionalEffect
          {
            Inner = new BecomesCopyEffect
            {
              Subject = ObjectReference.Self(),
              CopyTarget = new ObjectReference
              {
                Kind = ObjectReferenceKind.Any,
                Filter = copyFilter,
              },
              Modifications = modifications,
            },
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses "a [type1] or [type2] you control" into an <see cref="ObjectFilter"/>
  /// whose <see cref="ObjectFilter.CardTypes"/> carries every disjunct. Returns
  /// <c>null</c> for any unrecognised card-type token or shape.
  /// </summary>
  private static ObjectFilter? ParseCopyTarget(string phrase)
  {
    var match = CopyTargetPattern.Match(phrase);
    if (!match.Success)
    {
      return null;
    }

    var types = match.Groups["types"].Value
      .Split(" or ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(t => t.ToLowerInvariant())
      .ToList();

    if (types.Count == 0 || types.Any(t => !CardTypeTokens.Contains(t)))
    {
      return null;
    }

    return new ObjectFilter
    {
      CardTypes = types,
      Controller = ControllerFilter.You,
    };
  }

  /// <summary>
  /// Splits the except-clause tail on comma/"and" boundaries and classifies each
  /// sub-clause into a <see cref="CopyModification"/>. Returns <c>null</c> if any
  /// sub-clause doesn't match a recognised shape, so the rule declines rather than
  /// dropping an unrecognised modifier.
  /// </summary>
  private static IReadOnlyList<CopyModification>? ParseExceptions(string exceptions)
  {
    var subClauses = Regex.Split(exceptions, @",\s*(?:and\s+)?|\s+and\s+")
      .Select(s => s.Trim())
      .Where(s => s.Length > 0)
      .ToList();

    if (subClauses.Count == 0)
    {
      return null;
    }

    var modifications = new List<CopyModification>();
    foreach (var subClause in subClauses)
    {
      var counterMatch = ConditionalCounterClause.Match(subClause);
      if (counterMatch.Success)
      {
        var cardType = counterMatch.Groups["cardType"].Value.ToLowerInvariant();
        if (!CardTypeTokens.Contains(cardType))
        {
          return null;
        }

        modifications.Add(new ConditionalModification
        {
          Condition = new ObjectHasCardTypeCondition
          {
            CardType = cardType,
            Subject = "Self",
          },
          Modification = new CounterAdder
          {
            CounterType = counterMatch.Groups["counterType"].Value.ToLowerInvariant(),
            Count = LiteralQuantity.Of(1),
          },
        });
        continue;
      }

      if (NotLegendaryClause.IsMatch(subClause))
      {
        modifications.Add(new SupertypeRemover { Supertypes = ["Legendary"] });
        continue;
      }

      // Unrecognised sub-clause — decline rather than drop a modifier silently.
      return null;
    }

    return modifications;
  }
}
