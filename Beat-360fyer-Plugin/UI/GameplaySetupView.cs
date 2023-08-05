using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Beat360fyerPlugin;//for access to config file
using UnityEngine;
using IPA.Config.Data;

namespace Beat360fyerPlugin.UI
{
    internal class GameplaySetupView : BSMLAutomaticViewController//BW added BSMLAutomaticViewController so could use NotifyPropertyChanged() which is needed for interactable bsml
    {

        [UIValue("Wireless360")]
        public bool Wireless360
        {
            get => Config.Instance.Wireless360;
            set {
                Config.Instance.Wireless360 = value;
                EnableSlider = !value;
                if (EnableSlider) TextColor = "#ffffff"; else TextColor = "#555555";
                NotifyPropertyChanged();
            }
            //set => Config.Instance.Wireless360 = value;//before i added interactable bsml slider
        }
        //BW need a variable to be !Wireless360 to disable or enable the LimitRotations360 Slider so created this
        [UIValue("EnableSlider")]
        public bool EnableSlider
        {
            get => !Config.Instance.Wireless360;
            set
            {
                Config.Instance.Wireless360 = !value;
                NotifyPropertyChanged();
            }
        }
        //BW LimitRotations360 slider text dimmed if Wireless360 enabled
        [UIValue("TextColor")]
        public String TextColor
        {
            get => Config.Instance.TextColor;
            set
            {
                Config.Instance.TextColor = value;
                NotifyPropertyChanged();
                Plugin.Log.Info($"BW    TextColor is: {TextColor}");
            }
        }
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
        public string AngleFormatter(float value)//BW This will output the text on the slider to be an integer with a degree symbol in BSML
        {
            int intValue = Mathf.RoundToInt(value);
            return $"{intValue}°";
        }
        [UIValue("EnableWallGenerator")]
        public bool EnableWallGenerator
        {
            get => Config.Instance.EnableWallGenerator;
            set => Config.Instance.EnableWallGenerator = value;
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
        /*
        //Don't need this. Feels same as the speed multiplier
        [UIValue("RotationAngleMultiplier")]
        public float RotationAngleMultiplier
        {
            get => Config.Instance.RotationAngleMultiplier;
            set => Config.Instance.RotationAngleMultiplier = value;
        }
        */
        [UIValue("RotationSpeedMultiplier")]
        public float RotationSpeedMultiplier
        {
            get => Config.Instance.RotationSpeedMultiplier;
            set => Config.Instance.RotationSpeedMultiplier = value;
        }
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
        [UIValue("OnlyOneSaber")]
        public bool OnlyOneSaber
        {
            get => Config.Instance.OnlyOneSaber;
            set => Config.Instance.OnlyOneSaber = value;
        }
        [UIValue("LeftHandedOneSaber")]
        public bool LeftHandedOneSaber
        {
            get => Config.Instance.LeftHandedOneSaber;
            set => Config.Instance.LeftHandedOneSaber = value;
        }
        [UIValue("available-bases")]
        private readonly List<object> _bases = Enum.GetNames(typeof(Config.Base)).Select(x => (object)x).ToList();

        [UIValue("BasedOn")]
        public string BasedOn
        {
            get => Config.Instance.BasedOn.ToString();
            set => Config.Instance.BasedOn = (Config.Base)Enum.Parse(typeof(Config.Base), value);
        }
    }
}
