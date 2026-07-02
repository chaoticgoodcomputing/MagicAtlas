namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "exile up to one [other] target [type] you control, then return that card to
/// the battlefield under [its owner's | your] control" — the "up to one" flicker
/// (blink): the cast-trigger of Displacer Kitten ("Avoidance — Whenever you cast a
/// noncreature spell, …", nonland permanent / its owner's control) and the end-step
/// trigger of Thassa, Deep-Dwelling ("At the beginning of your end step, exile up to
/// one other target creature you control, … under your control").
///
/// <para>
/// This is the triggered-side wrapper of the same flicker effect the spell-side
/// <see cref="Spell.Rules.FlickerTargetPermanentsSpellRule"/> (Ghostly Flicker) builds:
/// an <see cref="ExileEffect"/> on the chosen permanent, then a
/// <see cref="ReturnToBattlefieldEffect"/> of the just-exiled card. The trigger
/// condition itself ("Whenever you cast a noncreature spell" / "At the beginning of
/// your end step") is recognised separately per CR 603.2 (a game event matching the
/// trigger event triggers the ability); this rule only builds the effect.
/// </para>
///
/// <para>
/// "that card" is NOT free text — it is the linked exiled reference (CR 607.2 /
/// ADR 0004 "reference, not resolution"): a <see cref="ObjectReferenceKind.Designated"/>
/// card in <see cref="Zone.Exile"/> exiled with this object
/// (<c>ExiledWith = {Kind: Self}</c>), the Petravark return shape. "up to one" is a
/// structured <see cref="UpToQuantity"/> on the target reference's
/// <see cref="ObjectReference.Quantity"/>, not a literal. The target keeps its printed
/// filter — "[other] target [type] you control" (<c>CardTypes</c> + the
/// <c>ExcludeSelf</c> "other" self-exclusion, CR 109.5 + <c>Controller = You</c>; the
/// "nonland permanent" shape adds <c>ExcludedCardTypes = ["land"]</c>). "under its
/// owner's control" rides on <see cref="ReturnToBattlefieldEffect.UnderControl"/> as a
/// <see cref="ObjectReferenceKind.Owner"/> reference, "under your control" as a
/// <see cref="ObjectReferenceKind.You"/> reference (CR 400.6). The two effects sit in
/// one <see cref="CompositeEffect"/> because the oracle states them as a single
/// "exile …, then return …" action.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class FlickerUpToOnePermanentTriggeredRule : ITriggeredRule
{
  // "exile up to one [other] target <type> you control, then return that card to
  //  the battlefield under <its owner's | your> control"
  private static readonly Regex Pattern = new(
    @"^exile\s+up\s+to\s+one\s+(?<target>.+?)\s+you\s+control,\s+then\s+return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+(?<control>its\s+owner'?s|your)\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
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

    effect = new CompositeEffect
    {
      Effects =
      [
        new ExileEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Quantity = new UpToQuantity { Maximum = 1 },
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
    };
    return true;
  }

  /// <summary>
  /// Builds the exile target filter from the "[other] target [nonland] [type]"
  /// fragment that precedes "you control" (the regex peeled the trailing "you
  /// control", so the result is always You-controlled). Handles the "other"
  /// self-exclusion (CR 109.5) and the "nonland permanent" land exclusion
  /// (CR 205.3 — Displacer Kitten). The card type must be a recognised permanent word.
  /// </summary>
  private static ObjectFilter? BuildExileTargetFilter(string fragment)
  {
    var lower = fragment.ToLowerInvariant();

    // "up to one other target ..." excludes the source permanent (CR 109.5).
    var excludeSelf = Regex.IsMatch(lower, @"\bother\b") ? (bool?)true : null;

    // "nonland permanent" — exclude the land card type (CR 205.3).
    IReadOnlyList<string>? excludedCardTypes = Regex.IsMatch(lower, @"\bnonland\b")
      ? ["land"]
      : null;

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
      ExcludedCardTypes = excludedCardTypes,
      Controller = ControllerFilter.You,
      ExcludeSelf = excludeSelf,
    };
  }
}
