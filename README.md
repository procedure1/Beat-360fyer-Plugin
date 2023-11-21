# Beat Saber 360fyer Plugin
A Beat Saber mod to play most beatmaps in 360 or 90 degree mode. 

This mod was created by the genius CodeStix. https://github.com/CodeStix/
I have updated the mod since it has been dormant for a long time.

I added menu settings and lots of customization. Much of the customization in this update is centered around attempted visual improvements for 360 maps. As you probably know, 360 maps have had the same environment since they first came out in 2019. The 360 environment is very low key with dim narrow lasers compared to modern environments. So I attempted to make it a bit flashier :) Turn it off if you don't approve. Note: Noodle maps are currently disabled for 360.
[![showcase video](https://github.com/CodeStix/Beat-360fyer-Plugin/raw/master/preview.gif)](https://www.youtube.com/watch?v=xUDdStGQwq0)
## Beat Saber PC Version
v1.31.1

## Installation

- You can install this mod using ModAssistant.
- Or install this mod manually by downloading a release from the [releases]https://github.com/procedure1/Beat-360fyer-Plugin/releases) tab and placing it in the `Plugins/` directory of your modded Beat Saber installation.
- Requires CustomJSONData Mod

After doing this, **every** beatmap will have the 360 degree gamemode enabled. Just choose `360` when you select a song. The level will be generated once you start the level. 90 degree levels can also be enabled in the menu.

## Algorithm

The algorithm is completely deterministic and does not use random chance, it generates rotation events based on the notes in the *Standard* beatmap (the base map can be changed in the menus from "Standard" to "OneSaber", "NoArrows", or "NinetyDegree" as well).

Wireless headset users can use the "Wireless 360" menu setting which has no rotation limits and less tendencies to reverse direction. Tethered headset users have rotation limiting settings to make sure to not ruin the cable by rotating too much. You can also use these settings if your play space is limited (for example you could limit rotations to 150° or 180° if you want to face forward only).

## Menu Settings and Config file

There is a settings menu in-game. Or you can tweak settings in the `Beat Saber/UserData/Beat-360fyer-Plugin.json` config file. You should open this file with notepad or another text editor.

|Option|Description|
|---|---|
||360|
|`ShowGenerated360`| If you want to enable 360 degree mode generation. Requires restart for maps that have already been selected in the menu. (default = `true`)|
|`Wireless360`| For wireless VR with no rotation restrictions and less tendencies to reverse direction. (default = `false`)|
|`LimitRotations360`| Disabled if Wireless360 is enabled. For wired headsets use 360° or less. 720° will allow 2 full revolutions (cable rip!) etc. Disables score submission if set less than 150. (default = `360`)|
||Rotation|
|`RotationSpeedMultiplier`| Increase or decrease rotations. Disables score submission if set below 0.8. I like to set this as high as 1.7 for tons of rotations. Large values will begin to move rotations outside of peripheral view. (default = `1.0`)|
|`ArcFixFull`| Remove rotations during arcs to keep the tails connected to tail notes. Maps with very long arcs will completely halt rotations during the arcs. Turn this off for more rotations. (default = `true`)|
|`AddXtraRotation`| Adds more rotations in a consistent direction. Useful for maps with low overall rotation. Can feel slightly monotonous for maps that already have lots of rotations. I like to keep this set to true. (default = `false`)|
||Walls|
|`EnableWallGenerator`| Walls are cool in 360! Adds more walls for visual interest. Set to `false` to disable wall generation (default = `true`)|
|`BigWalls`| Adds super wide walls for visual interest if Wall Generator is enabled.  (default = `true`)|
|`AllowCrouchWalls`| Allow crouch walls. This can be difficult to see coming in a fast 360 map. I like to keep this set to true. (default = `false`)|
|`AllowLeanWalls`| Allow lean walls. This can be difficult to see coming in a fast 360 map. I like to keep this set to true. (default = `false`)|
||Lighting|
|`BigLasers`| Scales up laser size. This affects all 360/90 degree maps (whether generated by this plugin or not).  (default = `true`)|
|`BrightLights`| Brightens low key lights. This affects all 360/90 degree maps (whether generated by this plugin or not). (default = `true`)|
|`BoostLighting`| Adds 'boost' lighting events to maps that don't have an authored color scheme. Occurs based on rotations. Use COLORS > OVERRIDE DEFAULT COLORS. (default = `true`)|
||90|
|`ShowGenerated90`| If you want to enable 90 degree mode generation. Requires restart for maps that have already been selected in the menu. (default = `false`)|
|`LimitRotations90`| Limit or expand the rotation amount for 90° maps. Works the same as the setting above for 360° maps. Disables score submission if set less than 90. (default = `90`)|
||One Saber|
|`OnlyOneSaber`| If you want to only keep one color during generation, this allows you to play `OneSaber` in 360 degree on any level, even if a OneSaber gamemode doesn't exist. This is right handed. (default = `false`, caution: experimental)|
|`LeftHandedOneSaber`| If you want to play left handed one saber mode (default = `false`, caution: experimental)|
||Base Map|
|`BasedOn`|Which game mode to generate the 360 mode from. Can be `"Standard"` (default), `"OneSaber"` or `"NoArrows"` or  `"90Degree"`. Requires restart for maps that have already been selected in the menu.|

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

**Config file not working? Config not doing anything?** Make sure you saved the file in the original location, and make sure you didn't place a comma after the last option (see working example config above).


## How to build

To test and build this project locally, do the following:
1. Clone this repo & open the project in Visual Studio
2. Make sure to add the right dll references. The needed dlls are located in your modded Beat Saber installation folder.
3. Build. Visual Studio should copy the plugin to your Beat Saber installation automatically.

## Todo

- Allow rotatons during arcs while keeping the tails lined up with tail notes.
