using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using static AlphabetScrollInfo;
using CustomJSONData.CustomBeatmap;
using BS_Utils;//BW added to Disable Score submission https://github.com/Kylemc1413/Beat-Saber-Utils 
using BS_Utils.Gameplay;
using SongCore;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static IPA.Logging.Logger;
using static BeatmapObjectSpawnMovementData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace Beat360fyerPlugin.Patches
{
    #region Bright Lasers
    //Taken directly from Technicolor mod - needs no other code except the BSML & Config. without this, rotating lasers are very dull
    [HarmonyPatch("ColorWasSet")]
    [HarmonyPriority(Priority.Low)]
    internal static class BrightLights
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BloomPrePassBackgroundLightWithId), "ColorWasSet")]

        private static void BoostNewColor(ref Color newColor)
        {
            BoostColor(ref newColor);
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BloomPrePassBackgroundColorsGradientTintColorWithLightIds), "ColorWasSet")]
        [HarmonyPatch(typeof(BloomPrePassBackgroundColorsGradientTintColorWithLightId), "ColorWasSet")]
        [HarmonyPatch(typeof(BloomPrePassBackgroundColorsGradientElementWithLightId), "ColorWasSet")]
        [HarmonyPatch(typeof(TubeBloomPrePassLightWithId), "ColorWasSet")]

        private static void BoostColor(ref Color color)
        {
            if (TransitionPatcher.characteristicSerializedName == "Generated360Degree" || TransitionPatcher.characteristicSerializedName == "Generated90Degree" || TransitionPatcher.characteristicSerializedName == "360Degree" || TransitionPatcher.characteristicSerializedName == "90Degree")//only do this for gen 360 or else it will do this for all maps
            {
                if (Config.Instance.BrightLights)
                {
                    color.a *= 2f;// mult;
                }
            }
        }
    }
    #endregion
    #region Big Lasers
    //Used plugin.cs zenject installer in order to access ParametricBoxController() method. it seems to control lasers. but some unknown method calls it. so instead i scaled the gameObject that uses ParametricBoxController()
    public class BigLasers
    {
        public void Big()
        {
            if (TransitionPatcher.characteristicSerializedName == "Generated360Degree" || TransitionPatcher.characteristicSerializedName == "Generated90Degree" || TransitionPatcher.characteristicSerializedName == "360Degree" || TransitionPatcher.characteristicSerializedName == "90Degree")//only do this for gen 360 or else it will do this for all maps
            {
                if (Config.Instance.BigLasers)
                {
                    // Get all ParametricBoxController objects in the scene
                    //Environment>TopLaser>BoxLight, Environment>DownLaser>BoxLight, Environment/RotatingLaser/Pair/BaseR or BaseL/Laser/BoxLight
                    ParametricBoxController[] boxControllers = GameObject.FindObjectsOfType<ParametricBoxController>();
                    // Modify the ParametricBoxController properties of all BoxLights
                    int i = 1;
                    foreach (ParametricBoxController boxController in boxControllers)
                    {
                        //scale gameOject parent
                        Transform parentTransform = boxController.gameObject.transform.parent;
                        Vector3 currentScale = parentTransform.localScale;

                        if (i == 1)//so doesn't repear several times
                            Plugin.Log.Info($"BoxLights Scaled");


                        if (parentTransform.name == "Laser")//Rotating lasers
                        {
                            parentTransform.localScale = new Vector3(currentScale.x * 8, currentScale.y * 1, currentScale.z * 8);
                        }
                        else if (parentTransform.name == "TopLaser")
                            parentTransform.localScale = new Vector3(currentScale.x * 5, currentScale.y * 5, currentScale.z * 5);//y seems to be the length of the long top laser bars
                        else if (i == 4 || i == 5 || i == 10 || i == 11 || i == 12)//These are all DownLasers but I think some misnamed since these particular ones work with the rest of the 6 TopLasers
                            parentTransform.localScale = new Vector3(currentScale.x * 5, currentScale.y * 5, currentScale.z * 5);
                        else
                            parentTransform.localScale = new Vector3(currentScale.x * 2, currentScale.y * 1, currentScale.z * 2);//Don' scale these actual DownLasers to be longer since looks messy
                        i++;

                    }
                }
            }
        }
    }
    #endregion
    #region 5 Prefix - BeatmapObjectSpawnMovementData NJS NJO
    //BW 5th item that runs. JDFixer uses this method so that the user can update the MaxJNS over and over. i tried it in LevelUpdatePatcher. it works but can only be updated before play song one time https://github.com/zeph-yr/JDFixer/blob/b51c659def0e9cefb9e0893b19647bb9d97ee9ae/HarmonyPatches.cs
    //note jump offset determines how far away notes spawn from you. A negative modifier means notes will spawn closer to you, and a positive modifier means notes will spawn further away

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), "Init")]
    [HarmonyPriority(Priority.Low)]//was asked to do this for other mods
    internal class SpawnMovementDataUpdatePatch
    {
        //private static bool OriginalValuesSet = false; // Flag to ensure original values are only stored once
        public static float OriginalNJS; // Store the original startNoteJumpMovementSpeed
        public static float OriginalNJO;
        internal static void Prefix(ref float startNoteJumpMovementSpeed, float startBpm, NoteJumpValueType noteJumpValueType, ref float noteJumpValue)//, IJumpOffsetYProvider jumpOffsetYProvider, Vector3 rightVec, Vector3 forwardVec)
        {
            if (TransitionPatcher.characteristicSerializedName == "Generated360Degree" || TransitionPatcher.characteristicSerializedName == "Generated90Degree" || TransitionPatcher.characteristicSerializedName == "360Degree" || TransitionPatcher.characteristicSerializedName == "90Degree")//only do this for gen 360 or else it will do this for all maps
            {
                BigLasers myOtherInstance = new BigLasers();
                myOtherInstance.Big();
            }

            if (TransitionPatcher.characteristicSerializedName == "Generated360Degree" || TransitionPatcher.characteristicSerializedName == "Generated90Degree")//only do this for gen 360 or else it will do this for all maps
            {


                //BW Version 2, uses enable/disable. Will change the NJS & NJO to the user value no matter whether the original is higher or lower
                //if (!OriginalValuesSet)// Store the original values if they haven't been stored yet
                //{
                OriginalNJS = TransitionPatcher.noteJumpMovementSpeed;
                OriginalNJO = TransitionPatcher.noteJumpStartBeatOffset;

                //OriginalValuesSet = true;
                //}

                //Plugin.Log.Info("BW SpawnMovementDataUpdatePatch SongName: " + LevelUpdatePatcher.SongName);
                //Plugin.Log.Info("BW SpawnMovementDataUpdatePatch Original TransitionPatcher.noteJumpMovementSpeed: " + OriginalNJS + " and from SpawnMovementDataUpdatePatch startNoteJumpMovementSpeed: " + startNoteJumpMovementSpeed);
                //Plugin.Log.Info("BW SpawnMovementDataUpdatePatch Original TransitionPatcher.noteJumpStartBeatOffset: " + OriginalNJO);

                if (Config.Instance.EnableNJS)
                {
                    startNoteJumpMovementSpeed = Config.Instance.NJS;
                    noteJumpValue = Config.Instance.NJO;// this works but if you read this before setting it, it has the wrong number. its always .5 i think.

                    if (Config.Instance.NJS < OriginalNJS || Config.Instance.NJO > OriginalNJO)
                    {
                        ScoreSubmission.DisableSubmission("360Fyer");
                        Plugin.Log.Info(LevelUpdatePatcher.SongName + "Score disabled by NJS NJO - NJS Orig: " + OriginalNJS + " New NJS " + Config.Instance.NJS + " Orig NJO " + OriginalNJO + " New NJO: " + Config.Instance.NJO);
                    }
                    else
                    {
                        //Plugin.Log.Info(LevelUpdatePatcher.SongName + "Score NOT disabled by NJS NJO");
                    }

                    //Plugin.Log.Info("--------------------");
                    //Plugin.Log.Info("SongName: " + LevelUpdatePatcher.SongName);
                    //Plugin.Log.Info("New noteJumpMovementSpeed: "   + startNoteJumpMovementSpeed);
                    //Plugin.Log.Info("New noteJumpStartBeatOffset: " + noteJumpValue);
                }
            }
        }
    }
    #endregion

    #region 4 Postfix - CreateTransformedBeatmapData
    //BW 4th item that runs after LevelUpdatePatcher & GameModeHelper & TransitionPatcher https://harmony.pardeike.net/articles/patching-prefix.html
    //This runs after 3rd item automatically
    //This alters the beat map data such as rotation events...
    [HarmonyPatch(typeof(BeatmapDataTransformHelper), "CreateTransformedBeatmapData")]
    public class BeatmapDataTransformHelperPatcher
    {
        static void Postfix(ref IReadonlyBeatmapData __result, IReadonlyBeatmapData beatmapData, IPreviewBeatmapLevel beatmapLevel, GameplayModifiers gameplayModifiers, bool leftHanded, EnvironmentEffectsFilterPreset environmentEffectsFilterPreset, EnvironmentIntensityReductionOptions environmentIntensityReductionOptions, MainSettingsModelSO mainSettingsModel)
        {

            if (beatmapData is CustomBeatmapData customBeatmapData)
            {

                if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_360DEGREE_MODE || TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_90DEGREE_MODE)
                {

                    Plugin.Log.Info($"\nGenerating rotation events for {TransitionPatcher.startingGameMode}...");

                    Generator360 gen = new Generator360();
                    gen.WallGenerator = Config.Instance.EnableWallGenerator;
                    gen.OnlyOneSaber = Config.Instance.OnlyOneSaber;
                    gen.RotationSpeedMultiplier = (float)Math.Round(Config.Instance.RotationSpeedMultiplier, 1);
                    gen.AllowCrouchWalls = Config.Instance.AllowCrouchWalls;
                    gen.AllowLeanWalls = Config.Instance.AllowLeanWalls;
                    gen.LeftHandedOneSaber = Config.Instance.LeftHandedOneSaber;

                    if (Config.Instance.BasedOn != Config.Base.Standard || gen.RotationSpeedMultiplier < 0.8f)//|| gen.OnlyOneSaber || gen.AllowCrouchWalls || gen.AllowLeanWalls)// || gen.RotationAngleMultiplier != 1.0f)
                    {
                        ScoreSubmission.DisableSubmission("360Fyer");
                        Plugin.Log.Info("Score disabled by Standard or Multiplier " + gen.RotationSpeedMultiplier);
                    }



                    if (TransitionPatcher.startingGameMode == GameModeHelper.GENERATED_90DEGREE_MODE)
                    {   //BW devided by 2 to make the rotation angle accurate. 90 degrees was 180 degress without this 
                        gen.LimitRotations = (int)((Config.Instance.LimitRotations90 / 360f / 2f) * (24f));// / Config.Instance.RotationAngleMultiplier));//BW this convert the angle into LimitRotation units of 15 degree slices. Need to divide the Multiplier since it causes the angle to change from 15 degrees. this will keep the desired limit to work if a multiplier is added.
                        gen.BottleneckRotations = gen.LimitRotations / 2;

                        if (Config.Instance.LimitRotations90 < 90)
                        {
                            ScoreSubmission.DisableSubmission("360Fyer");
                            Plugin.Log.Info("Score disabled by LimitRotations90 set less than 90.");
                        }
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
                            if (Config.Instance.LimitRotations360 < 135)//|| gen.OnlyOneSaber || gen.AllowCrouchWalls || gen.AllowLeanWalls)// || gen.RotationAngleMultiplier != 1.0f)
                            {
                                ScoreSubmission.DisableSubmission("360Fyer");
                                Plugin.Log.Info("Score disabled by LimitRotations360 set less than 135.");
                            }
                            gen.LimitRotations = (int)((Config.Instance.LimitRotations360 / 360f / 2f) * (24f));// / Config.Instance.RotationAngleMultiplier));//BW this convert the angle into LimitRotation units of 15 degree slices. Need to divide the Multiplier since it causes the angle to change from 15 degrees. this will keep the desired limit to work if a multiplier is added.
                            gen.BottleneckRotations = gen.LimitRotations / 2;
                        }
                    }

                    __result = gen.Generate(__result, beatmapLevel.beatsPerMinute);
                }

            }
        }
    }
    #endregion
    #region 3 Prefix - custom Colors
    //This will set an author's custom color scheme to work. But built-in and non-customized colors on custom maps will get the standard default color scheme. Colors will revert to the player's color override if that is set.
    [HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO), "Init")]
    internal class ColorSchemeUpdatePatch
    {
        internal static void Prefix(IDifficultyBeatmap difficultyBeatmap, ref ColorScheme overrideColorScheme, ColorScheme beatmapOverrideColorScheme)
        {
            if (TransitionPatcher.characteristicSerializedName == "Generated360Degree")//only do this for gen 360 or else it will do this for all maps
            {
                // Find the Environment GameObject
                GameObject environment = GameObject.Find("Environment");

                if (environment != null)
                {
                    Plugin.Log.Info($"Environment found!");
                    // Iterate through all child objects of the Environment GameObject
                    foreach (Transform lightPillar in environment.transform)
                    {
                        // Check if the child object is a LightPillar
                        if (lightPillar.gameObject.name == "LightPillar")
                        {
                            Plugin.Log.Info($"LightPillar found!");
                            // Iterate through all child objects of the LightPillar
                            foreach (Transform rlp in lightPillar)
                            {
                                // Check if the child object is a RotatingLasersPair by its name or tag
                                if (rlp.gameObject.name == "RotatingLasersPair")
                                {
                                    // Set the localScale of the RotatingLasersPair
                                    rlp.localScale = new Vector3(5, 5, 5);
                                    Plugin.Log.Info($"Scaled all RotatingLasersPair!");
                                }
                                else
                                    Plugin.Log.Info($"RotatingLasersPair NOT found!!!!");
                            }
                        }
                        else
                            Plugin.Log.Info($"LightPillar NOT found!!!!");
                    }
                }


                if (TransitionPatcher.beatMapDataCB != null)//a Custom Beatmap Data file exits and it may have custom color scheme data added by the author
                {
                    //Plugin.Log.Info("1");

                    string json = JsonConvert.SerializeObject(TransitionPatcher.beatMapDataCB.beatmapCustomData);//this has the choseninfo.dat individual difficulty custom data such as _suggestion chroma and _colorRight etc for custom colors

                    //Plugin.Log.Info($"TransitionPatcher: beatmapCustomData:");
                    JObject beatmapCustomData = JObject.Parse(json);

                    if (beatmapCustomData["_colorLeft"] != null || beatmapCustomData["_envColorLeft"] != null || beatmapCustomData["_obstacleColor"] != null)//the author has provided at least some custom color data
                    {
                        // Extract the color values
                        // Check for null values and set to default color scheme items if any are null
                        Color saberAColor = beatmapCustomData["_colorLeft"] != null ? new Color((float)beatmapCustomData["_colorLeft"]["r"], (float)beatmapCustomData["_colorLeft"]["g"], (float)beatmapCustomData["_colorLeft"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.saberAColor;
                        Color saberBColor = beatmapCustomData["_colorRight"] != null ? new Color((float)beatmapCustomData["_colorRight"]["r"], (float)beatmapCustomData["_colorRight"]["g"], (float)beatmapCustomData["_colorRight"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.saberBColor;
                        Color environmentColor0 = beatmapCustomData["_envColorLeft"] != null ? new Color((float)beatmapCustomData["_envColorLeft"]["r"], (float)beatmapCustomData["_envColorLeft"]["g"], (float)beatmapCustomData["_envColorLeft"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.environmentColor0;
                        Color environmentColor1 = beatmapCustomData["_envColorRight"] != null ? new Color((float)beatmapCustomData["_envColorRight"]["r"], (float)beatmapCustomData["_envColorRight"]["g"], (float)beatmapCustomData["_envColorRight"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.environmentColor1;
                        bool supportsEnvironmentColorBoost = beatmapCustomData.Property("_envColorLeftBoost") != null && beatmapCustomData.Property("_envColorRightBoost") != null;
                        Color environmentColor0Boost = beatmapCustomData.Property("_envColorLeftBoost") != null ? new Color((float)beatmapCustomData["_envColorLeftBoost"]["r"], (float)beatmapCustomData["_envColorLeftBoost"]["g"], (float)beatmapCustomData["_envColorLeftBoost"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.environmentColor0Boost;
                        Color environmentColor1Boost = beatmapCustomData.Property("_envColorRightBoost") != null ? new Color((float)beatmapCustomData["_envColorRightBoost"]["r"], (float)beatmapCustomData["_envColorRightBoost"]["g"], (float)beatmapCustomData["_envColorRightBoost"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.environmentColor1Boost;
                        Color obstaclesColor = beatmapCustomData.Property("_obstacleColor") != null ? new Color((float)beatmapCustomData["_obstacleColor"]["r"], (float)beatmapCustomData["_obstacleColor"]["g"], (float)beatmapCustomData["_obstacleColor"]["b"]) : LevelUpdatePatcher.OriginalColorScheme.obstaclesColor;

                        // Create the ColorScheme object and assign it
                        overrideColorScheme = new ColorScheme(
                            "theAuthorsColorScheme",
                            "theAuthorsLocalizationKey",
                            false,
                            "Author's Color Scheme",
                            true,
                            saberAColor,
                            saberBColor,
                            environmentColor0,
                            environmentColor1,
                            supportsEnvironmentColorBoost,
                            environmentColor0Boost,
                            environmentColor1Boost,
                            obstaclesColor
                        );

                        Plugin.Log.Info($"Authors's Custom Color Scheme found and applied!");
                    }
                    else//there is no custom color set added by the author
                    {
                        Plugin.Log.Info("Author has not set custom color Scheme.");

                        //Find if the user has chosen colors>Override Default Colors
                        PlayerDataModel _playerDataModel = UnityEngine.Object.FindObjectOfType<PlayerDataModel>();
                        PlayerData _playerData = _playerDataModel.playerData;
                        bool overrideDefaultColours = _playerData.colorSchemesSettings.overrideDefaultColors;

                        if (overrideDefaultColours)//if the user has an override color scheme, do nothing since it will automatically override default colors
                        {
                            Plugin.Log.Info("User HAS set an Override Color Scheme so it will be used.");
                            return;

                        }
                        else//if the user doesn't have an override chose my default color scheme.
                        {
                            Plugin.Log.Info("User HAS NOT set an Override Color Scheme. Using the original level color scheme.");
                            overrideColorScheme = LevelUpdatePatcher.OriginalColorScheme;
                        }
                        //Plugin.Log.Info("No Custom Beat Map Data with Color Scheme");
                    }
                }
            }

        }
    }
    #endregion
    #region 2 Prefix - StartStandardLevel
    //BW 2nd item that runs after LevelUpdatePatcher & GameModeHelper
    //Runs when you click play button
    //BW v1.31.0 Class MenuTransitionsHelper method StartStandardLevel has 1 new item added. After 'ColorScheme overrideColorScheme', 'ColorScheme beatmapOverrideColorScheme' has been added. so i added: typeof(ColorScheme) after typeof(ColorScheme)
    [HarmonyPatch(typeof(MenuTransitionsHelper))]
    [HarmonyPatch("StartStandardLevel", new[] { typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool), typeof(bool), typeof(Action), typeof(Action<DiContainer>), typeof(Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults>), typeof(Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults>) })]
    public class TransitionPatcher
    {
        public static string startingGameMode;
        public static string characteristicSerializedName;//will be "Generated360Degree" for gen 360
        public static ColorScheme theOverrideColorScheme;
        public static CustomBeatmapData beatMapDataCB;

        public static float noteJumpMovementSpeed;
        public static float noteJumpStartBeatOffset;

        static void Prefix(string gameMode, IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel, OverrideEnvironmentSettings overrideEnvironmentSettings, ColorScheme overrideColorScheme, GameplayModifiers gameplayModifiers, PlayerSpecificSettings playerSpecificSettings, PracticeSettings practiceSettings, string backButtonText, bool useTestNoteCutSoundEffects, bool startPaused, Action beforeSceneSwitchCallback, Action<DiContainer> afterSceneSwitchCallback, Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback, Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults> levelRestartedCallback)
        {

            //sends this to ColorSchemeUpdatePatch prefix in order to change color scheme there
            IReadonlyBeatmapData RetrieveBeatmapData(IDifficultyBeatmap theDifficultyBeatmap, EnvironmentInfoSO environmentInfo, PlayerSpecificSettings thePlayerSpecificSettings)
            {
                IReadonlyBeatmapData theBeatmapData = Task.Run(() => difficultyBeatmap.GetBeatmapDataAsync(environmentInfo, playerSpecificSettings)).Result;
                Plugin.Log.Info($"PlayerSpecificSettings - NoteJumpDurationTypeSettings: {playerSpecificSettings.noteJumpDurationTypeSettings}. if Static - noteJumpFixedDuration(reaction time): {playerSpecificSettings.noteJumpFixedDuration} or if Dynamic - Note Jump Offset: {playerSpecificSettings.noteJumpStartBeatOffset}");

                return theBeatmapData;
            }
            if (RetrieveBeatmapData(difficultyBeatmap, previewBeatmapLevel.environmentInfo, playerSpecificSettings) is CustomBeatmapData dataCB)
            {
                beatMapDataCB = dataCB;//this is the custom Beatmap Data which has customData JSON if it exists
            }


            characteristicSerializedName = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            noteJumpMovementSpeed = difficultyBeatmap.noteJumpMovementSpeed;
            noteJumpStartBeatOffset = difficultyBeatmap.noteJumpStartBeatOffset;

            Plugin.Log.Info($"\nTransitionPatcher original NJS: {noteJumpMovementSpeed} original NJO {noteJumpStartBeatOffset}");

            //Sets the variable to the name of the map being started (Standard, Generated360Degree, etc)          
            startingGameMode = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            //Plugin.Log.Info($"\nBW 5 TransitionPatcher startingGameMode: {startingGameMode} (should be Generated360Degree)\n");
        }
    }
    #endregion
    #region 1 Prefix - StandardLevelDetailView
    //BW 1st item that runs. This calls GameModeHelper.cs next.
    //Called when a song's been selected and its levels are displayed in the right menu
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch("SetContent")]
    public class LevelUpdatePatcher
    {
        public static string SongName;
        public static bool BeatSage;
        public static float SongDuration;
        public static ColorScheme OriginalColorScheme;
        public static bool AlreadyUsingEnvColorBoost;
        //public static float CuttableNotesCount;

        static void Prefix(StandardLevelDetailView __instance, IBeatmapLevel level, BeatmapDifficulty defaultDifficulty, BeatmapCharacteristicSO defaultBeatmapCharacteristic, PlayerData playerData)//level actually is of the class CustomBeatmapLevel which impliments interface IBeatmapLevel
        {
            SongDuration = level.songDuration;
            SongName = level.songName;
            BeatSage = level.levelAuthorName.Contains("Beat Sage");

            //This will get the color scheme of the 1st level or main level (usually standard)
            OriginalColorScheme = level.beatmapLevelData.difficultyBeatmapSets[0].difficultyBeatmaps[0].GetEnvironmentInfo().colorScheme.colorScheme; ;

            //This is an empty set
            List<BeatmapCharacteristicSO> toGenerate = new List<BeatmapCharacteristicSO>();

            //Plugin.Log.Info($"\nBW 1 LevelUpdatePatcher toGenerate Count: {toGenerate.Count}");


            //these properties get added to the empty set: "icon", "_characteristicNameLocalizationKey", "GEN360","_descriptionLocalizationKey", "Generated 360 mode", "_serializedName", "Generated360Degree" and a few more...
            if (Config.Instance.ShowGenerated360)
                toGenerate.Add(GameModeHelper.GetGenerated360GameMode());
            if (Config.Instance.ShowGenerated90)
                toGenerate.Add(GameModeHelper.GetGenerated90GameMode());

            //This initializes the 'sets' list with the difficultyBeatmapSets of the chosen song
            List<IDifficultyBeatmapSet> sets = new List<IDifficultyBeatmapSet>(level.beatmapLevelData.difficultyBeatmapSets);

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
                        Plugin.Log.Info($"{SongName} - Requires Noodle - So skipping 360 Generation");
                        continue;
                    }
                }
                if (level is CustomPreviewBeatmapLevel)
                {
                    var extras = Collections.RetrieveExtraSongData(new string(level.levelID.Skip(13).ToArray()));

                    if (extras != null)
                    {
                        var difficultyData = extras._difficulties.FirstOrDefault(difficultyDataInner => difficultyDataInner._envColorLeftBoost != null || difficultyDataInner._envColorRightBoost != null);

                        if (difficultyData != null)
                        {
                            Plugin.Log.Info($"{SongName} - Author already uses _envColorLeftBoost/_envColorRightBoost.");
                            AlreadyUsingEnvColorBoost = true;
                        }
                        else
                        {
                            Plugin.Log.Info($"{SongName} - Author NOT using _envColorLeftBoost/_envColorRightBoost.");
                            AlreadyUsingEnvColorBoost = false;
                        }
                    }
                }


                IDifficultyBeatmapSet newSet;

                //Plugin.Log.Info(" ");
                //Plugin.Log.Info("BW 1 StandardLevelDetailView Song Name: " + SongName);

                //This is true for built-in songs
                if (basedOnGameMode.difficultyBeatmaps[0] is BeatmapLevelSO.DifficultyBeatmap)
                {
                    BeatmapLevelSO.DifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Select((bm) => new BeatmapLevelSO.DifficultyBeatmap(

                        bm.level,
                        bm.difficulty,
                        bm.difficultyRank,
                        bm.noteJumpMovementSpeed,
                        bm.noteJumpStartBeatOffset,
                        bm.environmentNameIdx,
                        FieldHelper.Get<BeatmapDataSO>(bm, "_beatmapData")
                    )).ToArray();//BW v1.31.0 added bm.environmentNameIdx for new parameter environmentNameIdx

                    newSet = new BeatmapLevelSO.DifficultyBeatmapSet(customGameMode, difficultyBeatmaps);
                    foreach (BeatmapLevelSO.DifficultyBeatmap dbm in difficultyBeatmaps)
                    {
                        dbm.SetParents(level, newSet);
                        //Plugin.Log.Info($"BW 5 LevelUpdatePatcher {difficultyBeatmaps.ToString()}");
                    }
                }
                //This is true for custom songs
                //checks if the first difficulty beatmap in the difficultyBeatmaps collection of basedOnGameMode is of type CustomDifficultyBeatmap or a subclass of it.
                else if (basedOnGameMode.difficultyBeatmaps[0] is CustomDifficultyBeatmap)
                {

                    //creates a new instance of the CustomDifficultyBeatmapSet class and initializing it with data based on the customGameMode object
                    CustomDifficultyBeatmapSet customSet = new CustomDifficultyBeatmapSet(customGameMode);

                    //Testing this in its place: adjusting NJS and NJO
                    CustomDifficultyBeatmap[] difficultyBeatmaps = basedOnGameMode.difficultyBeatmaps.Cast<CustomDifficultyBeatmap>().Select((cbm) => {
                        return new CustomDifficultyBeatmap(
                            cbm.level,
                            customSet,
                            cbm.difficulty,
                            cbm.difficultyRank,
                            cbm.noteJumpMovementSpeed,
                            cbm.noteJumpStartBeatOffset,
                            cbm.beatsPerMinute,
                            cbm.beatmapColorSchemeIdx,
                            cbm.environmentNameIdx,
                            cbm.beatmapSaveData,
                            cbm.beatmapDataBasicInfo);
                    }).ToArray();



                    //allows you to set the custom difficulty beatmaps for a CustomDifficultyBeatmapSet instance.
                    //effectively populating the instance with the provided difficulty beatmaps.
                    customSet.SetCustomDifficultyBeatmaps(difficultyBeatmaps);
                    newSet = customSet;

                }
                else
                {
                    Plugin.Log.Error($"Cannot create generated mode for {basedOnGameMode.difficultyBeatmaps[0]}");
                    continue;
                }

                //IDifficultyBeatmapSet 'sets' was populated with sets of the chosen song. Now we are adding the custom content to it. will have standard and generated 360
                //This is working. v1.29 and v1.31 seem identical except beatmapColorSchemeIdx: 0 and environmentNameIdx: 0 has been added to every difficulty level in v1.31
                sets.Add(newSet);
            }


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
            }
            else
            {
                Plugin.Log.Info("Unsupported beatmapLevelData: " + (level.beatmapLevelData?.GetType().FullName ?? "null"));
            }

            //Plugin.Log.Info("\nBW 9 LevelUpdatePatcher Properties of IBeatmapLevel AFTER update:\n");

        }
    }
    #endregion
}