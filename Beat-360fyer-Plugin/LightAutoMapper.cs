using CustomJSONData.CustomBeatmap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

//https://github.com/Loloppe/ChroMapper-AutoMapper/
//@Lowoppe
namespace Beat360fyerPlugin
{
    public class BasicEventData//BW I created this instead of directly using the real equivalent BS class BasicBeatmapEventData since that class has some readonly attributes which made it hard to alter values in lights.
    {
        public float time;

        public EventType eventType;

        public EventValue eventValue;

        public float floatValue;

        public BasicEventData(float time, EventType eventType, EventValue eventValue, float floatValue = 1f)//floatValue serves as a multiplier. 0 will turn off the light.
        {
            this.time = time;
            this.eventType = eventType;
            this.eventValue = eventValue;
            this.floatValue = floatValue;
        }
    }
    public enum EventType
    {
        BACK = BasicBeatmapEventType.Event0,//top laser bars
        RING = BasicBeatmapEventType.Event1,//far back bars
        LEFT = BasicBeatmapEventType.Event2,//rotating laser (points to the right)
        RIGHT = BasicBeatmapEventType.Event3,//rotating laser (points to the left)
        CENTER = BasicBeatmapEventType.Event4,//in testing these didn't seem to work but i see it in finished output maps from the plugin
        BOOST = BasicBeatmapEventType.Event5,//in testing these didn't seem to work but i see it in finished output maps from the plugin --v3 says not working - use my own separate method instead.
        RING_SPIN = BasicBeatmapEventType.Event8,//no effect in 360 i think
        RING_ZOOM = BasicBeatmapEventType.Event9,//no effect in 360 i think
        LEFT_SPEED = BasicBeatmapEventType.Event12,//rotating laser rotation speed
        RIGHT_SPEED = BasicBeatmapEventType.Event13//rotating laser rotation speed
    }
    public enum EventValue
    {
        OFF = 0,
        BLUE_ON = 1,   // Changes the lights to blue, and turns the lights on.
        BLUE_FLASH = 2,// Changes the lights to blue, and flashes brightly before returning to normal.
        BLUE_FADE = 3, // Changes the lights to blue, and flashes brightly before fading to black.
        BLUE_TRANSITION = 4, //Changes the lights to blue by fading from the current state.
        RED_ON = 5,
        RED_FLASH = 6,
        RED_FADE = 7,
        RED_TRANSITION = 8,
        ON = 9, //white
        FLASH = 10,
        FADE = 11,
        TRANSITION = 12
    }
    public enum Type
    {
        ON = 1,
        FLASH = 2,
        FADE = 3,
        TRANSITION = 4
    }
    #region Light
    public static class Light
    {
        private static float colorOffset = 0.0f;
        private static float colorSwap = 4.0f;
        //private static float colorBoostSwap = 8.0f;

        public static float ColorOffset { set => colorOffset = value > -100.0f ? value : 0.0f; get => colorOffset; }
        //public static float ColorBoostSwap { set => colorBoostSwap = value > 0.0f ? value : 8.0f; get => colorBoostSwap; }
        public static float ColorSwap { set => colorSwap = value > 0.0f ? value : 4.0f; get => colorSwap; }
        //public static bool UseBoostColor { set; get; } = false;
        public static bool NerfStrobes { set; get; } = true;//true adds less offs

        /// <summary>
        /// Method to swap between Fade and On for EventLightValue
        /// </summary>
        /// <param name="value">Current EventLightValue</param>
        /// <returns>Swapped EventLightValue</returns>
        public static EventValue Swap(EventValue x)
        {
            switch (x)
            {
                case EventValue.BLUE_FADE: return EventValue.BLUE_ON;
                case EventValue.RED_FADE: return EventValue.RED_ON;
                case EventValue.BLUE_ON: return EventValue.BLUE_FADE;
                case EventValue.RED_ON: return EventValue.RED_FADE;
                default: return EventValue.OFF;
            }
        }

        /// <summary>
        /// Method to inverse the current EventLightValue between Red and Blue
        /// </summary>
        /// <param name="value">Current EventLightValue</param>
        /// <returns>Inversed EventLightValue</returns>
        public static EventValue Inverse(EventValue eventValue)
        {
            if (eventValue > EventValue.BLUE_TRANSITION)
                return eventValue - 4; //Turn to blue
            else
                return eventValue + 4; //Turn to red
        }

        /// <summary>
        /// Method to randomise the element of a List
        /// </summary>
        /// <typeparam name="T">Object</typeparam>
        /// <param name="list">List</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do rng.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
    #endregion
    //---------------------------------------------------------------------------------------------------------------------------
    #region LightAutoMapper
    public class LightAutoMapper
    {
        public static List<CustomBasicBeatmapEventData> CreateLight(List<BasicBeatmapEventData> currentLightEvents, List<NoteData> Notes, List<SliderData> Sliders, bool needsBACK, bool needsRING, bool needsLEFT, bool needsRIGHT, bool needsCENTER)//List<NoteData> Selection)//selection is for chromapper user selects group of notes
        {
            Dictionary<EventType, EventValue> lastEventColors = new Dictionary<EventType, EventValue>();//test

            Type lightStyle = (Type)Config.Instance.LightStyle;

            float brightnessMultiplier = Config.Instance.BrightnessMultiplier;

            float frequencyMultiplier = Config.Instance.LightFrequencyMultiplier;

            // Use a counter to track when to trigger a light event based on the multiplier
            float lightEventMultiplierCounter;

            // Bunch of var to keep timing in check
            float last = new float();
            float[] time = new float[4];
            int[] light = new int[3];
            float offset = Notes[0].time;
            float firstNote = 0;

            //Light counter, stop at maximum.
            int count;

            // For laser speed
            int currentSpeed = 3;

            // Rhythm check
            float lastSpeed = 0;

            // To not light up Double twice
            float nextDouble = 0;

            // Slider stuff
            bool firstSlider = false;
            float nextSlider = new float();
            List<int> sliderLight = new List<int>() { 4, 1, 0 };//4 = center, 1 = ring, 0
            int sliderIndex = 0;
            float sliderNoteCount = 0;
            bool wasSlider = false;

            // Pattern for specific rhythm
            List<int> pattern = new List<int>(Enumerable.Range(0, 5));
            int patternIndex = 0;
            int patternCount = 20;

            // The new events
            List<BasicEventData> eventTempo = new List<BasicEventData>();

            // If double notes lights are on
            bool doubleOn = false;

            // Make sure this is the right timing for color swap with Boost Event
            float ColorOffset = Light.ColorOffset;
            float ColorSwap = Light.ColorSwap;

            // To make sure that slider doesn't apply as double
            List<SliderData> sliderTiming = new List<SliderData>(); //List<NoteData> sliderTiming = new List<NoteData>();//FIX

            // Order note, necessary if we're converting V3 bomb from notes
            Notes = Notes.OrderBy(o => o.time).ToList();

            void ResetTimer() //Pretty much reset everything necessary.
            {
                firstNote = Notes[0].time;
                offset = firstNote;
                count = 1;
                for (int i = 0; i < 2; i++)
                {
                    time[i] = 0.0f;
                    light[i] = 0;
                }
                time[2] = 0.0f;
                time[3] = 0.0f;
            }

            ResetTimer();

            bool found = false;

            ResetTimer();

            // Find all sliders
            for (int i = 1; i < Sliders.Count; i++)
            {
                // Between 1/8 and 0, same cut direction or dots
                if (Sliders[i].time - Sliders[i - 1].time <= 0.125 && Sliders[i].time - Sliders[i - 1].time > 0 && (Sliders[i].headCutDirection == Sliders[i - 1].headCutDirection || (int)Sliders[i].headCutDirection == 8 || (int)Sliders[i - 1].headCutDirection == 8))
                {
                    sliderTiming.Add(Sliders[i - 1]);
                    found = true;
                }
                else if (found)
                {
                    sliderTiming.Add(Sliders[i - 1]);
                    found = false;
                }
            }

            #region Foreach Note Process specific light using time

            lightEventMultiplierCounter = 0.0f;
            
            int lastLeftSpeed = -1;// Variables to keep track of the last speeds
            int lastRightSpeed = -1;

            foreach (NoteData note in Notes)//Selection) //Process specific light using time.
            {
                float now = note.time;
                time[0] = now;

                // Accumulate the fractional value based on the multiplier - instead of incrmenting a counter, this will allow fractional changes to the multiplier. so 1.5 will reduce .666 and 2 will reduce by .5 etc
                lightEventMultiplierCounter += frequencyMultiplier;

                if (lightEventMultiplierCounter >= 1.0f)
                {
                    lightEventMultiplierCounter -= 1.0f;

                    if (!Light.NerfStrobes && doubleOn && now != last) //Off event
                    {
                        if (now - last >= 1)
                        {
                            if (needsBACK) eventTempo.Add(new BasicEventData(now - (now - last) / 2, EventType.BACK, EventValue.OFF));
                            if (needsRING) eventTempo.Add(new BasicEventData(now - (now - last) / 2, EventType.RING, EventValue.OFF));
                            if (needsLEFT) eventTempo.Add(new BasicEventData(now - (now - last) / 2, EventType.LEFT, EventValue.OFF));
                            if (needsRIGHT) eventTempo.Add(new BasicEventData(now - (now - last) / 2, EventType.RIGHT, EventValue.OFF));
                            if (needsCENTER) eventTempo.Add(new BasicEventData(now - (now - last) / 2, EventType.CENTER, EventValue.OFF));
                        }
                        else
                        {
                            // Will be fused with some events, but we will sort that out later on.
                            if (needsBACK) eventTempo.Add(new BasicEventData(now, EventType.BACK, EventValue.OFF));
                            if (needsRING) eventTempo.Add(new BasicEventData(now, EventType.RING, EventValue.OFF));
                            if (needsLEFT) eventTempo.Add(new BasicEventData(now, EventType.LEFT, EventValue.OFF));
                            if (needsRIGHT) eventTempo.Add(new BasicEventData(now, EventType.RIGHT, EventValue.OFF));
                            if (needsCENTER) eventTempo.Add(new BasicEventData(now, EventType.CENTER, EventValue.OFF));
                        }

                        doubleOn = false;
                    }

                    //If not same note, same beat and not slider, apply once.
                    if ((now == time[1] || (now - time[1] <= 0.02 && time[1] != time[2])) && (time[1] != 0.0D && now != last) && !sliderTiming.Exists(e => e.time == now))
                    {
                        if (needsBACK)
                        {
                            (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle, true);
                            eventTempo.Add(new BasicEventData(now, EventType.BACK, color, floatValue * brightnessMultiplier)); //Back Top Laser
                        }

                        if (needsRING)
                        {
                            (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle, true);
                            eventTempo.Add(new BasicEventData(now, EventType.RING, color, floatValue * brightnessMultiplier)); //Track Ring Neons
                        }
                        if (needsLEFT || needsRIGHT)
                        {
                            (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle);
                            if (needsLEFT) eventTempo.Add(new BasicEventData(now, EventType.LEFT, color, floatValue * brightnessMultiplier)); //Left Laser
                            if (needsRIGHT) eventTempo.Add(new BasicEventData(now, EventType.RIGHT, color, floatValue * brightnessMultiplier)); //Right Laser
                        }
                        if (needsCENTER)
                        {
                            (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle, true);
                            eventTempo.Add(new BasicEventData(now, EventType.CENTER, color, floatValue * brightnessMultiplier)); //Side Light
                        }

                        /*
                        //TESTING NOW!!!!!
                        AddEventIfColorChanged(now, EventType.BACK,   color, floatValue * brightnessMultiplier, needsBACK, eventTempo, lastEventColors);
                        AddEventIfColorChanged(now, EventType.RING,   color, floatValue * brightnessMultiplier, needsRING, eventTempo, lastEventColors);
                        AddEventIfColorChanged(now, EventType.LEFT,   color, floatValue * brightnessMultiplier, needsLEFT, eventTempo, lastEventColors);
                        AddEventIfColorChanged(now, EventType.RIGHT,  color, floatValue * brightnessMultiplier, needsRIGHT, eventTempo, lastEventColors);
                        AddEventIfColorChanged(now, EventType.CENTER, color, floatValue * brightnessMultiplier, needsCENTER, eventTempo, lastEventColors);
                        */
                        //Plugin.Log.Info($"time[0]: {time[0]} - time[1]: {time[1]} = {time[0] - time[1]}");

                        // Laser speed based on rhythm -- !!!!!!!!!!!!!!! this section is not working. time[0] always = tiem[1]
                        if (time[0] - time[1] < 0.15)
                        {
                            currentSpeed = 12;
                        }
                        else if (time[0] - time[1] >= 0.15 && time[0] - time[1] < 0.25)
                        {
                            currentSpeed = 7;
                        }
                        else if (time[0] - time[1] >= 0.25 && time[0] - time[1] < 0.5)
                        {
                            currentSpeed = 5;// 5; BW lasers keep reseting the beams when this changes so only change when tempo really changes a lot
                        }
                        else if (time[0] - time[1] >= 0.5 && time[0] - time[1] < 1)
                        {
                            currentSpeed = 3;// 3; BW lasers keep reseting the beams when this changes so only change when tempo really changes a lot
                        }
                        else
                        {
                            currentSpeed = 1;
                        }
                        //Plugin.Log.Info($"Potential currentSpeed: {currentSpeed} and lastLeftSpeed: { lastLeftSpeed} and lastRightSpeed: {lastRightSpeed}");

                        // Add events only if the current speed is different from the last set speed
                        if (needsLEFT && lastLeftSpeed != currentSpeed)
                        {
                            eventTempo.Add(new BasicEventData(now, EventType.LEFT_SPEED, (EventValue)currentSpeed));
                            lastLeftSpeed = currentSpeed; // Update the last speed for the left laser
                            //Plugin.Log.Info($"1 Left Speed updated to {currentSpeed}");
                        }
                        //else
                            //Plugin.Log.Info("1 Left Speed NO UPDATE");

                        if (needsRIGHT && lastRightSpeed != currentSpeed)
                        {
                            eventTempo.Add(new BasicEventData(now, EventType.RIGHT_SPEED, (EventValue)currentSpeed));
                            lastRightSpeed = currentSpeed; // Update the last speed for the right laser
                            //Plugin.Log.Info($"1 Right Speed updated to {currentSpeed}");
                        }
                        //else
                            //Plugin.Log.Info("1 Right Speed NO UPDATE");

                        doubleOn = true;
                        last = now;
                    }

                    for (int i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                    {
                        time[i] = time[i - 1];
                    }
                }
            }

            #endregion

            nextSlider = new float();

            #region Convert quick light color swap
            // Convert quick light color swap
            if (Light.NerfStrobes)
            {
                float lastTimeTop = 100;
                float lastTimeRing = 100;
                float lastTimeCenter = 100;
                float lastTimeLeft = 100;
                float lastTimeRight = 100;

                foreach (BasicEventData x in eventTempo)
                {
                    if (x.eventType == EventType.BACK)//if (x.basicBeatmapEventType == BasicBeatmapEventType.Event0)
                    {
                        if (x.time - lastTimeTop <= 0.5)
                        {
                            x.eventValue = Light.Swap(x.eventValue);
                        }
                        lastTimeTop = x.time;
                    }
                    else if (x.eventType == EventType.RING)
                    {
                        if (x.time - lastTimeRing <= 0.5)
                        {
                            x.eventValue = Light.Swap(x.eventValue);
                        }
                        lastTimeRing = x.time;
                    }
                    else if (x.eventType == EventType.CENTER)
                    {
                        if (x.time - lastTimeCenter <= 0.5)
                        {
                            x.eventValue = Light.Swap(x.eventValue);
                        }
                        lastTimeCenter = x.time;
                    }
                    else if (x.eventType == EventType.LEFT)
                    {
                        if (x.time - lastTimeLeft <= 0.5)
                        {
                            x.eventValue = Light.Swap(x.eventValue);
                        }
                        lastTimeLeft = x.time;
                    }
                    else if (x.eventType == EventType.RIGHT)
                    {
                        if (x.time - lastTimeRight <= 0.5)
                        {
                            x.eventValue = Light.Swap(x.eventValue);
                        }
                        lastTimeRight = x.time;
                    }
                }
            }
            #endregion


            ResetTimer();

            #region Process all notes using time

            lightEventMultiplierCounter = 0.0f;

            // Variables to keep track of the last speeds
            lastLeftSpeed  = -1;
            lastRightSpeed = -1;

            foreach (NoteData note in Notes)//Selection) //Process all notes using time.
            {
                time[0] = note.time;

                // Accumulate the fractional value based on the multiplier - instead of incrmenting a counter, this will allow fractional changes to the multiplier. so 1.5 will reduce .666 and 2 will reduce by .5 etc
                lightEventMultiplierCounter += frequencyMultiplier;

                if (lightEventMultiplierCounter >= 1.0f)
                {
                    lightEventMultiplierCounter -= 1.0f;

                    if (wasSlider)
                    {
                        if (sliderNoteCount != 0)
                        {
                            sliderNoteCount--;

                            for (int i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                            {
                                time[i] = time[i - 1];
                            }
                            continue;
                        }
                        else
                        {
                            wasSlider = false;
                        }
                    }

                    if (firstSlider)
                    {
                        firstSlider = false;
                        continue;
                    }

                    // Find the next double
                    if (time[0] >= nextDouble)
                    {
                        for (int i = Notes.FindIndex(n => n == note); i < Notes.Count - 1; i++)
                        {
                            if (i != 0)
                            {
                                if (Notes[i].time == Notes[i - 1].time)
                                {
                                    nextDouble = Notes[i].time;
                                    break;
                                }
                            }
                        }
                    }

                    // Find the next slider (1/8 minimum) or chain
                    if (time[0] >= nextSlider)
                    {
                        sliderNoteCount = 0;

                        for (int i = Notes.FindIndex(n => n == note); i < Notes.Count - 1; i++)
                        {
                            if (i != 0 && i < Notes.Count)
                            {
                                // Between 1/8 and 0, same cut direction or dots
                                if (Notes[i].time - Notes[i - 1].time <= 0.125 && Notes[i].time - Notes[i - 1].time > 0 && (Notes[i].cutDirection == Notes[i - 1].cutDirection || (int)Notes[i].cutDirection == 8))
                                {
                                    // Search for the last note of the slider
                                    if (sliderNoteCount == 0)
                                    {
                                        // This is the first note of the slider
                                        nextSlider = Notes[i - 1].time;
                                    }
                                    sliderNoteCount++;
                                }
                                else if (sliderNoteCount != 0)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // It's the next slider or chain
                    if (nextSlider == note.time)
                    {
                        // Take a light between neon, side or backlight and strobes it via On/Flash
                        if (sliderIndex == -1)
                        {
                            sliderIndex = 2;
                        }

                        //BW
                        EventType et = EventType.CENTER;

                        if (sliderLight[sliderIndex] == 4)
                            et = EventType.CENTER;
                        else if (sliderLight[sliderIndex] == 1)
                            et = EventType.RING;
                        else if (sliderLight[sliderIndex] == 0)
                            et = EventType.BACK;

                        // Place light
                        (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle);

                        if ((needsCENTER && et == EventType.CENTER) || (needsRING && et == EventType.RING) || (needsBACK && et == EventType.BACK))
                        {
                            eventTempo.Add(new BasicEventData(time[0], et, (color - 2), floatValue * brightnessMultiplier));
                            eventTempo.Add(new BasicEventData(time[0] + 0.125f, et, (color - 1), floatValue * brightnessMultiplier));
                            eventTempo.Add(new BasicEventData(time[0] + 0.25f, et, (color - 2), floatValue * brightnessMultiplier));
                            eventTempo.Add(new BasicEventData(time[0] + 0.375f, et, (color - 1), floatValue * brightnessMultiplier));
                            eventTempo.Add(new BasicEventData(time[0] + 0.5f, et, 0));
                        }

                        sliderIndex--;

                        wasSlider = true;
                    }
                    // Not a double
                    else if (time[0] != nextDouble)
                    {
                        if (time[1] - time[2] >= lastSpeed + 0.02 || time[1] - time[2] <= lastSpeed - 0.02 || patternCount == 20) // New speed or 20 notes of the same pattern
                        {
                            int old = 0;
                            // New pattern
                            if (patternIndex != 0)
                            {
                                old = pattern[patternIndex - 1];
                            }
                            else
                            {
                                old = pattern[4];
                            }

                            do
                            {
                                pattern.Shuffle();
                            } while (pattern[0] == old);
                            patternIndex = 0;
                            patternCount = 0;
                        }

                        // Place the next light
                        if ((needsBACK && (EventType)pattern[patternIndex] == EventType.BACK) || (needsRING && (EventType)pattern[patternIndex] == EventType.RING) || (needsLEFT && (EventType)pattern[patternIndex] == EventType.LEFT) || (needsRIGHT && (EventType)pattern[patternIndex] == EventType.RIGHT) || (needsCENTER && (EventType)pattern[patternIndex] == EventType.CENTER))
                        {
                            (EventValue color, float floatValue) = FindColor(Notes.First().time, time[0], lightStyle);
                            eventTempo.Add(new BasicEventData(time[0], (EventType)pattern[patternIndex], color, floatValue * brightnessMultiplier));
                        }

                        // Speed based on rhythm
                        if (time[0] - time[1] < 0.15)
                        {
                            currentSpeed = 12;
                        }
                        else if (time[0] - time[1] >= 0.15 && time[0] - time[1] < 0.25)
                        {
                            currentSpeed = 7;
                        }
                        else if (time[0] - time[1] >= 0.25 && time[0] - time[1] < 0.5)
                        {
                            currentSpeed = 5;
                        }
                        else if (time[0] - time[1] >= 0.5 && time[0] - time[1] < 1)
                        {
                            currentSpeed = 3;
                        }
                        else
                        {
                            currentSpeed = 1;
                        }

                        // Add laser rotation if necessary
                        if (needsLEFT && pattern[patternIndex] == 2 && lastLeftSpeed != currentSpeed)
                        {
                            eventTempo.Add(new BasicEventData(time[0], EventType.LEFT_SPEED, (EventValue)currentSpeed));
                            lastLeftSpeed = currentSpeed; // Update the last speed for the left laser
                            //Plugin.Log.Info($"2 Left Speed updated to {currentSpeed}");
                        }
                        //else
                            //Plugin.Log.Info("2 Left Speed NO UPDATE");

                        if (needsRIGHT && pattern[patternIndex] == 3 && lastRightSpeed != currentSpeed)
                        {
                            eventTempo.Add(new BasicEventData(time[0], EventType.RIGHT_SPEED, (EventValue)currentSpeed));
                            lastRightSpeed = currentSpeed; // Update the last speed for the right laser
                            //Plugin.Log.Info($"2 Right Speed updated to {currentSpeed}");
                        }
                        //else
                            //Plugin.Log.Info("2 Right Speed NO UPDATE");

                        // Place off event

                        if (Notes[Notes.Count - 1].time != note.time)
                        {
                            if (Notes[Notes.FindIndex(n => n == note) + 1].time == nextDouble)
                            {
                                if (Notes[Notes.FindIndex(n => n == note) + 1].time - time[0] <= 2)
                                {
                                    float value = (Notes[Notes.FindIndex(n => n == note) + 1].time - Notes[Notes.FindIndex(n => n == note)].time) / 2;
                                    if ((needsBACK && (EventType)pattern[patternIndex] == EventType.BACK) || (needsRING && (EventType)pattern[patternIndex] == EventType.RING) || (needsLEFT && (EventType)pattern[patternIndex] == EventType.LEFT) || (needsRIGHT && (EventType)pattern[patternIndex] == EventType.RIGHT) || (needsCENTER && (EventType)pattern[patternIndex] == EventType.CENTER))
                                        eventTempo.Add(new BasicEventData(Notes[Notes.FindIndex(n => n == note)].time + value, (EventType)pattern[patternIndex], 0));
                                }
                            }
                            else
                            {
                                if ((needsBACK && (EventType)pattern[patternIndex] == EventType.BACK) || (needsRING && (EventType)pattern[patternIndex] == EventType.RING) || (needsLEFT && (EventType)pattern[patternIndex] == EventType.LEFT) || (needsRIGHT && (EventType)pattern[patternIndex] == EventType.RIGHT) || (needsCENTER && (EventType)pattern[patternIndex] == EventType.CENTER))
                                    eventTempo.Add(new BasicEventData(Notes[Notes.FindIndex(n => n == note) + 1].time, (EventType)pattern[patternIndex], 0));
                            }
                        }

                        // Pattern have 5 notes in total (5 lights available)
                        if (patternIndex < 4)
                        {
                            patternIndex++;
                        }
                        else
                        {
                            patternIndex = 0;
                        }

                        patternCount++;
                        lastSpeed = time[0] - time[1];
                    }

                    for (int i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                    {
                        time[i] = time[i - 1];
                    }
                }

            }
            #endregion

            //Add original current lights into newly created lights (at bottom of list)
            foreach (BasicBeatmapEventData e in currentLightEvents)//Convert BasicBeatmapEventData to CustomBasicBeatmapEventData to avoid chroma plugin errors.
            {
                BasicEventData currentLight = new BasicEventData(e.time, (EventType)e.basicBeatmapEventType, (EventValue)e.value, e.floatValue);

                eventTempo.Add(currentLight);
            }

            // Sort lights
            eventTempo = eventTempo.OrderBy(o => o.time).ToList();

            // Remove fused or move off event between
            eventTempo = RemoveFused(eventTempo);

            // Sort lights
            eventTempo = eventTempo.OrderBy(o => o.time).ToList();

            //Convert my class BasicEventData to CustomBasicBeatmapEventData to avoid chroma plugin errors.

            List<CustomBasicBeatmapEventData> lights = new List<CustomBasicBeatmapEventData>();

            foreach (BasicEventData e in eventTempo)
            {
                CustomBasicBeatmapEventData customLightData = new CustomBasicBeatmapEventData(e.time, (BasicBeatmapEventType)e.eventType, (int)e.eventValue, e.floatValue, new CustomData(), true);

                //Plugin.Log.Info($"time: {Math.Round(bbed.time,2)} type: {bbed.basicBeatmapEventType} value: {bbed.value}");

                lights.Add(customLightData);
            }

            return lights;
        }
        //END CreateLight------------------------------------------------------------

        private static List<BasicEventData> RemoveFused(List<BasicEventData> events)
        {
            float? closest = 0f;

            // Get all fused events of a specific type
            for (int i = 0; i < events.Count; i++)
            {
                BasicEventData e = events[i];

                BasicEventData MapEvent = events.Find(o => o.eventType == e.eventType && (o.time - e.time >= -0.02 && o.time - e.time <= 0.02) && o != e);
                if (MapEvent != null)
                {
                    BasicEventData MapEvent2 = events.Find(o => o.eventType == MapEvent.eventType && (o.time - MapEvent.time >= -0.02 && o.time - MapEvent.time <= 0.02) && o != MapEvent);

                    if (MapEvent2 != null)
                    {
                        BasicEventData temp = events.FindLast(o => o.time < e.time && e.time > closest && o.eventValue != 0);

                        if (temp != null)
                        {
                            closest = temp.time;

                            if (MapEvent2.eventValue == EventValue.OFF)
                            {
                                // Move off event between fused note and last note
                                events[(events.FindIndex(o => o.time == MapEvent2.time && o.eventValue == MapEvent2.eventValue && o.eventType == MapEvent2.eventType))].time = (float)(MapEvent2.time - ((MapEvent2.time - closest) / 2));
                            }
                            else
                            {
                                // Move off event between fused note and last note
                                if (MapEvent.eventValue == EventValue.OFF || MapEvent.eventValue == EventValue.BLUE_TRANSITION || MapEvent.eventValue == EventValue.RED_TRANSITION)
                                {
                                    events[(events.FindIndex(o => o.time == MapEvent.time && o.eventValue == MapEvent.eventValue && o.eventType == MapEvent.eventType))].time = (float)(MapEvent.time - ((MapEvent.time - closest) / 2));
                                }
                                else // Delete event
                                {
                                    events.RemoveAt(events.FindIndex(o => o.time == MapEvent.time && o.eventValue == MapEvent.eventValue && o.eventType == MapEvent.eventType));
                                }
                            }
                        }
                    }
                }
            }

            return events;
        }

        private static (EventValue color, float floatValue) FindColor(float first, float current, Type type, bool random = false)
        {
            EventValue baseColor = EventValue.RED_FADE;

            for (int i = 0; i < ((current - first + Light.ColorOffset) / Light.ColorSwap); i++)
            {
                baseColor = Light.Inverse(baseColor); // Swap color
            }

            if (first == current)
            {
                baseColor = EventValue.BLUE_FADE;
            }

            // Randomly decide whether to keep the color or switch to White
            System.Random rnd = new System.Random();

            if (random)
            {
                int randomNumber = rnd.Next(2);// Generates 0 or 1
                if (randomNumber == 0)
                {
                    baseColor = EventValue.BLUE_FADE;
                }
                else
                {
                    baseColor = EventValue.RED_FADE;
                }
            }

            double chance = rnd.NextDouble(); // Generates a random number between 0.0 and 1.0
            if (chance < 0.10) // 10% chance
            {
                baseColor = EventValue.TRANSITION; // For simplicity, using TRANSITION as a placeholder for white
            }

            // Determine final color based on baseColor and type
            EventValue finalColor = baseColor;
            switch (baseColor)
            {
                case EventValue.RED_FADE:
                case EventValue.RED_ON:
                case EventValue.RED_FLASH:
                case EventValue.RED_TRANSITION:
                    finalColor = GetColorForType(EventValue.RED_ON, type);
                    break;
                case EventValue.BLUE_FADE:
                case EventValue.BLUE_ON:
                case EventValue.BLUE_FLASH:
                case EventValue.BLUE_TRANSITION:
                    finalColor = GetColorForType(EventValue.BLUE_ON, type);
                    break;
                case EventValue.TRANSITION: // Placeholder for white
                    finalColor = GetColorForType(EventValue.ON, type);
                    break;
            }

            //Plugin.Log.Info($"Final Type: {finalColor}");

            float floatValue = (float)Math.Round((chance * 0.5 + 0.5), 1); // Randomize the floatValue between 0.5 and 1 with 1 digit

            return (finalColor, floatValue);
        }

        private static EventValue GetColorForType(EventValue baseColor, Type type)
        {
            switch (type)
            {
                case Type.ON:
                    return baseColor;
                case Type.FLASH:
                    return baseColor + 1;
                case Type.FADE:
                    return baseColor + 2;
                case Type.TRANSITION:
                    return baseColor + 3;
                default:
                    return baseColor;
            }
        }

        //BW test to see if removes flickering if i don't add an event  with type and value that was added the last time
        private static void AddEventIfColorChanged(float time, EventType eventType, EventValue color, float floatValue, bool needsEventType, List<BasicEventData> eventTempo, Dictionary<EventType, EventValue> lastEventColors)
        {
            if (needsEventType)
            {
                if (!lastEventColors.TryGetValue(eventType, out EventValue lastColor) || lastColor != color)
                {
                    eventTempo.Add(new BasicEventData(time, eventType, color));
                    lastEventColors[eventType] = color;
                }
            }
        }

    }
    #endregion
}
