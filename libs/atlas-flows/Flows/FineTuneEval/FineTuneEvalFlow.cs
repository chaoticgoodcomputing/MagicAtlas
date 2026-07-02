using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.FineTuneEval;

/// <summary>
/// Diagnostic flow: encodes the oracle-line corpus AND the training-pair string set under both
/// the base and fine-tuned models, then computes a base-vs-fine-tuned scorecard. Lets you
/// empirically answer "is the fine-tune helping or hurting?" rather than guessing.
/// </summary>
/// <list type="number">
/// <item><b>EmbedOracleTextBase</b> — sibling of OracleEmbedding.EmbedOracleText that runs the
///   same oracle-line corpus through the BASE model. Produces <see cref="Catalog.EncodedTextsBase"/>.</item>
/// <item><b>EmbedTrainingTextsFineTuned</b> — encodes the union of TrainingPairs.anchor/positive/
///   negative strings under the fine-tuned model. Most of these (glossary defs, CR section bodies)
///   are NOT in the oracle-line corpus and so need their own cached encoding.</item>
/// <item><b>EmbedTrainingTextsBase</b> — same training-pair strings, encoded under the base model.</item>
/// <item><b>EvaluateFineTuneHealth</b> — produces <see cref="Catalog.FineTuneHealthMetrics"/>,
///   a long-form scorecard with geometry-tier metrics (corpus-wide pairwise-cosine spread,
///   hubness) and objective-tier metrics (per-training-source triplet margins).</item>
/// </list>
/// <remarks>
/// Independent of the explorer pipeline — run on demand to interrogate fine-tune health,
/// not as part of every pipeline invocation.
/// </remarks>
public static class FineTuneEvalFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("FineTuneEval", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "EmbedOracleTextBase",
        module: "Flows.FineTuneEval.embed_oracle_text_base",
        function: "embed_oracle_text_base",
        input: (catalog.OracleLines, catalog.DefaultEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.EncodedTextsBase,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "EmbedTrainingTextsFineTuned",
        module: "Flows.FineTuneEval.embed_training_texts",
        function: "embed_training_texts_finetuned",
        input: (catalog.TrainingPairs, catalog.FineTunedEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.EncodedTrainingTextsFineTuned,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "EmbedTrainingTextsBase",
        module: "Flows.FineTuneEval.embed_training_texts",
        function: "embed_training_texts_base",
        input: (catalog.TrainingPairs, catalog.DefaultEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.EncodedTrainingTextsBase,
        executor: executor
      );

      // Down-sample the full ~30k-row encoded corpora to a fixed sample size before passing
      // them through the Python step boundary — the eval step's JSON-marshalled inputs would
      // otherwise exceed System.Text.Json's value-length cap.
      pipeline.AddPythonStep(
        label: "SampleEncodedTexts",
        module: "Flows.FineTuneEval.sample_encoded_corpus",
        function: "sample_encoded_texts",
        input: catalog.EncodedTexts,
        output: catalog.EncodedTextsSampled,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "SampleEncodedTextsBase",
        module: "Flows.FineTuneEval.sample_encoded_corpus",
        function: "sample_encoded_texts_base",
        input: catalog.EncodedTextsBase,
        output: catalog.EncodedTextsBaseSampled,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "EvaluateFineTuneHealth",
        module: "Flows.FineTuneEval.evaluate_fine_tune_health",
        function: "evaluate_fine_tune_health",
        input: (
          catalog.TrainingPairs,
          catalog.EncodedTextsSampled,
          catalog.EncodedTextsBaseSampled,
          catalog.EncodedTrainingTextsFineTuned,
          catalog.EncodedTrainingTextsBase
        ),
        output: catalog.FineTuneHealthMetrics,
        executor: executor
      );
    });
  }
}
