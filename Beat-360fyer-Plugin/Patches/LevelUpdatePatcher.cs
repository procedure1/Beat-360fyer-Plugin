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
using BeatSaberMarkupLanguage;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Collections;
using static IPA.Logging.Logger;
using System.Xml.Linq;
using System.Threading;

namespace Beat360fyerPlugin.Patches
{

    //Testing Kyle 1413 says trying patching CustomLevelLoader.LoadCustomBeatmapLevelAsync or BeatmapLevelLoader.LoadBeatmapLevelAsync
    //Afaik the game uses these methods to load the map initially so you could try patching them with a postfix containing the logic to add the new difficultybeatmapsets from your setdata prefix
    /*
    [HarmonyPatch(typeof(CustomLevelLoader))]
    [HarmonyPatch("LoadCustomBeatmapLevelAsync")]
    public class Patch
    {
        static void Postfix(ref CustomPreviewBeatmapLevel customPreviewBeatmapLevel, CancellationToken cancellationToken)
        {
            StandardLevelInfoSaveData standardLevelInfoSaveData = customPreviewBeatmapLevel.standardLevelInfoSaveData;
        }
    }
    */

    //BW 4rd item that runs after LevelUpdatePatcher & GameModeHelper & TransitionPatcher
    //This runs after 3rd item automatically
    //This alters the beat map data such as rotation events...
    [HarmonyPatch(typeof(BeatmapDataTransformHelper), "CreateTransformedBeatmapData")]
    public class BeatmapDataTransformHelperPatcher
    {
        static void Postfix(ref IReadonlyBeatmapData __result, IReadonlyBeatmapData beatmapData, IPreviewBeatmapLevel beatmapLevel, GameplayModifiers gameplayModifiers, bool leftHanded, EnvironmentEffectsFilterPreset environmentEffectsFilterPreset, EnvironmentIntensityReductionOptions environmentIntensityReductionOptions, MainSettingsModelSO mainSettingsModel)
        {
            Plugin.Log.Info("BW 00 postfix:");
            int i = 0;
            foreach (PreviewDifficultyBeatmapSet difficultyBeatmapSet in beatmapLevel.previewDifficultyBeatmapSets)
            {
                {
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info("Set index: " + i);
                    Plugin.Log.Info("SerializedName: " + difficultyBeatmapSet.beatmapCharacteristic.serializedName);
                    i++;
                }
            }
            if (beatmapData is CustomBeatmapData customBeatmapData)
            {
                Plugin.Log.Info("BW 0 postfix beatmapLevel.previewDifficultyBeatmapSets: " + beatmapLevel.previewDifficultyBeatmapSets);
                int j = 0;
                foreach (PreviewDifficultyBeatmapSet previewDifficultyBeatmapSet in beatmapLevel.previewDifficultyBeatmapSets)
                {
                    Plugin.Log.Info("Set index: " + j);
                    Plugin.Log.Info("SerializedName: " + previewDifficultyBeatmapSet.beatmapCharacteristic.serializedName);
                    j++;
                }
                Plugin.Log.Info(" ");
                Plugin.Log.Info($"BW 1 postfix beatmapLevel.levelID: {beatmapLevel.levelID}");
                Plugin.Log.Info(" ");
                Plugin.Log.Info($"BW 1 postfix TransitionPatcher.startingGameMode: {TransitionPatcher.startingGameMode}");
              
                //v1.31.0 can't get past this if statement
                if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_360DEGREE_MODE || TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_90DEGREE_MODE)
                {
                    Plugin.Log.Info(" ");
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
    /*
    //https://harmony.pardeike.net/articles/patching-prefix.html
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("HandleBeatmapCharacteristicSegmentedControlControllerDidSelectBeatmapCharacteristic")]
    class PatchTemp
    {
        static void Prefix(BeatmapCharacteristicSegmentedControlController controller, BeatmapCharacteristicSO beatmapCharacteristic)
        {
            Plugin.Log.Info("HandleBeatmapCharacteristicSegmentedControlControllerDidSelectBeatmapCharacteristic: " + beatmapCharacteristic.serializedName);
        }
    }
    */

    //BW 3nd item that runs after LevelUpdatePatcher & GameModeHelper
    //Runs when you click play button
    //BW v1.31.0 Class MenuTransitionsHelper method StartStandardLevel has 1 new item added. After 'ColorScheme overrideColorScheme', 'ColorScheme beatmapOverrideColorScheme' has been added. so i added: typeof(ColorScheme) after typeof(ColorScheme)
    [HarmonyPatch(typeof(MenuTransitionsHelper))]
    [HarmonyPatch("StartStandardLevel", new[] { typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool), typeof(bool), typeof(Action), typeof(Action<DiContainer>), typeof(Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults>), typeof(Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults>) })]
    public class TransitionPatcher
    {
        public static string startingGameMode;
        static void Prefix(string gameMode, ref IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel, OverrideEnvironmentSettings overrideEnvironmentSettings, ColorScheme overrideColorScheme, GameplayModifiers gameplayModifiers, PlayerSpecificSettings playerSpecificSettings, PracticeSettings practiceSettings, string backButtonText, bool useTestNoteCutSoundEffects, bool startPaused, Action beforeSceneSwitchCallback, Action<DiContainer> afterSceneSwitchCallback, Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback, Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults> levelRestartedCallback)
        {
            //BW FIX!!!!  v1.31.0 didn't output the proper gameMode in this log. It should say "Generated360DegreeExpert" like in v1.29 in the serialized name instead of just Expert. and no log statements appear from postfix
            Plugin.Log.Info(" ");
            Plugin.Log.Info($"BW 0 TransitionPatcher - IDifficultyBeatmap contains these sets:");
            int i = 0;
            int Gen360Index = -1;
            foreach (IDifficultyBeatmapSet difficultyBeatmapSet in difficultyBeatmap.level.beatmapLevelData.difficultyBeatmapSets)
            {
                Plugin.Log.Info(" ");
                Plugin.Log.Info("Set index: " + i);
                Plugin.Log.Info("SerializedName: " + difficultyBeatmapSet.beatmapCharacteristic.serializedName);
                Plugin.Log.Info("compoundIdPartName: " + difficultyBeatmapSet.beatmapCharacteristic.compoundIdPartName);
                if (difficultyBeatmapSet.beatmapCharacteristic.serializedName == "Generated360Degree")
                    Gen360Index = i;
                i++;
            }

            Plugin.Log.Info(" ");
            Plugin.Log.Info("Current Difficulty: " + difficultyBeatmap.difficulty);
            Plugin.Log.Info(" ");

            string currentCharacteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            string correctCharacteristic = FindWhichIconWasSelectedByUser.LastSelectedSO.serializedName;


            //AI Chatgpt to swap out the wrong IDifficultyBeatmap -----------------------------------------------
            //----------------------------------------------------------------------------------------------------
            if (currentCharacteristic != correctCharacteristic && correctCharacteristic == "Generated360Degree")
            {
                BeatmapDifficulty targetDifficulty = difficultyBeatmap.difficulty;

                CustomDifficultyBeatmap gen360DifficultyBeatmap = null;

                foreach (IDifficultyBeatmapSet myDifficultyBeatmapSet in difficultyBeatmap.level.beatmapLevelData.difficultyBeatmapSets)
                {
                    Plugin.Log.Info($"Found difficultyBeatmapSet with serializedName: {myDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");

                    if (myDifficultyBeatmapSet.beatmapCharacteristic.serializedName == correctCharacteristic)
                    {
                        
                        Plugin.Log.Info($"Working on: {myDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");

                        foreach (CustomDifficultyBeatmap myBeatmap in myDifficultyBeatmapSet.difficultyBeatmaps)
                        {
                            Plugin.Log.Info($"   Found These difficulties: {myBeatmap.difficulty}");
                            if (myBeatmap.difficulty == targetDifficulty)
                            {
                                FieldHelper.Set(myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic, "_serializedName", correctCharacteristic);
                                FieldHelper.Set(myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic, "_compoundIdPartName", correctCharacteristic);
                                Plugin.Log.Info($"myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName: {myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");

                                Plugin.Log.Info($"myBeatmap.level.levelID: {myBeatmap.level.levelID}");
                                Plugin.Log.Info($"myBeatmap.difficulty.SerializedName(): {myBeatmap.difficulty.SerializedName()}");
                                Plugin.Log.Info($"myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.compoundIdPartName: {myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.compoundIdPartName}");
                                Plugin.Log.Info($"   Working on: {myBeatmap.difficulty}");
                                Plugin.Log.Info($"       IDifficultyBeatmap Fullname: ({myBeatmap.GetType().FullName}) SerializedName: {myBeatmap.SerializedName()} Gamemode: {gameMode} Difficulty: {myBeatmap.difficulty} SongName: {myBeatmap.level.songName} SerializedName: {myBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");

                                gen360DifficultyBeatmap = myBeatmap;
                                break; // Exit the loop once you find the desired difficulty level
                            }
                        }

                        if (gen360DifficultyBeatmap != null)
                        {
                            // Now you have the correct IDifficultyBeatmap with the specified characteristic and difficulty
                            // You can use gen360DifficultyBeatmap for further operations
                            break; // Exit the outer loop since you've found the desired beatmap
                        }
                        else
                        {
                            Plugin.Log.Info("Desired difficulty not found in the specified characteristic.");
                        }
                    }
                }

                if (gen360DifficultyBeatmap == null)
                {
                    Plugin.Log.Info("BW 1 TransitionPatcher - Desired characteristic not found - unable to change to correct IDifficultyBeatmap.");
                }
                else
                {
                    difficultyBeatmap = gen360DifficultyBeatmap;
                    Plugin.Log.Info($"BW 2 TransitionPatcher - CHANGED TO CORRECT IDifficultyBeatmap Fullname: ({gen360DifficultyBeatmap.GetType().FullName}) SerializedName: {gen360DifficultyBeatmap.SerializedName()} Gamemode: {gameMode} Difficulty: {gen360DifficultyBeatmap.difficulty} SongName: {gen360DifficultyBeatmap.level.songName} SerializedName: {gen360DifficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");


                }
            }
            else
            {
                //This will happen with Built-in levels like $100 Bills
                Plugin.Log.Info("BW 3 TransitionPatcher - Did not need to change the IDifficultyBeatmap.");
            }

            //-----------------------------------------------------------------------------------

            Plugin.Log.Info(" ");
            Plugin.Log.Info($"BW 4 TransitionPatcher Starting Fullname: ({difficultyBeatmap.GetType().FullName}) SerializedName: {difficultyBeatmap.SerializedName()} Gamemode: {gameMode} Difficulty: {difficultyBeatmap.difficulty} SongName: {difficultyBeatmap.level.songName} SerializedName: {difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName}");

            //Sets the variable to the name of the map being started (Standard, Generated360Degree, etc)          
            startingGameMode = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            //difficultyBeatmap = difficultyBeatmap.level.beatmapLevelData.difficultyBeatmapSets[1].difficultyBeatmaps[0];
            //difficultyBeatmap = difficultyBeatmap.parentDifficultyBeatmapSet.difficultyBeatmaps[1];

            Plugin.Log.Info(" ");
            Plugin.Log.Info($"BW 5 TransitionPatcher startingGameMode: {startingGameMode} (should be Generated360Degree I think)");
            Plugin.Log.Info(" ");
        }
    }
    //BW 2st item that runs.
    //Runs after user selects a level (Standard, OneSaber, Generated 360, etc
    [HarmonyPatch(typeof(StandardLevelDetailView), "HandleBeatmapCharacteristicSegmentedControlControllerDidSelectBeatmapCharacteristic")]
    public class FindWhichIconWasSelectedByUser
    {
        public static BeatmapCharacteristicSO LastSelectedSO;
        static void Prefix(BeatmapCharacteristicSO beatmapCharacteristic)
        {
            LastSelectedSO = beatmapCharacteristic;

            Plugin.Log.Info($"BW 1 FindWhichIconWasSelectedByUser prefix: {LastSelectedSO.serializedName}");
        }
    }
    //BW 1st item that runs. This calls GameModeHelper.cs next.
    //Called when a song's been selected and its levels are displayed in the right menu
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("SetContent")]
    public class LevelUpdatePatcher
    {
        //public static List<IDifficultyBeatmapSet> myset;//bw i think
        static void Prefix(StandardLevelDetailView __instance, IBeatmapLevel level, BeatmapDifficulty defaultDifficulty, BeatmapCharacteristicSO defaultBeatmapCharacteristic, PlayerData playerData)//level actually is of the class CustomBeatmapLevel which impliments interface IBeatmapLevel
        {
            //This is an empty set
            List<BeatmapCharacteristicSO> toGenerate = new List<BeatmapCharacteristicSO>();
            Plugin.Log.Info(" ");
            Plugin.Log.Info($"BW 1 LevelUpdatePatcher toGenerate Count: {toGenerate.Count}");

            //Plugin.Log.Info("__instance.selectedDifficultyBeatmap: " + __instance.selectedDifficultyBeatmap);

            //__instance.SetContent(level, defaultDifficulty, defaultBeatmapCharacteristic, playerData);  

            //these properties get added to the empty set: "icon", "_characteristicNameLocalizationKey", "GEN360","_descriptionLocalizationKey", "Generated 360 mode", "_serializedName", "Generated360Degree" and a few more...
            if (Config.Instance.ShowGenerated360)
                toGenerate.Add(GameModeHelper.GetGenerated360GameMode());
            if (Config.Instance.ShowGenerated90)
                toGenerate.Add(GameModeHelper.GetGenerated90GameMode());

            //BW Printing all elements in the list --------------------------------------------------
            Plugin.Log.Info(" ");
            Plugin.Log.Info("BW 2 LevelUpdatePatcher Elements in the toGenerate list:");
            Plugin.Log.Info("");
            foreach (BeatmapCharacteristicSO element in toGenerate)
            {
                Plugin.Log.Info(" ");
                Plugin.Log.Info($"Properties of BeatmapCharacteristicSO element:---------------------------");
                Plugin.Log.Info(" ");
                Type elementType = element.GetType();
                foreach (FieldInfo field in elementType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    object value = field.GetValue(element);
                    Plugin.Log.Info($"{field.Name}: {value}");
                }
                Plugin.Log.Info(" "); // Add an empty line between elements
            }

            //This initializes the 'sets' list with the difficultyBeatmapSets of the chosen song
            List<IDifficultyBeatmapSet> sets = new List<IDifficultyBeatmapSet>(level.beatmapLevelData.difficultyBeatmapSets);

            //BW Logging the properties of each element in the sets list --------------------------------------------
            foreach (IDifficultyBeatmapSet set in sets)
            {
                Plugin.Log.Info(" ");
                Plugin.Log.Info($"BW 3 LevelUpdatePatcher Properties of 'sets' IDifficultyBeatmapSet:---------------------------");
                Plugin.Log.Info(" ");   
                Plugin.Log.Info($"'Set' serialized name: {set.beatmapCharacteristic.serializedName}");
                Plugin.Log.Info(" ");

                Type setElementType = set.GetType();
                PropertyInfo[] setProperties = setElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo setProperty in setProperties)
                {
                    object setPropertyValue = setProperty.GetValue(set);
                    Plugin.Log.Info($"{setProperty.Name}: {setPropertyValue}");

                    // Now iterate through properties of setPropertyValue and display them
                    PropertyInfo[] subProperties = setPropertyValue.GetType().GetProperties();
                    foreach (PropertyInfo subProperty in subProperties)
                    {
                        object subPropertyValue = subProperty.GetValue(setPropertyValue);

                        if (subPropertyValue is IEnumerable enumerable && !(subPropertyValue is string))
                        {
                            // Handle the case where subPropertyValue is a list or collection
                            int index = 0;
                            foreach (object item in enumerable)
                            {
                                Plugin.Log.Info($"    {subProperty.Name}[{index}]:");
                                PropertyInfo[] itemProperties = item.GetType().GetProperties();
                                foreach (PropertyInfo itemProperty in itemProperties)
                                {
                                    object itemPropertyValue = itemProperty.GetValue(item);
                                    Plugin.Log.Info($"        {itemProperty.Name}: {itemPropertyValue}");
                                }
                                index++;
                                Plugin.Log.Info(" ");
                            }
                            Plugin.Log.Info(" ");
                        }
                        else
                        {
                            // Display regular sub-property
                            Plugin.Log.Info($"    {subProperty.Name}: {subPropertyValue}");
                        }
                    }
                    Plugin.Log.Info(" ");
                }
                Plugin.Log.Info(" ");
            }
                /*foreach (IDifficultyBeatmapSet set in sets)
                {
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"BW 3 LevelUpdatePatcher Properties of 'sets' IDifficultyBeatmapSet:---------------------------");
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"'Set' serialized name: {set.beatmapCharacteristic.serializedName}");
                    Plugin.Log.Info(" ");

                    Type setElementType = set.GetType();
                    PropertyInfo[] setProperties = setElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (PropertyInfo setProperty in setProperties)
                    {
                        object setPropertyValue = setProperty.GetValue(set);
                        Plugin.Log.Info($"{setProperty.Name}: {setPropertyValue}");
                    }
                    Plugin.Log.Info("");


                    foreach (IDifficultyBeatmap difficultyBeatmap in set.difficultyBeatmaps)
                    {
                        Plugin.Log.Info(" ");
                        Plugin.Log.Info($"Properties of IDifficultyBeatmap:");
                        Plugin.Log.Info(" ");
                        Type difficultyBeatmapType = difficultyBeatmap.GetType();
                        PropertyInfo[] difficultyBeatmapProperties = difficultyBeatmapType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        foreach (PropertyInfo difficultyBeatmapProperty in difficultyBeatmapProperties)
                        {
                            object difficultyBeatmapPropertyValue = difficultyBeatmapProperty.GetValue(difficultyBeatmap);
                            Plugin.Log.Info($"{difficultyBeatmapProperty.Name}: {difficultyBeatmapPropertyValue}");
                        }
                        Plugin.Log.Info("");
                    }
                    Plugin.Log.Info("");
                }

                Plugin.Log.Info($"BW 4 LevelUpdatePatcher toGenerate Count: {toGenerate.Count}");
                */
                //BW end -----------------------------------------------------------------------------------

                // Generate each custom gamemode
                foreach (BeatmapCharacteristicSO customGameMode in toGenerate)
            {
                if (level.beatmapLevelData.difficultyBeatmapSets.Any((e) => e.beatmapCharacteristic.serializedName == GameModeHelper.GENERATED_360DEGREE_MODE))
                {
                    // Already added the generated gamemode
                    continue;
                }

                //BW had to add this since BSML can't use 90Degree since it has numbers in the beginning if i recall correctly
                string basedOn;
                if (Config.Instance.BasedOn.ToString() == "NinetyDegree")
                {
                    basedOn = "90Degree";
                }
                else
                {
                    basedOn = Config.Instance.BasedOn.ToString();
                }

                //searches through the difficultyBeatmapSets collection within level.beatmapLevelData for the first IDifficultyBeatmapSet object that has a beatmapCharacteristic with a serializedName equal to "Standard" or other basedOn string.
                //If such an object is found, it is assigned to the basedOnGameMode variable. If not, basedOnGameMode will be null.
                //basedOnGameMode will now hold the Standard level IDifficultyBeatmapSet of the current song.
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

                //This is true for built-in songs
                if (basedOnGameMode.difficultyBeatmaps[0] is BeatmapLevelSO.DifficultyBeatmap)
                {
                    BeatmapLevelSO.DifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Select((bm) => new BeatmapLevelSO.DifficultyBeatmap(bm.level, bm.difficulty, bm.difficultyRank, bm.noteJumpMovementSpeed, bm.noteJumpStartBeatOffset, bm.environmentNameIdx, FieldHelper.Get<BeatmapDataSO>(bm, "_beatmapData"))).ToArray();//BW v1.31.0 added bm.environmentNameIdx for new parameter environmentNameIdx
                    newSet = new BeatmapLevelSO.DifficultyBeatmapSet(customGameMode, difficultyBeatmaps);
                    foreach (BeatmapLevelSO.DifficultyBeatmap dbm in difficultyBeatmaps)
                    {
                        dbm.SetParents(level, newSet);
                        Plugin.Log.Info($"BW 5 LevelUpdatePatcher {difficultyBeatmaps.ToString()}");
                    }
                }
                //This is true for custom songs
                //checks if the first difficulty beatmap in the difficultyBeatmaps collection of basedOnGameMode is of type CustomDifficultyBeatmap or a subclass of it.
                else if (basedOnGameMode.difficultyBeatmaps[0] is CustomDifficultyBeatmap)
                {

                    //creates a new instance of the CustomDifficultyBeatmapSet class and initializing it with data based on the customGameMode object
                    CustomDifficultyBeatmapSet customSet = new CustomDifficultyBeatmapSet(customGameMode);

                    //retrieves the difficultyBeatmaps collection from the basedOnGameMode object, which seems to be an IDifficultyBeatmapSet
                    //Cast is used to convert the elements in the difficultyBeatmaps collection to CustomDifficultyBeatmap objects.
                    CustomDifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Cast<CustomDifficultyBeatmap>().Select((cbm) => new CustomDifficultyBeatmap(
                        cbm.level,
                        cbm.parentDifficultyBeatmapSet,//BW v1.31.0 added
                        cbm.difficulty, 
                        cbm.difficultyRank, 
                        cbm.noteJumpMovementSpeed, 
                        cbm.noteJumpStartBeatOffset, 
                        cbm.beatsPerMinute,//BW v1.31.0 was in the wrong order
                        cbm.beatmapColorSchemeIdx,//BW v1.31.0 added
                        cbm.environmentNameIdx,//BW v1.31.0 added
                        cbm.beatmapSaveData, 
                        cbm.beatmapDataBasicInfo)).ToArray();

                    //allows you to set the custom difficulty beatmaps for a CustomDifficultyBeatmapSet instance.
                    //effectively populating the instance with the provided difficulty beatmaps.
                    customSet.SetCustomDifficultyBeatmaps(difficultyBeatmaps);
                    newSet = customSet;


                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"BW 6 LevelUpdatePatcher {difficultyBeatmaps.ToString()}");
                    Plugin.Log.Info("");
                    Plugin.Log.Info("CustomDifficultyBeatmapSet:");
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"customGameMode: Name: {customSet.beatmapCharacteristic.name} SerializedName: {customSet.beatmapCharacteristic.serializedName}");
                    Plugin.Log.Info(" ");
                    foreach (CustomDifficultyBeatmap cbm in difficultyBeatmaps)// Logging the properties of each CustomDifficultyBeatmap in the array
                    {
                        Plugin.Log.Info("CustomDifficultyBeatmap:");
                        Plugin.Log.Info(" ");
                        Plugin.Log.Info($"level: {cbm.level}");
                        Plugin.Log.Info($"parentDifficultyBeatmapSet: {cbm.parentDifficultyBeatmapSet}");
                        Plugin.Log.Info($"difficulty: {cbm.difficulty}");
                        Plugin.Log.Info($"difficultyRank: {cbm.difficultyRank}");
                        Plugin.Log.Info($"noteJumpMovementSpeed: {cbm.noteJumpMovementSpeed}");
                        Plugin.Log.Info($"noteJumpStartBeatOffset: {cbm.noteJumpStartBeatOffset}");
                        Plugin.Log.Info($"beatsPerMinute: {cbm.beatsPerMinute}");
                        Plugin.Log.Info($"beatmapColorSchemeIdx: {cbm.beatmapColorSchemeIdx}");
                        Plugin.Log.Info($"environmentNameIdx: {cbm.environmentNameIdx}");
                        Plugin.Log.Info($"beatmapSaveData: {cbm.beatmapSaveData}");
                        Plugin.Log.Info($"beatmapDataBasicInfo: {cbm.beatmapDataBasicInfo}");
                    }
                    Plugin.Log.Info(" ");
                }
                else
                {
                    Plugin.Log.Error($"Cannot create generated mode for {basedOnGameMode.difficultyBeatmaps[0]}");
                    continue;
                }

                //IDifficultyBeatmapSet 'sets' was populated with sets of the chosen song. Now we are adding the custom content to it. will have standard and generated 360
                //This is working. v1.29 and v1.31 seem identical except beatmapColorSchemeIdx: 0 and environmentNameIdx: 0 has been added to every difficulty level in v1.31
                sets.Add(newSet);


                //myset = sets;//bw

                // Logging the properties of each element in the sets list
                foreach (IDifficultyBeatmapSet set in sets)
                {
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"BW 7 LevelUpdatePatcher Properties of 'sets' after 'newset' added - IDifficultyBeatmapSet:---------------------------");
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info($"'Set' serialized name: {set.beatmapCharacteristic.serializedName}");
                    Plugin.Log.Info(" ");
                    Type setElementType = set.GetType();
                    PropertyInfo[] setProperties = setElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (PropertyInfo setProperty in setProperties)
                    {
                        object setPropertyValue = setProperty.GetValue(set);
                        Plugin.Log.Info($"{setProperty.Name}: {setPropertyValue}");

                        // Now iterate through properties of setPropertyValue and display them
                        PropertyInfo[] subProperties = setPropertyValue.GetType().GetProperties();
                        foreach (PropertyInfo subProperty in subProperties)
                        {
                            object subPropertyValue = subProperty.GetValue(setPropertyValue);

                            if (subPropertyValue is IEnumerable enumerable && !(subPropertyValue is string))
                            {
                                // Handle the case where subPropertyValue is a list or collection
                                int index = 0;
                                foreach (object item in enumerable)
                                {
                                    Plugin.Log.Info($"    {subProperty.Name}[{index}]:");
                                    PropertyInfo[] itemProperties = item.GetType().GetProperties();
                                    foreach (PropertyInfo itemProperty in itemProperties)
                                    {
                                        object itemPropertyValue = itemProperty.GetValue(item);
                                        Plugin.Log.Info($"        {itemProperty.Name}: {itemPropertyValue}");
                                    }
                                    index++;
                                    Plugin.Log.Info(" ");
                                }
                                Plugin.Log.Info(" ");
                            }
                            else
                            {
                                // Display regular sub-property
                                Plugin.Log.Info($"    {subProperty.Name}: {subPropertyValue}");
                            }
                        }
                        Plugin.Log.Info(" ");
                    }
                    Plugin.Log.Info(" ");
                }
            }

            /*
            //log level and its children before its altered:
            void LogPropertiesRecursively(object obj, int depth = 0)
            {
                Type objType = obj.GetType();
                PropertyInfo[] properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo property in properties)
                {
                    object propertyValue = property.GetValue(obj);

                    string indentation = new string(' ', depth * 4);
                    Plugin.Log.Info($"{indentation}{property.Name}: {propertyValue}");

                    if (propertyValue != null)
                    {
                        if (propertyValue is IEnumerable<object> enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                LogPropertiesRecursively(item, depth + 1);
                            }
                        }
                        else
                        {
                            LogPropertiesRecursively(propertyValue, depth + 1);
                        }
                    }
                }
            }

            // Assuming 'level' is already defined
            LogPropertiesRecursively(level);
            */

            Plugin.Log.Info("");
            Plugin.Log.Info("BW 8 LevelUpdatePatcher Properties of IBeatmapLevel ORIGINAL BEFORE update:");
            Plugin.Log.Info("");

            if (level is CustomBeatmapLevel customLevelPre)
            {
                BWCustomLevelLogger(customLevelPre);
            }
            else
            {
                BWBuiltInLevelLogger(level);
            }

            //TEST - This works to replace Standard with Gen360 - comment out to return to normal - no other changes necessary !!!!!!!!!!!!
            void testChangeStandardsCharacteristicsInto360Directly()
            {
                Texture2D tex = new Texture2D(50, 50);
                Sprite icon = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                FieldHelper.Set(defaultBeatmapCharacteristic, "_icon", icon);// GameModeHelper.GetDefault360Mode().icon);
                FieldHelper.Set(defaultBeatmapCharacteristic, "_characteristicNameLocalizationKey", "GEN360");
                FieldHelper.Set(defaultBeatmapCharacteristic, "_descriptionLocalizationKey", "Generated 360 mode");
                FieldHelper.Set(defaultBeatmapCharacteristic, "_serializedName", "Generated360Degree");
                FieldHelper.Set(defaultBeatmapCharacteristic, "_compoundIdPartName", "Generated360Degree"); // What is _compoundIdPartName?
                FieldHelper.Set(defaultBeatmapCharacteristic, "_sortingOrder", 100);
                FieldHelper.Set(defaultBeatmapCharacteristic, "_containsRotationEvents", true);
                FieldHelper.Set(defaultBeatmapCharacteristic, "_requires360Movement", true);
                FieldHelper.Set(defaultBeatmapCharacteristic, "_numberOfColors", 2);
            }
            //testChangeStandardsCharacteristicsInto360Directly();


            //**************************
            //Update difficultyBeatmapSets - This is where the actual set is altered! adds new List<IDifficultyBeatmapSet> to level.
            //level.beatmapLevelData is being checked to determine if it is an instance of the BeatmapLevelData class. The 'is' keyword is used for this type of pattern matching. If the matching is successful, the instance is assigned to the variable 'data', and the code within the if block will execute with data representing the matched instance of BeatmapLevelData.
            if (level.beatmapLevelData is BeatmapLevelData data)
            {
                if (!FieldHelper.Set(data, "_difficultyBeatmapSets", sets.ToArray()))
                {
                    Plugin.Log.Warn("Could not set new difficulty sets");
                    return;
                }

                //TEST Reverse Index order of Sets Gen360 will be 0 and Standard will be 1 --------------------------------------
                // Get the field value using reflection
                /*
                var fieldInfo = data.GetType().GetField("_difficultyBeatmapSets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (fieldInfo != null)
                {
                    var fieldValue = (IEnumerable<IDifficultyBeatmapSet>)fieldInfo.GetValue(data);

                    // Reverse the order of the field value and create a new list
                    var reversedList = fieldValue.Reverse().ToList();

                    // Update the field value using reflection
                    fieldInfo.SetValue(data, reversedList);
                }
                else
                {
                    Plugin.Log.Warn("TEST - Field _difficultyBeatmapSets not found");
                }
                */
            }
            else
            {
                Plugin.Log.Info("Unsupported beatmapLevelData: " + (level.beatmapLevelData?.GetType().FullName ?? "null"));
            }


            Plugin.Log.Info("");
            Plugin.Log.Info("BW 9 LevelUpdatePatcher Properties of IBeatmapLevel AFTER update:");
            Plugin.Log.Info("");
            if (level is CustomBeatmapLevel customLevelPost)
            {
                BWCustomLevelLogger(customLevelPost);
            }
            else
            {
                BWBuiltInLevelLogger(level);
            }

            void BWCustomLevelLogger(CustomBeatmapLevel customLevel)
            {
                //Edge AI version
                Plugin.Log.Info("IBeatmapLevel (actually CustomBeatmapLevel): ");
                Plugin.Log.Info("  songName: " + customLevel.songName);
                Plugin.Log.Info("  levelID: " +  customLevel.levelID);
                Plugin.Log.Info("  GetBeatmapLevelColorScheme(0): " + customLevel.GetBeatmapLevelColorScheme(0));

                CustomPreviewBeatmapLevel customPreviewLevel = (CustomPreviewBeatmapLevel)customLevel;
                Plugin.Log.Info("  CustomPreviewBeatmapLevel:");
                Plugin.Log.Info("    spriteAsyncLoader: " + customPreviewLevel.spriteAsyncLoader);
                Plugin.Log.Info("    standardLevelInfoSaveData: " + customPreviewLevel.standardLevelInfoSaveData);
                Plugin.Log.Info("    customLevelPath: " + customPreviewLevel.customLevelPath);
                Plugin.Log.Info("    songSubName: " + customPreviewLevel.songSubName);
                Plugin.Log.Info("    songAuthorName: " + customPreviewLevel.songAuthorName);
                Plugin.Log.Info("    levelAuthorName: " + customPreviewLevel.levelAuthorName);
                Plugin.Log.Info("    beatsPerMinute: " + customPreviewLevel.beatsPerMinute);
                Plugin.Log.Info("    songTimeOffset: " + customPreviewLevel.songTimeOffset);
                Plugin.Log.Info("    shuffle: " + customPreviewLevel.shuffle);
                Plugin.Log.Info("    shufflePeriod: " + customPreviewLevel.shufflePeriod);
                Plugin.Log.Info("    previewStartTime: " + customPreviewLevel.previewStartTime);
                Plugin.Log.Info("    previewDuration: " + customPreviewLevel.previewDuration);
                Plugin.Log.Info("    environmentInfo: " + customPreviewLevel.environmentInfo);
                //Plugin.Log.Info("      sceneInfo: " + customPreviewLevel.environmentInfo.sceneInfo);
                //Plugin.Log.Info("      environmentName: " + customPreviewLevel.environmentInfo.environmentName);
                //Plugin.Log.Info("      colorScheme: " + customPreviewLevel.environmentInfo.colorScheme);
                //Plugin.Log.Info("      serializedName: " + customPreviewLevel.environmentInfo.serializedName);
                //Plugin.Log.Info("      environmentAssetDirectory: " + customPreviewLevel.environmentInfo.environmentAssetDirectory);
                //Plugin.Log.Info("      environmentType: " + customPreviewLevel.environmentInfo.environmentType);
                //Plugin.Log.Info("      environmentSizeData: " + customPreviewLevel.environmentInfo.environmentSizeData);
                //Plugin.Log.Info("      environmentIntensityReductionOptions: " + customPreviewLevel.environmentInfo.environmentIntensityReductionOptions);
                //Plugin.Log.Info("      environmentKeywords: " + string.Join(", ", customPreviewLevel.environmentInfo.environmentKeywords));
                //Plugin.Log.Info("      lightGroups: " + customPreviewLevel.environmentInfo.lightGroups);
                //Plugin.Log.Info("      defaultEnvironmentEvents: " + customPreviewLevel.environmentInfo.defaultEnvironmentEvents);
                Plugin.Log.Info("    allDirectionsEnvironmentInfo: " + customPreviewLevel.allDirectionsEnvironmentInfo);
                Plugin.Log.Info("    beatmapOverrideColorSchemes: " + customPreviewLevel.beatmapOverrideColorSchemes);
                Plugin.Log.Info("  beatmapLevelData: " + customLevel.beatmapLevelData);

                IBeatmapLevelData beatmapLevelData = customLevel.beatmapLevelData;
                //Plugin.Log.Info("customLevel.beatmapLevelData.audioClip: " + beatmapLevelData.audioClip);
                Plugin.Log.Info("    difficultyBeatmapSets: " + beatmapLevelData.difficultyBeatmapSets);

                int i = 0;
                foreach (IDifficultyBeatmapSet difficultyBeatmapSet in beatmapLevelData.difficultyBeatmapSets)
                {
                    Plugin.Log.Info("      Set index: " + i);

                    BeatmapCharacteristicSO beatmapCharacteristic = difficultyBeatmapSet.beatmapCharacteristic;

                    Plugin.Log.Info("        BeatmapCharacteristicSO:");
                    Plugin.Log.Info("          serializedName: " + beatmapCharacteristic.serializedName);
                    Plugin.Log.Info("          descriptionLocalizationKey: " + beatmapCharacteristic.descriptionLocalizationKey);
                    Plugin.Log.Info("          characteristicNameLocalizationKey: " + beatmapCharacteristic.characteristicNameLocalizationKey);
                    Plugin.Log.Info("          compoundIdPartName: " + beatmapCharacteristic.compoundIdPartName);
                    Plugin.Log.Info("          icon: " + beatmapCharacteristic.icon);
                    Plugin.Log.Info("          sortingOrder: " + beatmapCharacteristic.sortingOrder);
                    Plugin.Log.Info("          containsRotationEvents: " + beatmapCharacteristic.containsRotationEvents);
                    Plugin.Log.Info("          requires360Movement: " + beatmapCharacteristic.requires360Movement);
                    Plugin.Log.Info("          numberOfColors: " + beatmapCharacteristic.numberOfColors);
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info("        IDifficultyBeatmap:");

                    EnvironmentInfoSO environmentInfo = customPreviewLevel.environmentInfo;

                    int j = 0;
                    foreach (IDifficultyBeatmap difficultyBeatmap in difficultyBeatmapSet.difficultyBeatmaps)
                    {
                        Plugin.Log.Info("          Index: " + j);

                        Plugin.Log.Info("            level: " + difficultyBeatmap.level);
                        Plugin.Log.Info("            parentDifficultyBeatmapSet: " + difficultyBeatmap.parentDifficultyBeatmapSet);
                        Plugin.Log.Info("            difficulty: " + difficultyBeatmap.difficulty);
                        Plugin.Log.Info("            difficultyRank: " + difficultyBeatmap.difficultyRank);
                        Plugin.Log.Info("            noteJumpMovementSpeed: " + difficultyBeatmap.noteJumpMovementSpeed);
                        Plugin.Log.Info("            noteJumpStartBeatOffset: " + difficultyBeatmap.noteJumpStartBeatOffset);
                        Plugin.Log.Info("            environmentNameIdx: " + difficultyBeatmap.environmentNameIdx);//v1.31 added
                        Plugin.Log.Info(" ");
                        j++;
                    }
                    Plugin.Log.Info(" ");
                    i++;
                }
            }
            void BWBuiltInLevelLogger(IBeatmapLevel builtInLevel)
            {
                //Edge AI version
                Plugin.Log.Info("IBeatmapLevel: ");
                Plugin.Log.Info("  songName: " + builtInLevel.songName);
                Plugin.Log.Info("  levelID: " + builtInLevel.levelID);
                Plugin.Log.Info("  songSubName: " + builtInLevel.songSubName);
                Plugin.Log.Info("  songAuthorName: " + builtInLevel.songAuthorName);
                Plugin.Log.Info("  levelAuthorName: " + builtInLevel.levelAuthorName);
                Plugin.Log.Info("  beatsPerMinute: " + builtInLevel.beatsPerMinute);
                Plugin.Log.Info("  songTimeOffset: " + builtInLevel.songTimeOffset);
                Plugin.Log.Info("  shuffle: " + builtInLevel.shuffle);
                Plugin.Log.Info("  shufflePeriod: " + builtInLevel.shufflePeriod);
                Plugin.Log.Info("  previewStartTime: " + builtInLevel.previewStartTime);
                Plugin.Log.Info("  previewDuration: " + builtInLevel.previewDuration);
                Plugin.Log.Info("  environmentInfo: " + builtInLevel.environmentInfo);
                //Plugin.Log.Info("      sceneInfo: " + customPreviewLevel.environmentInfo.sceneInfo);
                //Plugin.Log.Info("      environmentName: " + customPreviewLevel.environmentInfo.environmentName);
                //Plugin.Log.Info("      colorScheme: " + customPreviewLevel.environmentInfo.colorScheme);
                //Plugin.Log.Info("      serializedName: " + customPreviewLevel.environmentInfo.serializedName);
                //Plugin.Log.Info("      environmentAssetDirectory: " + customPreviewLevel.environmentInfo.environmentAssetDirectory);
                //Plugin.Log.Info("      environmentType: " + customPreviewLevel.environmentInfo.environmentType);
                //Plugin.Log.Info("      environmentSizeData: " + customPreviewLevel.environmentInfo.environmentSizeData);
                //Plugin.Log.Info("      environmentIntensityReductionOptions: " + customPreviewLevel.environmentInfo.environmentIntensityReductionOptions);
                //Plugin.Log.Info("      environmentKeywords: " + string.Join(", ", customPreviewLevel.environmentInfo.environmentKeywords));
                //Plugin.Log.Info("      lightGroups: " + customPreviewLevel.environmentInfo.lightGroups);
                //Plugin.Log.Info("      defaultEnvironmentEvents: " + customPreviewLevel.environmentInfo.defaultEnvironmentEvents);
                Plugin.Log.Info("    allDirectionsEnvironmentInfo: " + builtInLevel.allDirectionsEnvironmentInfo);
                Plugin.Log.Info("  beatmapLevelData: " + builtInLevel.beatmapLevelData);

                IBeatmapLevelData beatmapLevelData = builtInLevel.beatmapLevelData;
                //Plugin.Log.Info("customLevel.beatmapLevelData.audioClip: " + beatmapLevelData.audioClip);
                Plugin.Log.Info("    difficultyBeatmapSets: " + beatmapLevelData.difficultyBeatmapSets);

                int i = 0;
                foreach (IDifficultyBeatmapSet difficultyBeatmapSet in beatmapLevelData.difficultyBeatmapSets)
                {
                    Plugin.Log.Info("      Set index: " + i);

                    BeatmapCharacteristicSO beatmapCharacteristic = difficultyBeatmapSet.beatmapCharacteristic;

                    Plugin.Log.Info("        BeatmapCharacteristicSO:");
                    Plugin.Log.Info("          serializedName: " + beatmapCharacteristic.serializedName);
                    Plugin.Log.Info("          descriptionLocalizationKey: " + beatmapCharacteristic.descriptionLocalizationKey);
                    Plugin.Log.Info("          characteristicNameLocalizationKey: " + beatmapCharacteristic.characteristicNameLocalizationKey);
                    Plugin.Log.Info("          compoundIdPartName: " + beatmapCharacteristic.compoundIdPartName);
                    Plugin.Log.Info("          icon: " + beatmapCharacteristic.icon);
                    Plugin.Log.Info("          sortingOrder: " + beatmapCharacteristic.sortingOrder);
                    Plugin.Log.Info("          containsRotationEvents: " + beatmapCharacteristic.containsRotationEvents);
                    Plugin.Log.Info("          requires360Movement: " + beatmapCharacteristic.requires360Movement);
                    Plugin.Log.Info("          numberOfColors: " + beatmapCharacteristic.numberOfColors);
                    Plugin.Log.Info(" ");
                    Plugin.Log.Info("        IDifficultyBeatmap:");

                    EnvironmentInfoSO environmentInfo = builtInLevel.environmentInfo;

                    int j = 0;
                    foreach (IDifficultyBeatmap difficultyBeatmap in difficultyBeatmapSet.difficultyBeatmaps)
                    {
                        Plugin.Log.Info("          Index: " + j);

                        Plugin.Log.Info("            level: " + difficultyBeatmap.level);
                        Plugin.Log.Info("            parentDifficultyBeatmapSet: " + difficultyBeatmap.parentDifficultyBeatmapSet);
                        Plugin.Log.Info("            difficulty: " + difficultyBeatmap.difficulty);
                        Plugin.Log.Info("            difficultyRank: " + difficultyBeatmap.difficultyRank);
                        Plugin.Log.Info("            noteJumpMovementSpeed: " + difficultyBeatmap.noteJumpMovementSpeed);
                        Plugin.Log.Info("            noteJumpStartBeatOffset: " + difficultyBeatmap.noteJumpStartBeatOffset);
                        Plugin.Log.Info("            environmentNameIdx: " + difficultyBeatmap.environmentNameIdx);//v1.31 added
                        Plugin.Log.Info(" ");
                        j++;
                    }
                    Plugin.Log.Info(" ");
                    i++;
                }
            }
        }
    }
}