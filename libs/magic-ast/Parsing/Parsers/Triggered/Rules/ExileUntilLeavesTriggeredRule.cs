namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target [filter] [an opponent controls] until this [type] leaves the battlefield."
/// — the Banisher Priest / Oblivion Ring / Hieromancer's Cage ETB exile family.
///
/// Generalises across:
/// <list type="bullet">
///   <item>self subject — creature (Banisher Priest, Bishop of Binding), enchantment
///   (Citizen's Arrest, Hieromancer's Cage), artifact/Vehicle (Detention Chariot), …;
///   carried in the duration's <c>Object</c> self-reference phrase;</item>
///   <item>exile target — a single card type ("creature"), a card-type disjunction
///   ("creature or planeswalker", "artifact or creature"), or "nonland permanent";</item>
///   <item>optional "an opponent controls" controller restriction.</item>
/// </list>
///
/// The effect is a temporary exile: the exiled permanent returns when this [type] leaves
/// the battlefield (Rule 611 — continuous effects with duration; Rule 406 — the exile
/// zone). MAST records the exile action descriptively (Rule 701.13 — Exile) with an
/// <see cref="UntilLeavesBattlefieldDuration"/> whose <c>Object</c> is the literal
/// self-reference phrase from oracle text ("this enchantment", "this creature", etc.).
///
/// The combined "exile ... until this leaves the battlefield" clause IS the linkage
/// (Rule 603.6 — zone-change triggers; Rule 603.7 — the delayed/linked return). The
/// implicit LTB return is engine territory; MAST does not emit a separate return effect
/// for the combined-clause form (descriptive-not-engine doctrine). Cards that instead
/// print two explicit triggers (Fiend Hunter, Petravark) are modelled as two abilities
/// elsewhere — not this rule.
/// </summary>
[TriggeredRule]
public sealed class ExileUntilLeavesTriggeredRule : ITriggeredRule
{
  // "exile target <filter> [an opponent controls] until this <type> leaves the battlefield".
  // <filter> is captured non-greedily and resolved by ParseTargetFilter; the optional
  // controller clause and the self-reference type are captured separately.
  private static readonly Regex Pattern = new(
    @"^exile\s+target\s+(?<filter>.+?)(?<oppctrl>\s+an\s+opponent\s+controls)?\s+until\s+this\s+(?<type>creature|artifact|enchantment|permanent|aura|land|vehicle)\s+leaves\s+the\s+battlefield$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filter = ParseTargetFilter(m.Groups["filter"].Value.Trim());
    if (filter is null)
    {
      return false;
    }

    if (m.Groups["oppctrl"].Success)
    {
      filter = filter with { Controller = ControllerFilter.Opponent };
    }

    var selfType = m.Groups["type"].Value.ToLowerInvariant();

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      Duration = new UntilLeavesBattlefieldDuration
      {
        Object = $"this {selfType}",
      },
    };
    return true;
  }

  // Bare card-type tokens accepted in the exile-target filter (Rule 205.2 — card types).
  private static readonly HashSet<string> CardTypeTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature",
    "artifact",
    "enchantment",
    "planeswalker",
    "permanent",
    "land",
    "battle",
  };

  /// <summary>
  /// Resolves the exile-target filter phrase that sits between "target" and the
  /// optional "an opponent controls" / "until this ... leaves the battlefield" tail.
  /// Handles "nonland permanent", a single card type, and a card-type disjunction
  /// ("creature or planeswalker", "artifact or creature"). Returns null for any
  /// other shape so this rule declines rather than over-approximating.
  /// </summary>
  private static ObjectFilter? ParseTargetFilter(string phrase)
  {
    var lower = phrase.ToLowerInvariant();

    // "nonland permanent" — the Oblivion-Ring shape (permanent, with land excluded).
    if (lower == "nonland permanent")
    {
      return new ObjectFilter
      {
        CardTypes = ["permanent"],
        ExcludedCardTypes = ["land"],
      };
    }

    // Single card type, or a disjunction joined by "or" ("creature or planeswalker",
    // "artifact or creature"). Split on the disjunction and validate each token.
    var withoutOr = Regex.Replace(lower, @"\s*,?\s+or\s+", ",", RegexOptions.IgnoreCase);
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var cardTypes = new List<string>();
    foreach (var rawToken in withoutOr.Split(','))
    {
      var token = rawToken.Trim();
      if (token.Length == 0 || !CardTypeTokens.Contains(token))
      {
        return null;
      }
      if (seen.Add(token))
      {
        cardTypes.Add(token);
      }
    }

    if (cardTypes.Count == 0)
    {
      return null;
    }

    return new ObjectFilter { CardTypes = cardTypes };
  }
}
