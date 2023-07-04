using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Beat360fyerPlugin;//for access to config file
using UnityEngine;

namespace Beat360fyerPlugin.UI
{
    internal class GameplaySetupView
    {
        [UIValue("ShowGenerated360")]
        public bool ShowGenerated360
        {
            get => Config.Instance.ShowGenerated360;
            set => Config.Instance.ShowGenerated360 = value;
        }
        [UIValue("ShowGenerated90")]
        public bool ShowGenerated90
        {
            get => Config.Instance.ShowGenerated90;
            set => Config.Instance.ShowGenerated90 = value;
        }
        [UIValue("Wireless360")]
        public bool Wireless360
        {
            get => Config.Instance.Wireless360;
            set => Config.Instance.Wireless360 = value;
        }
        [UIValue("available-bases")]
        private readonly List<object> _bases = Enum.GetNames(typeof(Config.Base)).Select(x => (object)x).ToList();

        [UIValue("BasedOn")]
        public string BasedOn
        {
            get => Config.Instance.BasedOn.ToString();
            set => Config.Instance.BasedOn = (Config.Base)Enum.Parse(typeof(Config.Base), value);
        }
        public string AngleFormatter(float value)//This will output the text on the slider to be an integer with a degree symbol
        {
            int intValue = Mathf.RoundToInt(value);
            return $"{intValue}°";
        }
        /*
        [UIAction("EnableDisable")]
        public bool EnableDisable
        {
            get => !Wireless360;
        }
        */
        /*
        public bool EnableDisable(bool value)//This will disable a slider when Wireless360 is enabled and vice-versa
        {
            return !Wireless360; // Example condition: Set interactable to the opposite value of Wireless360 toggle
        }
        */
        [UIValue("LimitRotations360")]
        public float LimitRotations360
        {
            get => Config.Instance.LimitRotations360;
            set => Config.Instance.LimitRotations360 = value;
        }
        [UIValue("LimitRotations90")]
        public float LimitRotations90
        {
            get => Config.Instance.LimitRotations90;
            set => Config.Instance.LimitRotations90 = value;
        }
        [UIValue("EnableWallGenerator")]
        public bool EnableWallGenerator
        {
            get => Config.Instance.EnableWallGenerator;
            set => Config.Instance.EnableWallGenerator = value;
        }
        [UIValue("OnlyOneSaber")]
        public bool OnlyOneSaber
        {
            get => Config.Instance.OnlyOneSaber;
            set => Config.Instance.OnlyOneSaber = value;
        }
        [UIValue("AllowCrouchWalls")]
        public bool AllowCrouchWalls
        {
            get => Config.Instance.AllowCrouchWalls;
            set => Config.Instance.AllowCrouchWalls = value;
        }
        [UIValue("AllowLeanWalls")]
        public bool AllowLeanWalls
        {
            get => Config.Instance.AllowLeanWalls;
            set => Config.Instance.AllowLeanWalls = value;
        }
        [UIValue("RotationSpeedMultiplier")]
        public float RotationSpeedMultiplier
        {
            get => Config.Instance.RotationSpeedMultiplier;
            set => Config.Instance.RotationSpeedMultiplier = value;
        }
        [UIValue("RotationAngleMultiplier")]
        public float RotationAngleMultiplier
        {
            get => Config.Instance.RotationAngleMultiplier;
            set => Config.Instance.RotationAngleMultiplier = value;
        }
    }
}
