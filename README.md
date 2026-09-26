# Orange World

A chaotic, physics-driven VR action RPG for **Meta Quest**, built in **Unity 6** with OpenXR and the XR Interaction Toolkit.
You are a floppy, stretchy creature exploring a cluster of floating planetoids in a fever-dream universe, fighting bizarre
creatures with momentum-based melee combat. Nothing here is scripted: damage, knockback and movement all come from physics.

## What's in the prototype

| System | How it plays |
| --- | --- |
| **Floppy hands** | Your hands are physics bodies pulled toward your controllers by underdamped springs. They lag, overshoot and wobble, and noodle arms stretch from your shoulders to reach them. Heavy objects feel heavy because they drag your hands down. |
| **Momentum combat** | Damage = (impact speed − threshold) × √mass. A gentle tap does nothing; you have to *swing*. Weapons, rocks, even thrown creatures all deal damage. |
| **Planetoid gravity** | Every planetoid pulls with inverse-square falloff. Walk all the way around a planet, jump, or drift through zero-g between worlds. |
| **Climb & fling** | Grip on static scenery (trees, planets, crystals) to anchor your hand and pull yourself around, Gorilla Tag–style. Let go mid-pull to fling yourself. |
| **Jump Shrooms** | Mushroom launch pads that fire you (or anything else) across to a neighboring planetoid. |
| **Bloblings** | Jiggly, many-eyed blobs that hop toward you and lunge. When one dies it splits into two smaller blobs, until they're too small to split. Grab them and use them as weapons. |
| **Weapons** | An eyeball flail on a chain of physics links, and a squeaky mallet. |
| **Everything watches you** | Eyes on stalks, eyes on creatures, and one very large eye in the sky. |

All the art is Unity primitives and procedural placeholder audio, so the prototype runs without any imported assets.

## Requirements

- **Unity 6 LTS** (`6000.0.x`) with the **Android Build Support** module (including OpenJDK and Android SDK & NDK).
- A **Meta Quest 2, 3, 3S or Pro** with [developer mode enabled](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/).
- Optional: Meta Quest Link on a Windows PC, to press Play in the Editor and test on the headset without building.

## Setup

1. **Open the project.** In Unity Hub choose *Add → Add project from disk* and pick this folder. If your installed Unity 6
   patch is different from the one in `ProjectVersion.txt`, open it with your version anyway. Package Manager then resolves
   the packages in `Packages/manifest.json`.
2. **Enable the new Input System.** If Unity asks whether to enable the new input backends, click **Yes**. Unity restarts.
3. **Run `Orange World → 1. Configure Project for Quest`.** This switches to Android and applies the Quest player settings:
   IL2CPP, ARM64, Vulkan, Linear color, ASTC and minimum API level 32. It also creates a URP pipeline asset tuned for
   Quest. When it finishes it opens XR Plug-in Management.
4. **Configure XR Plug-in Management.** Unity doesn't support setting this up reliably from a script, so do it by hand:
   - On the **Android** tab, check **OpenXR**, then check the **Meta Quest feature group**.
   - Under **XR Plug-in Management → OpenXR**, Android tab: in **Enabled Interaction Profiles** click **+** and add
     **Oculus Touch Controller Profile**. Add **Meta Quest Touch Pro/Plus** too if you have those controllers.
   - Optional, for Quest Link: on the **Windows** tab check **OpenXR** and add the same interaction profile.
   - Open **XR Plug-in Management → Project Validation** and click **Fix All**.
5. **Run `Orange World → 2. Build Prototype Scene`.** This generates `Assets/_Project/Scenes/Prototype.unity`, along with
   its materials and the Blobling prefab, and adds the scene to Build Settings.
6. **Play it.**
   - On the headset: plug in your Quest, then *File → Build And Run*.
   - Through Quest Link: press Play in the Editor.
   - Without a headset: press Play and use the desktop controls below.

You can re-run step 5 whenever you like. It regenerates the scene from code, so any hand edits to the scene are lost.
Once you start laying out levels by hand, stop re-running it.

## Controls

| Action | Quest | Desktop (no headset) |
| --- | --- | --- |
| Move | Left thumbstick | W A S D |
| Snap turn | Right thumbstick | Hold right mouse button and move the mouse to look |
| Jump | A | Space |
| Grab / climb | Grip (either hand) | Q (left hand) / E (right hand) |
| Swing | Swing your arms | Hold left mouse to windmill your right arm; hold F to punch with your left |

## Project layout

```
Assets/_Project/
  Scripts/
    Core/       PhysicsConfig (72 Hz physics), Haptics, Juice (procedural boing sound)
    Gravity/    GravityAttractor (planetoid), GravityBody (anything that falls toward planetoids)
    Player/     PlanetWalker (locomotion), FloppyHand (spring hands), HandGrabber (grab/climb/fling),
                ElasticArm (noodle arms), PlayerVitals (health feedback), DesktopDebugRig
    Combat/     Damageable, ImpactDamager (momentum-based damage)
    Creatures/  CreatureBrain (hop/chase/lunge AI), Jiggle (squash & stretch), SplitOnDeath,
                WaveSpawner, RandomTint
    World/      JumpPad, LookAtCamera, Orbiter
  Editor/       QuestProjectSetup, PrototypeSceneBuilder (menu: Orange World)
```

### How the key pieces fit together

- **Player rig:** `Player` (Rigidbody + capsule + `PlanetWalker`) → `XR Origin` → `Camera Offset` → camera and controllers.
  The capsule follows your head as you move around your room. The rig's up axis slerps toward local gravity, so you can
  walk around a planetoid.
- **Hands aren't children of the rig.** They're independent rigidbodies, so physics can move them freely, and a spring
  pulls each one toward its tracked controller. `FloppyHand.stiffness` and `damping` control how floppy they feel. The
  critical damping value is `2·√stiffness`, and anything below it wobbles.
- **Climbing:** `HandGrabber` makes an anchored hand kinematic and sends a pull velocity to `PlanetWalker`, which
  applies it instead of normal movement. The player keeps that velocity after release, which is what makes flinging work.
- **Damage:** any rigidbody with `ImpactDamager` hurts any `Damageable` it hits hard enough. Creatures also have
  `selfDamageMinSpeed`, so slamming one into the ground hurts it.

## Tuning cheat sheet

| Feel | Where |
| --- | --- |
| Hands floppier / stiffer | `FloppyHand.stiffness`, `damping` (on the Left/Right Hand objects) |
| How hard you must swing | `ImpactDamager.minImpactSpeed`, `damagePerSpeed` |
| Planet gravity strength / reach | `GravityAttractor.surfaceGravity`, `influenceRadii` |
| Enemy count and ramp | `WaveSpawner` in the scene |
| Blobling aggression | `CreatureBrain` on `Prefabs/Blobling` |
| Launch strength between worlds | `JumpPad.launchSpeed` on each Jump Shroom |

## Comfort note

Rotating to match a new planetoid's gravity, getting flung, and launching between worlds are intense in VR. That suits
the fever-dream tone, but before any public build add comfort options: a tunnel vignette during fast motion, an option to
fade instead of rotate when gravity switches, and a seated mode.

## Suggested next steps

1. **Full floppy avatar.** Give the protagonist an active-ragdoll body you can see in mirrors, in shadows and when you
   look down, with legs that flail during flings.
2. **More creature types.** Candidates: a flying jellyfish that drops from above, a planet-sized worm, and a creature
   that only takes damage from thrown objects.
3. **RPG layer.** Stretch upgrades such as longer arms, heavier fists and stickier grip, dropped by creatures, plus a
   hub planetoid.
4. **Real art and audio.** Replace the primitives and procedural boings, and budget draw calls for Quest (aim for fewer
   than about 150).
5. **Performance.** Profile on the device, use `Application.targetFrameRate` with the OpenXR display refresh rate, and
   pool the creatures.
