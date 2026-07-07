namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "You may have this [permanent] enter as a copy of [filter]." — the Clone
/// enter-as-a-copy replacement (CR 707.2). A static/replacement ability that
/// modifies how the permanent enters the battlefield: as it enters, its controller
/// may have it enter as a copy of any qualifying object already on the battlefield.
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
/// The copy target — "any creature on the battlefield" — is an indefinite
/// controller choice (<see cref="ObjectReferenceKind.Any"/>: not a target, no
/// "target" keyword; CR 115.1), carrying a typed
/// <see cref="ObjectFilter"/> for the object class and zone. Anchored ^…$ so the
/// surface phrase cannot be matched as a substring inside a more-specific sibling
/// (the "…, except [modification]" copy-with-except forms end past the anchor and
/// correctly decline here).
/// </para>
/// </summary>
[StaticRule(Priority = 966)]
public sealed class EnterAsCopyRule : IStaticRule
{
  // "You may have this [noun] enter as a copy of [copyTarget]."
  // Anchored ^…$; the trailing period is optional. The copyTarget group is parsed
  // into a typed ObjectFilter; an unrecognised copyTarget phrase declines (returns
  // null), never a lossy/free-text parse.
  private static readonly Regex _pattern = new(
    @"^You\s+may\s+have\s+this\s+(?:permanent|creature|artifact|land|enchantment|planeswalker)"
    + @"\s+enter\s+as\s+a\s+copy\s+of\s+(?<copyTarget>.+?)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "any [type] on the battlefield" — the copy-source selection. Typed, anchored.
  private static readonly Regex _copyTargetPattern = new(
    @"^any\s+(?<type>creature|artifact|permanent|enchantment|planeswalker)\s+on\s+the\s+battlefield$",
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
            },
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses the "any [type] on the battlefield" copy-source phrase into a typed
  /// <see cref="ObjectFilter"/>. Returns <c>null</c> for any unrecognised form so
  /// the rule declines instead of producing a lossy parse.
  /// </summary>
  private static ObjectFilter? ParseCopyTarget(string phrase)
  {
    var match = _copyTargetPattern.Match(phrase);
    if (!match.Success)
    {
      return null;
    }

    return new ObjectFilter
    {
      CardTypes = [match.Groups["type"].Value.ToLowerInvariant()],
      Zone = Zone.Battlefield,
    };
  }
}
