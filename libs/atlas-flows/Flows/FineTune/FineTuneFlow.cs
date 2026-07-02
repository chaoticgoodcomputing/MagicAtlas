using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.FineTune.Nodes;

namespace MagicAtlas.Flows.FineTune;

/// <summary>
/// Owns everything needed to produce the fine-tuned embedding model:
/// </summary>
/// <list type="number">
/// <item><b>DownloadBaseModel</b> — populates <c>DefaultEmbeddingModel</c> from HuggingFace.
/// Also feeds the production OracleEmbedding flow's <c>EmbedOracleText</c> step, so this flow
/// must run before that one on a fresh checkout.</item>
/// <item><b>BuildTrainingPairs</b> — turns rules/glossary/oracle text into the training corpus.</item>
/// <item><b>MineHardNegatives</b> — augments each negative-less pair with K base-model-mined
/// hard negatives, turning <see cref="Catalog.TrainingPairs"/> into
/// <see cref="Catalog.TrainingPairsMined"/>. Replaces MNR's random in-batch negative sampling
/// with explicit contrastive triplets (see citations in mine_hard_negatives.py).</item>
/// <item><b>FineTuneEmbeddingModel</b> — trains the MTG-domain model from the mined training
/// corpus and writes <see cref="Catalog.FineTunedEmbeddingModel"/>.</item>
/// </list>
public static class FineTuneFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("FineTune", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "DownloadBaseModel",
        module: "Flows.FineTune.download_base_model",
        function: "download_base_model",
        input: catalog.FineTuneConfig,
        output: catalog.DefaultEmbeddingModel,
        executor: executor
      );

      pipeline.AddStep<IEnumerable<CardCoreData>, IEnumerable<CardOracleText>>(
        label: "ProjectCardOracleText",
        transform: ProjectCardOracleTextNode.Create(),
        inputs: catalog.FilteredCardCoreData,
        outputs: catalog.CardOracleTexts
      );

      pipeline.AddPythonStep(
        label: "BuildTrainingPairs",
        module: "Flows.FineTune.build_training_pairs",
        function: "build_training_pairs",
        input: (
          catalog.GlossaryText,
          catalog.RulesText,
          catalog.CardOracleTexts,
          catalog.FineTuneConfig
        ),
        output: catalog.TrainingPairs,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "MineHardNegatives",
        module: "Flows.FineTune.mine_hard_negatives",
        function: "mine_hard_negatives",
        input: (catalog.TrainingPairs, catalog.DefaultEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.TrainingPairsMined,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "FineTuneEmbeddingModel",
        module: "Flows.FineTune.fine_tune_embedding_model",
        function: "fine_tune_embedding_model",
        input: (catalog.TrainingPairsMined, catalog.FineTuneConfig),
        output: catalog.FineTunedEmbeddingModel,
        executor: executor
      );
    });
  }
}
