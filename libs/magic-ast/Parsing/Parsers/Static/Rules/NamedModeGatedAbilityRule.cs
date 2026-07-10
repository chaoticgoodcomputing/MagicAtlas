namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "• [Name] — [ability]" — one option-labelled ability line of the Believe/Doubt
/// named-mode family (Phenomenon Investigators). The line is an ordinary triggered
/// ability that FUNCTIONS only when the printed option name was the one chosen by the
/// as-enters binder (<see cref="ChooseNamedOptionOnEntryRule"/> /
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseNamedOptionEffect"/>). This rule peels
/// the "• [Name] — " label, recovers the underlying trigger by re-parsing the ability
/// body through <see cref="OracleParser"/> (reusing the existing trigger recognisers —
/// e.g. the "a nontoken creature you control dies" and end-step triggers), builds the
/// mode-specific effects, and couples the whole ability to its option via a
/// <see cref="ChosenModeCondition"/> gate carried in
/// <see cref="TriggeredAbility.InterveningIf"/> (CR 603.4 intervening-if shape;
/// CR 700.2 modal; CR 614.12 choose-on-enter).
///
/// <para>
/// Anchored to a leading bullet (U+2022) + a single Capitalised mode word + an em-dash
/// (U+2014): only the unconsumed named-mode option lines reach the static parser in this
/// shape (a "Choose one —" modal card's bullets are consumed into a modal header by the
/// clause splitter and never surface here), so there is no sibling to collide with. The
/// mode-specific effect construction is self-contained (it constructs effects directly
/// rather than delegating to the trigger-effect pipeline, whose sentence-bundle splitter
/// would flatten the "you may … If you do, …" idiom), so this rule affects no other card.
/// </para>
/// </summary>
[StaticRule(Priority = 1003)]
public sealed class NamedModeGatedAbilityRule : IStaticRule
{
  // Lazy so the OracleParser (and its parser registry, which discovers THIS rule) is not
  // constructed at type-load — mirrors ModalAbilityParser's lazy sub-parser. The re-parsed
  // body never contains another "• [Name] —" line, so there is no parse recursion.
  private static readonly Lazy<OracleParser> _bodyParser = new(() => new OracleParser());

  private static readonly Regex _labelPattern = new(
    "^\\s*\\u2022\\s*(?<mode>[A-Z][A-Za-z]+)\\s*\\u2014\\s*(?<body>.+)$",
    RegexOptions.Compiled | RegexOptions.Singleline
  );

  // "create a [P]/[T] [color] [Subtype] enchantment creature token" — the Believe token.
  private static readonly Regex _enchantmentCreatureToken = new(
    @"\bcreate\s+(?:a|an|one)\s+(?<p>\d+)/(?<t>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<sub>[A-Z][A-Za-z]+)\s+enchantment\s+creature\s+tokens?\.?\s*$",
    RegexOptions.Compiled
  );

  // "you may return a nonland permanent you own to your hand. If you do, draw a card" — the Doubt effect.
  private static readonly Regex _mayReturnNonlandThenDraw = new(
    @"\byou\s+may\s+return\s+a\s+nonland\s+permanent\s+you\s+own\s+to\s+your\s+hand\.\s+If\s+you\s+do,\s+draw\s+a\s+card\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly Dictionary<string, string> _colorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var label = _labelPattern.Match(clause.RawText);
    if (!label.Success)
    {
      return null;
    }

    var mode = label.Groups["mode"].Value;
    var body = label.Groups["body"].Value.Trim();

    // Recover the trigger by re-parsing the label-stripped body through the full pipeline.
    var reparsed = _bodyParser.Value.Parse(body);
    var baseTrigger = reparsed.Output.Abilities.OfType<TriggeredAbility>().FirstOrDefault();
    if (baseTrigger is null)
    {
      return null;
    }

    var effects = BuildModeEffects(body) ?? baseTrigger.Effects;

    return
    [
      baseTrigger with
      {
        InterveningIf = new ChosenModeCondition { Mode = mode },
        Effects = effects,
      },
    ];
  }

  /// <summary>
  /// Builds the mode-specific effects directly from the ability body. Returns null when the
  /// body is not one of the recognised named-mode effect shapes, leaving the caller to reuse
  /// the re-parsed body's effects.
  /// </summary>
  private static IReadOnlyList<Effect>? BuildModeEffects(string body)
  {
    var token = _enchantmentCreatureToken.Match(body);
    if (token.Success && _colorMap.TryGetValue(token.Groups["color"].Value, out var colorCode))
    {
      return
      [
        new CreateTokenEffect
        {
          Count = LiteralQuantity.Of(1),
          Token = new AST.Effects.TokenDefinition
          {
            Power = token.Groups["p"].Value,
            Toughness = token.Groups["t"].Value,
            Colors = [colorCode],
            Types = ["enchantment", "creature"],
            Subtypes = [token.Groups["sub"].Value],
            IsCopy = false,
          },
          Player = ObjectReference.You(),
        },
      ];
    }

    if (_mayReturnNonlandThenDraw.IsMatch(body))
    {
      return
      [
        new OptionalEffect
        {
          Inner = new ReturnToHandEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Any,
              Filter = new ObjectFilter
              {
                CardTypes = ["permanent"],
                ExcludedCardTypes = ["land"],
                Owner = ControllerFilter.You,
              },
            },
          },
          IfYouDo = new DrawCardsEffect
          {
            Count = LiteralQuantity.Of(1),
            Player = ObjectReference.You(),
          },
        },
      ];
    }

    return null;
  }
}
