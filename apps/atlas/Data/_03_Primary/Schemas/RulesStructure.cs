using Flowthru.Abstractions;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Hierarchical structure of MTG comprehensive rules.
/// </summary>
public record RulesStructure : IStructuredSerializable
{
  [SerializedLabel("sections")]
  public List<MajorSection> Sections { get; init; } = new();
}

/// <summary>
/// Major section (e.g., "1. Game Concepts").
/// </summary>
public record MajorSection : IStructuredSerializable
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
public record Subsection : IStructuredSerializable
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
public record Rule : IStructuredSerializable
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
public record Subrule : IStructuredSerializable
{
  [SerializedLabel("letter")]
  public string Letter { get; init; } = null!;

  [SerializedLabel("text")]
  public string Text { get; init; } = null!;
}
