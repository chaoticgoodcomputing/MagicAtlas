namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "You may put a[n] [filter] card from your hand onto the battlefield [tapped]."
///
/// <para>
/// Spell-resolution sentence for the optional hand-to-battlefield land drop that
/// closes cards like Embrace the Paradox ("Draw three cards. You may put a land card
/// from your hand onto the battlefield tapped."). The draw sentence is handled by
/// <see cref="DrawCardsSimpleRule"/>; this rule matches the second sentence of the
/// bundle. The "you may" wrapper is expressed as an
/// <see cref="OptionalEffect"/> around a
/// <see cref="PutFromHandOntoBattlefieldEffect"/> (ADR 0005 clause-modifier
/// composition), mirroring the triggered
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.DrawThenMayPutLandFromHandRule"/>
/// (Spelunking) so both surfaces emit the same node shape.
/// </para>
///
/// <para>
/// Rule citations:
/// </para>
/// <list type="bullet">
///   <item>CR 400.7: "An object that moves from one zone to another becomes a new
///   object..." — the hand-to-battlefield move.</item>
///   <item>CR 110.5: a permanent's status includes tapped/untapped — the optional
///   "tapped" qualifier sets <see cref="PutFromHandOntoBattlefieldEffect.Tapped"/>.</item>
/// </list>
///
/// <para>
/// ANCHORED (^…$): the whole sentence is matched exactly so this rule cannot fire as
/// a substring of any more-specific sibling. Default priority (50) — the anchor makes
/// it mutually exclusive with other spell rules.
/// </para>
/// </summary>
[SpellRule]
public sealed class MayPutCardFromHandOntoBattlefieldRule : ISpellRule
{
  // Anchored: "you may put a[n] <filter> card from your hand onto the battlefield [tapped]"
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+put\s+a(?:n)?\s+(?<filter>.+?)\s+card\s+from\s+your\s+hand\s+onto\s+the\s+battlefield(?<tapped>\s+tapped)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known card types for filter parsing (mirrors DrawThenMayPutLandFromHandRule).
  private static readonly HashSet<string> KnownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filter = BuildFilter(m.Groups["filter"].Value.Trim());
    if (filter is null)
    {
      return false;
    }

    var put = new PutFromHandOntoBattlefieldEffect
    {
      Filter = filter,
      Tapped = m.Groups["tapped"].Success,
    };

    effect = new OptionalEffect { Inner = put };
    return true;
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the qualifier phrase between
  /// "a[n] " and " card from your hand". Handles bare card type and
  /// "subtype cardtype" forms; Zone is always <see cref="Zone.Hand"/>.
  /// </summary>
  private static ObjectFilter? BuildFilter(string qualifier)
  {
    if (string.IsNullOrWhiteSpace(qualifier))
    {
      return null;
    }

    var trimmed = qualifier.Trim();
    var lastSpaceIdx = trimmed.LastIndexOf(' ');

    string cardTypePart;
    string? subtypePart;

    if (lastSpaceIdx >= 0)
    {
      cardTypePart = trimmed[(lastSpaceIdx + 1)..];
      subtypePart = trimmed[..lastSpaceIdx].Trim();
    }
    else
    {
      cardTypePart = trimmed;
      subtypePart = null;
    }

    if (!KnownCardTypes.Contains(cardTypePart))
    {
      // Treat entire qualifier as a subtype.
      return new ObjectFilter
      {
        Subtypes = [qualifier],
        Zone = Zone.Hand,
        Controller = ControllerFilter.You,
      };
    }

    List<string>? subtypes = null;
    if (!string.IsNullOrWhiteSpace(subtypePart))
    {
      subtypes = [subtypePart];
    }

    return new ObjectFilter
    {
      CardTypes = [cardTypePart.ToLowerInvariant()],
      Subtypes = subtypes is { Count: > 0 } ? subtypes : null,
      Zone = Zone.Hand,
      Controller = ControllerFilter.You,
    };
  }
}
