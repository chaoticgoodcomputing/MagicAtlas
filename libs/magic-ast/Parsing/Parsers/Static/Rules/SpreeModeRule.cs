namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.Parsing;

/// <summary>
/// "+ [cost] — [effect]" — one selectable mode of a <b>Spree</b> spell (CR 702.172a:
/// "Spree is a static ability found on some modal spells (see rule 700.2) … 'Spree'
/// means 'Choose one or more additional costs.'"). Each printed mode line pairs an
/// additional cost ("+ {1}") with an effect ("Destroy target artifact."); the caster
/// chooses one or more modes and pays each chosen mode's cost as an additional cost to
/// cast the spell.
///
/// <para>
/// The clause splitter emits the leading "Spree (…)" keyword line as its own
/// <see cref="StaticAbility"/> (recording the "choose one or more" selection) and each
/// "+ [cost] — [effect]" paragraph as a separate clause that classifies as static and
/// reaches this rule. This rule peels the "+ [cost] — " prefix, parses the cost with
/// <see cref="ManaCostParser"/>, and recovers the mode's effect by re-parsing the
/// label-stripped body through the full pipeline (reusing the existing effect
/// recognisers — the destroy and put-counter rules), emitting a <see cref="SpreeModeAbility"/>.
/// </para>
///
/// <para>
/// Anchored to a leading "+" plus one-or-more brace-wrapped cost symbols and an em-dash
/// (U+2014): only the Spree mode lines surface in this shape, so there is no sibling to
/// collide with. Declines (returns null) when the body fails to parse to a spell effect,
/// leaving the gap visible rather than emitting a malformed mode.
/// </para>
/// </summary>
[StaticRule(Priority = 1100)]
public sealed class SpreeModeRule : IStaticRule
{
  // Lazy so the OracleParser (and its parser registry, which discovers THIS rule) is not
  // constructed at type-load — mirrors NamedModeGatedAbilityRule / ModalAbilityParser's lazy
  // sub-parser. The re-parsed body never carries a "+ [cost] —" prefix, so no parse recursion.
  private static readonly Lazy<OracleParser> _bodyParser = new(() => new OracleParser());
  private static readonly ManaCostParser _manaCostParser = new();

  // "+ {1} — Destroy target artifact." : one-or-more {…} cost symbols, an em-dash, the body.
  private static readonly Regex _pattern = new(
    "^\\+\\s*(?<cost>(?:\\{[^}]+\\})+)\\s*\\u2014\\s*(?<body>.+)$",
    RegexOptions.Compiled | RegexOptions.Singleline
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var parsedCost = _manaCostParser.Parse(m.Groups["cost"].Value);
    if (parsedCost.Symbols.Count == 0)
    {
      return null;
    }

    var body = m.Groups["body"].Value.Trim();
    var reparsed = _bodyParser.Value.Parse(body);

    // The mode body is a spell effect ("Destroy target artifact.", "Put a +1/+1 counter …").
    // Require a clean spell parse; decline if the body did not resolve so the gap stays visible.
    var spell = reparsed.Output.Abilities.OfType<SpellAbility>().FirstOrDefault();
    if (spell is null || reparsed.Output.Abilities.Any(a => a is IUnparsed))
    {
      return null;
    }

    return
    [
      new SpreeModeAbility
      {
        AdditionalCost = new ManaCost { Symbols = parsedCost.Symbols },
        Effects = spell.Effects,
      },
    ];
  }
}
