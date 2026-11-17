# Bouncing Back (Getting Over It style 2D climber)

## Basics

- Unity 6000.6.9f1. Use Unity 6 APIs (linearVelocity, linearDamping, FindAnyObjectByType).
- C# only, no new packages. Scripts are in Assets/Scripts, editor tools in Assets/Editor, the scene is Assets/Scenes/TestLevel.unity.
- Source control is git.
- Active Input Handling is the new Input System. Mouse and keys go through MouseInput.cs.
- The code has no comments on purpose. This file is the documentation.
- Hammer physics is based on rossning92/getting-0ver-it. Keep the credit line in README.md.
- Unity is usually open, so batch mode compile fails. Check code by compiling with Roslyn against the Unity DLLs.
- Never use em dashes in text.

## Menu order after a change

Stop Play mode, let Unity compile, then BounceBack > Calm Map Textures, BounceBack > Build Map, BounceBack > Build Background Layers, BounceBack > Add Background, save the scene.
BounceBack > Build Test Scene builds the player and the whole level in one go.

## The player

- Two rigidbodies. The body is a dynamic capsule with real gravity. The hammer head is a small circle joined to the body by a rope (DistanceJoint2D, max distance only). The head sticks to everything.
- PlayerController drives it in FixedUpdate: aim the hammer from the cursor, track the head contact, pull the body toward the planted head, cap the speeds, move the head.
- PlayerController.rigScale (2.5) scales lengths and speeds. Gravity is a true 9.81 and does not scale.
- Colours and sprites: rat_main_1.png (Assets/Sprites) is drawn by PlayerVisuals. The colliders never change with the art.
- Super jump: landings fill a yellow pool, Space spends it and launches straight up. Crashing into a block during the jump makes the rat dizzy. See SuperJump, DizzyStars, ChargeAura and the numbers in PlayerSettings.cs.
- Lava: body or hammer head in a LavaZone for 2 seconds burns the rat. It goes back to the spawn point (PlayerSpawn).
- Surfaces: BlockType decides the grip (rock, ice, sticky, bounce pad). The bounce pad only bounces from its top.
- Hook grip and launch release are in ContactTracker and PlayerController.ReleaseIfLaunching.
- Sounds: SoundEffects loads hit_ground and click from Resources/Sfx. BackgroundMusic loops Resources/Music/background if that file exists (the repo ships without music, so it stays silent).

## Menu and opening

- MainMenu (OnGUI) shows at start: PLAY, SETTINGS (volume, fullscreen), QUIT. The picture is Resources/Menu/main_menu_background.png with a waving red cape drawn in code.
- OpeningScene starts after PLAY (not after an R restart). Bedroom picture with the rat lying on the bed and three typed lines, then a short fall (dark sky, moon, small stars), then the card MISSION / Reach the moon., then a fade into the game. Escape skips it. Pictures are in Resources/Opening.
- All OnGUI text goes through GuiText.Label, which rounds the rect to whole pixels. Font sizes are whole numbers from the screen height. This keeps the text sharp.

## Ending

- The invisible trigger EruptionTrigger (6 x 4 box at the tip of the right mountain) and CheeseMoon (90 units above the tip) are made by MapSky. CutsceneDirector finds them by name.
- The float starts when the rat body touches the trigger or goes above EndingSettings.floatStartY (96). Control is frozen (PlayerController.Frozen), gravity is off. The rat and the hammer rise straight up from where the rat is, 3.5 units per second after a 2 second ramp, with a slow spin. Before it starts, a capsule cast upward checks the way is clear. If it is not, the rat is moved sideways in free space, at most 4 units. During the float the body and head ignore all level colliders.
- Letterbox bars slide in, the music gets quiet, AscensionGlow draws soft white sprites around the rat.
- After 4.5 seconds EndingVideo plays Assets/StreamingAssets/Ending/part1.mp4 and part2.mp4 full screen with fades. The music is silent. Then two lines on black, "I woke up." and "The whole climb was a dream.", then the win card (run time, credits, R restarts). If a video file is missing, the float goes on and touching the moon plays a flash, a kick, a fade and the win card.
- After a lava death the float can start again (CutsceneDirector.OnPlayerDied).
- Gravity (SkyStage, EndingSettings.GravityScaleAt): 1 up to y 33, smoothstep down to 0 at y 98. The text LOW GRAVITY shows when the scale first drops under 0.6.
- JourneyQuotes shows four lines spread along the float: Higher. / It is quiet up here. / Almost there. / The moon.
- Debug: Shift + Space teleports the rat to the tip (EndingSettings.debugKeys).
- Videos are H.264 high profile, yuv420p, 30 fps, bt709, AAC.

## Background

- Parallax layers cut by code from Assets/Sprites/Background/background_main.png (BuildBackgroundLayers, BackgroundLayerSplitter, BackgroundField, BackgroundDetail). Layers: sky, far clouds, fog, back mountains, center mountain, field, grass. The old moon is cut out of the sky. There are no tree layers (treeLayerCount is 0).
- BackgroundParallax moves the layers in LateUpdate from the camera position. AddBackground makes the Background object with all tile copies.
- SkySpace (a child of Level) fades in between y 55 and 80: a smooth gradient picture from blue into near black and 60 small stars, a few of them twinkle. SunLight draws a faint warm sun glow and rays with OnGUI, fading out between y 55 and 90.

## Map

- BounceBack > Build Map (BuildMap.cs) deletes and rebuilds only the root object Level. Textures are PNG files in Assets/Textures/Map. MapGrass writes the generated ones, MapAssets sets the import settings.
- MapMinimal flattens the generated pictures (few colours, majority filter) before they are written. MapMinimal.Enabled = false turns that off.
- Start ground (x -16 to 31) is one generated picture: thin grass, dirt, stone. The foot of the first mountain is a smooth curve from x 12 to x 31 (MapArt.FootStart, FootEnd).
- Left mountain, main mountain (with ice on the peak) and the bridge over the lava pit are as before. The pit has only lava, 11 units deep, no floor.
- Right side: one long slope (MapSlope). It starts at the bridge landing (87.4, 25.65) at 20 degrees, gets steeper to about 44, rounds over at the peak (about x 211, y 100) and falls to the right boundary wall. The mass is solid down to y -10. Ice patches near the top, plain white snow from y 40, small boulders as footholds (MapRockDetail.PlanSlope, one every 1.5 of rise).
- Footholds on the other faces come from MapRockDetail.PlanFace. The Build Map log prints the largest gap on each face. The hammer reaches 2.25.
- Loose boulders on the grass and the first boulders at the foot of the left mountain have no collider. Boulders on the faces are solid.
- Snow is physical (MapCaps.Snow, SnowSurface, SnowTrail). The collider is lowered so the rat sinks in. Snow is one flat colour. SnowTrail presses the snow down under the rat.
- Boundaries: Left Boundary and Right Boundary are invisible walls with a red BoundaryGlow when the rat is near. The right wall goes up above the moon. Two SkyBarrier walls stand 20 units either side of the tip above the peak.
- MapProps scatters pictures (trees, rocks, a globe, bridge brackets, stars). They have no colliders.
- Lava is one flat red. Ice is one flat light blue.

## Rules for edits

- Do not rename or delete a MonoBehaviour the scene uses, a serialized public field, a PlayerPrefs key, a Resources path, a menu path or an object name the code finds by name (Level, EruptionTrigger, CheeseMoon).
- Keep the sorting orders: Background -100 and up, map 1 to 8, rat 10 and up, snow 18.
