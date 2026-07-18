namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

[StaticRule(Priority = 995)]
public sealed class GrantedAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // Anchors a single clause (no in-line newlines reach this layer — clauses
  // are split before us). Captures the noun-phrase subject and the quoted body
  // verbatim; nested quotes inside the body are unlikely in oracle text and
  // are out of scope for this first cut.
  //
  // Verb is "has" or "have" — oracle text agrees the verb with the subject:
  // singular ("Enchanted creature has", Find the Path's Aura grant) vs. plural
  // ("All Slivers have", Telekinetic Sliver's global tribal grant). Both shapes
  // land on the same GainAbilityEffect node; ClassifyGrantTarget distinguishes
  // the subject.
  //
  // The trailing (?:\s*\([^)]*\))? allows an optional parenthetical reminder
  // that appears after the closing quote on the same oracle text line —
  // e.g. Umbral Mantle's "({Q} is the untap symbol.)" reminder (CR 107.6
  // defines the untap symbol {Q}). The reminder carries no rules semantics
  // and is stripped before the inner body is parsed.
  private static readonly Regex _grantedAbilityPattern = new(
    @"^\s*(?<filter>[^""""]+?)\s+(?:has|have)\s+[""""](?<body>[^""""]+)[""""]\.?(?:\s*\([^)]*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _grantedAbilityPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    // Guard against compound "[subject] gets +X/+X and has \"...\"" buff-plus-grant lines: the
    // non-greedy filter group would swallow the "gets +X/+X and" buff into the subject and DROP it.
    // A pure grant subject ("Equipped creature", "Enchanted creature") never contains "gets"/" and ";
    // such compound lines are owned by the buff+grant rule, so bail here rather than mislabel.
    if (
      filterText.Contains(" gets ", System.StringComparison.OrdinalIgnoreCase)
      || filterText.Contains(" and ", System.StringComparison.OrdinalIgnoreCase)
    )
    {
      return null;
    }

    var target = ClassifyGrantTarget(filterText);
    if (target is null)
    {
      return null;
    }

    // Rebase the quoted body's span onto its REAL absolute offset in the original
    // oracle text: `body` is only a substring of `clause.RawText`, so the naive
    // 0-based span the inner parser would otherwise stamp on the granted ability
    // (and its nested effects/triggers, which are computed relative to whatever
    // SourceSpan.Start we hand it) needs `clause.SourceSpan.Start` PLUS the body's
    // own offset within `clause.RawText` — not `clause.SourceSpan.Start` alone.
    var bodyOffsetInClause = clause.RawText.IndexOf(body, System.StringComparison.Ordinal);
    var bodyAbsoluteStart = clause.SourceSpan.Start + (bodyOffsetInClause >= 0 ? bodyOffsetInClause : 0);

    var innerAbility = TryParseGrantedBody(body, bodyAbsoluteStart);
    if (innerAbility is null)
    {
      // The body's shape isn't yet supported by ActivatedAbilityParser.
      // Surface as a parser miss — the fallback path will record the gap.
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = target,
          GainedAbility = innerAbility,
        }],
      },
    ];
  }

  /// <summary>
  /// Hands the quoted body off to <see cref="ActivatedAbilityParser"/>.
  /// </summary>
  /// <param name="body">The quoted ability text, verbatim.</param>
  /// <param name="bodyAbsoluteStart">
  /// The body's real absolute offset into the original oracle text (NOT 0-based —
  /// <see cref="ActivatedAbilityParser"/> computes every nested effect/cost span off
  /// this clause's <c>SourceSpan.Start</c>, so a wrong basis here silently corrupts
  /// every span the inner parser produces).
  /// </param>
  private Ability? TryParseGrantedBody(string body, int bodyAbsoluteStart)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue
      ? tokenResult.Value
      : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(bodyAbsoluteStart, body.Length),
    };
    var innerClassification = new ClauseClassification
    {
      Kind = AbilityKind.Activated,
      Confidence = 1.0,
    };

    var inner = new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
    return inner;
  }

  /// <summary>
  /// Maps the noun-phrase left of "has" onto an ObjectReference target.
  /// Three shapes are recognized today:
  /// <list type="bullet">
  ///   <item>Aura-vocabulary ("enchanted [type]" / "equipped [type]") collapses to
  ///         <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>; the kind itself
  ///         conveys the relationship, so no filter is emitted.</item>
  ///   <item>"All [Subtype]s" (e.g. <c>All Slivers</c>, <c>All Zombies</c>) — the
  ///         global tribal grant shape (Sliver-lords, anthem-style enchantments).
  ///         Maps to an <see cref="ObjectReferenceKind.Each"/> reference with a
  ///         <see cref="ObjectFilter.Subtypes"/> singleton holding the depluralised
  ///         subtype. The leading capital is the disambiguator: lower-case "all
  ///         creatures" would be a card-type grant (next bullet).</item>
  ///   <item>"[CardType]s you control" / "[CardType]s an opponent controls" — the
  ///         controller-scoped card-type grant (Citanul Hierophants, anthem-style
  ///         lords). Lowercase plural card-type noun followed by a controller
  ///         clause. Maps to an <see cref="ObjectReferenceKind.Each"/> reference
  ///         with the singularised card-type on <see cref="ObjectFilter.CardTypes"/>
  ///         and the matching <see cref="ControllerFilter"/>.</item>
  /// </list>
  /// </summary>
  private static ObjectReference? ClassifyGrantTarget(string filterText)
  {
    var trimmed = filterText.Trim();
    var lower = trimmed.ToLowerInvariant();
    if (lower.StartsWith("enchanted ") || lower.StartsWith("equipped "))
    {
      return new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
    }

    // "White creatures you control" / "Red artifacts an opponent controls" —
    // colour-scoped card-type grant (Resplendent Mentor). Same shape as the
    // controller-scoped card-type branch below, plus a colour adjective that
    // lands on ObjectFilter.Colors. The colour word is capitalised at the
    // start of a clause (oracle convention); the regex matches the colour
    // case-insensitively but the resulting code is normalised to the
    // single-letter colour code on ObjectFilter.Colors. Listed before the
    // bare card-type branch because that pattern is anchored at the
    // card-type noun — a leading colour word would simply fail it, not be
    // misclassified, but ordering keeps the colour-specific branch self-evident.
    var colorTypeMatch = Regex.Match(
      trimmed,
      @"^(?<color>White|Blue|Black|Red|Green)\s+(?<type>creatures|artifacts|enchantments|lands|planeswalkers|permanents)\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$",
      RegexOptions.IgnoreCase
    );
    if (colorTypeMatch.Success)
    {
      var colorName = colorTypeMatch.Groups["color"].Value.ToLowerInvariant();
      var colorCode = colorName switch
      {
        "white" => "W",
        "blue" => "U",
        "black" => "B",
        "red" => "R",
        "green" => "G",
        _ => null,
      };
      if (colorCode is null)
      {
        return null;
      }
      var pluralType = colorTypeMatch.Groups["type"].Value.ToLowerInvariant();
      var singularType = pluralType.EndsWith('s') ? pluralType[..^1] : pluralType;
      var ctrlText = colorTypeMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var colorController = ctrlText.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [singularType],
          Colors = [colorCode],
          Controller = colorController,
        },
      };
    }

    // "Creatures you control" / "Artifacts an opponent controls" — controller-scoped
    // card-type grant. The lower-case plural card-type noun is what distinguishes
    // this from the capitalised-subtype "All [Subtype]s" branch below; the trailing
    // controller clause carries the scope onto ObjectFilter.Controller.
    var controlMatch = Regex.Match(
      trimmed,
      @"^(?<type>Creatures|Artifacts|Enchantments|Lands|Planeswalkers|Permanents)\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$",
      RegexOptions.IgnoreCase
    );
    if (controlMatch.Success)
    {
      var plural = controlMatch.Groups["type"].Value.ToLowerInvariant();
      // Depluralise — oracle plurals here are always simple "-s".
      var singular = plural.EndsWith('s') ? plural[..^1] : plural;
      var ctrl = controlMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var controller = ctrl.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [singular],
          Controller = controller,
        },
      };
    }

    // "Shamans you control" / "Goblins an opponent controls" — controller-scoped
    // tribal grant (Sachi, Daughter of Seshiro). Distinguished from the
    // card-type branch above by a capitalised subtype-plural noun, and from the
    // global "All [Subtype]s" branch below by the trailing controller clause —
    // the controller scope is what's load-bearing for this shape, and we
    // surface it onto ObjectFilter.Controller. No CardTypes is emitted: the
    // subtype carries the type-line constraint implicitly (Rule 205.3), and
    // the gold for Sachi confirms it.
    var tribalControlMatch = Regex.Match(
      trimmed,
      @"^(?<sub>[A-Z][a-z]+)s\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$"
    );
    if (tribalControlMatch.Success)
    {
      var subtype = tribalControlMatch.Groups["sub"].Value;
      var ctrl = tribalControlMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var controller = ctrl.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = controller,
        },
      };
    }

    // "All Slivers" / "All Zombies" — capitalised plural noun after a literal "All ".
    // We match the singular by stripping a trailing "s"; oracle text capitalises
    // creature subtypes, which is what lets us distinguish a subtype grant from a
    // generic "all creatures" grant (different shape, not handled here yet).
    var allMatch = Regex.Match(
      trimmed,
      @"^All\s+(?<sub>[A-Z][a-z]+)s\b\.?$"
    );
    if (allMatch.Success)
    {
      var subtype = allMatch.Groups["sub"].Value;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { Subtypes = [subtype] },
      };
    }
    return null;
  }
}
