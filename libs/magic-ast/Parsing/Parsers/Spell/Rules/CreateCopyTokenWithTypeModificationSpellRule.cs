namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "Create a token that's a copy of target [type1] or [type2] you control, except it's a
/// [addedType] in addition to its other types." — Molten Duplication (DFT).
///
/// <para>
/// Maps a copy-a-token spell with a type-disjunction target and a <c>typeAdder</c>
/// modification to a <see cref="CopyEffect"/> whose
/// <see cref="CopyEffect.Target"/> carries the disjunction as a multi-element
/// <see cref="ObjectFilter.CardTypes"/> list and whose
/// <see cref="CopyEffect.Modifications"/> carry a single
/// <see cref="TypeAdder"/> recording the added card type.
/// </para>
///
/// <para>
/// CR 707.2: "When copying an object, the copy acquires the copiable values of the
/// original object's characteristics…" — the "except it's [X] in addition to its other
/// types" clause overrides the copiable type values by adding one card type without
/// removing existing ones. The <see cref="TypeAdder"/> modification encodes this
/// additive type change structurally.
/// </para>
///
/// <para>
/// Priority 70: sits above the generic <see cref="CreateCopyTokenRule"/> (priority 65)
/// so the more-specific type-modification form is claimed first.
/// </para>
///
/// Rule citations: CR 707.2 (copy — copiable values), CR 111.1 (token definition).
/// </summary>
[SpellRule(Priority = 70)]
public sealed class CreateCopyTokenWithTypeModificationSpellRule : ISpellRule
{
  // Matches:
  //   "Create a token that's a copy of target [type1] or [type2] you control,
  //    except it's a[n] [addedType] in addition to its other types"
  // The controller clause ("you control") is required for this shape (Molten Duplication).
  // The "an" variant before vowel-starting types is handled by optional "n".
  private static readonly Regex _pattern = new(
    @"^Create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+target\s+" +
    @"(?<types>[a-z]+(?:\s+or\s+[a-z]+)+)\s+you\s+control," +
    @"\s+except\s+it's\s+an?\s+(?<addedType>[a-z]+)\s+in\s+addition\s+to\s+its\s+other\s+types$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    // Parse the type disjunction: "artifact or creature" → ["artifact", "creature"]
    var typesPhrase = m.Groups["types"].Value;
    var cardTypes = SpellRuleHelpers.SplitTypeDisjunction(typesPhrase);
    if (cardTypes.Count < 2)
    {
      return false;
    }

    var addedType = m.Groups["addedType"].Value.ToLowerInvariant();

    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Controller = ControllerFilter.You,
        },
      },
      Modifications =
      [
        new TypeAdder { CardTypes = [addedType] },
      ],
    };
    return true;
  }
}
