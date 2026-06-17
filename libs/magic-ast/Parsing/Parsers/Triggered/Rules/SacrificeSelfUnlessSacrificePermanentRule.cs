namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice [this creature / this permanent / CardName] unless you sacrifice a [type]"
/// — the upkeep-tax pattern where the cost to prevent self-sacrifice is sacrificing
/// another permanent of a given type (Rule 701.21a — Sacrifice; Rule 117.7 — unless clause).
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "At the beginning of your upkeep"
///   effect  = "sacrifice [this creature|The Gitrog Monster] unless you sacrifice a land"
/// </para>
///
/// <para>
/// The self-reference ("this creature", "this permanent", or a named card like "The Gitrog
/// Monster") is the source object itself (CR 201.4 / CR 109.2) — MAST models this as
/// <see cref="ObjectReferenceKind.Self"/>. The preventive cost is a
/// <see cref="SacrificeCost"/> whose Filter restricts to the stated permanent type.
/// </para>
///
/// <para>
/// Representative cards: The Gitrog Monster (SOI), Yavimaya Ants (ALL).
/// Rule citations: 701.21 (Sacrifice), 117.7 (unless clause), 201.4 (self-name reference).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeSelfUnlessSacrificePermanentRule : ITriggeredRule
{
  // Matches:
  //   "sacrifice this creature unless you sacrifice a land"
  //   "sacrifice this permanent unless you sacrifice a land"
  //   "sacrifice The Gitrog Monster unless you sacrifice a land"
  //   "sacrifice <any name words> unless you sacrifice a <type>"
  // The named-card form ("sacrifice The Gitrog Monster") is a self-reference (CR 201.4).
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+(?:this\s+(?:creature|permanent)|(?:[A-Z][A-Za-z',\-]*(?:\s+(?:[A-Za-z][A-Za-z',\-]*|the|of|a|an|in|on|at|for|to|with|by|and))*))\s+unless\s+you\s+sacrifice\s+a\s+(?<type>\w+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var typeName = m.Groups["type"].Value.ToLowerInvariant();

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      new SacrificeEffect { Target = ObjectReference.Self() },
      new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = new SacrificeCost
        {
          Filter = new ObjectFilter { CardTypes = [typeName] },
          Quantity = LiteralQuantity.Of(1),
        },
      });
    return true;
  }
}
