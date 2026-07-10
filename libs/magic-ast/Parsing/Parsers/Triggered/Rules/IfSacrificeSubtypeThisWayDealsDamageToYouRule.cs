namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "If you sacrifice a[n] [Subtype] this way, this creature deals [N] damage to
/// you." — a within-resolution back-reference to the object sacrificed by a
/// PRECEDING effect in the same ability (Serendib Djinn's "At the beginning of your
/// upkeep, sacrifice a land. If you sacrifice an Island this way, this creature
/// deals 3 damage to you.").
///
/// <para>
/// Mirrors <see cref="IfPutSubtypeOntoBattlefieldThisWayGainLifeRule"/>: the "this
/// way" causation-gate plus a subtype filter on the object affected by the earlier
/// effect is a compound shape without a dedicated structured arm yet, so the
/// condition is deferred honestly via <see cref="OtherCondition"/> (ADR 0001
/// residual — a typed, counted deferral, not a destination). CR 603.12 governs the
/// general "…this way" reflexive-condition idiom (a resolving effect's own
/// follow-up checking how an earlier part of the SAME resolution played out).
/// </para>
///
/// <para>
/// CR 120.1 (verbatim): "Objects can deal damage to battles, creatures,
/// planeswalkers, and players. … An object that deals damage is the source of that
/// damage." The damage source is "this creature" — the ability's own source object
/// (CR 109) — modelled as <see cref="ObjectReferenceKind.Self"/>. CR 119.3: "If an
/// effect causes a player to gain life or lose life, that player's life total is
/// adjusted accordingly" (the "to you" recipient — the ability's controller).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): fully anchored to prevent false-positive substring
/// matches against any broader sibling sentence.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class IfSacrificeSubtypeThisWayDealsDamageToYouRule : ITriggeredRule
{
  // Groups: article (a/an), subtype (e.g. "Island"), amount (digit or word).
  private static readonly Regex _pattern = new(
    @"^If\s+you\s+sacrifice\s+(?<article>a|an)\s+(?<subtype>[A-Z][a-zA-Z]+)\s+this\s+way,\s+"
    + @"this\s+creature\s+deals\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+you\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, int> _wordNumbers =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var article = m.Groups["article"].Value.ToLowerInvariant();
    var subtype = m.Groups["subtype"].Value.Trim();
    var amountStr = m.Groups["amount"].Value.Trim();

    int amount;
    if (!int.TryParse(amountStr, out amount) && !_wordNumbers.TryGetValue(amountStr, out amount))
    {
      return false;
    }

    // The condition: "you sacrifice a[n] [Subtype] this way" is a causation-gate +
    // subtype filter on the object affected by the preceding effect. No dedicated
    // structured arm exists for this compound shape, so we defer honestly via
    // OtherCondition (ADR 0001 residual), matching the Spelunking precedent.
    var condition = new OtherCondition
    {
      Text = $"you sacrifice {article} {subtype} this way",
    };

    var dealDamage = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = ObjectReference.You(),
    };

    effect = new ConditionalEffect
    {
      Condition = condition,
      Then = dealDamage,
    };
    return true;
  }
}
