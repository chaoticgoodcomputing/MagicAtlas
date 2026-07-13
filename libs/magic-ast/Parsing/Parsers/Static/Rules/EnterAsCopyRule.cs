namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "You may have this [permanent] enter as a copy of [filter][, except it's a
/// [Subtype] in addition to its other types]." — the Clone / Glasspool Mimic
/// enter-as-a-copy replacement (CR 707.2). A static/replacement ability that
/// modifies how the permanent enters the battlefield: as it enters, its controller
/// may have it enter as a copy of a qualifying object, optionally with an "except"
/// rider overriding one of the copy's copiable values.
///
/// <para>
/// CR 707.2 (verbatim): "When copying an object, the copy acquires the copiable
/// values of the original object's characteristics and, for an object on the stack,
/// choices made when casting or activating it…" — the entering permanent is the
/// copy; no new object is created, so the copy relationship is modelled by
/// <see cref="BecomesCopyEffect"/> (Subject: Self becomes a copy of the chosen
/// object), NOT <see cref="MagicAST.AST.Effects.TokenCopy.CopyEffect"/> (which
/// creates a NEW token/spell copy).
/// </para>
///
/// <para>
/// Decomposition (ADR 0003): timing and effect are separate composable axes. The
/// "as it enters" replacement timing lives on the enclosing
/// <see cref="StaticAbility.When"/> = <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 603.6d/614.1c — a static replacement modifying entry, not a triggered
/// ability); the "may" optionality lives on an <see cref="OptionalEffect"/> wrapper
/// (CR 117.7); the copy relationship lives on the plain
/// <see cref="BecomesCopyEffect"/> inner effect. None of the three axes is baked
/// into another.
/// </para>
///
/// <para>
/// The copy target — "any creature on the battlefield" (Clone) or "a creature you
/// control" (Glasspool Mimic) — is an indefinite controller choice
/// (<see cref="ObjectReferenceKind.Any"/>: not a target, no "target" keyword; CR
/// 115.1), carrying a typed <see cref="ObjectFilter"/> for the object class, zone,
/// and (for the "you control" form) controller. Anchored ^…$ so the surface phrase
/// cannot be matched as a substring inside a more-specific sibling.
/// </para>
///
/// <para>
/// Glasspool Mimic's trailing "except it's a Shapeshifter Rogue in addition to its
/// other types" is a <see cref="TypeAdder"/> modification on the
/// <see cref="BecomesCopyEffect.Modifications"/> list (CR 707.2 copiable values —
/// the except-clause overrides the copy's inherited subtypes without removing
/// them, CR 205.1b). Optional group: Clone's plain form has no except-clause and
/// still matches with a null <c>Modifications</c>.
/// </para>
/// </summary>
[StaticRule(Priority = 966)]
public sealed class EnterAsCopyRule : IStaticRule
{
  // "You may have this [noun] enter as a copy of [copyTarget][, except it's a[n]
  // [Types] in addition to its other types]."
  // Anchored ^…$; the trailing period is optional. The copyTarget group is parsed
  // into a typed ObjectFilter; an unrecognised copyTarget phrase declines (returns
  // null), never a lossy/free-text parse.
  private static readonly Regex _pattern = new(
    @"^You\s+may\s+have\s+this\s+(?:permanent|creature|artifact|land|enchantment|planeswalker)"
    + @"\s+enter\s+as\s+a\s+copy\s+of\s+(?<copyTarget>.+?)"
    + @"(?:,\s*except\s+it's\s+an?\s+(?<addedTypes>[A-Z][a-zA-Z]*(?:\s+[A-Z][a-zA-Z]*)*)"
    + @"\s+in\s+addition\s+to\s+its\s+other\s+types)?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "any [type] on the battlefield" (Clone) or "a [type] you control" (Glasspool
  // Mimic) — the copy-source selection. Typed, anchored.
  private static readonly Regex _copyTargetPattern = new(
    @"^(?:any\s+(?<battlefieldType>creature|artifact|permanent|enchantment|planeswalker)\s+on\s+the\s+battlefield"
    + @"|a\s+(?<controlledType>creature|artifact|permanent|enchantment|planeswalker)\s+you\s+control)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText.Trim());
    if (!match.Success)
    {
      return null;
    }

    var copyFilter = ParseCopyTarget(match.Groups["copyTarget"].Value.Trim());
    if (copyFilter is null)
    {
      // Unrecognised copy-source phrase — decline rather than emit a lossy parse.
      return null;
    }

    IReadOnlyList<CopyModification>? modifications = null;
    if (match.Groups["addedTypes"].Success)
    {
      var subtypes = match.Groups["addedTypes"].Value
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant())
        .ToList();
      modifications = [new TypeAdder { Subtypes = subtypes }];
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects =
        [
          new OptionalEffect
          {
            Inner = new BecomesCopyEffect
            {
              Subject = ObjectReference.Self(),
              CopyTarget = new ObjectReference
              {
                Kind = ObjectReferenceKind.Any,
                Filter = copyFilter,
              },
              Modifications = modifications,
            },
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses the "any [type] on the battlefield" / "a [type] you control" copy-source
  /// phrase into a typed <see cref="ObjectFilter"/>. Returns <c>null</c> for any
  /// unrecognised form so the rule declines instead of producing a lossy parse.
  /// </summary>
  private static ObjectFilter? ParseCopyTarget(string phrase)
  {
    var match = _copyTargetPattern.Match(phrase);
    if (!match.Success)
    {
      return null;
    }

    if (match.Groups["battlefieldType"].Success)
    {
      return new ObjectFilter
      {
        CardTypes = [match.Groups["battlefieldType"].Value.ToLowerInvariant()],
        Zone = Zone.Battlefield,
      };
    }

    return new ObjectFilter
    {
      CardTypes = [match.Groups["controlledType"].Value.ToLowerInvariant()],
      Controller = ControllerFilter.You,
    };
  }
}
