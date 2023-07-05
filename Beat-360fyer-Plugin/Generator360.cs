using Beat360fyerPlugin.Patches;
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
//using Newtonsoft.Json;//BW added to work with JSON files - rt click solution explorer and manage NuGet packages
//using System.IO;//BW for file writing

namespace Beat360fyerPlugin
{
    public class Generator360
    {
        /// <summary>
        /// The preferred bar duration in seconds. The generator will loop the song in bars. 
        /// This is called 'preferred' because this value will change depending on a song's bpm (will be aligned around this value).
        /// Affects the speed at which the rotation occurs. It will not affect the total number of rotations or the range of rotation.
        /// BW CREATED CONFIG ROTATION  SPEED to allow user to set this.
        /// </summary>
        public float PreferredBarDuration { get; set; } = 1.84f;//BW I like 1.5f instead of 1.84f but very similar to changing LimitRotations, 1.0f is too much and 0.2f freezes beat saber  // Calculated from 130 bpm, which is a pretty standard bpm (60 / 130 bpm * 4 whole notes per bar ~= 1.84)
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
        public float RotationAngleMultiplier { get; set; } = 1f;//BW added this to lessen/increase rotation angle amount. 1 means 15 decre
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
            BeatmapData data = bmData.GetCopy();
            LinkedList<BeatmapDataItem> dataItems = data.allBeatmapDataItems;

            bool containsCustomWalls = dataItems.Count((e) => e is CustomObstacleData d && (d.customData?.ContainsKey("_position") ?? false)) > 12;
            Plugin.Log.Info($"Contains custom walls: {containsCustomWalls}");

            // Amount of rotation events emitted
            int eventCount = 0;
            // Current rotation
            int totalRotation = 0;
            // Moments where a wall should be cut
            List<(float, int)> wallCutMoments = new List<(float, int)>();
            // Previous spin direction, false is left, true is right
            bool previousDirection = true;
            float previousSpinTime = float.MinValue;


            //Each rotation is 15 degree increments so 24 positive rotations is 360. Negative numbers rotate to the left, positive to the right
            void Rotate(float time, int amount, SpawnRotationBeatmapEventData.SpawnRotationEventType moment, bool enableLimit = true)
            {
                //Allows 4*15=60 degree turn max and -60 degree min
                if (amount == 0)
                    return;
                if (amount < -4)
                    amount = -4;
                if (amount > 4)
                    amount = 4;

                if (enableLimit)
                {
                    if (totalRotation + amount > LimitRotations)
                        amount = Math.Min(amount, Math.Max(0, LimitRotations - totalRotation));
                    else if (totalRotation + amount < -LimitRotations)
                        amount = Math.Max(amount, Math.Min(0, -(LimitRotations + totalRotation)));
                    if (amount == 0)
                        return;

                    totalRotation += amount;
                }

                previousDirection = amount > 0;
                eventCount++;
                wallCutMoments.Add((time, amount));

                //BW Discord help said to change InsertBeatmapEventData to InsertBeatmapEventDataInOrder which allowed content to be stored to data.
                data.InsertBeatmapEventDataInOrder(new SpawnRotationBeatmapEventData(time, moment, amount * 15.0f * RotationAngleMultiplier));//discord suggestion

            }

            float beatDuration = 60f / bpm;

            // Align PreferredBarDuration to beatDuration
            float barLength = beatDuration;
            while (barLength >= PreferredBarDuration * 1.25f / RotationSpeedMultiplier)
                barLength /= 2f;
            while (barLength < PreferredBarDuration * 0.75f / RotationSpeedMultiplier)
                barLength *= 2f;

            List<NoteData> notes = dataItems.OfType<NoteData>().ToList();
            List<NoteData> notesInBar = new List<NoteData>();
            List<NoteData> notesInBarBeat = new List<NoteData>();

            // Align bars to first note, the first note (almost always) identifies the start of the first bar
            float firstBeatmapNoteTime = notes[0].time;

#if DEBUG
            Plugin.Log.Info($"Setup bpm={bpm} beatDuration={beatDuration} barLength={barLength} firstNoteTime={firstBeatmapNoteTime}");
#endif

            for (int i = 0; i < notes.Count;)
            {
                float currentBarStart = Floor((notes[i].time - firstBeatmapNoteTime) / barLength) * barLength;
                float currentBarEnd = currentBarStart + barLength - 0.001f;

                notesInBar.Clear();
                for (; i < notes.Count && notes[i].time - firstBeatmapNoteTime < currentBarEnd; i++)
                {
                    // If isn't bomb
                    if (notes[i].cutDirection != NoteCutDirection.None)
                        notesInBar.Add(notes[i]);
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
                    for (int s = 0; s < 24; s++)
                    {
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

                if (barDivider <= 0)
                    continue;
#if DEBUG
                StringBuilder builder = new StringBuilder();
#endif
                // Iterate all the notes in the current bar in barDiviver pieces (bar is split in barDiviver pieces)
                float dividedBarLength = barLength / barDivider;
                for (int j = 0, k = 0; j < barDivider && k < notesInBar.Count; j++)
                {
                    notesInBarBeat.Clear();
                    for (; k < notesInBar.Count && Floor((notesInBar[k].time - firstBeatmapNoteTime - currentBarStart) / dividedBarLength) == j; k++)
                    {
                        notesInBarBeat.Add(notesInBar[k]);
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

                    NoteData afterLastNote = (k < notesInBar.Count ? notesInBar[k] : i < notes.Count ? notes[i] : null);

                    // Determine amount to rotate at once
                    // TODO: Create formula out of these if statements
                    int rotationCount = 1;
                    if (afterLastNote != null)
                    {
                        float timeDiff = afterLastNote.time - lastNote.time;
                        if (notesInBarBeat.Count >= 1)
                        {
                            if (timeDiff >= barLength)
                                rotationCount = 3;
                            else if (timeDiff >= barLength / 8)
                                rotationCount = 2;
                        }
                    }

                    int rotation = 0;
                    if (leftCount > rightCount)
                    {
                        // Most of the notes are pointing to the left, rotate to the left
                        rotation = -rotationCount;
                    }
                    else if (rightCount > leftCount)
                    {
                        // Most of the notes are pointing to the right, rotate to the right
                        rotation = rotationCount;
                    }
                    else
                    {
                        // Rotate to left or right
                        if (totalRotation >= BottleneckRotations)
                        {
                            // Prefer rotating to the left if moved a lot to the right
                            rotation = -rotationCount;
                        }
                        else if (totalRotation <= -BottleneckRotations)
                        {
                            // Prefer rotating to the right if moved a lot to the left
                            rotation = rotationCount;
                        }
                        else
                        {
                            // Rotate based on previous direction
                            rotation = previousDirection ? rotationCount : -rotationCount;
                        }
                    }

                    if (totalRotation >= BottleneckRotations && rotationCount > 1)
                    {
                        rotationCount = 1;
                    }
                    else if (totalRotation <= -BottleneckRotations && rotationCount < -1)
                    {
                        rotationCount = -1;
                    }

                    if (totalRotation >= LimitRotations - 1 && rotationCount > 0)
                    {
                        rotationCount = -rotationCount;
                    }
                    else if (totalRotation <= -LimitRotations + 1 && rotationCount < 0)
                    {
                        rotationCount = -rotationCount;
                    }

                    // Finally rotate
                    Rotate(lastNote.time, rotation, SpawnRotationBeatmapEventData.SpawnRotationEventType.Late);

                    //Plugin.Log.Info($"Total Rotation: {totalRotation} Rotation: {rotation}");

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

                    // Generate wall. BW fyi v2 wall with _type 1 or v3 wall y: noteLineLayer.Top or 2 is a crouch wall -- but must be must be wide enough to and over correct x: lineIndex to be over a player
                    if (WallGenerator && !containsCustomWalls)
                    {
                        float wallTime = currentBarBeatStart;
                        float wallDuration = dividedBarLength;

                        // Check if there is already a wall
                        bool generateWall = true;
                        foreach (ObstacleData obs in dataItems.OfType<ObstacleData>())
                        {
                            if (obs.time + obs.duration >= wallTime && obs.time < wallTime + wallDuration)
                            {
                                generateWall = false;
                                break;
                            }
                        }

                        if (generateWall && afterLastNote != null)
                        {
                            if (!notesInBarBeat.Any((e) => e.lineIndex == 3))//line index 3 is far right
                            {
                                int wallHeight = notesInBarBeat.Any((e) => e.lineIndex == 2) ? 1 : 3;//BW I think this just sets some walls shorter or taller for visual interest

                                if (afterLastNote.lineIndex == 3 && !(wallHeight == 1 && afterLastNote.noteLineLayer == NoteLineLayer.Base))
                                    wallDuration = afterLastNote.time - WallBackCut - wallTime;

                                if (wallDuration > MinWallDuration)
                                {   //BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, 3, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, 1, 5));//note width is always 1 here. BW changed to make all walls 5 high since this version of plugin shortens height of walls which i don't like - default:  wallHeight)); wallHeight));
                                }
                            }
                            if (!notesInBarBeat.Any((e) => e.lineIndex == 0))//line index 0 is far left
                            {
                                int wallHeight = notesInBarBeat.Any((e) => e.lineIndex == 1) ? 1 : 3;

                                if (afterLastNote.lineIndex == 0 && !(wallHeight == 1 && afterLastNote.noteLineLayer == NoteLineLayer.Base))
                                    wallDuration = afterLastNote.time - WallBackCut - wallTime;

                                if (wallDuration > MinWallDuration)
                                {   //Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    data.AddBeatmapObjectDataInOrder(new ObstacleData(wallTime, 0, wallHeight == 1 ? NoteLineLayer.Top : NoteLineLayer.Base, wallDuration, 1, 5));//BW wallHeight));
                                }
                            }
                        }
                    }

#if DEBUG
                    Plugin.Log.Info($"[{currentBarBeatStart}] Rotate {rotation} (c={notesInBarBeat.Count},lc={leftCount},rc={rightCount},lastNotes={lastNotes.Count()},rotationTime={lastNote.time + 0.01f},afterLastNote={afterLastNote?.time},rotationCount={rotationCount})");
#endif
                }


#if DEBUG
                Plugin.Log.Info($"[{currentBarStart + firstBeatmapNoteTime}({(currentBarStart + firstBeatmapNoteTime) / beatDuration}) -> {currentBarEnd + firstBeatmapNoteTime}({(currentBarEnd + firstBeatmapNoteTime) / beatDuration})] count={notesInBar.Count} segments={builder} barDiviver={barDivider}");
#endif
            }//End for loop over all notes

            //BW noodle extensions causes BS crash in the section somewhere below. Could drill down and figure out why. Haven't figured out how to test for noodle extensions but noodle extension have custom walls that crash Beat Saber so BW added test for custom walls.
            if (!containsCustomWalls)
            {
                //Cut walls, walls will be cut when a rotation event is emitted
                Queue<ObstacleData> obstacles = new Queue<ObstacleData>(dataItems.OfType<ObstacleData>());

                while (obstacles.Count > 0)
                {
                    ObstacleData ob = obstacles.Dequeue();
                    foreach ((float cutTime, int cutAmount) in wallCutMoments)
                    {
                        if (ob.duration <= 0f)
                            break;

                        // Do not cut a margin around the wall if the wall is at a custom position
                        bool isCustomWall = false;
                        //if (ob.customData != null)
                        //{
                        //    isCustomWall = ob.customData.ContainsKey("_position");
                        //}
                        float frontCut = isCustomWall ? 0f : WallFrontCut;
                        float backCut = isCustomWall ? 0f : WallBackCut;


                        if (!isCustomWall && ((ob.lineIndex == 1 || ob.lineIndex == 2) && ob.width == 1))//BW lean walls that are only width 1 and hard to see coming in 360)
                        {
                            dataItems.Remove(ob);
                        }
                        else if (!isCustomWall && !AllowLeanWalls && ((ob.lineIndex == 0 && ob.width == 2) || (ob.lineIndex == 2 && ob.width > 1)))//BW lean walls
                        {
                            dataItems.Remove(ob);
                        }
                        else if (!isCustomWall && !AllowCrouchWalls && (ob.lineIndex == 0 && ob.width > 2))//BW crouch walls
                        {
                            dataItems.Remove(ob);
                        }
                        // If moved in direction of wall
                        else if (isCustomWall || (ob.lineIndex <= 1 && cutAmount < 0) || (ob.lineIndex >= 2 && cutAmount > 0))
                        {
                            int cutMultiplier = Math.Abs(cutAmount);
                            if (cutTime > ob.time - frontCut && cutTime < ob.time + ob.duration + backCut * cutMultiplier)
                            {
                                float originalTime = ob.time;
                                float originalDuration = ob.duration;

                                float firstPartTime = ob.time;// 225.431: 225.631(0.203476) -> 225.631() <|> 225.631(0.203476)
                                float firstPartDuration = (cutTime - backCut * cutMultiplier) - firstPartTime; // -0.6499969
                                float secondPartTime = cutTime + frontCut; // 225.631
                                float secondPartDuration = (ob.time + ob.duration) - secondPartTime; //0.203476

                                if (firstPartDuration >= MinWallDuration && secondPartDuration >= MinWallDuration)
                                {
                                    // Update duration of existing obstacle
                                    ob.UpdateDuration(firstPartDuration);

                                    // And create a new obstacle after it
                                    ObstacleData secondPart = new ObstacleData(secondPartTime, ob.lineIndex, ob.lineLayer, secondPartDuration, ob.width, ob.height);
                                    data.AddBeatmapObjectDataInOrder(secondPart);//BW Discord help said to change AddBeatmapObjectData to AddBeatmapObjectDataInOrder which allowed content to be stored to data.
                                    obstacles.Enqueue(secondPart);
                                }
                                else if (firstPartDuration >= MinWallDuration)
                                {
                                    // Just update the existing obstacle, the second piece of the cut wall is too small
                                    ob.UpdateDuration(firstPartDuration);
                                }
                                else if (secondPartDuration >= MinWallDuration)
                                {
                                    // Reuse the obstacle and use it as second part
                                    if (secondPartTime != ob.time && secondPartDuration != ob.duration)
                                    {
                                        //Plugin.Log.Info("Queue 7");
                                        ob.UpdateTime(secondPartTime);
                                        ob.UpdateDuration(secondPartDuration);
                                        obstacles.Enqueue(ob);
                                    }
                                }
                                else
                                {
                                    // When this wall is cut, both pieces are too small, remove it
                                    dataItems.Remove(ob);
                                }
#if DEBUG
                                Plugin.Log.Info($"Split wall at {cutTime}: {originalTime}({originalDuration}) -> {firstPartTime}({firstPartDuration}) <|> {secondPartTime}({secondPartDuration}) cutMultiplier={cutMultiplier}");
#endif
                            }
                        }
                    }

                }

            }
            // Remove bombs (just problamatic ones)
            // ToList() is used so the Remove operation does not update the list that is being iterated
            foreach (NoteData nd in dataItems.OfType<NoteData>().Where((e) => e.cutDirection == NoteCutDirection.None).ToList())
            {
                foreach ((float cutTime, int cutAmount) in wallCutMoments)
                {
                    if (nd.time >= cutTime - WallFrontCut && nd.time < cutTime + WallBackCut)
                    {
                        if ((nd.lineIndex <= 2 && cutAmount < 0) || (nd.lineIndex >= 1 && cutAmount > 0))
                        {
                            // Will be removed later
                            dataItems.Remove(nd);
                        }
                    }
                }
            }

            //Plugin.Log.Info($"Emitted {eventCount} rotation events");
            //Plugin.Log.Info($"LimitRotations: {LimitRotations}");
            //Plugin.Log.Info($"BottleneckRotations: {BottleneckRotations}");

            int rotationEventsCount = data.allBeatmapDataItems.OfType<SpawnRotationBeatmapEventData>().Count();
            int obstaclesCount = data.allBeatmapDataItems.OfType<ObstacleData>().Count();

            Plugin.Log.Info($"rotationEventsCount: {rotationEventsCount}");
            Plugin.Log.Info($"obstaclesCount: {obstaclesCount}");

            return data;
        }

    }

    public static class BeatmapDataItemExtensions
    {
        public static void UpdateTime(this BeatmapDataItem item, float newTime)
        {
            FieldHelper.SetProperty(item, "<time>k__BackingField", newTime);
        }
    }
}

