namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create (a|X|&lt;num&gt;) &lt;P&gt;/&lt;T&gt; &lt;color&gt; [subtype ...] creature token(s) [with &lt;keyword&gt;]."
/// Handles literal counts ("a"), variable counts ("X"), and numeric literals.
/// Also handles predefined artifact tokens (Food, Treasure, Clue, Blood) which
/// have no P/T and whose activated ability is reminder text only (Rule 107.10b).
/// Reminder text is stripped by <see cref="SpellAbilityParser"/> before dispatch.
/// The optional "with &lt;keyword&gt;" suffix captures a single granted keyword ability
/// for the created token (e.g. "with flying", "with haste").
///
/// Multi-subtype tokens (e.g. "Knight Ally") are supported: the subtypes group
/// captures all words between the color and "creature", which are split on
/// whitespace to produce a <see cref="TokenDefinition.Subtypes"/> list.
/// (CR 111.1: a token is a marker representing a permanent that isn't a card.)
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CreateTokenRule : ISpellRule
{
  private static readonly Regex CreaturePattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtypes>(?:\w+\s+)+)creature\s+tokens?(?:\s+with\s+(?<keyword>[a-z][a-z\s]*[a-z]|[a-z]+))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Builds a <see cref="StaticAbility"/> for a single simple keyword granted to a
  /// created token via "with &lt;keyword&gt;" oracle syntax. Returns <c>null</c> for
  /// unrecognised keywords — callers must handle the null case.
  ///
  /// <para>
  /// <c>KeywordSource</c> uses the canonical capitalized keyword name
  /// (e.g. <c>"Flying"</c>, <c>"Haste"</c>), matching the convention across the
  /// keyword definitions and existing fixtures.
  /// </para>
  /// </summary>
  private static StaticAbility? BuildGrantedKeywordAbility(string keywordText)
  {
    return keywordText.ToLowerInvariant() switch
    {
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
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
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
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food(),
      };
      return true;
    }

    // --- Predefined artifact token: Treasure ---
    if (TreasureTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Treasure(),
      };
      return true;
    }

    // --- Predefined artifact token: Clue ---
    if (ClueTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Clue(),
      };
      return true;
    }

    // --- Predefined artifact token: Blood ---
    if (BloodTokenPattern.IsMatch(text))
    {
      effect = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Blood(),
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
    // Split the subtypes group (e.g. "Knight Ally ") into individual canonical
    // subtype strings. Capitalize first letter to match canonical MTG form.
    var subtypes = m.Groups["subtypes"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => char.ToUpperInvariant(s[0]) + s[1..])
      .ToArray();

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
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = power,
        Toughness = toughness,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = subtypes,
        Abilities = grantedAbilities,
        IsCopy = false,
      },
    };
    return true;
  }
}
