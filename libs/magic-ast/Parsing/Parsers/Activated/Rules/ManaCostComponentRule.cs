namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;
using MagicAST.Parsing;

/// <summary>
/// Mana / tap / untap cost component: "{1}", "{2}{G}", "{T}", "{Q}". Returns a
/// <see cref="TapCost"/> for {T}, an <see cref="UntapCost"/> for {Q}, otherwise a
/// <see cref="ManaCost"/>. Only fires for components containing a mana brace.
/// </summary>
[ActivatedCostRule(Priority = 1000)]
public sealed class ManaCostComponentRule : IActivatedCostRule
{
  private static readonly ManaCostParser _manaCostParser = new();

  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    if (!costText.Contains('{'))
    {
      return null;
    }

    // Check for tap symbol
    if (costText == "{T}")
    {
      return new TapCost();
    }

    // Check for untap symbol
    if (costText == "{Q}")
    {
      return new UntapCost();
    }

    // Try to parse as mana cost using ManaCostParser
    try
    {
      var parsed = _manaCostParser.Parse(costText);
      if (parsed.Symbols.Count > 0)
      {
        return new ManaCost { Symbols = parsed.Symbols };
      }
    }
    catch
    {
      // Parsing failed, return null
    }

    return null;
  }
}
