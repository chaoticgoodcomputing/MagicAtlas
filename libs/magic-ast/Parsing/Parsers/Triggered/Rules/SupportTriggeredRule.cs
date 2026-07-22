namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "support N" — Rule 701.41 keyword action on the triggered side.
///
/// <para>
/// Rule 701.41a: "\"Support N\" on a permanent means \"Put a +1/+1 counter on
/// each of up to N other target creatures.\"" This rule covers the permanent
/// form (the "other" variant); a permanent's "support N" never targets itself.
/// </para>
///
/// <para>
/// The keyword action is unpacked into its literal meaning per the rule (MAST
/// describes what the keyword says, not how it resolves): a single +1/+1 counter
/// placed on each of an up-to-N choice of OTHER target creatures. The "up to N"
/// is the multiplicity of the target reference (Rule 115.1 — "up to N target"),
/// not the per-target counter count, which is one. Hence <c>Count</c> is the
/// literal 1 and the "up to N" lives on the reference's <see cref="ObjectReference.Quantity"/>.
/// </para>
///
/// <para>
/// The reminder text "(Put a +1/+1 counter on each of up to two other target
/// creatures.)" has already been stripped by the dispatcher before this rule is
/// called, so the matched fragment is the bare "support N".
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SupportTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();

    var match = Regex.Match(
      trimmed,
      @"^support\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return false;
    }

    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(match.Groups["count"].Value);
    if (count is null)
    {
      return false;
    }

    effect = new PutCountersEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = new UpToQuantity { Maximum = count.Value },
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          // "other target creatures" — support excludes the source object itself
          // (CR 702.100a / 109.5): the first-class ExcludeSelf axis, not a residual.
          ExcludeSelf = true,
        },
      },
      CounterType = "+1/+1",
      Count = LiteralQuantity.Of(1),
    };
    return true;
  }
}
