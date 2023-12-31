using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BGLib;

namespace Beat360fyerPlugin
{
    //BW v1.31.0 changed 3 BeatmapCharacteristicSO to BeatmapCharacteristicCollection which is the new location in main.dll
    public static class GameModeHelper
    {
        private static Dictionary<string, BeatmapCharacteristicSO> customGamesModes = new Dictionary<string, BeatmapCharacteristicSO>();

        public const string GENERATED_360DEGREE_MODE = "Generated360Degree";
        public const string GENERATED_90DEGREE_MODE = "Generated90Degree";

        public static BeatmapCharacteristicSO GetGenerated360GameMode()
        {
            return GetCustomGameMode(GENERATED_360DEGREE_MODE, GetDefault360Mode().icon, "GEN360", "Generated 360 mode");
        }

        public static BeatmapCharacteristicSO GetGenerated90GameMode()
        {
            return GetCustomGameMode(GENERATED_90DEGREE_MODE, GetDefault90Mode().icon, "GEN90", "Generated 90 mode");
        }

        public static BeatmapCharacteristicSO GetCustomGameMode(string serializedName, Sprite icon, string name, string description, bool requires360Movement = true, bool containsRotationEvents = true, int numberOfColors = 2)
        {
            if (customGamesModes.TryGetValue(serializedName, out BeatmapCharacteristicSO bcso))
            {
                //Plugin.Log.Info($"BW 1 GameModeHelper {bcso}");
                return bcso;
            }
            if (icon == null)
            {
                Texture2D tex = new Texture2D(50, 50);
                icon = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            //Have to get this from songcore and i have registered this in OnApplicationStart() as per Meivyn
            BeatmapCharacteristicSO customGameMode = SongCore.Collections.customCharacteristics.First(x => x.serializedName == serializedName);

            FieldHelper.Set(customGameMode, "_icon", icon);
            FieldHelper.Set(customGameMode, "_characteristicNameLocalizationKey", name);
            FieldHelper.Set(customGameMode, "_descriptionLocalizationKey", description);
            //FieldHelper.Set(customGameMode, "_serializedName", serializedName);
            FieldHelper.Set(customGameMode, "_compoundIdPartName", serializedName); // What is _compoundIdPartName? It gets added to the IDifficultyBeatMap serializedName 
            FieldHelper.Set(customGameMode, "_sortingOrder", 100);
            FieldHelper.Set(customGameMode, "_containsRotationEvents", containsRotationEvents);
            FieldHelper.Set(customGameMode, "_requires360Movement", requires360Movement);
            FieldHelper.Set(customGameMode, "_numberOfColors", numberOfColors);
           
            /*
            // Logging the properties of the customGameMode object
            Plugin.Log.Info("BW 2 GameModeHelper - Properties of customGameMode:");
            Type customGameModeType = customGameMode.GetType();
            foreach (FieldInfo field in customGameModeType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object value = field.GetValue(customGameMode);
                Plugin.Log.Info($"{field.Name}: {value}");
            }
            */

            /*
            //BW--------try to see all data related to set---------------------------------------------------------------------
            List<IDifficultyBeatmapSet> thesets = new List<IDifficultyBeatmapSet>();

            Plugin.Log.Info("\nBW 3 NEW GameModeHelper GetCustomGameMode IDifficultyBeatmapSet get ALL:\n");

            foreach (IDifficultyBeatmapSet theset in thesets)
            {
                Type setElementType = theset.GetType();
                PropertyInfo[] setProperties = setElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo setProperty in setProperties)
                {
                    object setPropertyValue = setProperty.GetValue(theset);
                    Plugin.Log.Info($"{setProperty.Name}: {setPropertyValue}");
                }
                Plugin.Log.Info("");

                foreach (IDifficultyBeatmap difficultyBeatmap in theset.difficultyBeatmaps)
                {
                    Plugin.Log.Info("");
                    Plugin.Log.Info($"Properties of difficultyBeatmap: ");
                    Plugin.Log.Info("");
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
            //BW------------------------------------------------------------------------------
            */

            return customGameMode;
        }

        //This is only used to get the icon from origianl 360 and 90 modes
        private static BeatmapCharacteristicCollection GetDefaultGameModes()
        {
            CustomLevelLoader customLevelLoader = UnityEngine.Object.FindObjectOfType<CustomLevelLoader>();
            if (customLevelLoader == null)
            {
                Plugin.Log.Info("customLevelLoader is null");
                return null;
            }
            BeatmapCharacteristicCollection defaultGameModes = FieldHelper.Get<BeatmapCharacteristicCollection>(customLevelLoader, "_beatmapCharacteristicCollection");
            if (defaultGameModes == null)
            {
                Plugin.Log.Warn("defaultGameModes is null");
            }
            /*
            // Logging the properties of each element in defaultGameModes
            Plugin.Log.Info("BW 4 GameModeHelper - Properties of elements in defaultGameModes:");
            foreach (BeatmapCharacteristicSO element in defaultGameModes.beatmapCharacteristics)
            {
                Type elementType = element.GetType();
                PropertyInfo[] properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                Plugin.Log.Info($"Properties of {elementType.Name}:--------------------------");
                foreach (PropertyInfo property in properties)
                {
                    object value = property.GetValue(element);
                    Plugin.Log.Info($"{property.Name}: {value}");
                }
                Plugin.Log.Info(" ");
            }
            */
            return defaultGameModes;
        }

        private static BeatmapCharacteristicSO GetDefault360Mode()
        {
            return GetDefaultGameModes().GetBeatmapCharacteristicBySerializedName("360Degree");
        }

        private static BeatmapCharacteristicSO GetDefault90Mode()
        {
            return GetDefaultGameModes().GetBeatmapCharacteristicBySerializedName("90Degree");
        }
    }
}
