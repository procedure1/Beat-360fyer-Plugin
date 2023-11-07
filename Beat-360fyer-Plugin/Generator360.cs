﻿using Beat360fyerPlugin.Patches;
using BeatmapSaveDataVersion3;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IPA.Config.Data;
using static IPA.Logging.Logger;
using System.Runtime;
using UnityEngine.Serialization;
using Newtonsoft.Json;
using static BeatmapLevelSO.GetBeatmapLevelDataResult;
using static HMUI.IconSegmentedControl;
using static NoteData;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static SpawnRotationBeatmapEventData;
using System.Text.RegularExpressions;
//using Newtonsoft.Json;//BW added to work with JSON files - rt click solution explorer and manage NuGet packages
//using System.IO;//BW for file writing

namespace Beat360fyerPlugin
{
    public class Generator360//actually generates 90 maps too
    {
        /// <summary>
        /// The preferred bar duration in seconds. The generator will loop the song in bars. 
        /// This is called 'preferred' because this value will change depending on a song's bpm (will be aligned around this value).
        /// Affects the speed at which the rotation occurs. It will not affect the total number of rotations or the range of rotation.
        /// BW CREATED CONFIG ROTATION  SPEED to allow user to set this.
        /// </summary>
        public float PreferredBarDuration { get; set; } = 2.75f;//BW I like 1.5f instead of 1.84f but very similar to changing LimitRotations, 1.0f is too much and 0.2f freezes beat saber  // Calculated from 130 bpm, which is a pretty standard bpm (60 / 130 bpm * 4 whole notes per bar ~= 1.84)
        public float RotationSpeedMultiplier { get; set; } = 1.0f;//BW This is a mulitplyer for PreferredBarDuration
        /// <summary>
        /// The amount of 15 degree rotations before stopping rotation events (rip cable otherwise) (24 is one full 360 rotation)
        /// </summary>
        public int LimitRotations { get; set; } = 28;//BW 24 is equivalent to 360 (24*15) so this is 420 degrees.
        /// <summary>
        /// The amount of rotations before preferring the other direction (24 is one full rotation)
        /// </summary>
        public int BottleneckRotations { get; set; } = 14; //BW 14 default. This is set by LevelUpdatePatcher which sets this to LimitRotations/2
        /// <summary>
        /// Enable the spin effect when no notes are coming.
        /// </summary>
        public bool EnableSpin { get; set; } = false;
        /// <summary>
        /// The total time 1 spin takes in seconds.
        /// </summary>
        public float TotalSpinTime { get; set; } = 0.6f;
        /// <summary>
        /// Minimum amount of seconds between each spin effect.
        /// </summary>
        public float SpinCooldown { get; set; } = 10f;
        /// <summary>
        /// Amount of time in seconds to cut of the front of a wall when rotating towards it.
        /// </summary>
        public float WallFrontCut { get; set; } = 0.2f;
        /// <summary>
        /// Amount of time in seconds to cut of the back of a wall when rotating towards it.
        /// </summary>
        public float WallBackCut { get; set; } = 0.45f;
        /// <summary>
        /// True if you want to generate walls, walls are cool in 360 mode
        /// </summary>
        public bool WallGenerator { get; set; } = false;

        /// <summary>
        /// The minimum duration of a wall before it gets discarded
        /// </summary>
        public float MinWallDuration { get; set; } = 0.001f;//BW try shorter duration walls because i like the cool short walls that some authors use default: 0.1f;
        /// <summary>
        /// Use to increase or decrease general rotation amount. This doesn't alter the number of rotations - .5 will reduce rotations size by 50% and 2 will double the rotation size.
        /// Set to default for rotations in increments of 15 degrees. 2 would make increments of 30 degrees etc.
        /// </summary>
        //public float RotationAngleMultiplier { get; set; } = 1f;//BW added this to lessen/increase rotation angle amount. 1 means 15 decre
        /// <summary>
        /// Allow crouch obstacles
        /// </summary>
        public bool AllowCrouchWalls { get; set; } = false;//BW added
        /// <summary>
        /// Allow lean obstacles (step to the left, step to the right)
        /// </summary>
        public bool AllowLeanWalls { get; set; } = false;//BW added
        /// <summary>
        /// True if you only want to keep notes of one color.
        /// </summary>
        public bool OnlyOneSaber { get; set; } = false;
        /// <summary>
        /// Left handed mode when OnlyOneSaber is activated
        /// </summary>
        public bool LeftHandedOneSaber { get; set; } = false;//BW added


        private static int Floor(float f)
        {
            int i = (int)f;
            return f - i >= 0.999f ? i + 1 : i;
        }

        public IReadonlyBeatmapData Generate(IReadonlyBeatmapData bmData, float bpm)
        {
            Plugin.Log.Info($"Song: {LevelUpdatePatcher.SongName}-----------------------------");
            Plugin.Log.Info($"PBD:  {PreferredBarDuration}");
            Plugin.Log.Info($"RotationGroupLimit:  {Config.Instance.RotationGroupLimit} RotationGroupSize: {Config.Instance.RotationGroupSize}"); 
            Plugin.Log.Info(" ");

            // Find the MultiplierSet based on the RotationAngleMultiplier
            //MultiplierSet selectedMultiplierSet = multiplierSets.FirstOrDefault(ms => ms.Multiplier == RotationAngleMultiplier);

            BeatmapData data = bmData.GetCopy();

            LinkedList<BeatmapDataItem> dataItems = data.allBeatmapDataItems;

            //decides if there are at least 12 custom obstacles with with _position
            bool containsCustomWalls = dataItems.Count((e) => e is CustomObstacleData d && (d.customData?.ContainsKey("_position") ?? false)) > 12;
            Plugin.Log.Info($"Contains custom walls: {containsCustomWalls}");

            // Amount of rotation events emitted
            int eventCount = 0;
            // Current rotation
            int totalRotation = 0;
            // Moments where a wall should be cut
            List<(float, int)> wallCutMoments = new List<(float, int)>();

            // BW added - keeps track of total rotations at time of each rotation so that later i can decide if want to add more rotations for maps that are not getting many rotations.
            //List<(float, int)> totalRotationMoments = new List<(float, int)>();

            // Previous spin direction, false is left, true is right
            bool previousDirection = true;
            float previousSpinTime = float.MinValue;

            #region Rotate
            int  boostInteration = 0; // Counter for tracking iterations
            bool boostOn = true; // Initial boolean value

            int r = 1;
            int totalRotationsGroup = 0;
            //int totalRotationUpdated = 0;
            bool prevRotationPositive = true;
            //List<(bool, float, int, int)> newRotations = new List<(bool, float, int, int)>();//bool - replace with new rotations group?, float - time, int - rotation step, int - how many in are in this group
            //bool needsMoreRotations = true;
            //int prevGroupIndex = 0;
            //nt groupCount = 0;
            int newRotation = 0;
            bool addMoreRotations = false;
            int RotationGroupLimit = (int)Config.Instance.RotationGroupLimit;

            int RotationGroupSize  = (int)Config.Instance.RotationGroupSize;

            bool alternateParams = false;

            int offSetR = 0;

            //Each rotation is 15 degree increments so 24 positive rotations is 360. Negative numbers rotate to the left, positive to the right
            void Rotate(float time, int amount, SpawnRotationBeatmapEventData.SpawnRotationEventType moment, bool enableLimit = true)
            {
                //Allows 4*15=60 degree turn max and -60 degree min -- however amounts are never passed in higher than 3 or lower than -3. I in testing I only see 2 to -2
                if (amount == 0)
                    return;
                if (amount < -4)
                    amount = -4;
                if (amount > 4)
                    amount = 4;

                if (enableLimit)//always true unless you enableSpin in settings
                {
                    if (totalRotation + amount > LimitRotations)
                        amount = Math.Min(amount, Math.Max(0, LimitRotations - totalRotation));
                    else if (totalRotation + amount < -LimitRotations)
                        amount = Math.Max(amount, Math.Min(0, -(LimitRotations + totalRotation)));
                    if (amount == 0)
                        return;

                    totalRotation += amount;
                    //Plugin.Log.Info($"totalRotation: {totalRotation} at time: {time}.");
                }

                previousDirection = amount > 0;
                eventCount++;
                wallCutMoments.Add((time, amount));
                //totalRotationMoments.Add((time, totalRotation));

                /*
                Plugin.Log.Info($"Rotate Event - amount before: {amount}");
                if (RotationAngleMultiplier > 1)
                {
                    if (selectedMultiplierSet.Values[selectedMultiplierIndex])
                    {
                        if (amount == 1) { amount = 2; }
                            else if (amount == -1) { amount = -2; };
                    }
                    // Increment the index and wrap around to 0 after reaching the end or 10 in this case
                    selectedMultiplierIndex = (selectedMultiplierIndex + 1) % 10;// selectedMultiplierSet.Values.Length;
                }
                Plugin.Log.Info($"Rotate Event - amount after: {amount}");
                */

                //BW Discord help said to change InsertBeatmapEventData to InsertBeatmapEventDataInOrder which allowed content to be stored to data.
                data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(time, moment, amount * 15.0f));// * RotationAngleMultiplier));//discord suggestion

                //Plugin.Log.Info($"Rotate Event--- Time: {time}, Rotation: {amount}, Type: {moment}");
                //Plugin.Log.Info("{");
                //Plugin.Log.Info($"\t_time: {time}");
                //Plugin.Log.Info($"\t_type: {(int)moment+13}");
                //Log.Info($"\t_value: {amount * 15.0f}");
                //Plugin.Log.Info("},");

                //Creates a boost lighting event. if ON, will set color left to boost color left new color etc.
                if (Config.Instance.BoostLighting && !LevelUpdatePatcher.AlreadyUsingEnvColorBoost)
                {
                    boostInteration++;
                    if (boostInteration == 24 || boostInteration == 29)//33)//5 & 13 is good but frequent
                    {
                        data.InsertBeatmapEventDataInOrder(new ColorBoostBeatmapEventData(time, boostOn));
                        //Plugin.Log.Info($"Boost Light! --- Time: {time} On: {boostOn}");
                        boostOn = !boostOn; // Toggle the boolean
                    }

                    // Reset the iteration counter if it reaches 13
                    if (boostInteration == 33) { boostInteration = 0; }
                }
            }

            #endregion

            float beatDuration = 60f / bpm;

            // Align PreferredBarDuration to beatDuration
            float barLength = beatDuration;

            //float multiplier = 1;
            //if (RotationSpeedMultiplier > 1)
            //    multiplier = 1 + (RotationSpeedMultiplier - 1) / 2.5f;//example user sets 1.4 but this sets it to 1.2
            //else if (RotationSpeedMultiplier < 1)
            //    multiplier = 1 - (1 - RotationSpeedMultiplier) / 2.5f;//example user sets .8 but this set it to .9

            while (barLength >= PreferredBarDuration * 1.25f / RotationSpeedMultiplier)
            {
                barLength /= 2f;
            }
            while (barLength < PreferredBarDuration * 0.75f / RotationSpeedMultiplier)
            {
                barLength *= 2f;
            }

            //Plugin.Log.Info($"beatDuration: {beatDuration} barLength: {barLength}");
            Plugin.Log.Info($"PreferredBarDuration: {PreferredBarDuration} * RotationSpeedMultiplier: {RotationSpeedMultiplier} = {PreferredBarDuration/RotationSpeedMultiplier}");
            //Plugin.Log.Info($"RotationAngleMultiplier: {RotationAngleMultiplier}");

            //All in seconds
            List<NoteData> notes = dataItems.OfType<NoteData>().ToList();//List<NoteData> notes = data.GetBeatmapDataItems<NoteData>(0).ToList();
            List<NoteData> notesInBar = new List<NoteData>();
            List<NoteData> notesInBarBeat = new List<NoteData>();

            // Align bars to first note, the first note (almost always) identifies the start of the first bar
            float firstBeatmapNoteTime = notes[0].time;

#if DEBUG
            Plugin.Log.Info($"Setup bpm={bpm} beatDuration={beatDuration} barLength={barLength} firstNoteTime={firstBeatmapNoteTime} firstnoteGameplayType={notes[0].gameplayType} firstnoteColorType={notes[0].colorType}");
#endif

            for (int i = 0; i < notes.Count;)
            {
                float currentBarStart = Floor((notes[i].time - firstBeatmapNoteTime) / barLength) * barLength;
                float currentBarEnd = currentBarStart + barLength - 0.001f;

                //Plugin.Log.Info($"Setup currentBarStart={currentBarStart} currentBarEnd={currentBarEnd}");

                notesInBar.Clear();
                for (; i < notes.Count && notes[i].time - firstBeatmapNoteTime < currentBarEnd; i++)
                {
                    //Plugin.Log.Info($"Setup Notes added to NotesInBar:");
                    // If isn't bomb
                    //if (notes[i].gameplayType != NoteData.GameplayType.Bomb)//BW CHANGED THIS TO TEST to see if have more rotations
                    //{
                    notesInBar.Add(notes[i]);
                    //Plugin.Log.Info($"Setup Note added to NotesInBar={notes[i].time}");
                    //Plugin.Log.Info($"notesInBar --- _cutDirection: {notes[i].cutDirection} _lineIndex: {(int)notes[i].lineIndex} _lineLayer: {(int)notes[i].noteLineLayer} _time: {notes[i].time} _type: {notes[i].colorType}");//convert to beats
                    //}
                    //else
                    //{
                    //    Plugin.Log.Info($"Bomb not added to notesInBar: {notes[i].time}");
                    //}
                }

                if (notesInBar.Count == 0)
                    continue;

                if (EnableSpin && notesInBar.Count >= 2 && currentBarStart - previousSpinTime > SpinCooldown && notesInBar.All((e) => Math.Abs(e.time - notesInBar[0].time) < 0.001f))
                {
#if DEBUG
                    Plugin.Log.Info($"[Generator] Spin effect at {firstBeatmapNoteTime + currentBarStart}");
#endif
                    int leftCount = notesInBar.Count((e) => e.cutDirection == NoteCutDirection.Left || e.cutDirection == NoteCutDirection.UpLeft || e.cutDirection == NoteCutDirection.DownLeft);
                    int rightCount = notesInBar.Count((e) => e.cutDirection == NoteCutDirection.Right || e.cutDirection == NoteCutDirection.UpRight || e.cutDirection == NoteCutDirection.DownRight);

                    int spinDirection;
                    if (leftCount == rightCount)
                        spinDirection = previousDirection ? -1 : 1;
                    else if (leftCount > rightCount)
                        spinDirection = -1;
                    else
                        spinDirection = 1;

                    float spinStep = TotalSpinTime / 24;
                    for (int s = 0; s < 24; s++)//amount (spinDirectin) is either -1 or 1
                    {
                        //EnableSpin is FALSE in the settings. So this never runs.
                        //Plugin.Log.Info($"Spin Rotate--- Time: {firstBeatmapNoteTime + currentBarStart + spinStep * s} Rotation: {spinDirection * 15.0f} Type: {(int)SpawnRotationBeatmapEventData.SpawnRotationEventType.Early}");
                        Rotate(firstBeatmapNoteTime + currentBarStart + spinStep * s, spinDirection, SpawnRotationBeatmapEventData.SpawnRotationEventType.Early, false);
                    }

                    // Do not emit more rotation events after this
                    previousSpinTime = currentBarStart;
                    continue;
                }

                // Divide the current bar in x pieces (or notes), for each piece, a rotation event CAN be emitted
                // Is calculated from the amount of notes in the current bar
                // barDivider | rotations
                // 0          | . . . . (no rotations)
                // 1          | r . . . (only on first beat)
                // 2          | r . r . (on first and third beat)
                // 4          | r r r r 
                // 8          |brrrrrrrr
                // ...        | ...
                // TODO: Create formula out of these if statements
                int barDivider;
                if (notesInBar.Count >= 58)
                    barDivider = 0; // Too mush notes, do not rotate
                else if (notesInBar.Count >= 38)
                    barDivider = 1;
                else if (notesInBar.Count >= 26)
                    barDivider = 2;
                else if (notesInBar.Count >= 8)
                    barDivider = 4;
                else
                    barDivider = 8;

                //Plugin.Log.Info($"notesInBar.Count: {notesInBar.Count} barDivider: {barDivider}");

                if (barDivider <= 0)
                    continue;
#if DEBUG
                StringBuilder builder = new StringBuilder();
#endif
                // Iterate all the notes in the current bar in barDiviver pieces (bar is split in barDivider pieces)
                float dividedBarLength = barLength / barDivider;
                for (int j = 0, k = 0; j < barDivider && k < notesInBar.Count; j++)
                {
                    notesInBarBeat.Clear();
                    for (; k < notesInBar.Count && Floor((notesInBar[k].time - firstBeatmapNoteTime - currentBarStart) / dividedBarLength) == j; k++)
                    {
                        notesInBarBeat.Add(notesInBar[k]);
                        //Plugin.Log.Info($"notesInBarBeat {k} --- Time: {notesInBar[k].time} CutDirection: {notesInBar[k].cutDirection}");
                    }

#if DEBUG
                    // Debug purpose
                    if (j != 0)
                        builder.Append(',');
                    builder.Append(notesInBarBeat.Count);
#endif

                    if (notesInBarBeat.Count == 0)
                        continue;

                    float currentBarBeatStart = firstBeatmapNoteTime + currentBarStart + j * dividedBarLength;

                    // Determine the rotation direction based on the last notes in the bar
                    NoteData lastNote = notesInBarBeat[notesInBarBeat.Count - 1];
                    IEnumerable<NoteData> lastNotes = notesInBarBeat.Where((e) => Math.Abs(e.time - lastNote.time) < 0.005f);

                    // Amount of notes pointing to the left/right
                    int leftCount = lastNotes.Count((e) => e.lineIndex <= 1 || e.cutDirection == NoteCutDirection.Left || e.cutDirection == NoteCutDirection.UpLeft || e.cutDirection == NoteCutDirection.DownLeft);
                    int rightCount = lastNotes.Count((e) => e.lineIndex >= 2 || e.cutDirection == NoteCutDirection.Right || e.cutDirection == NoteCutDirection.UpRight || e.cutDirection == NoteCutDirection.DownRight);

                    //if (i == 12 ||  i == 26 || i == 37)
                    //Plugin.Log.Info($"1-i:{i} j:{j} k:{k} leftCount: {leftCount} rightCount: {rightCount} ");

                    NoteData afterLastNote = (k < notesInBar.Count ? notesInBar[k] : i < notes.Count ? notes[i] : null);

                    // Determine amount to rotate at once
                    // TODO: Create formula out of these if statements
                    int rotationCount = 1;
                    if (afterLastNote != null)
                    {
                        double barLength8thRound = Math.Round(barLength / 8, 4);
                        double timeDiff = Math.Round(afterLastNote.time - lastNote.time, 4);//BW without any rounding or rounding to 5 or more digits still produces a different rotation between exe and plugin.

                        //double epsilon = 0.00000001;
                        if (notesInBarBeat.Count >= 1)
                        {
                            if (timeDiff >= barLength)
                                rotationCount = 3;
                            else if (timeDiff >= barLength8thRound)//barLength / 8)BW ---- This is the place where exe vs plugin maps will differ due to rounding between the 2 applications. i added rounding to 4 digits in order to match the output between the 2
                                rotationCount = 2;
                            //else if (timeDiff - barLength / 8 > epsilon)//(timeDiff >= barLength / 8)//works also (Math.Round(timeDiff, 7) >= Math.Round(barLength / 8, 7))
                            //rotationCount = 2;
                            //else if (Math.Abs(timeDiff - barLength / 8) < epsilon)
                            //rotationCount = 2;
                        }
                        /*
                        if (i == 12 || i == 26 || i == 37)
                        {
                            Plugin.Log.Info($"1-i:{i} j:{j} k:{k} timeDiff: {timeDiff} = afterLastNote.time: {afterLastNote.time} - lastNote.time: {lastNote.time}");
                            //Plugin.Log.Info($"3-i:{i} j:{j} k:{k} barLength: {barLength} barLength/8: {barLength / 8}");
                            Plugin.Log.Info($"2-i:{i} j:{j} k:{k} timeDiff: {timeDiff} barLength8thRound: {barLength8thRound}");
                            //Plugin.Log.Info($"2-rotationCount: {rotationCount}");
                        }
                        */
                    }


                    int rotation = 0;
                    if (leftCount > rightCount)
                    {
                        //if (i == 12 || i == 26 || i == 37)
                        //Plugin.Log.Info("3-Rotation Tree One: 1");
                        // Most of the notes are pointing to the left, rotate to the left
                        rotation = -rotationCount;
                    }
                    else if (rightCount > leftCount)
                    {
                        //if (i == 12 || i == 26 || i == 37)
                        //Plugin.Log.Info("3-Rotation Tree One: 2");
                        // Most of the notes are pointing to the right, rotate to the right
                        rotation = rotationCount;
                    }
                    else
                    {
                        // Rotate to left or right
                        if (totalRotation >= BottleneckRotations)
                        {
                            //Plugin.Log.Info("Rotation Tree One: 3");
                            // Prefer rotating to the left if moved a lot to the right
                            rotation = -rotationCount;
                        }
                        else if (totalRotation <= -BottleneckRotations)
                        {
                            //Plugin.Log.Info("Rotation Tree One: 4");
                            // Prefer rotating to the right if moved a lot to the left
                            rotation = rotationCount;
                        }
                        else
                        {
                            //Plugin.Log.Info("Rotation Tree One: 5");
                            // Rotate based on previous direction
                            rotation = previousDirection ? rotationCount : -rotationCount;
                        }
                    }

                    //Plugin.Log.Info($"Rotation will be--------------: {rotation *15}");

                    if (totalRotation >= BottleneckRotations && rotationCount > 1)
                    {
                        //Plugin.Log.Info("Rotation Tree TWO: 1");
                        rotationCount = 1;
                    }
                    else if (totalRotation <= -BottleneckRotations && rotationCount < -1)
                    {
                        //Plugin.Log.Info("Rotation Tree TWO: 2");
                        rotationCount = -1;
                    }

                    if (totalRotation >= LimitRotations - 1 && rotationCount > 0)
                    {
                        //Plugin.Log.Info("Rotation Tree THREE: 1");
                        rotationCount = -rotationCount;
                    }
                    else if (totalRotation <= -LimitRotations + 1 && rotationCount < 0)
                    {
                        //Plugin.Log.Info("Rotation Tree THREE: 2");
                        rotationCount = -rotationCount;
                    }


                    #region AddXtraRotation
                    //############################################################################
                    //BW had to add more rotations directly in the main loop. tried it outside this main loop. the problem with being outside the loop is you cannot decide if a map is really low on rotations until after the map is finished.
                    //add more rotation to maps without much rotation. If there are few rotations, look for directionless notes up/down/dot/bomb and make their rotation direction the same as the previous direction so that there will be increased totalRotation.
                    //Once rotation steps pass the RotationGroupLimit, make this inactive. Stay inactive for RotationGroupSize number of rotations and if there are few rotations while off, activate this again.

                    if (Config.Instance.AddXtraRotation)
                    {
                        if (addMoreRotations)//this stays on until passes the rotation limit
                        {
                            if(Math.Abs(totalRotationsGroup) < Math.Abs(RotationGroupLimit))
                            {
                                if (lastNote.cutDirection == NoteCutDirection.Up || lastNote.cutDirection == NoteCutDirection.Down || lastNote.cutDirection == NoteCutDirection.Any || lastNote.cutDirection == NoteCutDirection.None)//only change rotation if using a non-directional note. if remove this will allow a lot more rotations
                                {
                                    if (prevRotationPositive)//keep direction the same as the previous note
                                        newRotation =  Math.Abs(rotation);
                                    else
                                        newRotation = -Math.Abs(rotation);

                                    if (newRotation != rotation)
                                        Plugin.Log.Info($"r: {r} Old Rotation: {rotation} New Rotation: {newRotation}");// totalRotationsGroup: {totalRotationsGroup}");

                                    rotation = newRotation;

                                    totalRotationsGroup += rotation;
                                }

                            }
                            else//has now passed the rotation limit now
                            {
                                addMoreRotations = false;

                                totalRotationsGroup = 0;

                                //Plugin.Log.Info($"Change to NOT ACTIVE since passed the limit!!! RotationGroupLimit: {RotationGroupLimit}\t totalRotationsGroup: {totalRotationsGroup}");

                                offSetR = r;//need this since when passes the limit, r may be close or equal to being a multiple of RotationGroupSize. that means it could be active soon again. so need to offset r so it will stay off for RotationGroupSize rotations.(r - offSetR) will be 0 on first rotation...
                            }

                        }
                        else//inactive
                        {
                            totalRotationsGroup += rotation;

                            if ((r - offSetR) % RotationGroupSize == 0)// after RotationGroupSize - offset number of iterations, this will check if rotations are over the limit
                            {
                                if (Math.Abs(totalRotationsGroup) >= Math.Abs(RotationGroupLimit))//if the total rotations was over the limit, stay inactive
                                { 
                                    addMoreRotations = false;

                                    //Plugin.Log.Info($"Continue to be NOT ACTIVE: Inactive rotations are over the limit so stay inactive for {RotationGroupSize} rotations. RotationGroupLimit: {RotationGroupLimit}\t RotationGroupSize set to: 0 ++++++++++++++++++++++++++++++++++++++++++++++++");
                                }
                                else//if the total rotations was under the limit, activate more rotations
                                { 
                                    addMoreRotations = true;

                                    if (alternateParams)
                                    {
                                        RotationGroupLimit += 4;//change the limit size for variety //could not alter RotationGroupSize since causing looping problem
                                    }
                                    else
                                    {
                                        RotationGroupLimit -= 4;//change the limit size for variety //could not alter RotationGroupSize since causing looping problem
                                    }

                                    alternateParams = !alternateParams; // Toggles every other time addMoreRotations is true

                                    //Plugin.Log.Info($"ACTIVE:     RotationGroupLimit: {RotationGroupLimit}\t RotationGroupSize: {RotationGroupSize}------------------------------------------------");
                                }

                                totalRotationsGroup = 0;

                            }
                        }                     

                        if (rotation > 0)
                            prevRotationPositive = true;
                        else
                            prevRotationPositive = false;                       
                       
                    }

                    //############################################################################
                    #endregion



                    //***********************************
                    //Finally rotate - possible values here are -3,-2,-1,0,1,2,3 but in testing I only see -2 to 2
                    //The condition for setting rotationCount to 3 is that timeDiff (the time difference between afterLastNote and lastNote) is greater than or equal to barLength. If your test data rarely or never satisfies this condition, you won't see rotation values of -3 or 3.
                    //Similarly, the condition for setting rotationCount to 2 is that timeDiff is greater than or equal to barLength / 8. If this condition is rarely met in your test cases, it would explain why you mostly see rotation values of - 2, -1, 0, 1, or 2.

                    Rotate(lastNote.time, rotation, SpawnRotationBeatmapEventData.SpawnRotationEventType.Late);

                    Plugin.Log.Info($"Rotate() r: {r}\t Time: {Math.Round(lastNote.time, 2).ToString("0.00")}\t Rotation Step:\t {rotation}\t lastNoteDir:\t {lastNote.cutDirection}\t totalRotation:\t {totalRotation}\t totalRotationsGroup:\t {totalRotationsGroup}");// Type: {(int)SpawnRotationBeatmapEventData.SpawnRotationEventType.Late}"); \t Beat: {lastNote.time * bpm / 60f}

                    r++;


                    /*
                    //THIS CONCEPT DOESN'T WORK well. its because updating rotations after walls are generated will cause some walls to be misplaced and will be vision blockers. would have to re-update walls or move them out of the main loop
                    //This works with a loop that is placed after this main note loop.

                    //A better solution is NOT to update the rotations again in a loop after the first loop of rotations. go ahead and add more rotations directly in the main loop. the problem is you cannot decide if a map is really low on rotations. the new idea will update all maps.

                    //BW try to add more rotation to maps without much rotation. see if there are few total rotations in 20 rotations. if not, look for directionless notes up/down/dot/bomb
                    if (r % 20 != 0)//multiple of 20 of lastNotes since lastNote is when rotation occurs so counting rotations
                    {
                        if (lastNote.cutDirection == NoteCutDirection.Up || lastNote.cutDirection == NoteCutDirection.Down || lastNote.cutDirection == NoteCutDirection.Any || lastNote.cutDirection == NoteCutDirection.None)
                        {
                            int newRot = 0;

                            if (prevRotation < 0)
                            {
                                if (rotation == -3 || rotation == 3)
                                {
                                    newRot = -3;
                                }
                                else
                                    newRot = -2;
                            }
                            else if (prevRotation > 0)
                            {
                                if (rotation == -3 || rotation == 3)
                                {
                                    newRot = 3;
                                }
                                else
                                    newRot = 2;
                            }
                            else
                                newRot = rotation;

                            newRotations.Add((false, lastNote.time, newRot, 0));
                            groupCount++;

                            totalRotationUpdated += newRot;

                            Plugin.Log.Info($"--- New Rotation: {newRot} Index: {newRotations.Count-1} -- only used if group rotations are too low. totalRotationUpdated: {totalRotationUpdated}");
                        }
                        else
                            totalRotationUpdated += rotation;

                        prevRotation = rotation;
                        totalRotationsGroup += rotation;

                        if (totalRotationsGroup < -12 && totalRotationsGroup > 12)
                        {
                            needsMoreRotations = false;
                            Plugin.Log.Info($"+++++++++++++++++++ Contains MORE than -12 and 12 rotations so we are good here! Will not add rotations to this group.");
                        }

                    }
                    else
                    {
                        Plugin.Log.Info($"------ Group of 20 rotations!!! r: {r} totalRotationsGroup: {totalRotationsGroup} prevGroupIndex: {prevGroupIndex} and newRotations.Count is {newRotations.Count}");
                        if (needsMoreRotations)
                        {
                            newRotations[prevGroupIndex] = (true, newRotations[prevGroupIndex].Item2, newRotations[prevGroupIndex].Item3, groupCount);
                            Plugin.Log.Info($"^^^^^^^^^^^^ Contains between -12 and 12 rotations or less so needs more! {newRotations[prevGroupIndex].Item1}\t time: {newRotations[prevGroupIndex].Item2}\t new rotation: {newRotations[prevGroupIndex].Item3}");

                        }

                        needsMoreRotations = true;
                        totalRotationsGroup = 0;
                        prevGroupIndex = newRotations.Count-1;
                        groupCount = 0;
                    }
                   
                    r++;   
                    */

                    /*
                    if (i == 12 || i == 26 || i == 37)
                    {
                        Plugin.Log.Info($"3-i:{i} j:{j} k:{k} rotation: {rotation} rotationCount: {rotationCount} totalRotation: {totalRotation}");
                        Plugin.Log.Info("-----------");
                    }
                    */

                    //Plugin.Log.Info($"Total Rotations: {totalRotation*15} Time: {lastNote.time} Rotation: {rotation*15}");
                    #region OneSaber
                    if (OnlyOneSaber)
                    {
                        foreach (NoteData nd in notesInBarBeat)
                        {
                            if (LeftHandedOneSaber)
                            {
                                if (nd.colorType == (rotation > 0 ? ColorType.ColorA : ColorType.ColorB))
                                {
                                    // Remove note
                                    dataItems.Remove(nd);
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.colorType == ColorType.ColorB)
                                    {
                                        nd.Mirror(data.numberOfLines);
                                    }
                                }
                            }
                            else
                            {
                                if (nd.colorType == (rotation < 0 ? ColorType.ColorB : ColorType.ColorA))
                                {
                                    // Remove note
                                    dataItems.Remove(nd);
                                }
                                else
                                {
                                    // Switch all notes to ColorA
                                    if (nd.colorType == ColorType.ColorA)
                                    {
                                        nd.Mirror(data.numberOfLines);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region Wall Generator


                    //Plugin.Log.Info("TEST");
                    //Plugin.Log.Info($"Wall Generate: wallTime: {currentBarBeatStart} wallDuration: {dividedBarLength}");

                    //Plugin.Log.Info("TEST!!!!!!!!!!!!!!!!!!!!!!!!");
                    // Generate wall. BW fyi v2 wall with _type 1 or v3 wall y: noteLineLayer.Top or 2 is a crouch wall -- but must be must be wide enough to and over correct x: lineIndex to be over a player
                    if (WallGenerator && !containsCustomWalls)
                    {
                        float wallTime = currentBarBeatStart;
                        float wallDuration = dividedBarLength;

                        // Create a Random instance
                        System.Random random = new System.Random();

                        // Check if there is already a wall
                        bool generateWall = true;
                        foreach (ObstacleData obs in dataItems.OfType<ObstacleData>())
                        {
                            if (obs.time + obs.duration >= wallTime && obs.time < wallTime + wallDuration)
                            {
                                generateWall = false;
                                //Plugin.Log.Info($"Wall Gen FALSE - obs.time: {obs.time} + obs.duration: {obs.duration} = {obs.time + obs.duration} >= wallTime: {wallTime} && obs.time < wallTime + wallDuration: {wallDuration}");
                                break;
                            }
                        }

                        if (generateWall && afterLastNote != null)
                        {
                            //Plugin.Log.Info($"Wall Gen Test");
                            //int width1Change = 0; // Counter for both 5th and 8th iterations
                            //int width1;
                            //int width2Change = 0; // Counter for both 5th and 8th iterations
                            //int width2;
                            int width = 1;

                            if (!notesInBarBeat.Any((e) => e.lineIndex == 3))//line index 3 is far right
                            {
                                int wallHeight = notesInBarBeat.Any((e) => e.lineIndex == 2) ? 1 : 3;//BW I think this just sets some walls shorter or taller for visual interest

                                //Plugin.Log.Info($"Wall Gen1: wallHeight: {wallHeight}");

                                if (afterLastNote.lineIndex == 3 && !(wallHeight == 1 && afterLastNote.noteLineLayer == NoteLineLayer.Base))
                                {
                                    wallDuration = afterLastNote.time - WallBackCut - wallTime;
                                    //Plugin.Log.Info($"Wall Gen1: wallDuration: {wallDuration}");
                                }

                                if (wallDuration > MinWallDuration)
                                {   //BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.

                                    //int randomNumber = random.Next(1, 13);// Generate a random number between 1 and 12

                                    if (Config.Instance.BigWalls)
                                    {
                                        if (i % 3 == 0 || i % 7 == 0)// Check for every 5th and 8th iteration
                                        {
                                            //width1Change++;//counts the 5th and 8th iterations.
                                            width = 12;// (width1Change % 3 == 0) ? 12 : 6;//every 3rd time we enter this block it sets width1 to -19; otherwise, it sets it to -11
                                        }
                                        else
                                        {
                                            width = 1; // Default value for all other iterations
                                        } 
                                    }

                                    int lineIndex = 3;// (width1 != 1) ? 4 : 3; //if width is one of the wide walls move the wall further away from user so not as obtrusive.

                                    data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, lineIndex, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, width, 5));//note width is always 1 here. BW changed to make all walls 5 high since this version of plugin shortens height of walls which i don't like - default:  wallHeight)); wallHeight));

                                    //string temp; if (wallHeight == 1) { temp = "Top"; } else { temp = "Base"; };
                                    //Plugin.Log.Info($"Wall Generate1: Time: {wallTime}, Index: 3, Layer: {temp}, Dur: {wallDuration}, Width: {randomNumber}, Height: 5");
                                }
                            }
                            if (!notesInBarBeat.Any((e) => e.lineIndex == 0))//line index 0 is far left
                            {
                                int wallHeight = notesInBarBeat.Any((e) => e.lineIndex == 1) ? 1 : 3;

                                //Plugin.Log.Info($"Wall Gen2: wallHeight: {wallHeight}");

                                if (afterLastNote.lineIndex == 0 && !(wallHeight == 1 && afterLastNote.noteLineLayer == NoteLineLayer.Base))
                                {
                                    wallDuration = afterLastNote.time - WallBackCut - wallTime;
                                    //Plugin.Log.Info($"Wall Gen2: wallDuration: {wallDuration}");
                                }

                                if (wallDuration > MinWallDuration)
                                {   //Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.

                                    //int randomNumber; // Generate a random number between -10 and 1 (inclusive) excluding 0 since 0 is 0 thickness wall
                                    //do {randomNumber = random.Next(-10, 2);} while (randomNumber == 0);

                                    if (Config.Instance.BigWalls)
                                    {
                                        if (i % 4 == 0 || i % 6 == 0)// Check for every 5th and 8th iteration
                                        {
                                            //width2Change++;//counts the 5th and 8th iterations.
                                            width = -11;// (width2Change % 3 == 0) ? -11 : -5;//every 3rd time we enter this block it sets width1 to -19; otherwise, it sets it to -11
                                        }
                                        else
                                        {
                                            width = 1; // Default value for all other iterations
                                        } 
                                    }

                                    int lineIndex = 0;//= (width2 != 1) ? -1 : 0; //if width is one of the wide walls move the wall further away from user so not as obtrusive.


                                    data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, lineIndex, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, width, 5));//BW wallHeight));

                                    //string temp; if (wallHeight == 1) { temp = "Top"; } else { temp = "Base"; };
                                    //Plugin.Log.Info($"Wall Generate2: Time: {wallTime}, Index: 0, Layer: {temp}, Dur: {wallDuration}, Width: {randomNumber}, Height: 5");

                                }
                            }
                        }
                    }
                    #endregion

#if DEBUG
                    Plugin.Log.Info($"[{currentBarBeatStart}] Rotate {rotation} (c={notesInBarBeat.Count},lc={leftCount},rc={rightCount},lastNotes={lastNotes.Count()},rotationTime={lastNote.time + 0.01f},afterLastNote={afterLastNote?.time},rotationCount={rotationCount})");
#endif
                }


#if DEBUG
                Plugin.Log.Info($"[{currentBarStart + firstBeatmapNoteTime}({(currentBarStart + firstBeatmapNoteTime) / beatDuration}) -> {currentBarEnd + firstBeatmapNoteTime}({(currentBarEnd + firstBeatmapNoteTime) / beatDuration})] count={notesInBar.Count} segments={builder} barDiviver={barDivider}");
#endif
            }//End for loop over all notes---------------------------------------------------------------
            /*
            if (needsMoreRotations)
            {
                int p = 1;
                foreach (var item in newRotations)
                {
                    Plugin.Log.Info($"{p} newRotations: Are rotations needed in the group of 20 starting here? {item.Item1} time: {item.Item2} new rotation: {item.Item3}");
                    p++;
                }
            }
            else
                Plugin.Log.Info($"Don't need to add more rotations.");
            */
            /*
            //v1 & v2 -- THIS CONCEPT DOESN'T WORK well. its because updating rotations after walls are generated will cause some walls to be misplaced and will be vision blockers. would have to re-update walls or move them out of the main loop



            //v2
            //FIX!! Not working since need for old rotations and newRotation to have same number and corresponding elements. currently, newRotations only saves a new item when cutdirection is up or down or any or none. so doesn't have all the direction left, right, etc rotations

            List <SpawnRotationBeatmapEventData> rd = dataItems.OfType<SpawnRotationBeatmapEventData>().ToList();

            for (int i = 0; i < newRotations.Count; i++)
            {
                var newRotation = newRotations[i];

                if (newRotation.Item1)
                {
                    data.RemoveBeatmapEventData(rd[i]);
                    data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(newRotation.Item2, SpawnRotationEventType.Late, newRotation.Item3 * 15.0f));// * RotationAngleMultiplier));//discord suggestion
                    Plugin.Log.Info($"*****REPLACED rotations.");
                }
            }

            /*
            //v1
            //If new rotations are needed (newRotation.Item1 == true) then finds if newRotation time and old rotation data match and replaces them. not really finished.
            //Not working anyway since time is not a good way to compare them since there can be many notes with the same time.
            foreach (SpawnRotationBeatmapEventData rd in dataItems.OfType<SpawnRotationBeatmapEventData>().ToList())
            {
                int p = 0;
                foreach (var newRotation in newRotations)
                {
                    if (newRotation.Item1)
                    {
                        if (Math.Abs(rd.time - newRotation.Item2) < .001f && rd.rotation != newRotation.Item3) // Check if the times are nearly equal and if the rotations are different between the 2 lists
                        {
                            // Update the rotation event with the new rotation value
                            data.RemoveBeatmapEventData(rd);
                            data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(newRotation.Item2, SpawnRotationEventType.Late, newRotation.Item3 * 15.0f));// * RotationAngleMultiplier));//discord suggestion
                            Plugin.Log.Info($"*****REPLACED rotations.");
                        }
                    }

                }
            }
            */
            //Works but rotations are stored at the current total map rotation. So so rotation may be -175 for example (where the prvious may have been -160). So deleting a rotation will allow the next rotation to be a giant rotation leap. 
            //Could update all other roations when a rotation is deleted. OR move a similar tool before rotations are created and stop a new rotation from being added if its inside an arc.
            #region Arc Fix
            if (Config.Instance.ArcFix)
            {
                Plugin.Log.Info($"Starting Arc Fix:");

                List<SliderData> sliders = dataItems.OfType<SliderData>().ToList();

                foreach (SliderData slider in dataItems.OfType<SliderData>().Where((e) => e.sliderType == SliderData.Type.Normal).ToList())//only search normal sliders (arcs) not burst sliders
                {

                    List<SpawnRotationBeatmapEventData> sliderRotations = new List<SpawnRotationBeatmapEventData>();//list of roations during this slider
                    
                    float lastRotationBeforeSlider = 0;

                    Plugin.Log.Info($"Looking inside ARC now: arc head: {slider.time} arc tail: {slider.tailTime}");

                    foreach (SpawnRotationBeatmapEventData ro in dataItems.OfType<SpawnRotationBeatmapEventData>().Where((e) => e.time >= sliders[0].time).ToList())
                    {
                        if (ro.time < slider.time - .001f)//if ro.time is at least .001s less than slider.time (so defiantely less)
                        {
                            lastRotationBeforeSlider = ro.rotation;//get the last rotation before or at the head of the slider
                        }
                        else if (ro.time <= slider.tailTime + .001f)//can be a tiny bit .01s past the tailTime
                        {
                            //totalSliderRotations.Add(ro.rotation);

                            sliderRotations.Add(ro);//list of roations during this slider
                            //dataItems.Remove(ro);
                            Plugin.Log.Info($"--- lastRotationBeforeSlider: {lastRotationBeforeSlider}. During Slider found rotation at: {ro.time} amount: {ro.rotation} -- # of rotations inside so far: {sliderRotations.Count}");
                        }
                        else//after the slider now
                        {
                            if (sliderRotations.Count > 0)
                            {
                                Plugin.Log.Info($"------ Checking the total rotations for all {sliderRotations.Count} rotation(s) inside the ARC:");
                                if (Config.Instance.ArcFixFull)
                                {
                                    foreach (SpawnRotationBeatmapEventData delRot in sliderRotations)
                                    {
                                        Plugin.Log.Info($"--------------- DELETE rotation at: {delRot.time} amount: {delRot.rotation}");
                                        dataItems.Remove(delRot); //data.RemoveBeatmapEventData(rot);//
                                        //delRot.RecalculateRotationFromPreviousEvent(delRot);
                                    }
                                }
                                else if (sliderRotations.Count == 1 || (lastRotationBeforeSlider != sliderRotations[sliderRotations.Count - 1].rotation))//if there is only 1 rotation then begin and end total rotations will be the same but needs to be deleted anyway. means there w
                                {
                                    foreach (SpawnRotationBeatmapEventData delRot in sliderRotations)
                                    {
                                        Plugin.Log.Info($"--------------- DELETE rotation at: {delRot.time} amount: {delRot.rotation}");
                                        dataItems.Remove(delRot); //data.RemoveBeatmapEventData(rot);//
                                        //delRot.RecalculateRotationFromPreviousEvent(delRot);
                                    }
                                }
                                else
                                {
                                    Plugin.Log.Info($"--------- ARC contains {sliderRotations.Count} rotations but can keep them all! lastRotationBeforeSlider: {lastRotationBeforeSlider} and end total rot: {sliderRotations[sliderRotations.Count - 1].rotation} -- Results in slight mismatch of the tail :(");
                                }

                            }
                            else
                            {
                                Plugin.Log.Info($"--- ARC contains NO rotations.");
                            }
                            //Plugin.Log.Info($"--- BREAK to next slider!");
                            break;
                        }
                    }
                }
                /*
                Plugin.Log.Info($"=========================== rotation that remain================================================");
                foreach (SpawnRotationBeatmapEventData ro in dataItems.OfType<SpawnRotationBeatmapEventData>().ToList())
                {
                    Plugin.Log.Info($"rotation amount: {ro.rotation} at: {ro.time}");
                }
                */
            }
            #endregion

            #region Wall Removal
            //BW noodle extensions causes BS crash in the section somewhere below. Could drill down and figure out why. Haven't figured out how to test for noodle extensions but noodle extension have custom walls that crash Beat Saber so BW added test for custom walls.

            Queue<ObstacleData> obstacles = new Queue<ObstacleData>(dataItems.OfType<ObstacleData>());

            while (obstacles.Count > 0)
            {
                ObstacleData ob = obstacles.Dequeue();

                int totalRotations = 0;//rotations during a single ob

                foreach ((float cutTime, int cutAmount) in wallCutMoments)// Iterate over the list of rotation moments for the current obstacle. Cut walls, walls will be cut when a rotation event is emitted
                {
                    if (ob.duration <= 0f)
                        break;

                    //FIX!!!!
                    bool isCustomWall = false;

                    float frontCut = isCustomWall ? 0f : WallFrontCut;
                    float backCut = isCustomWall ? 0f : WallBackCut;

                    if (!isCustomWall && ((ob.lineIndex == 1 || ob.lineIndex == 2) && ob.width == 1))//Lean wall of width 1. Hard to see coming.
                    {
                        //Plugin.Log.Info($"Remove Lean Wall of width 1: Time: {ob.time} cutTime: {cutTime}");
                        dataItems.Remove(ob);
                    }
                    else if (!isCustomWall && !AllowLeanWalls && ((ob.lineIndex == 0 && ob.width == 2) || (ob.lineIndex == 2 && ob.width > 1)))//Lean walls of width 2.
                    {
                        //Plugin.Log.Info($"Remove Lean Wall: Time: {ob.time } cutTime: {cutTime}");
                        dataItems.Remove(ob);
                    }
                    else if (!isCustomWall && !AllowCrouchWalls && (ob.lineIndex == 0 && ob.width > 2))//Crouch walls
                    {
                        //Plugin.Log.Info($"Remove Crouch Wall: Time: {ob.time} cutTime: {cutTime}");
                        dataItems.Remove(ob);
                    }
                    else if (isCustomWall || (ob.lineIndex <= 1 && cutAmount < 0) || (ob.lineIndex >= 2 && cutAmount > 0))//Removes, changes duration or splits problem walls
                    {
                        int cutMultiplier = Math.Abs(cutAmount);
                        if (cutTime > ob.time - frontCut && cutTime < ob.time + ob.duration + backCut * cutMultiplier)
                        {
                            float originalTime = ob.time;
                            float originalDuration = ob.duration;
                            float firstPartTime = ob.time;
                            float firstPartDuration = (cutTime - backCut * cutMultiplier) - firstPartTime;
                            float secondPartTime = cutTime + frontCut;
                            float secondPartDuration = (ob.time + ob.duration) - secondPartTime;
                            if (firstPartDuration >= MinWallDuration && secondPartDuration >= MinWallDuration)
                            {
                                ob.UpdateDuration(firstPartDuration);// Update duration of existing obstacle
                                ObstacleData secondPart = new ObstacleData(secondPartTime, ob.lineIndex, ob.lineLayer, secondPartDuration, ob.width, ob.height);// And create a new obstacle after it
                                data.AddBeatmapObjectDataInOrder(secondPart);
                                obstacles.Enqueue(secondPart);
                                //Plugin.Log.Info($"Wall SPLIT: 1st Half starts: {ob.time} duration: {firstPartDuration} 2nd Half starts: Time: {secondPartTime}, Index: {ob.lineIndex}, Layer: {ob.lineLayer}, Dur: {secondPartDuration}, Width: {ob.width}, Height: {ob.height}");

                            }
                            else if (firstPartDuration >= MinWallDuration)
                            {
                                ob.UpdateDuration(firstPartDuration);// Just update the existing obstacle, the second piece of the cut wall is too small
                                //Plugin.Log.Info($"Wall shortened starts: {ob.time} duration: {firstPartDuration}");
                            }
                            else if (secondPartDuration >= MinWallDuration)
                            {
                                // Reuse the obstacle and use it as second part
                                if (secondPartTime != ob.time && secondPartDuration != ob.duration)
                                {
                                    ob.UpdateTime(secondPartTime);
                                    ob.UpdateDuration(secondPartDuration);
                                    obstacles.Enqueue(ob);
                                    //Plugin.Log.Info($"Wall shortened 2nd half starts: {secondPartTime} duration: {secondPartDuration}");
                                }
                            }
                            else
                            {
                                dataItems.Remove(ob);// When this wall is cut, both pieces are too small, remove it
                                //Plugin.Log.Info($"Remove Wall since 1st & 2nd half too small: start: {ob.time} cutTime: {cutTime}");
                            }
                        }
                    }
                    //-------------BW added --- remove any walls whose duration is long enough to get enough rotations that the wall becomes visible again as it exits through the user space (is visible traveling backwards)-------------
                    // Check if the total rotations is more than 75 degrees (5*15) which means it is no longer visible and therefore probably not needed and possibly will be seen leaving the user space
                    if (cutTime >= ob.time && cutTime < ob.time + ob.duration)//checks if rotations occur during a wall
                    {
                        totalRotations += cutAmount;// Total number of rotations during the current obstacle - resets to 0 with each ob

                        if ((totalRotations > 5 || totalRotations < -5))
                        {
                            Plugin.Log.Info($"Wall found with more than 5 rotations during its duration -- starting: {ob.time} duration: {ob.duration} are: {totalRotations} rotations");
                            float newDuration = (cutTime - ob.time) / 2.3f;
                            if (newDuration >= MinWallDuration)
                            {
                                ob.UpdateDuration(newDuration);
                                Plugin.Log.Info($"------New Duration: {ob.duration} which is (cutTime - ob.time)/2 since half the wall occurs past the user play area");
                                break;
                            }
                            else
                            {
                                dataItems.Remove(ob);
                                Plugin.Log.Info($"------Wall removed since shorter than MinWallDuration");
                                break;
                            }
                        }
                    }
                }
            }
            /*
            //This WORKS!
            Queue<ObstacleData> obstacles1 = new Queue<ObstacleData>(dataItems.OfType<ObstacleData>());
            while (obstacles1.Count > 0)
            {
                ObstacleData ob = obstacles1.Dequeue();
                int totalRotations = 0;
                foreach ((float cutTime, int cutAmount) in wallCutMoments)
                {
                    //This works 
                    if (ob.duration <= 0f)
                        break;
                    if (cutTime <= ob.time)
                        continue;
                    if (cutTime > ob.time + ob.duration)
                        break;

                    {
                        totalRotations += cutAmount;
                        if ((totalRotations > 5 || totalRotations < -5))
                        {
                            Plugin.Log.Info($"Wall found with more than 5 rotations during its duration -- starting: {ob.time} duration: {ob.duration} are: {totalRotations} rotations");
                            float newDuration = (cutTime - ob.time) / 2.3f;
                            if (newDuration >= MinWallDuration)
                            {
                                ob.UpdateDuration(newDuration);
                                Plugin.Log.Info($"------New Duration: {ob.duration} which is (cutTime - ob.time)/2 since half the wall occurs past the user play area");
                                break;
                            }
                            else
                            {
                                dataItems.Remove(ob);
                                Plugin.Log.Info($"------Wall removed since shorter than MinWallDuration");
                                break;
                            }
                        }
                        
                }
            }*/

            #endregion

            #region Remove Bombs
            // Remove bombs (just problamatic ones)
            // ToList() is used so the Remove operation does not update the list that is being iterated
            foreach (NoteData nd in dataItems.OfType<NoteData>().Where((e) => e.gameplayType == NoteData.GameplayType.Bomb).ToList())
            {
                foreach ((float cutTime, int cutAmount) in wallCutMoments)
                {
                    if (nd.time >= cutTime - WallFrontCut && nd.time < cutTime + WallBackCut)
                    {
                        if ((nd.lineIndex <= 2 && cutAmount < 0) || (nd.lineIndex >= 1 && cutAmount > 0))
                        {
                            dataItems.Remove(nd);// Will be removed later
                            //Plugin.Log.Info($"Remove Bomb: {nd.time}");
                        }
                    }
                }
            }
            #endregion

            //Plugin.Log.Info($"Emitted {eventCount} rotation events");
            //Plugin.Log.Info($"LimitRotations: {LimitRotations}");
            //Plugin.Log.Info($"BottleneckRotations: {BottleneckRotations}");

            int rotationEventsCount = data.allBeatmapDataItems.OfType<SpawnRotationBeatmapEventData>().Count();
            int obstaclesCount = data.allBeatmapDataItems.OfType<ObstacleData>().Count();

            Plugin.Log.Info($"rotationEventsCount: {rotationEventsCount}");
            Plugin.Log.Info($"obstaclesCount: {obstaclesCount}");

            /*
            if (data is CustomBeatmapData dataCB)
            {
                string json = JsonConvert.SerializeObject(dataCB.beatmapCustomData, Newtonsoft.Json.Formatting.Indented);//this has the choseninfo.dat individual difficulty custom data such as _suggestion chroma and _colorRight etc for custom colors
                Plugin.Log.Info($"Generator: BeatmapData - GetBeatmapCustomData()");
                Plugin.Log.Info(json);
            }
            else
            {
                Plugin.Log.Info("Not custom json data");
            }
            */

            //Test to see if everything is sorted and yes they are sorted without any sort() function. it must be done internally somehow
            /*
            foreach (NoteData nd in dataItems.OfType<NoteData>().ToList()) {
                Plugin.Log.Info($"Notes: {nd.gameplayType} {nd.time}");
            }
            foreach (BeatmapEventData e in dataItems.OfType<BeatmapEventData>().ToList()) {
                Plugin.Log.Info($"Events: {e.time}");
            }
            
            foreach (ObstacleData o in dataItems.OfType<ObstacleData>().ToList()) {
                Plugin.Log.Info($"Obstacles: LineIndex: {o.lineIndex} {o.time}");
            }
            */

            if (LevelUpdatePatcher.BeatSage)
                CleanUpBeatSage(notes, new List<ObstacleData>(dataItems.OfType<ObstacleData>()));

            return data;
        }
        #region CleanUpBeatSage
        //REMOVE NOTES (and bombs) located at the same space and also remove any notes side by side with cutdirections facing out 
        private void CleanUpBeatSage(List<NoteData> notes, List<ObstacleData> obs)
        {
            Plugin.Log.Info($"Beat Sage Map being cleaned! Looking at {notes.Count} notes");
            List<int> indicesToRemove = new List<int>();

            // Iterate through the notes list
            for (int i = 0; i < notes.Count; i++)
            {
                NoteData currentNote = notes[i];

                // Iterate through the next three notes
                for (int j = i + 1; j < Math.Min(i + 4, notes.Count); j++)
                {
                    //Plugin.Log.Info($"Beat Sage j: {j}"); 

                    NoteData nextNote = notes[j];

                    //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                    //    Plugin.Log.Info($"BW 1 ********Found the offending notes!!!!!*******************");

                    // Check if the 2 notes are the same time or within .0001 sec of each other so they appear to almost overlap
                    if (nextNote.time - currentNote.time <= 0.05f)//0.03 seems good. 0.08 will start to catch notes from different beats.
                    {

                        //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                        //    Plugin.Log.Info($"BW 2 ********Found the offending notes!!!!!*******************");

                        //Plugin.Log.Info($"Beat Sage found 2 notes at the exact same time (or close) of {currentNote.time} current note: {currentNote.gameplayType} index: {currentNote.lineIndex} layer: {currentNote.noteLineLayer} --- Nextnote: {nextNote.gameplayType} index: {nextNote.lineIndex} layer: {nextNote.noteLineLayer}");

                        // Check for SIDE-BY-SIDE Notes. -- Check if the two notes (not any bombs) have the same layer, and different index (they may be side-by-side)
                        if (currentNote.noteLineLayer == nextNote.noteLineLayer && // Check for same layer
                            Math.Abs(currentNote.lineIndex - nextNote.lineIndex) == 1 && //side-by-side based on index values
                            currentNote.gameplayType  == GameplayType.Normal && // Check if both are "Normal" notes
                            nextNote.gameplayType     == GameplayType.Normal)
                        {
                            //if (Math.Round(currentNote.time, 2) == 17.58 && Math.Round(nextNote.time, 2) == 17.58)
                            //    Plugin.Log.Info($"BW 3 ********Found the offending notes!!!!!*******************");

                            // Check if the leftmost note has cutDirection Left and the rightmost note has cutDirection Right - and other impossible configurations
                            if (currentNote.lineIndex < nextNote.lineIndex)
                            {
                                if ((currentNote.cutDirection == NoteCutDirection.Left || currentNote.cutDirection == NoteCutDirection.DownLeft || currentNote.cutDirection == NoteCutDirection.UpLeft))
                                {
                                    indicesToRemove.Add(i);
                                    Plugin.Log.Info($"Beat Sage 1 - remove note side-by-side with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                            else
                            {
                                if ((currentNote.cutDirection == NoteCutDirection.Right || currentNote.cutDirection == NoteCutDirection.DownRight || currentNote.cutDirection == NoteCutDirection.UpRight))
                                {
                                    indicesToRemove.Add(i);
                                    Plugin.Log.Info($"Beat Sage 2 - remove note side-by-side with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                        }
                        // Check for ONE-ABOVE-THE-OTHER Notes. -- Check if the two notes (not any bombs) have the same index, and different layer (they may be one-above-the-other)
                        else if (currentNote.lineIndex   == nextNote.lineIndex && // Check for same index
                                Math.Abs(currentNote.noteLineLayer - nextNote.noteLineLayer) == 1 && //one-above-the-other based on layer values
                                currentNote.gameplayType == GameplayType.Normal && // Check if both are "Normal" notes
                                nextNote.gameplayType    == GameplayType.Normal)
                        {
                            // Check if the bottommost note has cutDirection Down and the uppermost note has cutDirection Up - and other impossible configurations
                            if (currentNote.noteLineLayer < nextNote.noteLineLayer)
                            {
                                if ((currentNote.cutDirection == NoteCutDirection.Down || currentNote.cutDirection == NoteCutDirection.DownLeft || currentNote.cutDirection == NoteCutDirection.DownRight))
                                {
                                    indicesToRemove.Add(i);
                                    Plugin.Log.Info($"Beat Sage 1 - remove note one-above-the-other with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                            else
                            {
                                if ((currentNote.cutDirection == NoteCutDirection.Up || currentNote.cutDirection == NoteCutDirection.UpLeft || currentNote.cutDirection == NoteCutDirection.UpRight))
                                {
                                    indicesToRemove.Add(i);
                                    Plugin.Log.Info($"Beat Sage 2 - remove note one-above-the-other with another note in impossible cutDirection at {currentNote.time}");
                                }
                            }
                        }
                        // Check for OVERLAPPING NOTES. -- Check if the two notes have the same lineIndex, and noteLineLayer
                        else if (currentNote.lineIndex     == nextNote.lineIndex &&
                                 currentNote.noteLineLayer == nextNote.noteLineLayer)
                        { 
                            Plugin.Log.Info($"Found overlapping notes at: {currentNote.time} of type: {currentNote.gameplayType} & {nextNote.gameplayType}. Should delete one of them in next log.");
                            // Check if either of the notes is a bomb
                            if (currentNote.gameplayType == GameplayType.Bomb)
                            {
                                // Remove the bomb note (1st note)
                                indicesToRemove.Add(i);
                                Plugin.Log.Info($"Beat Sage 1 - remove bomb overlapping a 2nd bomb/note at {currentNote.time}");
                            }
                            else
                            {
                                // remove the 2nd note whether a bomb or not
                                indicesToRemove.Add(j);
                                j++; //since j is removed, skip it in the next iteration
                                Plugin.Log.Info($"Beat Sage 2 - remove type: {nextNote.gameplayType} overlapping a note at {currentNote.time}");
                            }
                        }
                    }
                    else
                        break;//exits loop if a notes has time beyond .03sec from the currentNote
                }
            }


            //Only compares a note to the following note. so if 4 notes have the same or almost the same time and the 1st and 4th note are on the same grid space, this code will not delete it.
            /*
            // Create a list to store the indices of notes to remove
            List<int> indicesToRemove = new List<int>();

            // Iterate through the notes list
            for (int i = 0; i < notes.Count; i++)
            {
                NoteData currentNote = notes[i];

                // Iterate through the remaining notes
                for (int j = i + 1; j < notes.Count; j++)
                {
                    NoteData nextNote = notes[j];

                    // Check if the 2 notes are the same time or within .0001 sec of each other so they appear to almost overlap
                    if (nextNote.time - currentNote.time <= 0.03f)//0.08 will start to catch bombs from different beats.
                    {
                        // Check if the two notes (not any bombs) have the same lineIndex, and different noteLineLayer (they may be side-by-side)
                        if (currentNote.lineIndex == nextNote.lineIndex &&
                            currentNote.noteLineLayer != nextNote.noteLineLayer && // Check for different layers
                            currentNote.gameplayType == GameplayType.Normal && // Check if both are "Normal" notes
                            nextNote.gameplayType == GameplayType.Normal)
                        {
                            // Check if they are side by side based on layer values
                            if (Math.Abs(currentNote.noteLineLayer - nextNote.noteLineLayer) == 1)
                            {
                                // Check if the leftmost note has cutDirection Left and the rightmost note has cutDirection Right
                                if (currentNote.noteLineLayer < nextNote.noteLineLayer)
                                {
                                    if (currentNote.cutDirection == NoteCutDirection.Left && nextNote.cutDirection == NoteCutDirection.Right)
                                    {
                                        indicesToRemove.Add(j);
                                        Plugin.Log.Info($"Beat Sage 1");

                                    }
                                       
                                }
                                else
                                {
                                    if (currentNote.cutDirection == NoteCutDirection.Right && nextNote.cutDirection == NoteCutDirection.Left)
                                    {
                                        indicesToRemove.Add(i);
                                        Plugin.Log.Info($"Beat Sage 2");
                                    }
                                        
                                }
                            }
                        }
                        // Check if the two notes have the same lineIndex, and noteLineLayer
                        else if (currentNote.lineIndex == nextNote.lineIndex &&
                                 currentNote.noteLineLayer == nextNote.noteLineLayer)
                        {
                            Plugin.Log.Info($"Beat Sage 3/4: CurrentNote-time-Index-Layer: {currentNote.time} {currentNote.lineIndex} {currentNote.noteLineLayer} --NextNote: {nextNote.time} {nextNote.lineIndex} {nextNote.noteLineLayer}");
                            // Check if either of the notes is a bomb
                            if (currentNote.gameplayType == GameplayType.Bomb)
                            {
                                // Remove the bomb note (1st note)
                                indicesToRemove.Add(i);
                                Plugin.Log.Info($"Beat Sage 3");
                            }
                            else
                            {
                                // If its not a bomb, remove the 2nd note
                                indicesToRemove.Add(j);
                                Plugin.Log.Info($"Beat Sage 4");
                            }
                        }
                    }
                    else
                        break;
                }
            }
            */

            // Remove notes inside obstacles (walls)
            foreach (ObstacleData obstacle in obs)
            {
                float obstacleEndTime = obstacle.time + obstacle.duration;

                for (int i = 0; i < notes.Count; i++)
                {
                    NoteData note = notes[i];

                    // Check if the note is within the time and lineIndex and lineLayer boundaries of the obstacle
                    if (note.time >= obstacle.time && note.time <= obstacleEndTime && note.lineIndex == obstacle.lineIndex && note.noteLineLayer >= obstacle.lineLayer)
                    {
                        indicesToRemove.Add(i);
                        //Plugin.Log.Info($"Beat Sage Map had a note/bomb inside a wall at: {note.time} and will be removed later");
                    }
                    else if (note.time > obstacleEndTime)
                    {
                        break;
                    }
                }
            }

            // Remove the duplicate notes from the original list in reverse order to avoid index issues
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                //Plugin.Log.Info($"Beat Sage Map had a note/bomb cut at: {notes[indexToRemove].time}");
                notes.RemoveAt(indexToRemove);   
            }
            if (indicesToRemove.Count == 0)
                Plugin.Log.Info($"Nothing to clean!!!!!!");

            // At this point, 'notes' will contain the unique notes with no duplicates, with both specified rules applied.

        }
        #endregion

    }//Public Class Generator360

    public static class BeatmapDataItemExtensions
    {
        public static void UpdateTime(this BeatmapDataItem item, float newTime)
        {
            FieldHelper.SetProperty(item, "<time>k__BackingField", newTime);
        }
    }
    
}

