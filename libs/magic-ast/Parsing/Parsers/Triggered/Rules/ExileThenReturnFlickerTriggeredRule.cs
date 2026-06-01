namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// ETB "flicker" (blink) effect: "you may exile [target] you control, then return
/// that card to the battlefield under [its owner's / your] control." — the
/// resolution half of an ETB trigger (Felidar Guardian, Restoration Angel).
///
/// <para>
/// The "you may" is a structured <see cref="OptionalEffect"/> (CR 117.7), not a
/// flag: the controller may choose to perform the whole exile-then-return action.
/// The action itself is a <see cref="CompositeEffect"/> of two ordered effects —
/// an <see cref="ExileEffect"/> followed by a <see cref="ReturnToBattlefieldEffect"/>.
/// "then return that card" is the LINKED exiled reference (the Petravark /
/// Azula precedent), modelled via <see cref="ObjectFilter.ExiledWith"/> as a
/// <c>{Zone: Exile, ExiledWith: {Kind: Self}}</c> reference (ADR 0004
/// reference-not-resolution) rather than a threaded runtime binding or free text:
/// the just-exiled card and this return are the same one resolving ability.
/// </para>
///
/// <para>
/// The exile target keeps its printed filter — "another target permanent you
/// control" (<c>ExcludeSelf</c>, CR 109.5) or "target non-Angel creature you
/// control" (<c>ExcludedSubtypes</c>). "under its owner's control" rides on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> as an
/// <see cref="ObjectReferenceKind.Owner"/> reference; "under your control" as a
/// <see cref="ObjectReferenceKind.You"/> reference (CR 400.6).
/// </para>
///
/// CR 603.6a (enters-the-battlefield abilities) — this is the resolution half;
/// the trigger condition ("When this creature enters") is recognised separately.
/// CR 603.2 (the event-match is the trigger).
/// </summary>
[TriggeredRule(Priority = 600)]
public sealed class ExileThenReturnFlickerTriggeredRule : ITriggeredRule
{
  // "you may exile <target> you control, then return that card to the battlefield
  //  under <its owner's | your> control"
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+exile\s+(?<target>.+?)\s+you\s+control,\s*then\s+return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+(?<control>its\s+owner'?s|your)\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var targetFilter = BuildExileTargetFilter(match.Groups["target"].Value.Trim());
    if (targetFilter is null)
    {
      return false;
    }

    var underControl = match.Groups["control"].Value.Trim().StartsWith("its", StringComparison.OrdinalIgnoreCase)
      ? new ObjectReference { Kind = ObjectReferenceKind.Owner }
      : new ObjectReference { Kind = ObjectReferenceKind.You };

    effect = new OptionalEffect
    {
      Inner = new CompositeEffect
      {
        Effects =
        [
          new ExileEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Target,
              Filter = targetFilter,
            },
          },
          new ReturnToBattlefieldEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Designated,
              Filter = new ObjectFilter
              {
                Zone = Zone.Exile,
                ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
              },
            },
            UnderControl = underControl,
          },
        ],
      },
    };
    return true;
  }

  /// <summary>
  /// Builds the exile target filter from the "[another] target [non-Sub] [type]"
  /// fragment that precedes "you control". Always You-controlled (the regex peeled
  /// the trailing "you control"). Handles the "another" self-exclusion (CR 109.5)
  /// and a single "non-[Subtype]" exclusion (CR 205.3). The card type must be one
  /// of the recognised permanent words.
  /// </summary>
  private static ObjectFilter? BuildExileTargetFilter(string fragment)
  {
    var lower = fragment.ToLowerInvariant();

    // "another target ..." excludes the source permanent (CR 109.5).
    var excludeSelf = Regex.IsMatch(lower, @"^another\b") ? (bool?)true : null;

    // "non-[Subtype]" exclusion, e.g. "non-Angel creature" — preserve the
    // printed casing of the subtype word (CR 205.3 subtypes are proper-cased).
    IReadOnlyList<string>? excludedSubtypes = null;
    var nonMatch = Regex.Match(fragment, @"\bnon-(?<sub>[A-Za-z]+)\b", RegexOptions.IgnoreCase);
    if (nonMatch.Success)
    {
      var sub = nonMatch.Groups["sub"].Value;
      excludedSubtypes = [char.ToUpperInvariant(sub[0]) + sub[1..]];
    }

    string? cardType = null;
    foreach (var t in new[] { "permanent", "creature", "artifact", "enchantment", "land", "planeswalker" })
    {
      if (Regex.IsMatch(lower, $@"\b{t}\b"))
      {
        cardType = t;
        break;
      }
    }
    if (cardType is null)
    {
      return null;
    }

    return new ObjectFilter
    {
      CardTypes = [cardType],
      ExcludedSubtypes = excludedSubtypes,
      Controller = ControllerFilter.You,
      ExcludeSelf = excludeSelf,
    };
  }
}
