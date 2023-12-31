using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using Beat360fyerPlugin.UI;//BW UI
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;
using Zenject;
using SiraUtil.Zenject;//needed to get Zenjector for installer


using JetBrains.Annotations;
using System.Linq;
using UnityEngine;

namespace Beat360fyerPlugin
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(IPALogger logger, IPAConfig conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            Config.Instance = conf.Generated<Config>();
            Log.Info($"Beat-360fyer-Plugin initialized.");

            zenjector.Install<MyInstaller>(Location.App);//or Location.Player or Location.Menu
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Harmony harmony = new Harmony("nl.codestix.Beat360fyerPlugin");
            harmony.PatchAll();

            //BW UI
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab("360Fyer", "Beat360fyerPlugin.UI.GameplaySetupView.bsml", new GameplaySetupView());
        }
        //Zenject installer. Need this to access and control ParametricBoxController since I couldn't find another way to access that method. If could figure out how to patch a level of beatsaber i might be able to find it another way
        //I access this in LevelUpdatePatcher.cs so that the level has already started and gameObject that uses this has spawned. I cannot access it here since the gameObject hasn't spawned.
        public class MyInstaller : Installer
        {
            public override void InstallBindings()
            {
                Container.Bind<ParametricBoxController>().FromComponentInHierarchy().AsTransient();// Bind the ParametricBoxController as a transient dependency.
                                                                                                   //Plugin.Log.Info($"InstallBindings: ParametricBoxController");

                //Have to do this or will cause errors when do this. help from Meivyn
                //1 play song then close BS. PlayerData.dat gets this: "levelId": "Cathedral","beatmapCharacteristicName": "Generated360Degree",
                //2 Played Song then closed BS. PlayerData.dat gets this added to the above listed item: "levelId": "Cathedral","beatmapCharacteristicName": "MissingCharacteristic",
                //3 Got error on opening BS the 3rd time. it resets the PlayerData.dat file and you lose custom colors etc.
                BeatmapCharacteristicSO GameMode360 = GetCustomGameMode("GEN360", "Generated 360 mode", "Generated360Degree", "Generated360Degree");
                BeatmapCharacteristicSO GameMode90 = GetCustomGameMode("GEN90", "Generated 90 mode", "Generated90Degree", "Generated90Degree");

                BeatmapCharacteristicSO GetCustomGameMode(string characteristicName, string hintText, string serializedName, string compoundIdPartName, bool requires360Movement = true, bool containsRotationEvents = true, int sortingOrder = 99)
                {
                    BeatmapCharacteristicSO customGameMode = SongCore.Collections.customCharacteristics.Where(x => x.serializedName == serializedName).FirstOrDefault();
                    if (customGameMode != null)
                    {
                        return customGameMode;
                    }

                    Texture2D tex = new Texture2D(50, 50);//unable to load 360 icon at this stage
                    Sprite icon = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new UnityEngine.Vector2(0.5f, 0.5f));

                    customGameMode = SongCore.Collections.RegisterCustomCharacteristic(icon, characteristicName, hintText, serializedName, compoundIdPartName, requires360Movement, containsRotationEvents, sortingOrder);

                    return customGameMode;
                }
            }
        }
        [OnExit]
        public void OnApplicationQuit()
        {
        }               
    }
}
