namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Two-sentence "untap then pump" spell pattern:
///   "Untap up to two target creatures. They each get +2/+2 until end of turn."
///   (Join Forces) — up-to-N target creatures are untapped, and the same creatures
///   ("They each") receive a P/T bonus until end of turn via a plural back-reference.
///
/// <para>
/// Structural twin of <see cref="TapAndFreezeRule"/> (tap + freeze), swapping the
/// tap for an <see cref="UntapEffect"/> and the freeze for a <see cref="ModifyPTEffect"/>.
/// The "up to N target creatures" cardinality lives on
/// <see cref="ObjectReference.Quantity"/> (an <see cref="UpToQuantity"/>), the target
/// set of the untap; the pump sentence's "They each" back-reference is modeled with
/// <see cref="ObjectReferenceKind.It"/>, matching the established plural-back-reference
/// convention used by Frost Breath's "Those creatures" (see <see cref="TapAndFreezeRule"/>).
/// </para>
///
/// <para>
/// CR 701.26b (untap): "To untap a permanent, rotate it back to the upright position
/// from a sideways position." CR 115.1 ("target") + CR 107.3 ("up to N") make the
/// untap a 0–2 targeted choice. CR 611 (continuous effects) governs the P/T bonus
/// ending at end of turn (CR 514.2 cleanup discards "until end of turn" effects).
/// </para>
///
/// <para>
/// Implements <see cref="ISpellRule"/> (matching the pump sentence when the
/// sentence-bundle dispatcher has already consumed the untap sentence — the untap
/// fragment is matched by <see cref="UntapUpToNTargetsRule"/>) and
/// <see cref="IMultiSpellRule"/> (matching the full two-sentence text as one shape).
/// Every pattern is anchored (<c>^…$</c>) so it cannot match inside a longer clause.
/// </para>
/// </summary>
[SpellRule]
public sealed class UntapUpToNTargetsAndPumpRule : ISpellRule, IMultiSpellRule
{
  private const string TypesGroup = @"(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)";

  // Sentence 2 — pump (plural back-reference: "They each get +2/+2 until end of turn").
  private static readonly Regex PumpPattern = new(
    @"^They\s+each\s+get\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Full two-sentence "untap up to N target ... They each get +P/+T until end of turn".
  private static readonly Regex FullPattern = new(
    $@"^Untap\s+up\s+to\s+(?<n>\w+)\s+target\s+{TypesGroup}\.\s+They\s+each\s+get\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // -------------------------------------------------------------------------
  // ISpellRule — matches only the pump fragment (sentence 2).
  // -------------------------------------------------------------------------

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = PumpPattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = BuildPumpEffect(
      ObjectReference.It(),
      int.Parse(m.Groups["p"].Value),
      int.Parse(m.Groups["t"].Value)
    );
    return true;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — matches the full two-sentence text.
  // -------------------------------------------------------------------------

  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = FullPattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["n"].Value, out var maximum))
    {
      return false;
    }

    var types = ParseTypes(m.Groups["types"].Value);
    if (types.Count == 0)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new UntapEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = types },
          Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
        },
      },
      BuildPumpEffect(
        ObjectReference.It(),
        int.Parse(m.Groups["p"].Value),
        int.Parse(m.Groups["t"].Value)
      ),
    };
    return true;
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  private static ModifyPTEffect BuildPumpEffect(ObjectReference target, int power, int toughness) =>
    new()
    {
      Target = target,
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };

  private static List<string> ParseTypes(string typesPhrase) =>
    Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();
}
