namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] is also a [Subtype], [Subtype], ... and [Subtype]." — the self-by-name
/// creature-subtype-addition static template (e.g. Tajuru Paragon: "Tajuru Paragon is
/// also a Cleric, Rogue, Warrior, and Wizard.").
///
/// <para>
/// CR 205.1b (verbatim): "Some effects change an object's card type, supertype, or
/// subtype but specify that the object retains a prior card type, supertype, or
/// subtype. In such cases, all the object's prior card types, supertypes, and subtypes
/// are retained, and the effect causes the object to gain or lose other card types,
/// subtypes, and/or supertypes." CR 205.3: creature subtypes appear after a long dash on
/// the type line and are drawn from the creature-type list. "Is also a" is the additive
/// phrasing WotC uses for this grant — unlike "becomes a" or "is a" (which would replace
/// the subject's subtypes), "also" signals the listed subtypes are gained IN ADDITION TO
/// the subject's printed subtypes (no subtype is lost).
/// </para>
///
/// <para>
/// This is the self-by-name sibling of <see cref="NonlandCreatureTypeGrantRule"/> (which
/// handles the "in addition to their other types" phrasing over a group of permanents):
/// both emit <see cref="AddTypeEffect"/>, but this rule's subject is always the named
/// source card itself (CR 201.5 — a card's own name in its own text means that object),
/// so the target is <see cref="ObjectReferenceKind.Self"/> rather than an
/// <see cref="ObjectReferenceKind.Each"/> group reference, and only subtypes (never card
/// types) are granted by this surface phrase.
/// </para>
///
/// <para>
/// Anchored (^…$) to the full clause so this cannot match as a substring of a broader
/// sentence, and requires the literal "is also a[n]" connective — no sibling static rule
/// in this parser shares that exact connective, so this pattern is disjoint from every
/// other registered rule. Default priority (50): the anchoring makes the exact value
/// non-load-bearing.
/// </para>
/// </summary>
[StaticRule]
public sealed class SelfIsAlsoSubtypesRule : IStaticRule
{
  // "[CardName] is also a[n] [Subtype][, Subtype]*[,] and [Subtype]."
  // <name>: the self-by-name subject — one or more capitalised words (mirrors the
  // self-by-name convention used by SelfNameEntersTappedRule and CantBeBlockedRule).
  // <subtypes>: a comma/and-separated list of capitalised subtype words. Anchored at
  // both ends so this cannot steal a substring of a longer sentence.
  private static readonly Regex _pattern = new(
    @"^\s*(?<name>[A-Z][A-Za-z'\-]*(?:\s+[A-Za-z'\-]+)*)\s+is\s+also\s+an?\s+"
    + @"(?<subtypes>[A-Z][A-Za-z,\s]*[A-Za-z])\.?\s*$",
    RegexOptions.Compiled
  );

  // Collapses the trailing Oxford-comma-and ("Warrior, and Wizard" → "Warrior, Wizard")
  // and the bare-and two-item case ("Cleric and Wizard" → "Cleric, Wizard") before the
  // comma split below.
  private static readonly Regex _andNormalizer = new(@",?\s+and\s+", RegexOptions.Compiled);

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var subtypesRaw = m.Groups["subtypes"].Value.Trim();
    var subtypes = ParseSubtypeList(subtypesRaw);
    if (subtypes.Count == 0)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AddTypeEffect
          {
            Target = ObjectReference.Self(),
            AddedSubtypes = subtypes,
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Splits a comma/and-separated subtype list ("Cleric, Rogue, Warrior, and Wizard")
  /// into its individual PascalCase subtype tokens, in the order printed.
  /// </summary>
  private static IReadOnlyList<string> ParseSubtypeList(string subtypesRaw)
  {
    var normalized = _andNormalizer.Replace(subtypesRaw, ", ");
    return normalized
      .Split(',', StringSplitOptions.RemoveEmptyEntries)
      .Select(t => t.Trim())
      .Where(t => t.Length > 0)
      .ToList();
  }
}
