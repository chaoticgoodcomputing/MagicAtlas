using Flowthru.Data.Schema;

namespace MagicAtlas.Rules.Data._03_Primary.Schemas;

/// <summary>
/// Hierarchical structure of MTG comprehensive rules.
/// </summary>
[FlowthruSchema]
public partial record RulesStructure
{
  [SerializedLabel("sections")]
  public List<MajorSection> Sections { get; init; } = new();
}

/// <summary>
/// Major section (e.g., "1. Game Concepts").
/// </summary>
[FlowthruSchema]
public partial record MajorSection
{
  [SerializedLabel("number")]
  public int Number { get; init; }

  [SerializedLabel("title")]
  public string Title { get; init; } = null!;

  [SerializedLabel("subsections")]
  public List<Subsection> Subsections { get; init; } = new();
}

/// <summary>
/// Subsection (e.g., "100. General").
/// </summary>
[FlowthruSchema]
public partial record Subsection
{
  [SerializedLabel("number")]
  public int Number { get; init; }

  [SerializedLabel("title")]
  public string Title { get; init; } = null!;

  [SerializedLabel("rules")]
  public List<Rule> Rules { get; init; } = new();
}

/// <summary>
/// Individual rule (e.g., "100.1 These Magic rules apply...").
/// </summary>
[FlowthruSchema]
public partial record Rule
{
  [SerializedLabel("number")]
  public string Number { get; init; } = null!;

  [SerializedLabel("text")]
  public string Text { get; init; } = null!;

  [SerializedLabel("subrules")]
  public List<Subrule> Subrules { get; init; } = new();
}

/// <summary>
/// Subrule (e.g., "100.1a A two-player game...").
/// </summary>
[FlowthruSchema]
public partial record Subrule
{
  [SerializedLabel("letter")]
  public string Letter { get; init; } = null!;

  [SerializedLabel("text")]
  public string Text { get; init; } = null!;
}
