namespace MagicAST.Parsing.Parsers.Triggered;

using MagicAST.AST.Effects;

/// <summary>
/// One triggered-effect recognition rule. Dispatched by
/// <see cref="TriggeredAbilityParser"/> in priority order (descending). The
/// <c>text</c> input is the post-trigger effect fragment, already split off by
/// the trigger/effect boundary detection in the dispatcher.
/// </summary>
public interface ITriggeredRule
{
  bool TryMatch(string text, out Effect? effect);
}
