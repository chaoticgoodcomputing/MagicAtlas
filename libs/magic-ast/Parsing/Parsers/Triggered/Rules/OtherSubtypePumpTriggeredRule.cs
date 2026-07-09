namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "other [Subtype] you control get +N/+M until end of turn." — the single-subtype
/// tribal pump that appears as the RESOLUTION EFFECT of a triggered ability, e.g.
/// Perimeter Sergeant's "Whenever this creature attacks, other Humans you control
/// get +1/+0 until end of turn."
///
/// <para>
/// The leading "other" is the self-exclusion qualifier ("another" / CR 109.5): the
/// source creature is omitted from the pumped set, recorded as
/// <see cref="ObjectFilter.ExcludeSelf"/> = true. The subtype noun ("Humans") names a
/// creature subtype (CR 205.3m); it lands on <see cref="ObjectFilter.Subtypes"/> in its
/// singular oracle-canonical form ("Human"), alongside <c>CardTypes = ["creature"]</c>
/// — the same shape the static "Other [Subtype] creatures you control get +N/+M" lord
/// buff (LordPTBuffRule) produces for the isOther case.
/// </para>
///
/// <para>
/// The timing (Whenever this creature attacks) and the effect (other Humans get +1/+0
/// until end of turn) are separate composable nodes (CR 603.1): this rule recognises
/// only the effect half, which <see cref="TriggeredAbilityParser"/> pairs with the
/// already-parsing trigger. The boost is a layer-7c continuous P/T modification
/// (CR 613.4c), so it is a <see cref="ModifyPTEffect"/> with literal +N/+M modifiers
/// and <see cref="Duration"/> = <c>untilTime{Turn, End}</c>.
/// </para>
///
/// <para>
/// New, collision-free file. The pattern is anchored (<c>^…$</c>) and requires the
/// subtype token to be capitalised, so it never steals the lowercase generic-noun
/// mass shapes ("other creatures you control …", handled elsewhere). It requires a
/// SINGLE subtype word immediately followed by "you control", so it does not overlap
/// the comma/"and"-joined multi-subtype list handled by
/// <see cref="SubtypeListPumpTriggeredRule"/>, nor the bare "creatures you control"
/// shape handled by <see cref="EtbTeamPumpTriggeredRule"/> and
/// <see cref="MassModifyPTTriggeredRule"/>. It also requires "get" (plural), so it is
/// disjoint from the singular-subject "it/this creature/target creature gets" shape
/// handled by <see cref="ModifyPTTriggeredRule"/>.
/// </para>
/// </summary>
[TriggeredRule(Priority = 965)]
public sealed class OtherSubtypePumpTriggeredRule : ITriggeredRule
{
  // "other <CapitalisedSubtype> you control get +N/+M until end of turn"
  // "other" is matched case-insensitively (effect fragments are typically
  // mid-sentence lowercase, but tolerate a leading capital). The subtype token
  // MUST start with an uppercase letter — that is how oracle text distinguishes a
  // creature subtype ("Humans") from the generic lowercase noun ("creatures"), so
  // this rule never fires on the mass "other creatures you control …" shape.
  private static readonly Regex _pattern = new(
    @"^[Oo]ther\s+(?<subtype>[A-Z][a-zA-Z]+)\s+you\s+control\s+get\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled
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

    var subtype = Singularize(m.Groups["subtype"].Value);
    if (subtype.Length == 0)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  // Known irregular plural → singular creature-subtype forms; regular plurals
  // fall through to a trailing-"s" strip.
  private static readonly IReadOnlyDictionary<string, string> _irregularPlurals =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Elves"] = "Elf",
      ["Mice"] = "Mouse",
      ["Wolves"] = "Wolf",
      ["Dwarves"] = "Dwarf",
      ["Loci"] = "Locus",
      ["Djinn"] = "Djinn",
    };

  private static string Singularize(string plural)
  {
    if (_irregularPlurals.TryGetValue(plural, out var singular))
    {
      return singular;
    }
    if (plural.EndsWith('s') && plural.Length > 1)
    {
      return plural[..^1];
    }
    return plural;
  }
}
