using System;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;


[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace Beat360fyerPlugin
{

    internal class Config
    {
        public static Config Instance { get; set; }
        public virtual bool Wireless360 { get; set; } = false;//BW This assumes the user doesn't want rotation limits and it sets LimitRotations to 999 and BottleneckRotations to 999. only for 360 not 90.
        public virtual float LimitRotations360 { get; set; } = 360;//BW changed this to Degrees. Previously Default 28 where 24 is 360 degree circle. designed to avoid riping a cable
        public virtual float LimitRotations90 { get; set; } = 90;//BW changed this to Degrees
        public virtual bool EnableWallGenerator { get; set; } = true;
        public virtual bool BigWalls { get; set; } = true;
        public virtual bool BigLasers { get; set; } = true;
        public virtual bool BrightLights { get; set; } = true;
        public virtual bool BoostLighting { get; set; } = true;

        //public virtual bool EnableNJS { get; set; } = false;
        //public virtual float NJS { get; set; } = 15f;
        //public virtual float NJO { get; set; } = 0f;

        public virtual bool AllowCrouchWalls { get; set; } = false;//BW added this
        public virtual bool AllowLeanWalls { get; set; } = false;//BW added this
        
        //-------------------------------------------------------

        //BW Requires Beat Saber restart
        public virtual bool ShowGenerated360 { get; set; } = true;
        public virtual bool ShowGenerated90 { get; set; } = false;

        public virtual bool OnlyOneSaber { get; set; } = false;
        public virtual bool LeftHandedOneSaber { get; set; } = false;


        public virtual bool AddXtraRotation { get; set; } = false;//for periods of low rotation, will make sure rotations for direction-less notes move in same direction as last rotation so totalRotation will increase.
        public virtual float RotationGroupLimit { get; set; } = 10f;//If totalRotations are under this limit, will add more rotations
        public virtual float RotationGroupSize { get; set; } = 12;//The number of rotations to remain inactive for adding rotations

        //public virtual bool ArcFix { get; set; } = true;//remove rotation during sliders unless the head and tail rotation ends up the same. results is partial mismatch of tail
        public virtual bool ArcFixFull { get; set; } = true;//removes all rotations during sliders

        //BW Not needed since rotates outside of the 15 degree pasages
        //public virtual float RotationAngleMultiplier { get; set; } = 1.0f;//BW added this to lessen/increase rotation angle amount
        public virtual float RotationSpeedMultiplier { get; set; } = 1.0f;//BW This is a multiplier for PreferredBarDuration which has a default of 1.84f
        public virtual float MaxRotationSize { get; set; } = 30f;//disallows single rotations larger than this

        public virtual bool LightAutoMapper { get; set; } = true;
        public virtual float LightFrequencyMultiplier { get; set; } = 1.0f;// Default is 1, adjust as needed use from 0 - 1 to reduce frequency
        public virtual float BrightnessMultiplier { get; set; } = 1.0f;//affect the floatValue property can increase or decrease

        public enum Style
        {
            ON = 1,        // Fast Strobe: on & off events
            FLASH = 2,     // Med Flash: flash & fade events
            FADE = 3,      // Med Fade: fade events
            TRANSITION = 4 // Slow Transition: transition & fade
        }
        public virtual Style LightStyle { get; set; } = Style.FLASH;//not using off



        //BW added this baseded on NoteLimiter UI. enums cannot use a digit so had to change 90Degree to NinetyDegree
        //public virtual string TextColor { get; set; } = "#555555";//BW sets the color of the LimitRotations360 menu text. Dims it if deactivated by Wireless360;

        public enum Base
        {
            Standard,
            OneSaber,
            NoArrows,
            NinetyDegree
        }
        [UseConverter(typeof(EnumConverter<Base>))]
        public virtual Base BasedOn { get; set; } = Base.Standard;//BW Can be Standard,OneSaber,NoArrows,90Degree (but may keep old 90 rotation events. need to investigate)


        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Config other)
        {
            // This instance's members populated from other
        }
    }
}
