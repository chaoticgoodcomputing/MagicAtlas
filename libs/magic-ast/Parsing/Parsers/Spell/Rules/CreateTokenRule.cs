namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create (a|X|&lt;num&gt;) &lt;P&gt;/&lt;T&gt; &lt;color&gt; &lt;subtype&gt; creature token(s) [with &lt;keyword&gt;]."
/// Handles literal counts ("a"), variable counts ("X"), and numeric literals.
/// Also handles predefined artifact tokens (Food, Treasure, Clue, Blood) which
/// have no P/T and whose activated ability is reminder text only (Rule 107.10b).
/// Reminder text is stripped by <see cref="SpellAbilityParser"/> before dispatch.
/// The optional "with &lt;keyword&gt;" suffix captures a single granted keyword ability
/// for the created token (e.g. "with flying", "with haste").
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CreateTokenRule : ISpellRule
{
  private static readonly Regex CreaturePattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens?(?:\s+with\s+(?<keyword>[a-z][a-z\s]*[a-z]|[a-z]+))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Builds a <see cref="StaticAbility"/> for a single simple keyword granted to a
  /// created token via "with &lt;keyword&gt;" oracle syntax. Returns <c>null</c> for
  /// unrecognised keywords — callers must handle the null case.
  ///
  /// <para>
  /// The <c>KeywordSource</c> string follows each keyword definition's own convention:
  /// Flying hardcodes <c>"Flying"</c>; Haste preserves oracle-text case (<c>"haste"</c>)
  /// as in <c>HasteKeyword.cs</c> which uses <c>keyword.ToStringValue()</c>.
  /// </para>
  /// </summary>
  private static StaticAbility? BuildGrantedKeywordAbility(string keywordText)
  {
    return keywordText.ToLowerInvariant() switch
    {
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = ["flying", "reach"],
            },
          },
        ],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = keywordText, // preserve oracle-text case (matches HasteKeyword.ToStringValue())
        Effects = [new HasteEffect()],
      },
      _ => null,
    };
  }

  // Predefined artifact token patterns (Rule 107.10b). Reminder text is already
  // stripped by SpellAbilityParser.StripReminderText before these are evaluated.
  private static readonly Regex FoodTokenPattern =
    new(@"^create a Food token\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex TreasureTokenPattern =
    new(@"^create a Treasure token\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex ClueTokenPattern =
    new(@"^create a Clue token\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex BloodTokenPattern =
    new(@"^create a Blood token\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly IReadOnlyDictionary<string, string> ColorMap = new Dictionary<string, string>(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // --- Predefined artifact token: Food ---
    if (FoodTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food(),
        IsOptional = false,
      };
      return true;
    }

    // --- Predefined artifact token: Treasure ---
    if (TreasureTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Treasure(),
        IsOptional = false,
      };
      return true;
    }

    // --- Predefined artifact token: Clue ---
    if (ClueTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Clue(),
        IsOptional = false,
      };
      return true;
    }

    // --- Predefined artifact token: Blood ---
    if (BloodTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Blood(),
        IsOptional = false,
      };
      return true;
    }

    // --- P/T creature token ---
    var m = CreaturePattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value;
    Quantity count;
    var rawLower = rawCount.ToLowerInvariant();
    if (rawLower is "x" or "y" or "z")
    {
      count = new VariableQuantity { Name = rawLower.ToUpperInvariant() };
    }
    else
    {
      count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(rawCount));
    }

    var colorCode = ColorMap[m.Groups["color"].Value];
    var power = m.Groups["power"].Value;
    var toughness = m.Groups["toughness"].Value;
    // Capitalize first letter of subtype to match canonical MTG form.
    var subtype = m.Groups["subtype"].Value;
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    // Resolve optional "with <keyword>" granted ability.
    IReadOnlyList<Ability>? grantedAbilities = null;
    if (m.Groups["keyword"].Success)
    {
      var keywordAbility = BuildGrantedKeywordAbility(m.Groups["keyword"].Value.Trim());
      if (keywordAbility is not null)
      {
        grantedAbilities = [keywordAbility];
      }
    }

    effect = new CreateTokenEffect
    {
      Count = count,
      Token = new TokenDefinition
      {
        Power = power,
        Toughness = toughness,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        Abilities = grantedAbilities,
        IsCopy = false,
      },
      IsOptional = false,
    };
    return true;
  }
}
