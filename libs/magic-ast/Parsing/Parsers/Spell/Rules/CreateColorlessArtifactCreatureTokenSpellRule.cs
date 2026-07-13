namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create (a|X|&lt;num&gt;) &lt;P&gt;/&lt;T&gt; colorless &lt;Subtype&gt; artifact creature
/// token(s) [with &lt;keyword&gt;]." — the colorless-artifact-creature-token shape (Stone
/// Idol Trap: "Create a 6/12 colorless Construct artifact creature token with trample.").
///
/// <para>
/// CR 105.1: "Colorless is not a color"; the token's <see cref="TokenDefinition.Colors"/>
/// is <c>["C"]</c>, matching the colorless artifact-creature-token encoding used elsewhere
/// in MAST (e.g. Whirler Rogue's Thopter, Blade Splicer's Golem).
/// </para>
///
/// <para>
/// Sibling of <see cref="CreateTokenRule"/> (the colored "creature token" shape, whose
/// color-word group excludes "colorless"), scoped to the "colorless … artifact creature"
/// multi-type form instead — mirroring the Activated-side split between
/// <c>CreateColorlessCreatureTokenEffectRule</c> and
/// <c>CreateColorlessArtifactCreatureTokenEffectRule</c>. Fully anchored (^…$).
/// </para>
///
/// <para>CR 111.3 (token creation); CR 105.1 (colorless); CR 701.7 (Create).</para>
/// </summary>
[SpellRule(Priority = 61)]
public sealed class CreateColorlessArtifactCreatureTokenSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Create\s+(?<count>a|an|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+"
    + @"(?<power>\d+)/(?<toughness>\d+)\s+colorless\s+"
    + @"(?<subtype>[A-Za-z]+(?:\s+[A-Za-z]+)*?)\s+artifact\s+creature\s+tokens?"
    + @"(?:\s+with\s+(?<keyword>[a-z][a-z\s]*[a-z]|[a-z]+))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var stripped = text.Trim();
    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new VariableQuantity { Name = rawCount.ToUpperInvariant() },
      _ => LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(rawCount)),
    };

    var rawSubtype = m.Groups["subtype"].Value.Trim();
    var subtype = string.Join(
      " ",
      rawSubtype
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant())
    );

    IReadOnlyList<Ability>? tokenAbilities = null;
    if (m.Groups["keyword"].Success)
    {
      var grantedAbility = BuildGrantedKeywordAbility(m.Groups["keyword"].Value.Trim().ToLowerInvariant());
      if (grantedAbility is null)
      {
        // Unrecognised granted keyword — bail rather than silently drop it (CR 111.3).
        return false;
      }
      tokenAbilities = [grantedAbility];
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = ["C"],
        Types = ["artifact", "creature"],
        Subtypes = [subtype],
        Abilities = tokenAbilities,
        IsCopy = false,
      },
    };
    return true;
  }

  /// <summary>
  /// Builds a <see cref="StaticAbility"/> for a single simple keyword granted to a
  /// created token via "with &lt;keyword&gt;" oracle syntax, mirroring
  /// <see cref="CreateTokenRule"/>'s private helper of the same shape (kept local so
  /// this rule takes no dependency on that rule's internals). Returns <c>null</c> for
  /// an unrecognised keyword — callers must handle the null case.
  /// </summary>
  private static StaticAbility? BuildGrantedKeywordAbility(string keywordText) =>
    keywordText switch
    {
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Trample }],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Flying,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
            },
          },
        ],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Haste }],
      },
      _ => null,
    };
}
