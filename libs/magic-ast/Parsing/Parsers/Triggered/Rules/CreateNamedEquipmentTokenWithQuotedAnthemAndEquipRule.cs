namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create [Name], a legendary colorless Equipment artifact token with "[quoted
/// static ability]" and equip [cost]" — Mabel, Heir to Cragflame's ETB: "When
/// Mabel enters, create Cragflame, a legendary colorless Equipment artifact token
/// with "Equipped creature gets +1/+1 and has vigilance, trample, and haste" and
/// equip {2}."
///
/// <para>
/// The generic <see cref="CreateTokenRule"/> only recognises creature tokens
/// (its subtype/P-T helpers assume a "creature token" noun phrase) and has no
/// path for a non-creature Equipment token carrying both a quoted static
/// ability AND a separate bare "equip [cost]" keyword clause tacked onto the
/// SAME sentence (not its own oracle line). This dedicated rule builds the
/// token's two abilities directly:
/// <list type="bullet">
///   <item>The quoted body "Equipped creature gets +N/+M and has kw1, kw2[,
///   ...][, and kwN]." — an attached P/T buff (CR 613.4c layer 7c) plus an
///   N-keyword grant (CR 613.1f layer 6) to the equipped creature (CR 702.6),
///   built the same way as the two-keyword shape
///   <see cref="MagicAST.Parsing.Parsers.Static.EquippedCreaturesHaveKeywordListRule"/>
///   and the keyword+quoted-body shape
///   <see cref="MagicAST.Parsing.Parsers.Static.EquippedPTKeywordAndGrantedAbilityRule"/>
///   already cover, generalised to an arbitrary-length Oxford-comma keyword
///   list via the same <see cref="MagicAST.Parsing.Parsers.Static.StaticRuleHelpers.MapKeywordToStaticAbility"/>
///   keyword table both siblings read from.</item>
///   <item>"equip [cost]" (CR 702.6a verbatim: "'[Cost]: Attach this permanent
///   to target creature you control. Activate only as a sorcery.'") — built
///   identically to <see cref="MagicAST.Keywords.Definitions.EquipKeyword"/>'s
///   combinator output (same <see cref="AttachEffect"/> target shape, same
///   <see cref="MagicAST.AST.Abilities.ActivationRestriction.OnlyAsSorcery"/>
///   restriction), constructed directly here since that combinator only runs
///   over an independent top-level oracle clause, not an embedded sentence
///   fragment.</item>
/// </list>
/// </para>
///
/// <para>
/// Runs at priority 70, above the generic <see cref="CreateTokenRule"/> (default
/// 50), so this specific shape is matched first. Fully anchored (^…$) on the
/// literal "a legendary colorless Equipment artifact token with … and equip
/// {N}" spine, so it cannot match any other create-token shape.
/// </para>
///
/// <para>CR 111.3 (token creation); CR 205.4a (Legendary supertype); CR 105.1
/// (colorless); CR 702.6 (Equipment); CR 613.1f/613.4c (layered continuous
/// effects — descriptive only, MAST records what the text says).</para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class CreateNamedEquipmentTokenWithQuotedAnthemAndEquipRule : ITriggeredRule
{
  // "create <Name>, a legendary colorless Equipment artifact token with "<ability>" and equip {N}."
  // Accepts both straight and curly quotes around the ability body.
  private static readonly Regex _pattern = new(
    @"^create\s+(?<name>[A-Z][A-Za-z']+),\s+a\s+legendary\s+colorless\s+Equipment\s+artifact\s+token\s+with\s+[\x22“](?<ability>[^\x22”]+)[\x22”]\s+and\s+equip\s+\{(?<cost>\d+)\}\.?\s*$",
    RegexOptions.Compiled
  );

  // "Equipped creature gets +N/+M and has <kw1>[, <kw2>[, ...]][, and <kwN>]."
  private static readonly Regex _grantedAbilityPattern = new(
    @"^Equipped\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+(?<kws>.+?)\.?$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var name = match.Groups["name"].Value;
    var abilityText = match.Groups["ability"].Value.Trim();
    var cost = int.Parse(match.Groups["cost"].Value);

    var grantedAbility = TryParseEquippedGrantedAbility(abilityText);
    if (grantedAbility is null)
    {
      // The quoted body isn't the recognised "P/T + keyword list" shape — decline
      // rather than emit a token with a silently dropped ability (CR 111.3).
      return false;
    }

    var equipAbility = new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Equip,
      Costs = [new ManaCost { Symbols = [ManaSymbol.Generic(cost)] }],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
    };

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Name = name,
        Colors = ["C"],
        Types = ["artifact"],
        Subtypes = ["Equipment"],
        Supertypes = ["Legendary"],
        Abilities = [grantedAbility, equipAbility],
        IsCopy = false,
      },
    };
    return true;
  }

  /// <summary>
  /// Parses "Equipped creature gets +N/+M and has kw1[, kw2[, ...]][, and kwN]."
  /// into a single <see cref="StaticAbility"/> carrying a <see cref="ModifyPTEffect"/>
  /// followed by one <see cref="GainAbilityEffect"/> per listed keyword — the same
  /// shape <see cref="MagicAST.Parsing.Parsers.Static.EquippedPTKeywordAndGrantedAbilityRule"/>
  /// builds for its two-part (P/T + one keyword + one quoted ability) sibling shape,
  /// generalised to an arbitrary-length keyword list with no quoted ability tail.
  /// </summary>
  private static StaticAbility? TryParseEquippedGrantedAbility(string body)
  {
    var match = _grantedAbilityPattern.Match(body.Trim());
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var kwsRaw = match.Groups["kws"].Value.Trim().TrimEnd('.');
    var kwParts = kwsRaw.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (kwParts.Count == 0)
    {
      return null;
    }

    // Oxford-comma tail: the last item may be prefixed with "and ".
    var lastIndex = kwParts.Count - 1;
    if (kwParts[lastIndex].StartsWith("and ", StringComparison.OrdinalIgnoreCase))
    {
      kwParts[lastIndex] = kwParts[lastIndex]["and ".Length..].Trim();
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    var effects = new List<Effect>
    {
      new ModifyPTEffect
      {
        Target = target,
        PowerModifier = LiteralQuantity.Of(power),
        ToughnessModifier = LiteralQuantity.Of(toughness),
      },
    };

    foreach (var kw in kwParts)
    {
      var granted = MagicAST.Parsing.Parsers.Static.StaticRuleHelpers.MapKeywordToStaticAbility(
        kw.ToLowerInvariant()
      );
      if (granted is null)
      {
        // An unrecognised keyword in the list — decline entirely so the caller
        // surfaces the gap rather than emitting a partial grant.
        return null;
      }

      effects.Add(new GainAbilityEffect { Target = target, GainedAbility = granted });
    }

    return new StaticAbility { Effects = effects };
  }
}
