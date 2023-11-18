using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using Beat360fyerPlugin.UI;//BW UI
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;
using Zenject;
using SiraUtil.Zenject;//needed to get Zenjector for installer


using JetBrains.Annotations;

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
                Plugin.Log.Info($"InstallBindings: ParametricBoxController");
            }
        }
        [OnExit]
        public void OnApplicationQuit()
        {
        }               
    }
}
