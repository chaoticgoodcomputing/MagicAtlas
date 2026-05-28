namespace MagicAST.Parsing.Parsers.Activated;

using MagicAST.AST.Effects;

/// <summary>
/// One activated-ability effect recognizer. Dispatched by
/// <see cref="ActivatedAbilityParser"/> in descending <c>Priority</c> order, then
/// ordinal name for tie-breaking; the first non-null result wins.
/// <paramref name="effectText"/> is the post-colon effect fragment (a single
/// sentence when reached via the multi-sentence pre-pass). Each rule self-guards
/// and returns <c>null</c> when it doesn't recognise the text.
/// </summary>
public interface IActivatedEffectRule
{
  Effect? TryMatch(string effectText);
}
