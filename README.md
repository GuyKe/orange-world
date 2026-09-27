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
| **Gunner blobs** | About a fifth of spawned blobs carry a little gun and lob globs at you from range if they have line of sight. They still split and stretch-pop like any other blob. |
| **Melee blobs** | About a fifth of spawned blobs carry a club and hit harder and lunge faster than a normal blobling — the brute of the three types. |
| **Stretch-kill** | Grab a blob with one hand to hold it steady; grab it with your other hand too and pull apart to stretch it. Stretch it far enough and it pops instantly (and splits, if it's still big enough to). |
| **Weapons** | An eyeball flail on a chain of physics links, a squeaky mallet, a floppy boxing-glove bopper (light, low damage, huge knockback), and a yo-yo whose ball hangs from a spring instead of a rigid chain, so it stretches and snaps back unpredictably. |
| **Everything watches you** | Eyes on stalks, eyes on creatures, and one very large eye in the sky. |
| **Ultimate** | Killing blobs fills a translucent orange charge bar in the bottom-right of your view, labeled "ULTIMATE" — faded enough to stay out of your way, not a solid HUD block. Once it's full, click both thumbsticks to unleash it: bonus max health and double damage for 30 seconds, with a black vignette closing in at the edge of your vision while it's active. |
| **Health, Blob Bucks & level** | A faded red "HEALTH" bar (starts at 100 max) sits bottom-left, mirroring the ultimate charge; a yellow "Blob Bucks: N" counter sits top-right; a cyan "LEVEL N" bar sits top-left. Every corner has its own color, so you can tell them apart at a glance without reading. |
| **Leveling up** | Killing blobs also grants XP, and each level demands more than the last (the requirement compounds ×1.35 per level), so it's a slow climb that never plateaus into "trivial." Every level up grants a small permanent health bonus. Splitting a normal blob is worth 1 Blob Buck / 4 XP, a gunner or melee blob is worth 2 Blob Bucks and 6-7 XP. |
| **Upgrades shop** | Click B any time to warp to a small shop platform tucked away from the main play area; click B again to warp right back to where you were. Punch the glowing shrine there to spend 25 Blob Bucks on a permanent +10 max health upgrade — as many times as you can afford it. |
| **Boss planets** | Two big, bare arena planets, each reachable by its own Jump Shroom from Home and signed with a recommended level, hold one giant scaled-up blob apiece — a melee brute and a gunner — with much higher health and damage than their normal-sized kin. Each boss shows a floating red health bar overhead and, on a cooldown, unleashes a telegraphed special attack: the melee brute winds up and ground-slams everything nearby, the gunner winds up and fires a spreading barrage instead of its usual single shot. A boss just dies outright instead of splitting, and drops a big lump of Blob Bucks and XP, a full ultimate charge, and a floating "DEFEATED!" readout. |
| **Main menu** | You load into a small platform with a punchable PLAY button, while a few blobs hop around you and distant planetoids drift by — the menu background is just the real game running quietly. Punch the button to start. |

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
5. **Run `Orange World → 4. Build All Scenes`** (or the two individually: **2. Build Main Menu Scene** and
   **3. Build Prototype Scene**). This generates `Assets/_Project/Scenes/MainMenu.unity` and `Prototype.unity`, their
   materials and prefabs, and adds both to Build Settings with the main menu first.
6. **Play it.**
   - On the headset: plug in your Quest, then *File → Build And Run*. It opens on the main menu.
   - Through Quest Link: press Play in the Editor.
   - Without a headset: press Play and use the desktop controls below.

You can re-run step 5 whenever you like. It regenerates both scenes from code, so any hand edits to either scene are
lost. Once you start laying out levels by hand, stop re-running it.

All world-space text (menu title/button, shop signage, shrine label, Blob Bucks counter) previously faced backward -
`WorldText` had an unnecessary 180° flip baked in, since removing it is what actually fixed it once someone could
see the result in a headset. If any text still reads backward or invisible after pulling the latest version, select
that object and flip its Y rotation by 180° as a workaround, then let me know so I can dig into why.

The four head-locked HUD elements each get their own corner and color - health (red) bottom-left, ultimate (orange)
bottom-right, level (cyan) top-left, Blob Bucks (yellow) top-right - each labeled so it's clear what it is at a
glance without reading closely. If they look mispositioned, too close to each other, or clipped at the edge of view
in the headset, real VR headsets don't use the Camera component's field of view, so I picked comfortable-looking
offsets and distances without being able to preview them; nudge `PlayerVitals.barOffset`, `PlayerBucks.counterOffset`,
`PlayerLevel.barOffset` or `PlayerUltimate.meterOffset` on the `Player` object to taste - they all use the same
coordinate convention (X right, Y up, Z forward from your eyes) so moving one to a different corner is just flipping
a sign. These are the things in this project I couldn't verify visually without a running Editor.

## Controls

| Action | Quest | Desktop (no headset) |
| --- | --- | --- |
| Move | Left thumbstick | W A S D |
| Snap turn | Right thumbstick | Hold right mouse button and move the mouse to look |
| Jump | A | Space |
| Grab / climb | Grip (either hand) | Q (left hand) / E (right hand) |
| Stretch-kill a blob | Grip it with both hands, then pull your hands apart | Q and E on the same blob, then move apart |
| Swing | Swing your arms | Hold left mouse to windmill your right arm; hold F to punch with your left |
| Activate ultimate (once charged) | Click both thumbsticks | R |
| Warp to/from the upgrades shop | Click B | B |
| Buy a health upgrade | Punch the gold shrine in the shop (needs 25 Blob Bucks) | Same, in-world |
| Visit a boss | Follow a Jump Shroom from Home to a boss planet's sign | Same, in-world |

## Project layout

```
Assets/_Project/
  Scripts/
    Core/       PhysicsConfig (72 Hz physics), Haptics, Juice (procedural boing sound),
                HudSprite (faded HUD bars), WorldText (world-space UI Text labels)
    Gravity/    GravityAttractor (planetoid), GravityBody (anything that falls toward planetoids)
    Player/     PlanetWalker (locomotion + teleport), FloppyHand (spring hands), HandGrabber (grab/climb/fling),
                ElasticArm (noodle arms), PlayerVitals (health feedback + health bar), PlayerUltimate
                (charge/buff/HUD), PlayerBucks (currency + counter), PlayerLevel (XP + level bar),
                ShopTeleport (click B), DesktopDebugRig
    Combat/     Damageable, ImpactDamager (momentum-based damage), Projectile
    Creatures/  CreatureBrain (hop/chase/lunge AI), Jiggle (squash & stretch), SplitOnDeath,
                Stretchable (two-handed stretch-to-pop), BlobGun (ranged blobs), MeleeBlob (marker),
                WaveSpawner, RandomTint, BossHealthBar, BossSpecialAttack, BossReward
    World/      JumpPad, LookAtCamera, Orbiter, MenuButton (punchable scene-load button),
                HealthShrine (punchable Blob-Bucks-for-HP upgrade)
  Editor/       QuestProjectSetup, PrototypeSceneBuilder, MainMenuSceneBuilder (menu: Orange World)
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
- **Stretch-kill:** `HandGrabber.Grab()` checks for a `Stretchable` on the target before falling back to a normal
  `FixedJoint` hold. `Stretchable` pulls the body toward each gripping hand with a spring (no rigid joint, so two
  hands pulling opposite ways actually deforms it) and scales its `visual` along the hand-to-hand axis. It disables
  `Jiggle` for that object while held (`Jiggle.ExternalOverride`) so the two don't fight over the same scale. Past
  `snapLength` it calls `Damageable.Kill()`, which bypasses invulnerability for a guaranteed pop.
- **Gunner and melee blobs:** `WaveSpawner` rolls one number against `gunnerChance` then `meleeChance` to pick
  `GunnerBlobling`, `MeleeBlobling`, or the plain `Blobling`. `BlobGun` raycasts for line of sight and fires a
  `Projectile` (the `Glob` prefab) that only damages the player on contact. `MeleeBlobling` is just the base blob
  with a club (`PrototypeSceneBuilder.AddClub`) and higher `CreatureBrain.contactDamage`/`lungeSpeed`, plus an empty
  `MeleeBlob` marker component other systems (`PlayerBucks`, `PlayerLevel`) check for the same way they check for
  `BlobGun` on a gunner.
- **Weapons:** the flail and mallet are rigid (a `ConfigurableJoint` chain and a single body); the bopper is just a
  lighter, higher-`knockback` mallet variant. The yo-yo instead connects its ball to its handle with a `SpringJoint`
  (`spring`/`damper`/`minDistance`/`maxDistance`), so it genuinely stretches under load and snaps back, unlike the
  flail's fixed-length links.
- **Main menu:** `MainMenuSceneBuilder` shares most of its building blocks with `PrototypeSceneBuilder` (same palette,
  same `BuildPlayer`, same `Blobling` prefab) so the two scenes look and feel like one game. The menu player is built
  with `addVitals: false`, so it has no `Damageable` and the background blobs can bump into it harmlessly. `MenuButton`
  is a plain static collider — no XR ray interactor needed — so punching it hard enough (`OnCollisionEnter` +
  `relativeVelocity`) plays a short press animation and calls `SceneManager.LoadScene("Prototype")`.
- **Ultimate:** `Damageable` fires a static `AnyDied` event on every death; `PlayerUltimate` listens and adds charge
  whenever the dead thing has a `CreatureBrain` (i.e. it's a blob, not a rock or the player). The charge bar and the
  four vignette corners use `HudSprite` (see below); the vignette stays fully opaque black since the whole point
  there is to block your view, unlike the translucent bars. Activating calls `Damageable.AddMaxHealth` (raises the
  cap and current health together, and un-does it symmetrically after) and sets `ImpactDamager.GlobalDamageMultiplier`,
  a static multiplier every hit in the scene reads, so it buffs hands, weapons and thrown creatures alike without
  touching each one individually. `PlayerUltimate` only exists on the Prototype player (it's added alongside
  `PlayerVitals`, so the health-less menu player never gets one).
- **Faded HUD bars:** `HudSprite.Create` is the one place that builds a head-locked bar — a `SpriteRenderer` (a 1×1
  white pixel, tinted and stretched) parented to the camera. Sprites alpha-blend and render double-sided correctly
  out of the box on any pipeline, unlike a mesh + material, so bars can be genuinely see-through without me having
  to get a URP transparent-surface material or a quad's facing direction right blind. `PlayerVitals` and
  `PlayerUltimate` each call it twice (a background track plus a fill that scales/repositions to stay anchored to
  the track's left edge), then add a small `WorldText` label just above the bar so it's identifiable at a glance.
- **Blob Bucks & the health shrine:** `PlayerBucks` listens to the same `Damageable.AnyDied` event as the ultimate,
  and tells a normal `Blobling` from a `GunnerBlobling` by whether it has a `BlobGun` component. Its running total is
  a `WorldText` label (see below) rather than a bar, updated whenever it changes. `HealthShrine` is a punchable
  static collider (same `OnCollisionEnter` + `relativeVelocity` pattern as `MenuButton`) that reaches the puncher's
  `PlayerBucks`/`Damageable` via `HandGrabber.walker`, and calls `Damageable.AddMaxHealth` permanently (no un-do,
  unlike the Ultimate's temporary buff) if `PlayerBucks.TrySpend` succeeds.
- **World-space text:** `WorldText.Create` builds a small `Canvas` + `Text` label from code (used for the main menu's
  title/button label, the shop's signage, the shrine label, and the Blob Bucks counter), unrotated so it faces along
  its parent's local +Z. It's a runtime-safe utility (no `UnityEditor` calls), so both the editor-time scene builders
  and `PlayerBucks`/`PlayerVitals`/`PlayerUltimate` at play time can call it.
- **Upgrades shop:** `PrototypeSceneBuilder.BuildShop` places a small separate planetoid far from the main play area
  (so gameplay never has to route around it) with its own `GravityAttractor` and a `HealthShrine`. `ShopTeleport`
  listens for the B button and calls a new `PlanetWalker.Teleport(position, rotation)` (which `Respawn` now also
  routes through) to warp there, remembering your exact position/rotation so a second click sends you right back -
  no matter which planetoid you were standing on.
- **Leveling:** `PlayerLevel` listens to the same `Damageable.AnyDied` event as the ultimate and Blob Bucks, adding
  XP per kill. `XPToNextLevel` is `baseXPToLevel * xpGrowthPerLevel^(Level-1)`, so each level compounds on the last
  instead of needing a flat amount - level 10 demands roughly 20× the XP level 1 did. A level-up calls
  `Damageable.AddMaxHealth` (the same permanent-buff method the health shrine uses) for a small stat reward, so
  leveling is never purely cosmetic.
- **Boss planets:** `PrototypeSceneBuilder.BossPlanets` is a small array of big, bare planetoids with a recommended
  level each. `BuildBossPlanets` reuses `BuildJumpPad` (factored out of the regular `BuildJumpPads` loop so both can
  call it) to connect each one to Home, posts a `WorldText` sign with the recommended level, and calls `SpawnBoss` to
  drop in one scaled-up melee or gunner blob instance (`localScale` up to ~6.6×, health scaled by `size × 2.5`,
  `contactDamage` doubled) with a fixed color instead of the usual `RandomTint`, so it reads as a distinct boss
  rather than a big regular blob. `SpawnBoss` also strips the `SplitOnDeath` every other blob carries, so a boss
  just dies outright instead of shattering into a swarm of smaller blobs.
- **Boss rewards:** `BossReward` (added by `SpawnBoss`, sized off the boss's `RecommendedLevel`) listens for its own
  `Damageable.Died` and pays out directly: a lump of Blob Bucks and XP, a full `PlayerUltimate` charge via the new
  `FillCharge()`, and a floating `WorldText` "DEFEATED!" readout that billboards toward the player for a few
  seconds before destroying itself. `PlayerBucks` and `PlayerLevel` check for `BossReward` the same way they check
  for `MeleeBlob`/`BlobGun` and skip their usual per-type payout on that death, so these numbers are the whole
  reward rather than a bonus stacked on top of a normal kill.
- **Boss health bars and special attacks:** `SpawnBoss` also adds `BossHealthBar` and `BossSpecialAttack` to the
  instance. `BossHealthBar` builds its bar in world space rather than parenting it under the (much larger-scaled)
  boss, repositioning it above the boss's head every `LateUpdate` using its `GravityBody.Up` so it stays correctly
  "up" on a sphere - the same scaled-parent trap the boss planets themselves avoid, and it self-destroys its bar
  when the boss does since the two aren't otherwise linked. `BossSpecialAttack` picks its move from whichever fields
  `SpawnBoss` wired up: with no `projectilePrefab` it ground-slams a radius around itself for as much damage as one
  already-doubled contact hit; with one, it fires a spreading multi-shot barrage instead, sized to add up to the
  same total damage. Either move gets a brief wind-up (a negative `Jiggle.Punch`, stretching the blob upward instead
  of squashing it) as a dodgeable tell before it fires.

## Tuning cheat sheet

| Feel | Where |
| --- | --- |
| Hands floppier / stiffer | `FloppyHand.stiffness`, `damping` (on the Left/Right Hand objects) |
| How hard you must swing | `ImpactDamager.minImpactSpeed`, `damagePerSpeed` |
| Planet gravity strength / reach | `GravityAttractor.surfaceGravity`, `influenceRadii` |
| Enemy count and ramp | `WaveSpawner` in the scene |
| Blobling aggression | `CreatureBrain` on `Prefabs/Blobling` |
| How many blobs carry guns / clubs | `WaveSpawner.gunnerChance` / `meleeChance` |
| Gunner range, fire rate, damage | `BlobGun` on `Prefabs/GunnerBlobling` |
| Melee blob damage and lunge speed | `CreatureBrain.contactDamage`/`lungeSpeed` on `Prefabs/MeleeBlobling` |
| How far a blob stretches before it pops | `Stretchable.snapLength` (rest size is `restLength`) |
| Launch strength between worlds | `JumpPad.launchSpeed` on each Jump Shroom |
| How hard you must punch the menu button | `MenuButton.minImpactSpeed` on `Play Button` in `MainMenu` |
| Which scene the menu button loads | `MenuButton.sceneName` |
| How many kills to charge the ultimate | `PlayerUltimate.maxCharge` / `chargePerKill` |
| Ultimate strength and length | `PlayerUltimate.bonusHealth`, `damageMultiplier`, `duration` |
| Charge bar / vignette size and position | `PlayerUltimate.meterOffset`/`meterWidth`/`meterHeight`, `vignetteCornerOffset`/`vignetteQuadSize` |
| How see-through the charge bar / its label are | `PlayerUltimate.backgroundColor`/`chargingColor`/`readyColor`/`labelColor` alpha |
| How many blobs are around at once | `WaveSpawner.startingCount`/`maxCount`/`secondsPerExtraCreature`/`spawnInterval` |
| Starting/max health, regen | `Damageable.maxHealth` on `Player`, `PlayerVitals.regenDelay`/`regenPerSecond` |
| How see-through the health bar / Blob Bucks counter are | `PlayerVitals.barBackgroundColor` / `PlayerBucks.backgroundColor`/`textColor` |
| Blob Bucks per blob type | `PlayerBucks.normalBlobValue`/`gunnerBlobValue`/`meleeBlobValue` |
| Health upgrade cost and strength | `HealthShrine.cost`/`healthBonus` on `Health Shrine` in the shop |
| Bopper feel (light, high knockback) | `ImpactDamager` on `Boxing Glove Bopper` |
| Yo-yo stretchiness | `SpringJoint.spring`/`damper`/`maxDistance` on `Yo-yo Ball` |
| Where the shop is / how far you warp | `PrototypeSceneBuilder.ShopPosition`/`ShopPlatformRadius` (regenerate the scene after changing) |
| XP per blob type / how much harder each level gets | `PlayerLevel.xpPerNormalBlob`/`xpPerGunnerBlob`/`xpPerMeleeBlob`, `xpGrowthPerLevel` |
| Reward for leveling up | `PlayerLevel.bonusHealthPerLevel` |
| Boss planet position, size, recommended level, or which blob type | `PrototypeSceneBuilder.BossPlanets` (regenerate the scene after changing) |
| Boss toughness (health/damage/size scaling) | the `scale`/`SetMaxHealth`/`contactDamage` math in `PrototypeSceneBuilder.SpawnBoss` |
| Boss health bar look and height | `BossHealthBar.barWidth`/`barHeight`/colors/`heightAboveRadius` |
| Boss special attack cooldown, range, or damage | `BossSpecialAttack.cooldown`/`range`, `slamRadius`/`slamDamage`/`slamKnockback`, `barrageShots`/`barrageSpreadDegrees`/`barrageDamagePerShot` |
| Boss kill reward (Bucks/XP/toast) | `BossReward.bucksReward`/`xpReward`/`toastSeconds`/`toastColor`, set per-boss in `PrototypeSceneBuilder.SpawnBoss` |

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
