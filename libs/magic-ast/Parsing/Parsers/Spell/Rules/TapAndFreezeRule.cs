namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Two-sentence "tap then freeze" spell pattern. Handles both single-target and
/// bounded-multi-target variants:
/// <list type="bullet">
///   <item>
///     "Tap target creature. It doesn't untap during its controller's next untap step."
///     (Ojutai's Breath, DTK) — single creature; back-reference via "It".
///   </item>
///   <item>
///     "Tap up to two target creatures. Those creatures don't untap during their
///     controller's next untap step." (Frost Breath, M13) — up-to-N creatures;
///     back-reference via "Those creatures".
///   </item>
/// </list>
/// Implements both <see cref="ISpellRule"/> (for the freeze sentence when the
/// sentence-bundle dispatcher has already consumed the tap sentence) and
/// <see cref="IMultiSpellRule"/> (for the full two-sentence text as a dedicated
/// pattern). The <see cref="ISpellRule.TryMatch"/> path returns <c>false</c>;
/// all matching goes through <see cref="TryMatch"/> for the freeze fragment or
/// <see cref="TryMatchMulti"/> for the combined text.
/// </summary>
[SpellRule]
public sealed class TapAndFreezeRule : ISpellRule, IMultiSpellRule
{
  // Sentence 1 — tap (single target)
  private static readonly Regex TapSinglePattern = new(
    @"^Tap\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Sentence 1 — tap (up-to-N targets)
  private static readonly Regex TapUpToNPattern = new(
    @"^Tap\s+up\s+to\s+(?<n>\w+)\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Sentence 2 — freeze (singular back-reference: "It doesn't untap…")
  private static readonly Regex FreezeSinglePattern = new(
    @"^It\s+doesn'?t\s+untap\s+during\s+(?<whose>its\s+controller'?s)\s+next\s+untap\s+step$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Sentence 2 — freeze (plural back-reference: "Those creatures don't untap…")
  private static readonly Regex FreezePluralPattern = new(
    @"^Those\s+creatures\s+don'?t\s+untap\s+during\s+(?<whose>their\s+controller'?s)\s+next\s+untap\s+step$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Full two-sentence single-target pattern.
  private static readonly Regex FullSinglePattern = new(
    @"^Tap\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)\.\s+It\s+doesn'?t\s+untap\s+during\s+(?<whose>its\s+controller'?s)\s+next\s+untap\s+step$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Full two-sentence up-to-N-target pattern.
  private static readonly Regex FullUpToNPattern = new(
    @"^Tap\s+up\s+to\s+(?<n>\w+)\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)\.\s+Those\s+creatures\s+don'?t\s+untap\s+during\s+(?<whose>their\s+controller'?s)\s+next\s+untap\s+step$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // -------------------------------------------------------------------------
  // ISpellRule — matches only the freeze fragment (sentence 2).
  // -------------------------------------------------------------------------

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    // Singular: "It doesn't untap during its controller's next untap step"
    var singleM = FreezeSinglePattern.Match(trimmed);
    if (singleM.Success)
    {
      effect = BuildFreezeEffect(ObjectReference.It(), "its controller's next");
      return true;
    }

    // Plural: "Those creatures don't untap during their controller's next untap step"
    var pluralM = FreezePluralPattern.Match(trimmed);
    if (pluralM.Success)
    {
      effect = BuildFreezeEffect(ObjectReference.It(), "their controller's next");
      return true;
    }

    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — matches the full two-sentence text.
  // -------------------------------------------------------------------------

  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = text.Trim();

    // Single-target full match.
    var singleM = FullSinglePattern.Match(trimmed);
    if (singleM.Success)
    {
      var types = ParseTypes(singleM.Groups["types"].Value);
      if (types.Count == 0)
      {
        return false;
      }
      effects = BuildTapFreezeEffects(
        tapTarget: new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = types },
        },
        whoseUntapStep: "its controller's next",
        freezeTarget: ObjectReference.It()
      );
      return true;
    }

    // Up-to-N-target full match.
    var multiM = FullUpToNPattern.Match(trimmed);
    if (multiM.Success)
    {
      if (!SpellRuleHelpers.TryParseSmallWord(multiM.Groups["n"].Value, out var maximum))
      {
        return false;
      }
      var types = ParseTypes(multiM.Groups["types"].Value);
      if (types.Count == 0)
      {
        return false;
      }
      effects = BuildTapFreezeEffects(
        tapTarget: new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = types },
          Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
        },
        whoseUntapStep: "their controller's next",
        freezeTarget: ObjectReference.It()
      );
      return true;
    }

    return false;
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  private static IReadOnlyList<Effect> BuildTapFreezeEffects(
    ObjectReference tapTarget,
    string whoseUntapStep,
    ObjectReference freezeTarget
  ) =>
    new List<Effect>
    {
      new TapEffect { Target = tapTarget },
      BuildFreezeEffect(freezeTarget, whoseUntapStep),
    };

  private static DoesntUntapEffect BuildFreezeEffect(
    ObjectReference target,
    string whoseUntapStep
  ) =>
    new DoesntUntapEffect
    {
      Target = target,
      WhoseUntapStep = whoseUntapStep,
    };

  private static List<string> ParseTypes(string typesPhrase) =>
    Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();
}
