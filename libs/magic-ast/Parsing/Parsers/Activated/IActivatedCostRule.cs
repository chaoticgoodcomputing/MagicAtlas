namespace MagicAST.Parsing.Parsers.Activated;

using MagicAST.AST.Costs;

/// <summary>
/// One activated-ability cost-component recognizer. Dispatched by
/// <see cref="ActivatedAbilityParser"/> in descending <c>Priority</c> order over
/// each comma-separated cost component; the first non-null result wins. Each rule
/// self-guards and returns <c>null</c> when it doesn't recognise the component.
/// </summary>
public interface IActivatedCostRule
{
  Cost? TryMatch(string costText);
}
