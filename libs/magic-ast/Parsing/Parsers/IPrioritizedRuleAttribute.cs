namespace MagicAST.Parsing.Parsers;

/// <summary>
/// Contract for an attribute that marks a reflection-discovered rule and carries
/// its intra-tier dispatch priority. Shared by every registry-style parser family
/// (spell rules, triggered rules, keywords, …) so a single generic discovery
/// helper (<see cref="RuleRegistry"/>) can rank decorated types without knowing the
/// concrete attribute.
///
/// <para>
/// <b>Priority convention (range 0–100, default 50, higher = tried first):</b>
/// higher-priority rules are dispatched before lower-priority siblings. Each rule
/// family documents its own band convention on its concrete attribute; the shared
/// invariant is only that a larger number wins ties by sorting earlier.
/// </para>
/// </summary>
public interface IPrioritizedRuleAttribute
{
  /// <summary>
  /// Dispatch priority. Higher = tried first. Default <c>50</c> when omitted on the
  /// concrete attribute.
  /// </summary>
  int Priority { get; }
}
