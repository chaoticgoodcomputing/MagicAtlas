namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may put a permanent card with equal or lesser mana value from your hand onto the battlefield"
///
/// <para>
/// Handles the Kodama of the East Tree pattern: an optional zone-change from hand to
/// battlefield for a permanent card whose mana value is less than or equal to the mana
/// value of a referenced object (typically the permanent that triggered the enclosing
/// triggered ability — represented as <see cref="ObjectReferenceKind.It"/>).
/// </para>
///
/// <para>
/// The "equal or lesser mana value" phrase is a relative comparison (CR 202.3 — mana
/// value is the sum of the mana symbols in a card's mana cost) against the triggering
/// permanent's mana value, encoded as a <see cref="Comparison"/> with
/// <see cref="Comparison.RelativeTo"/> set to <see cref="ObjectReferenceKind.It"/> and
/// <see cref="Comparison.RelativeCharacteristic"/> set to
/// <see cref="RelativeCharacteristic.ManaValue"/>.
/// </para>
///
/// <para>Rule citations:</para>
/// <list type="bullet">
///   <item>CR 202.3 — mana value (formerly converted mana cost) is the sum of mana symbols.</item>
///   <item>CR 400.7 — zone change creates a new object.</item>
///   <item>CR 603.1 — triggered ability trigger + effect.</item>
/// </list>
///
/// <para>
/// Priority 64 — above <see cref="PutFromHandOntoBattlefieldTriggeredRule"/> (priority 63)
/// so this more-specific mana-value-comparison form is matched first. Fully anchored (^...$)
/// to prevent substring matches against sibling effects.
/// </para>
/// </summary>
[TriggeredRule(Priority = 64)]
public sealed class PutPermanentCardByManaValueFromHandRule : ITriggeredRule
{
  // Anchored pattern:
  //   [you may ]put a permanent card with [equal or lesser|equal or less] mana value from your hand onto the battlefield
  // Captures:
  //   optional: presence of "you may " prefix
  //   comparison: the comparison phrase (e.g. "equal or lesser", "equal or less", "lesser or equal")
  private static readonly Regex _pattern = new(
    @"^(?<optional>you\s+may\s+)?put\s+a\s+permanent\s+card\s+with\s+(?<comparison>equal\s+or\s+less(?:er)?|less(?:er)?\s+or\s+equal)\s+mana\s+value\s+from\s+your\s+hand\s+onto\s+the\s+battlefield\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var isOptional = m.Groups["optional"].Success;

    // "equal or lesser" / "equal or less" / "lesser or equal" → LessThanOrEqual
    // The triggering permanent is referenced as "It" — the permanent that just entered
    // and caused the enclosing triggered ability to fire (CR 603.2 / reference-not-resolution).
    var filter = new ObjectFilter
    {
      CardTypes = ["permanent"],
      Zone = Zone.Hand,
      Controller = ControllerFilter.You,
      ManaValueComparison = new Comparison
      {
        Operator = ComparisonOperator.LessThanOrEqual,
        RelativeTo = new ObjectReference { Kind = ObjectReferenceKind.It },
        RelativeCharacteristic = RelativeCharacteristic.ManaValue,
      },
    };

    var inner = new PutFromHandOntoBattlefieldEffect
    {
      Filter = filter,
      Tapped = false,
    };

    effect = isOptional ? new OptionalEffect { Inner = inner } : inner;
    return true;
  }
}
