using BeatmapSaveDataVersion3;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Zenject;
using static AlphabetScrollInfo;
using CustomJSONData.CustomBeatmap;
using IPA.Config.Data;
using BS_Utils;//BW added to Disable Score submission https://github.com/Kylemc1413/Beat-Saber-Utils 
using BS_Utils.Gameplay;
using SongCore;

namespace Beat360fyerPlugin.Patches
{

    [HarmonyPatch(typeof(BeatmapDataTransformHelper), "CreateTransformedBeatmapData")]
    public class BeatmapDataTransformHelperPatcher
    {
        static void Postfix(ref IReadonlyBeatmapData __result, IReadonlyBeatmapData beatmapData, IPreviewBeatmapLevel beatmapLevel, GameplayModifiers gameplayModifiers, bool leftHanded, EnvironmentEffectsFilterPreset environmentEffectsFilterPreset, EnvironmentIntensityReductionOptions environmentIntensityReductionOptions, MainSettingsModelSO mainSettingsModel)
        {
            /*
            //WORKS! BUt after start playng level - Finds if beat saber has noodle not if noodle is being used by the level. so always disables.
            if (beatmapLevel is CustomPreviewBeatmapLevel)
            {
                var extras = Collections.RetrieveExtraSongData(new string(beatmapLevel.levelID.Skip(13).ToArray()));
                var requirements = extras?._difficulties.SelectMany(difficulty => difficulty.additionalDifficultyData._requirements);


                if (requirements != null && requirements.Any(requirement => requirement == "Noodle Extensions"))
                {
                    Plugin.Log.Info($"BW - {beatmapLevel.songName} - Requires Noodle");
                }
                else
                {
                    Plugin.Log.Info($"BW - {beatmapLevel.songName} - Does NOT require Noodle");
                }

            }
            */
            if (beatmapData is CustomBeatmapData customBeatmapData)
            {

                if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_360DEGREE_MODE || TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_90DEGREE_MODE)
                {
                    Plugin.Log.Info($"Generating rotation events for {TransitionPatcher.startingGameMode}...");

                    Generator360 gen = new Generator360();
                    gen.WallGenerator = Config.Instance.EnableWallGenerator;
                    gen.OnlyOneSaber = Config.Instance.OnlyOneSaber;
                    //gen.RotationAngleMultiplier = Config.Instance.RotationAngleMultiplier;
                    gen.RotationSpeedMultiplier = Config.Instance.RotationSpeedMultiplier;
                    gen.AllowCrouchWalls = Config.Instance.AllowCrouchWalls;
                    gen.AllowLeanWalls = Config.Instance.AllowLeanWalls;
                    gen.LeftHandedOneSaber = Config.Instance.LeftHandedOneSaber;

                    if (Config.Instance.BasedOn != Config.Base.Standard || gen.OnlyOneSaber || gen.PreferredBarDuration != 1.84f || gen.AllowCrouchWalls || gen.AllowLeanWalls)// || gen.RotationAngleMultiplier != 1.0f)
                    {
                        ScoreSubmission.DisableSubmission("360Fyer");
                    }

                    if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_90DEGREE_MODE)
                    {   //BW devided by 2 to make the rotation angle accurate. 90 degrees was 180 degress without this 
                        gen.LimitRotations = (int)((Config.Instance.LimitRotations90 / 360f / 2f) * (24f));// / Config.Instance.RotationAngleMultiplier));//BW this convert the angle into LimitRotation units of 15 degree slices. Need to divide the Multiplier since it causes the angle to change from 15 degrees. this will keep the desired limit to work if a multiplier is added.
                        gen.BottleneckRotations = gen.LimitRotations / 2;
                    }
                    else if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_360DEGREE_MODE)
                    {
                        if (Config.Instance.Wireless360)
                        {
                            gen.LimitRotations = 99999;
                            gen.BottleneckRotations = 99999;
                        }
                        else
                        {   //BW devided by 2 to make the rotation angle accurate. 90 degrees was 180 degress without this 
                            gen.LimitRotations = (int)((Config.Instance.LimitRotations360 / 360f / 2f) * (24f));// / Config.Instance.RotationAngleMultiplier));//BW this convert the angle into LimitRotation units of 15 degree slices. Need to divide the Multiplier since it causes the angle to change from 15 degrees. this will keep the desired limit to work if a multiplier is added.
                            gen.BottleneckRotations = gen.LimitRotations / 2;
                        }
                    }

                    __result = gen.Generate(__result, beatmapLevel.beatsPerMinute);

                }
                
            }
        }
    }

    [HarmonyPatch(typeof(MenuTransitionsHelper))]
    [HarmonyPatch("StartStandardLevel", new[] { typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool), typeof(bool), typeof(Action), typeof(Action<DiContainer>), typeof(Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults>), typeof(Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults>) })]
    public class TransitionPatcher
    {
        public static string startingGameMode;

        static void Prefix(string gameMode, IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel, OverrideEnvironmentSettings overrideEnvironmentSettings, ColorScheme overrideColorScheme, GameplayModifiers gameplayModifiers, PlayerSpecificSettings playerSpecificSettings, PracticeSettings practiceSettings, string backButtonText, bool useTestNoteCutSoundEffects, bool startPaused, Action beforeSceneSwitchCallback, Action<DiContainer> afterSceneSwitchCallback, Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback, Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults> levelRestartedCallback)
        {
            /*
            //WORKS! BUt after start playng level - Finds if beat saber has noodle not if noodle is being used by the level. so always disables.
            if (previewBeatmapLevel is CustomPreviewBeatmapLevel)
            {
                var extras = Collections.RetrieveExtraSongData(new string(previewBeatmapLevel.levelID.Skip(13).ToArray()));
                var requirements = extras?._difficulties.SelectMany(difficulty => difficulty.additionalDifficultyData._requirements);


                if (requirements != null && requirements.Any(requirement => requirement == "Noodle Extensions"))
                {
                    Plugin.Log.Info($"BW - {previewBeatmapLevel.songName} - Requires Noodle");
                }
                else
                {
                    Plugin.Log.Info($"BW - {previewBeatmapLevel.songName} - Does NOT require Noodle");
                }

            }
            */

            Plugin.Log.Info($"Starting ({difficultyBeatmap.GetType().FullName}) {difficultyBeatmap.SerializedName()} {gameMode} {difficultyBeatmap.difficulty} {difficultyBeatmap.level.songName} {difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");
            startingGameMode = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

        }
    }

    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("SetContent")]
    public class LevelUpdatePatcher
    {
        static void Prefix(StandardLevelDetailView __instance, IBeatmapLevel level, BeatmapDifficulty defaultDifficulty, BeatmapCharacteristicSO defaultBeatmapCharacteristic, PlayerData playerData)
        {
            List<BeatmapCharacteristicSO> toGenerate = new List<BeatmapCharacteristicSO>();
            if (Config.Instance.ShowGenerated360)
                toGenerate.Add(GameModeHelper.GetGenerated360GameMode());
            if (Config.Instance.ShowGenerated90)
                toGenerate.Add(GameModeHelper.GetGenerated90GameMode());

            List<IDifficultyBeatmapSet> sets = new List<IDifficultyBeatmapSet>(level.beatmapLevelData.difficultyBeatmapSets);

            // Generate each custom gamemode
            foreach (BeatmapCharacteristicSO customGameMode in toGenerate)
            {
                if (level.beatmapLevelData.difficultyBeatmapSets.Any((e) => e.beatmapCharacteristic.serializedName == GameModeHelper.GENERATED_360DEGREE_MODE))
                {
                    // Already added the generated gamemode
                    continue;
                }

                //BW had to add this since
                string basedOn;
                if (Config.Instance.BasedOn.ToString() == "NinetyDegree")
                {
                    basedOn = "90Degree";
                }
                else
                {
                    basedOn = Config.Instance.BasedOn.ToString();
                }

                IDifficultyBeatmapSet basedOnGameMode = level.beatmapLevelData.difficultyBeatmapSets.FirstOrDefault((e) => e.beatmapCharacteristic.serializedName == basedOn);
                if (basedOnGameMode == null)
                {
                    // Level does not have a standard mode to base its 360 mode on
                    continue;
                }

                //Finds if beat saber has noodle being used by the level. if so skips creating the generated level. works with IPreviewBeatmapLevel as well as 
                if (level is CustomPreviewBeatmapLevel)
                {
                    var extras = Collections.RetrieveExtraSongData(new string(level.levelID.Skip(13).ToArray()));
                    var requirements = extras?._difficulties.SelectMany(difficulty => difficulty.additionalDifficultyData._requirements);


                    if (requirements != null && requirements.Any(requirement => requirement == "Noodle Extensions"))
                    {
                        Plugin.Log.Info($"{level.songName} - Requires Noodle - So skipping 360 Generation");
                        continue;
                    }
                }



                IDifficultyBeatmapSet newSet;
                if (basedOnGameMode.difficultyBeatmaps[0] is BeatmapLevelSO.DifficultyBeatmap)
                {
                    BeatmapLevelSO.DifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Select((bm) => new BeatmapLevelSO.DifficultyBeatmap(bm.level, bm.difficulty, bm.difficultyRank, bm.noteJumpMovementSpeed, bm.noteJumpStartBeatOffset, FieldHelper.Get<BeatmapDataSO>(bm, "_beatmapData"))).ToArray();
                    newSet = new BeatmapLevelSO.DifficultyBeatmapSet(customGameMode, difficultyBeatmaps);
                    foreach (BeatmapLevelSO.DifficultyBeatmap dbm in difficultyBeatmaps)
                    {
                        dbm.SetParents(level, newSet);
                    }
                }
                else if (basedOnGameMode.difficultyBeatmaps[0] is CustomDifficultyBeatmap)
                {
                    CustomDifficultyBeatmapSet customSet = new CustomDifficultyBeatmapSet(customGameMode);
                    CustomDifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Cast<CustomDifficultyBeatmap>().Select((cbm) => new CustomDifficultyBeatmap(cbm.level, customSet, cbm.difficulty, cbm.difficultyRank, cbm.noteJumpMovementSpeed, cbm.noteJumpStartBeatOffset, cbm.beatsPerMinute, cbm.beatmapSaveData, cbm.beatmapDataBasicInfo)).ToArray();
                    customSet.SetCustomDifficultyBeatmaps(difficultyBeatmaps);
                    newSet = customSet;
                }
                else
                {
                    Plugin.Log.Error($"Cannot create generated mode for {basedOnGameMode.difficultyBeatmaps[0]}");
                    continue;
                }

                sets.Add(newSet);
            }

            // Update difficultyBeatmapSets
            if (level.beatmapLevelData is BeatmapLevelData data)
            {
                if (!FieldHelper.Set(data, "_difficultyBeatmapSets", sets.ToArray()))
                {
                    Plugin.Log.Warn("Could not set new difficulty sets");
                    return;
                }
            }
            else
            {
                Plugin.Log.Info("Unsupported beatmapLevelData: " + (level.beatmapLevelData?.GetType().FullName ?? "null"));
            }
        }
    }
}


