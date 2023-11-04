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
                EnableSliderLimitRotations360 = !value;
                if (EnableSliderLimitRotations360) TextColorSliderLimitRotations360 = "#ffffff"; else TextColorSliderLimitRotations360 = "#555555";
                NotifyPropertyChanged();
            }
        }
        //BW need a variable to be !Wireless360 to disable or enable the LimitRotations360 Slider so created this
        [UIValue("EnableSliderLimitRotations360")]
        public bool EnableSliderLimitRotations360
        {
            get => !Config.Instance.Wireless360;
            set {
                Config.Instance.Wireless360 = !value;
                NotifyPropertyChanged();
            }
        }
        //BW LimitRotations360 slider text dimmed if Wireless360 enabled
        [UIValue("TextColorSliderLimitRotations360")]
        public String TextColorSliderLimitRotations360
        {
            get => Config.Instance.Wireless360 ? "#555555" : "#ffffff";
            set
            {
                NotifyPropertyChanged();
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
        //TESTING -----------------------------------------------------------
        /*
        [UIValue("AddXtraRotation")]
        public bool AddXtraRotation
        {
            get => Config.Instance.AddXtraRotation;
            set => Config.Instance.AddXtraRotation = value;
        }
        [UIValue("RotationGroupLimit")]
        public float RotationGroupLimit
        {
            get => Config.Instance.RotationGroupLimit;
            set => Config.Instance.RotationGroupLimit = value;
        }
        [UIValue("RotationGroupSize")]
        public float RotationGroupSize
        {
            get => Config.Instance.RotationGroupSize;
            set => Config.Instance.RotationGroupSize = value;
        }
        */
        //END TESTING ------------------------------------------------------
        [UIValue("EnableWallGenerator")]
        public bool EnableWallGenerator
        {
            get => Config.Instance.EnableWallGenerator;
            set => Config.Instance.EnableWallGenerator = value;
        }
        [UIValue("BigWalls")]
        public bool BigWalls
        {
            get => Config.Instance.BigWalls;
            set => Config.Instance.BigWalls = value;
        }
        [UIValue("BigLasers")]
        public bool BigLasers
        {
            get => Config.Instance.BigLasers;
            set => Config.Instance.BigLasers = value;
        }
        [UIValue("BrightLights")]
        public bool BrightLights
        {
            get => Config.Instance.BrightLights;
            set => Config.Instance.BrightLights = value;
        }
        [UIValue("BoostLighting")]
        public bool BoostLighting
        {
            get => Config.Instance.BoostLighting;
            set => Config.Instance.BoostLighting = value;
        }
        [UIValue("EnableSlidersNJS")]
        public bool EnableSlidersNJS
        {
            get => Config.Instance.EnableNJS;
            set
            {
                Config.Instance.EnableNJS = value;
                NotifyPropertyChanged();
            }
        }
        [UIValue("EnableNJS")]
        public bool EnableNJS
        {
            get => Config.Instance.EnableNJS;
            set
            {
                Config.Instance.EnableNJS = value;
                EnableSlidersNJS = value;
                if (EnableSlidersNJS) TextColorEnableSlidersNJS = "#ffffff"; else TextColorEnableSlidersNJS = "#555555";
                NotifyPropertyChanged();
            }
        }
        //BW NJS & NJO sliders text dimmed if EnableNJS disabled
        [UIValue("TextColorEnableSlidersNJS")]
        public String TextColorEnableSlidersNJS
        {
            get => Config.Instance.EnableNJS ? "#ffffff" : "#555555";
            set
            {
                NotifyPropertyChanged();
            }
        }
        [UIValue("NJS")]
        public float NJS
        {
            get => Config.Instance.NJS;
            set => Config.Instance.NJS = value;
        }
        [UIValue("NJO")]
        public float NJO
        {
            get => Config.Instance.NJO;
            set => Config.Instance.NJO = value;
        }
        /*
        [UIValue("MaxNJS")]
        public float MaxNJS
        {
            get => Config.Instance.MaxNJS;
            set => Config.Instance.MaxNJS = value;
        }
        
        [UIValue("AllowedRotationsPerSec")]
        public float AllowedRotationsPerSec
        {
            get => Config.Instance.AllowedRotationsPerSec;
            set => Config.Instance.AllowedRotationsPerSec = value;
        }
        [UIValue("FastMapPBD")]
        public float FastMapPBD
        {
            get => Config.Instance.FastMapPBD;
            set => Config.Instance.FastMapPBD = value;
        }
        [UIValue("SlowMapPBD")]
        public float SlowMapPBD
        {
            get => Config.Instance.SlowMapPBD;
            set => Config.Instance.SlowMapPBD = value;
        }
        */
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
