namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Parser for modal abilities ("Choose one —", "Choose two —", "Choose one or both —").
/// The clause splitter attaches each option as a child <see cref="OracleClause"/>
/// on <see cref="OracleClause.ModalOptions"/>; this parser interprets the header
/// text to derive a <see cref="ModeSelection"/> and dispatches each option clause
/// back through the classifier+registry to parse the option body.
///
/// <para>
/// Also handles the commander-conditional mode-expansion pattern:
/// "Choose one. If you control a commander as you cast this spell, you may
/// choose both instead." — a <see cref="ModeSelection"/> with base <c>ChooseOne</c>
/// and a <see cref="ModeSelectionOverride"/> whose condition is a
/// <see cref="CountCondition"/> filtering Commander-supertype creatures you control.
/// CR 903 (Commander format), MAST describes the condition as written.
/// </para>
/// </summary>
[OracleAbilityParser(AbilityKind.Modal)]
public sealed class ModalAbilityParser : IAbilityParser
{
  private readonly AbilityClassifier _classifier = new();
  private readonly FallbackParser _fallback = new();

  // Lazy so we don't recurse through registry construction at type-load time.
  private static readonly Lazy<AbilityParserRegistry> _registry = new(() => new AbilityParserRegistry());

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var selection = TryParseModeSelection(clause.RawText);
    if (selection is null)
    {
      return
      [
        _fallback.Parse(
          clause,
          classification,
          "Unrecognized modal selection phrase",
          lastAttemptedRule: "ModalAbilityParser.Parse",
          failurePosition: clause.SourceSpan.Start
        ),
      ];
    }

    var optionClauses = clause.ModalOptions;
    if (optionClauses is null || optionClauses.Count == 0)
    {
      // Header without attached options — degrade to unparsed so the gap is visible.
      return
      [
        _fallback.Parse(
          clause,
          classification,
          "Modal header has no attached options",
          lastAttemptedRule: "ModalAbilityParser.Parse",
          failurePosition: clause.SourceSpan.Start
        ),
      ];
    }

    var modes = new List<ModalOption>(optionClauses.Count);
    foreach (var optionClause in optionClauses)
    {
      var optionClassification = _classifier.Classify(optionClause);
      var optionAbilities = _registry
        .Value.GetParser(optionClassification.Kind)
        .Parse(optionClause, optionClassification);

      // A modal option is one effect. If the option parser produced multiple
      // siblings (e.g. comma-separated keywords), wrap the first; the rest
      // would be lost. That's a follow-up gap, not a regression — surface it
      // by attaching all of them as separate modes for now.
      foreach (var ability in optionAbilities)
      {
        modes.Add(new ModalOption { Ability = ability });
      }
    }

    // "Choose one or more" means 1..N, where N is the number of available modes.
    // TryParseModeSelection can't know N from the header alone (it falls through to
    // the "choose one" prefix), so resolve the upper bound here now that modes are known.
    if (clause.RawText.TrimStart().StartsWith("choose one or more", StringComparison.OrdinalIgnoreCase))
    {
      selection = ModeSelection.ChooseOneOrMore(modes.Count);
    }

    return [
      new ModalAbility
      {
        ModeSelection = selection,
        Modes = modes,
      },
    ];
  }

  // "Choose one. If you control a commander as you cast this spell, you may choose both instead."
  // Captures the base "choose one" and the conditional "choose both" override.
  private static readonly Regex _commanderChooseBothOverride = new(
    @"^choose\s+one\s*\.\s+if\s+you\s+control\s+a\s+commander\s+as\s+you\s+cast\s+this\s+spell,\s+you\s+may\s+choose\s+both\s+instead",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Maps the modal header text to a <see cref="ModeSelection"/>.
  /// </summary>
  private static ModeSelection? TryParseModeSelection(string headerText)
  {
    var lower = headerText.ToLowerInvariant().Trim();

    // "Choose one. If you control a commander as you cast this spell, you may
    // choose both instead." — base ChooseOne with a conditional override that
    // upgrades to ChooseOneOrBoth when you control a Commander-supertype creature.
    // CR 903 (Commander format); the condition is a CountCondition over Commander
    // creatures you control.
    if (_commanderChooseBothOverride.IsMatch(lower))
    {
      return ModeSelection.ChooseOne() with
      {
        ConditionalOverride = new ModeSelectionOverride
        {
          Condition = new CountCondition
          {
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Supertypes = ["Commander"],
              Controller = ControllerFilter.You,
            },
            Count = new Comparison
            {
              Operator = ComparisonOperator.GreaterThanOrEqual,
              Value = 1,
            },
          },
          Selection = ModeSelection.ChooseOneOrBoth(),
        },
      };
    }

    if (lower.StartsWith("choose one or both"))
    {
      return ModeSelection.ChooseOneOrBoth();
    }
    if (lower.StartsWith("choose one"))
    {
      return ModeSelection.ChooseOne();
    }
    if (lower.StartsWith("choose two"))
    {
      return ModeSelection.ChooseTwo();
    }
    if (lower.StartsWith("choose three"))
    {
      return ModeSelection.ChooseExactly(3);
    }
    if (lower.StartsWith("choose any number"))
    {
      // "Choose any number" — minimum 0, no fixed max. Model as ChooseUpTo(int.MaxValue).
      return ModeSelection.ChooseUpTo(int.MaxValue);
    }
    if (lower.StartsWith("choose up to"))
    {
      // "Choose up to N —"; pull the number after "up to".
      var afterUpTo = lower["choose up to".Length..].TrimStart();
      var numToken = afterUpTo.Split(new[] { ' ', '—', '-' }, StringSplitOptions.RemoveEmptyEntries);
      if (numToken.Length > 0 && TryParseWordNumber(numToken[0], out var n))
      {
        return ModeSelection.ChooseUpTo(n);
      }
      return null;
    }

    return null;
  }

  private static bool TryParseWordNumber(string token, out int value)
  {
    if (int.TryParse(token, out value))
    {
      return true;
    }
    switch (token)
    {
      case "one":
        value = 1;
        return true;
      case "two":
        value = 2;
        return true;
      case "three":
        value = 3;
        return true;
      case "four":
        value = 4;
        return true;
      case "five":
        value = 5;
        return true;
      default:
        value = 0;
        return false;
    }
  }
}
