using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using UnityEngine;

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
        [UIValue("ArcFixFull")]
        public bool ArcFix
        {
            get => Config.Instance.ArcFixFull;
            set => Config.Instance.ArcFixFull = value;
        }
        
        [UIValue("AddXtraRotation")]
        public bool AddXtraRotation
        {
            get => Config.Instance.AddXtraRotation;
            set => Config.Instance.AddXtraRotation = value;
        }
        [UIValue("MaxRotationSize")]
        public float MaxRotationSize
        {
            get => Config.Instance.MaxRotationSize;
            set => Config.Instance.MaxRotationSize = value;
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
        [UIValue("BoostLighting")]//Creates a boost lighting event. if ON, will set color left to boost color left new color etc. Will only boost a color scheme that has boost colors set so works primarily with COLORS > OVERRIDE DEFAULT COLORS. Or an authors color scheme must have boost colors set (that will probably never happen since they will have boost colors set if they use boost events).
        public bool BoostLighting
        {
            get => Config.Instance.BoostLighting;
            set => Config.Instance.BoostLighting = value;
        }
        [UIValue("LightAutoMapper")]//Creates a boost lighting event. if ON, will set color left to boost color left new color etc. Will only boost a color scheme that has boost colors set so works primarily with COLORS > OVERRIDE DEFAULT COLORS. Or an authors color scheme must have boost colors set (that will probably never happen since they will have boost colors set if they use boost events).
        public bool LightAutoMapper
        {
            get => Config.Instance.LightAutoMapper;
            set => Config.Instance.LightAutoMapper = value;
        }
        [UIValue("LightFrequencyMultiplier")]
        public float LightFrequencyMultiplier
        {
            get => Config.Instance.LightFrequencyMultiplier;
            set => Config.Instance.LightFrequencyMultiplier = value;
        }
        [UIValue("BrightnessMultiplier")]
        public float BrightnessMultiplier
        {
            get => Config.Instance.BrightnessMultiplier;
            set => Config.Instance.BrightnessMultiplier = value;
        }

        // Dictionary for custom labels
        private readonly Dictionary<Config.Style, string> _styleLabels = new Dictionary<Config.Style, string>
        {
            { Config.Style.ON, "Fast Strobe On" },
            { Config.Style.FADE, "Med Fade" },
            { Config.Style.FLASH, "Med Flash" },
            { Config.Style.TRANSITION, "Slow Transition" }
        };

        [UIValue("available-styles")]
        private List<object> _styles = new List<object>();

        public GameplaySetupView() // Constructor with the class name
        {
            // Populate the dropdown list in the desired order
            _styles.Add(_styleLabels[Config.Style.ON]);
            _styles.Add(_styleLabels[Config.Style.FADE]);
            _styles.Add(_styleLabels[Config.Style.FLASH]);
            _styles.Add(_styleLabels[Config.Style.TRANSITION]);
        }

        [UIValue("LightStyle")]
        public string LightStyle
        {
            get => _styleLabels[Config.Instance.LightStyle];
            set
            {
                if (_styleLabels.ContainsValue(value))
                {
                    Config.Style style = _styleLabels.FirstOrDefault(x => x.Value == value).Key;
                    Config.Instance.LightStyle = style;
                    NotifyPropertyChanged();
                }
            }
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
