using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Flowthru.Step;
using MagicAtlas.Rules.Data._03_Primary.Schemas;

namespace MagicAtlas.Rules.Flows.Ontology.Nodes;

/// <summary>
/// Derives the deterministic MTG type ontology from the structured rules tree. Pure facts pulled
/// from a fixed set of rules — card types (300.1), the permanent partition (110.4), colors
/// (105.1), supertypes (205.4a), and the 205.3 subtype pools — never the rules prose. Output is
/// fully sorted and content-hashed so the same input rules tree yields a byte-identical artifact.
/// </summary>
/// <remarks>
/// Pool → owning card type mapping encodes the rules-judge panel's findings: creature types are
/// shared with kindred (308.1) and spell types span instant/sorcery (205.3k), so those pools map
/// to multiple card types; every other pool is single-card-type. The operator consumes these
/// owner sets to decide overlap soundly (a subtype never derives disjointness across a straddling
/// pool).
/// </remarks>
[FlowthruStep]
public static class BuildTypeOntologyNode
{
  // Pool name (as 205.3 names it) → the card type(s) a member subtype implies.
  private static readonly Dictionary<string, string[]> PoolOwners =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["artifact"] = ["artifact"],
      ["enchantment"] = ["enchantment"],
      ["land"] = ["land"],
      ["planeswalker"] = ["planeswalker"],
      ["spell"] = ["instant", "sorcery"], // 205.3k — shared
      ["creature"] = ["creature", "kindred"], // 205.3m + 308.1 — shared
      ["planar"] = ["plane"],
      ["dungeon"] = ["dungeon"],
      ["battle"] = ["battle"],
    };

  public static Func<RulesStructure, Task<TypeOntology>> Create()
  {
    return async (rules) =>
    {
      var byNumber = new Dictionary<string, Rule>(StringComparer.Ordinal);
      var allTexts = new List<string>();
      foreach (var section in rules.Sections)
        foreach (var subsection in section.Subsections)
          foreach (var rule in subsection.Rules)
          {
            byNumber[rule.Number] = rule;
            allTexts.Add(rule.Text);
            foreach (var sub in rule.Subrules)
              allTexts.Add(sub.Text);
          }

      var cardTypes = Sorted(
        SplitList(Capture(RuleText(byNumber, "300.1"), @"(?i)the card types are ([^.]+)\."))
      );
      var permanentTypes = Sorted(
        SplitList(Capture(RuleText(byNumber, "110.4"), @"(?i)permanent types:\s*([^.]+)\."))
      );
      var colors = Sorted(
        SplitList(
          Capture(RuleText(byNumber, "105.1"), @"(?i)colors in the magic game:\s*([^.]+)\.")
        )
      );
      var supertypes = Sorted(SplitList(CaptureAny(allTexts, @"(?i)the supertypes are ([^.]+)\.")));

      var pools = new List<SubtypePool>();
      var subtypeMap = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
      var noSubtype = new SortedSet<string>(StringComparer.Ordinal);

      if (byNumber.TryGetValue("205.3", out var rule2053))
      {
        foreach (var sub in rule2053.Subrules)
        {
          var text = StripParens(sub.Text);

          // 205.3r: "Phenomenon cards, scheme cards, vanguard cards, and conspiracy cards have no subtypes."
          if (text.Contains("have no subtypes", StringComparison.OrdinalIgnoreCase))
          {
            foreach (Match m in Regex.Matches(text, @"(\w+) cards"))
              noSubtype.Add(m.Groups[1].Value.ToLowerInvariant());
            continue;
          }

          // Pool name tolerates the singular phrasings: "called a battle type",
          // "called a dungeon type" (205.3p/q) alongside the plural "called X types".
          var poolMatch = Regex.Match(text, @"(?i)called (?:an? )?(\w+) types?");
          if (!poolMatch.Success)
            continue;
          var poolName = poolMatch.Groups[1].Value.ToLowerInvariant();

          List<string> members;
          if (poolName == "creature")
          {
            // 205.3m: "...one word long: <list>." plus the lone two-word type "Time Lord".
            members = SplitList(Capture(text, @"(?i)one word long:\s*([^.]+)\."));
            members.AddRange(SplitList(Capture(text, @"(?i)two words long:\s*([^.]+)\.")));
          }
          else
          {
            // Plural enumeration ("The X types are A, B, ..."); fall back to the singular
            // form ("That battle type is Siege", "That dungeon type is Undercity" — 205.3p/q).
            members = SplitList(Capture(text, @"(?i)the \w+ types are ([^.]+)\."));
            if (members.Count == 0)
              members = SplitList(Capture(text, @"(?i)that \w+ type is ([^.]+)\."));
          }

          members = Sorted(members);
          if (members.Count == 0)
            continue;

          var owners = PoolOwners.TryGetValue(poolName, out var o) ? o : [poolName];
          pools.Add(
            new SubtypePool
            {
              Name = poolName,
              CardTypes = Sorted(owners.ToList()),
              RuleNumber = "205.3" + sub.Letter,
              Subtypes = members,
            }
          );

          foreach (var subtype in members)
          {
            if (!subtypeMap.TryGetValue(subtype, out var set))
            {
              set = new SortedSet<string>(StringComparer.Ordinal);
              subtypeMap[subtype] = set;
            }
            foreach (var owner in owners)
              set.Add(owner);
          }
        }
      }

      var permSet = new HashSet<string>(permanentTypes, StringComparer.Ordinal);
      var nonPermanent = Sorted(cardTypes.Where(c => !permSet.Contains(c)).ToList());

      var ontology = new TypeOntology
      {
        CardTypes = cardTypes,
        PermanentTypes = permanentTypes,
        NonPermanentTypes = nonPermanent,
        Supertypes = supertypes,
        Colors = colors,
        NoSubtypeCardTypes = noSubtype.ToList(),
        SubtypePools = pools.OrderBy(p => p.Name, StringComparer.Ordinal).ToList(),
        SubtypeToCardTypes = subtypeMap.ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
      };

      ontology = ontology with { OntologyHash = ComputeHash(ontology) };
      return await Task.FromResult(ontology);
    };
  }

  private static string RuleText(Dictionary<string, Rule> byNumber, string number) =>
    byNumber.TryGetValue(number, out var r) ? StripParens(r.Text) : "";

  private static string Capture(string text, string pattern)
  {
    var m = Regex.Match(text, pattern);
    return m.Success ? m.Groups[1].Value : "";
  }

  private static string CaptureAny(IEnumerable<string> texts, string pattern)
  {
    foreach (var t in texts)
    {
      var m = Regex.Match(StripParens(t), pattern);
      if (m.Success)
        return m.Groups[1].Value;
    }
    return "";
  }

  private static string StripParens(string s) => Regex.Replace(s, @"\([^)]*\)", "");

  private static List<string> SplitList(string s)
  {
    var result = new List<string>();
    foreach (var part in Regex.Split(s, @",|\band\b"))
    {
      var t = part.Trim().Trim('.').Trim();
      if (t.Length > 0)
        result.Add(t);
    }
    return result;
  }

  private static List<string> Sorted(List<string> xs) =>
    xs.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

  private static string ComputeHash(TypeOntology o)
  {
    var sb = new StringBuilder();
    sb.Append("cardTypes=").Append(string.Join(",", o.CardTypes)).Append('\n');
    sb.Append("permanentTypes=").Append(string.Join(",", o.PermanentTypes)).Append('\n');
    sb.Append("nonPermanentTypes=").Append(string.Join(",", o.NonPermanentTypes)).Append('\n');
    sb.Append("supertypes=").Append(string.Join(",", o.Supertypes)).Append('\n');
    sb.Append("colors=").Append(string.Join(",", o.Colors)).Append('\n');
    sb.Append("noSubtypeCardTypes=").Append(string.Join(",", o.NoSubtypeCardTypes)).Append('\n');
    foreach (var p in o.SubtypePools)
      sb.Append("pool:")
        .Append(p.Name)
        .Append('|')
        .Append(string.Join("/", p.CardTypes))
        .Append('|')
        .Append(string.Join(",", p.Subtypes))
        .Append('\n');
    foreach (var kv in o.SubtypeToCardTypes)
      sb.Append("map:").Append(kv.Key).Append('=').Append(string.Join(",", kv.Value)).Append('\n');
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
  }
}
