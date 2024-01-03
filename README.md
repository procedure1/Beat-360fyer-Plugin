# Beat Saber 360fyer Plugin
A Beat Saber mod to play most beatmaps in 360 or 90 degree mode. 

This mod was created by the genius CodeStix. https://github.com/CodeStix/
I have updated the mod since it has been dormant for a long time.

I added menu settings, lots of customization and now v3 maps with arcs and chains are supported. Much of the customization in this update is centered around attempted visual improvements for 360 maps. As you probably know, 360 maps have had the same environment since they first came out in 2019. The 360 environment is very low key with dim narrow lasers compared to modern environments. Since the 360 environment doesn't work with v3 GLS lights (Group Lighting System), new OST GLS maps have no lights in 360. So I attempted to make 360 a bit flashier :) This can easily be disabled. Note: Noodle maps are currently disabled for 360/90.

[![showcase video](https://github.com/CodeStix/Beat-360fyer-Plugin/raw/master/preview.gif)](https://www.youtube.com/watch?v=xUDdStGQwq0)
## Beat Saber PC Version
v1.34.2

## Installation

- You can install this mod using ModAssistant.
- Or install this mod manually by downloading a release from the [releases]https://github.com/procedure1/Beat-360fyer-Plugin/releases) tab and placing it in the `Plugins/` directory of your modded Beat Saber installation.
- Requires CustomJSONData Mod (also with ModAssistant)

After doing this, **every** beatmap will have the 360 degree gamemode enabled. Just choose `360` when you select a song. The level will be generated once you start the level. 90 degree levels can also be enabled in the menu.

## Algorithm

The algorithm is completely deterministic and does not use random chance, it generates rotation events based on the notes in the *Standard* beatmap (the base map can be changed in the menus from "Standard" to "OneSaber", "NoArrows", or "NinetyDegree" as well).

Wireless headset users can use the "Wireless 360" menu setting which has no rotation limits and less tendencies to reverse direction. Tethered headset users have rotation limiting settings to make sure to not ruin the cable by rotating too much. You can also use these settings if your play space is limited (for example you could limit rotations to 150° or 180° if you want to face forward only).

## Menu Settings and Config file

There is a settings menu in-game. (Or you can tweak settings in the `Beat Saber/UserData/Beat-360fyer-Plugin.json` config file using a text editor.)

|Option|Description|
|---|---|
|360||
|`Show Generated 360`| If you want to enable 360 degree mode generation. Requires restart for maps that have already been selected in the menu. (default = `true`)|
|`Wireless 360`| For wireless VR with no rotation restrictions and less tendencies to reverse direction. (default = `false`)|
|`Limit Rotations 360`| Disabled if Wireless360 is enabled. For wired headsets use 360° or less. 720° will allow 2 full revolutions (cable rip!) etc. Disables score submission if set less than 150. (default = `360`)|
|Rotation||
|`Rotation Speed Multiplier`| Increase or decrease rotations. Disables score submission if set below 0.8. I like to set this as high as 1.7 for tons of rotations. Large values will begin to move rotations outside of peripheral view. (default = `1.0`)|
|`Arc Fix`| Remove rotations during arcs to keep the tails connected to tail notes. Maps with very long arcs will completely halt rotations during the arcs. Turn this off for more rotations. (default = `true`)|
|`Add Xtra Rotation`| Adds more rotations in a consistent direction. Useful for maps with low overall rotation. Can feel slightly monotonous for maps that already have lots of rotations. I like to keep this set to true. (default = `false`)|
|`Max Rotation Size`| Rotations are typically 15° and 30°. 45° rotations may fall outside peripheral vision without a large FOV headset. (default = `30°`)|
|Walls||
|`Enable Wall Generator`| Walls are cool in 360! Adds more walls for visual interest. Set to `false` to disable wall generation (default = `true`)|
|`Big Walls`| Adds super wide walls for visual interest if Wall Generator is enabled.  (default = `true`)|
|`Allow Crouch Walls`| Allow crouch walls. This can be difficult to see coming in a fast 360 map. I like to keep this set to true. (default = `false`)|
|`Allow Lean Walls`| Allow lean walls. This can be difficult to see coming in a fast 360 map. I like to keep this set to true. (default = `false`)|
|Lighting||
|`Big Lasers`| Scales up laser size. This affects all 360/90 degree maps (whether generated by this plugin or not).  (default = `true`)|
|`Bright Lights`| Brightens low key lights. This affects all 360/90 degree maps (whether generated by this plugin or not). (default = `true`)|
|`Boost Lighting`| Adds 'boost' lighting events to maps that don't have them. Occurs based on rotations. Use COLORS > OVERRIDE DEFAULT COLORS. (default = `true`)|
|`Automapper Lights`| Adds note-based generated lighting events to maps that don't have them. Thanks to Loloppe (ChroMapper-AutoMapper). I tweaked this so any problems are my fault :) Human-crafted lights great! Machine-made lights ok. No lights sucks! (default = `true`)|
|`Frequency Multiplier`| Lower this to reduce the number of automapped light events. (default = `1`)|
|`Brightness Multiplier`| Controls the brightness of automapped lights. (default = `1`)|
|`Light Style`| Automapper light styles. (default = `Med Flash`)|
|90||
|`Show Generated 90`| If you want to enable 90 degree mode generation. Requires restart for maps that have already been selected in the menu. (default = `false`)|
|`Limit Rotations 90`| Limit or expand the rotation amount for 90° maps. Works the same as the setting above for 360° maps. Disables score submission if set less than 90. (default = `90`)|
|One Saber||
|`Only One Saber`| If you want to only keep one color during generation, this allows you to play `OneSaber` in 360 degree on any level, even if a OneSaber gamemode doesn't exist. This is right handed. (default = `false`, caution: experimental)|
|`Left Handed One Saber`| If you want to play left handed one saber mode (default = `false`, caution: experimental)|
|Base Map||
|`Based On`|Which game mode to generate the 360 mode from. Can be `"Standard"` (default), `"OneSaber"` or `"NoArrows"` or  `"90Degree"`. Requires restart for maps that have already been selected in the menu.|

## How to build

To test and build this project locally, do the following:
1. Clone this repo & open the project in Visual Studio
2. Make sure to add the right dll references. The needed dlls are located in your modded Beat Saber installation folder.
3. Build. Visual Studio should copy the plugin to your Beat Saber installation automatically.

## Todo

- Allow rotatons during arcs while keeping the tails lined up with tail notes.
