# Beat-360fyer-Plugin
A Beat Saber plugin to play any beatmap in 360 or 90 degree mode. 

This mod was created by the genius CodeStix. https://github.com/CodeStix/
I have updated the mod since it has been dormant for a long time.

This readme is a work-in-progress.

[![showcase video](https://github.com/CodeStix/Beat-360fyer-Plugin/raw/master/preview.gif)](https://www.youtube.com/watch?v=xUDdStGQwq0)
## Beat Saber PC Version
v1.31.1

## Installation

- You can install this mod using ModAssistant.
- Or install this mod manually by downloading a release from the [releases](https://github.com/CodeStix/Beat-360fyer-Plugin/releases) tab and placing it in the `Plugins/` directory of your modded Beat Saber installation.
- Requires CustomJSONData Mod

After doing this, **every** beatmap will have the 360/90 degree gamemode enabled. Just choose `360` when you select a song. The level will be generated once you start the level.

## Algorithm

The algorithm is completely deterministic and does not use random chance, it generates rotation events based on the notes in the *Standard* beatmap. 

**It also makes sure to not ruin your cable by rotating too much!** 

## Config file

There is a settings menu in-game. Or you can tweak settings in the `Beat Saber/UserData/Beat-360fyer-Plugin.json` config file. You should open this file with notepad or another text editor.

```js
{
  "Wireless360": false,
  "LimitRotations360": 360.0,
  "LimitRotations90": 90.0,
  "EnableWallGenerator": true,
  "BigWalls": true,
  "BigLasers": true,
  "BrightLights": true,
  "BoostLighting": true,
  "AllowCrouchWalls": false,
  "AllowLeanWalls": false,
  "RotationSpeedMultiplier": 1.0,
  "ShowGenerated360": true,
  "ShowGenerated90": false,
  "OnlyOneSaber": false,
  "LeftHandedOneSaber": false,
  "AddXtraRotation": false,
  "RotationGroupLimit": 10.0,
  "RotationGroupSize": 12.0,
  "ArcFixFull": true,
  "BasedOn": "Standard"
}
```
|Option|Description|
|---|---|
|`Wireless360`| `false` For wireless VR with no rotation restrictions and no tendencies to reverse direction. (default = `false`)|
|`LimitRotations360`| For wired headsets use 360° or less. 720° will allow 2 full revolutions (cable rip!). (default = `360`)|
|`LimitRotations90`| For wired headsets in 90 degree mode. Add more or less rotation if you want. (default = `90`)|
|`EnableWallGenerator`| Set to `false` to disable wall generation (default = `true`). Walls are not generated for NoodleExtension levels by default.|
|`AllowCrouchWalls`| Allow crouch walls. This can be difficult to see coming in a fast 360 map. (default = `false`)|
|`AllowLeanWalls`| Allow lean walls. This can be difficult to see coming in a fast 360 map. (default = `false`)|
|`RotationAngleMultiplier`| Default 1.0 rotates in increments of 15°. 2.0 will rotate in increments of 30° etc. (default = `1.0`)|
|`RotationSpeedMultiplier`| Change how frequently rotations are spawned. (default = `1.0`)|
|`ShowGenerated360`| `true` If you want to enable 360 degree mode generation (default = `true`)|
|`ShowGenerated90`| `true` If you want to enable 90 degree mode generation (default = `false`)|
|`OnlyOneSaber`|`true` If you want to only keep one color during generation, this allows you to play `OneSaber` in 360 degree on any level, also the ones that don't have a OneSaber gamemode. (default = `false`, caution: experimental)|
|`LeftHandedOneSaber`|`true` If you want to play left handed one saber mode (default = `false`, caution: experimental)|
|`BasedOn`|Which game mode to generate the 360 mode from, can be `"Standard"` (default), `"OneSaber"` or `"NoArrows"` or  `"90Degree"`|

**Not working? Config not doing anything?** Make sure you saved the file in the original location, and make sure you didn't place a comma after the last option (see working example config above).


## How to build

To test and build this project locally, do the following:
1. Clone this repo & open the project in Visual Studio
2. Make sure to add the right dll references. The needed dlls are located in your modded Beat Saber installation folder.
3. Build. Visual Studio should copy the plugin to your Beat Saber installation automatically.

## Todo

- Fix noodle crashes
- Fix arc's that don't line-up at the end
