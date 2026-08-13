# Third-Person Camera with Cinemachine — A Practical Guide

Quick reference for setting up an over-the-shoulder third-person camera in Cinemachine
3.x: interior collision, comfort/motion sickness, and a couple of controller bugs that
love to disguise themselves as camera bugs. Written to be reusable on any new project,
not a changelog of one specific game.

---

## 1. The components you actually need

| Component | What it does | When to use it |
|---|---|---|
| `CinemachineThirdPersonFollow` | Shoulder-cam rig: positions the camera relative to a target with a shoulder offset. | The base of any over-the-shoulder TPS camera. |
| `CinemachineDeoccluder` | Keeps line of sight to the target, pushing the camera away from whatever's blocking it. | Any time there's geometry between camera and character — interiors, corridors, tight rooms. |
| `CinemachineDecollider` | Pulls the camera out of geometry it got stuck in, and handles terrain, but does **not** try to preserve line of sight. | Extra safety net, or when keeping the character visible isn't the priority. |
| `CinemachineConfiner3D` | Confines the camera's position inside a 3D volume. | Very tight rooms/corridors, as a belt-and-suspenders addition to the Deoccluder. |
| `CinemachineImpulseSource` + `CinemachineImpulseListener` | Camera shake (damage, explosions) decoupled from follow/aim logic. | Whenever you need screen shake — never touch the gameplay camera's position/rotation directly for this. |
| `CinemachineBrain.UpdateMethod` | Which clock the camera updates on (Update / FixedUpdate / SmartUpdate). | **Smart Update** if your character uses a `CharacterController` (it moves in `Update`, not `FixedUpdate`) — avoids jitter. |

## 2. Minimum viable setup

1. Point `CinemachineCamera`'s `Follow` and `LookAt` at an empty `CameraTarget`
   (child of the character, roughly shoulder height). Don't point it straight at the
   character's root — you lose the ability to rotate the camera independently of the
   body.
2. Add `CinemachineThirdPersonFollow` (Body). Reasonable starting values:
   `Damping ~0.1/0.1/0.1`, `ShoulderOffset ~0.8/0/0`, `CameraDistance ~3-4`.
3. Add `CinemachineDeoccluder`:
   - `Collide Against`: only the layers that are actually solid geometry (walls,
     floor, furniture).
   - `Ignore Tag`: the character's own tag, otherwise the camera tries to dodge the
     player's own body.
   - `Minimum Occlusion Time ~0.05s`: ignores one-frame occlusions (a thin railing,
     a pole whipping past) so the camera doesn't react to every little thing.
   - `Smoothing Time ~0.15-0.2s`: stops the camera from "pumping" in and out near
     clusters of props.
   - `Damping When Occluded` is the single most important knob for how harsh the push
     feels. It's the reaction speed *entering* a collision, and the component's default
     is on the snappy/harsh side. If the camera feels like it's slamming into walls,
     raise this (somewhere around `0.5-0.7`) before touching anything else.
4. Do yaw/pitch rotation **in code**, not through `CinemachineInputAxisController` — you
   get direct control over sensitivity, vertical clamping, and the safety limits from
   section 3. Set `Rotation Control = None` on the `CinemachineCamera` when rotation is
   driven externally like this.
5. `CinemachineBrain.UpdateMethod = Smart Update` if the character uses a
   `CharacterController`.

## 3. Motion sickness, the short version

A handful of rules cover most of it:

1. **FOV for third person: 75°–85°.** First person usually wants 90-110° because there's
   no body to anchor against; third person already has that visual reference, so it
   needs less correction. Always expose an FOV slider — the right value depends on
   screen size and viewing distance, which you can't know in advance.
2. **Never snap the FOV.** Any zoom or FOV kick has to ease in slowly (~0.3-0.5s), never
   instantly — sudden FOV changes are one of the most commonly cited triggers for
   nausea.
3. **Cap rotation speed per frame.** Input spikes — a `deltaTime` hiccup, the cursor
   getting warped, alt-tabbing back into the game — can spin the camera an absurd amount
   in a single frame. Clamp the yaw/pitch delta applied per frame as a safety net,
   independent of whatever sensitivity the player has set.
4. **Camera shake goes through Impulse, always** — never write straight into the
   gameplay camera's position or rotation. That way a "reduce camera shake" toggle in
   your options menu is trivial and doesn't touch anything else.
5. **Don't auto-roll the camera.** Tilting the camera sideways (dutch) without the
   player explicitly asking for it is one of the most frequently reported discomfort
   triggers.
6. **Offer the options, don't decide for everyone.** FOV slider, camera shake toggle,
   motion blur toggle. Comfort tolerance varies wildly from person to person.

## 4. Crouching in tight spaces (low ceilings)

This one's nasty to debug if you don't already know the cause: the `CameraTarget` is
usually pinned at a fixed height, calibrated for the character standing upright. If the
player crouches under a low platform, that fixed point ends up **inside the ceiling
geometry** — and now the Deoccluder has no clean direction to resolve toward (it's
trying to preserve line of sight to a target that's already embedded in a wall). The
result is a visibly unstable, erratic camera, not just a slightly stuck one.

The Deoccluder isn't misconfigured here — its target is just somewhere impossible. Fix
it at the source instead of tuning around it:

1. Expose the crouch state from your controller (a plain `event Action<bool>
   CrouchStateChanged` is enough).
2. In whatever script drives camera rotation, listen for that event and `Lerp` (not
   snap) the `CameraTarget`'s local height between a standing and a crouching value.
   Dropping it by roughly 0.5-0.6 units usually covers most cases.

If that alone isn't enough in really tight corridors — the arm still doesn't have room
even at the lower height — the next lever is shrinking `CameraDistance` on the
`ThirdPersonFollow` while crouched too. Shorter arm needs less room to resolve. Same
event-driven pattern, just interpolating a second value.

## 5. Controller bugs that look like camera bugs

Two root causes that keep showing up disguised as something else, camera included:

- **`Animator.applyRootMotion = true` alongside manual `CharacterController` movement.**
  If your controller already computes velocity and calls `CharacterController.Move()`
  itself — the common case for a simple hand-rolled controller — leaving `Apply Root
  Motion` on makes Unity apply the animation's root motion *on top*, automatically,
  also through `CharacterController.Move()`. Two `Move()` calls fighting inside the same
  frame. Classic symptom: something like jumping works fine from some animation states
  and silently fails from others, with no obvious reason why. If the controller is
  fully manual, root motion should be off.
- **Ground check with too little margin.** A common mistake is sizing the "am I
  grounded" sphere/raycast so tight against the capsule's bottom that any tiny position
  jitter — moving fast, a minor collision correction — flips the result to `false`
  right when the player hits jump. If jumping works standing still but fails while
  sprinting, this is the first thing to check: widen the ground check radius/range until
  there's real margin, not a few millimeters.

## 6. What to skip for a simple template

Things like **dynamic shoulder swapping / corner peeking**, dedicated aim cameras,
dynamic vignette during fast turns, and acceleration curves on look sensitivity are all
legitimate AAA techniques — but they're gameplay-feel fine-tuning. They're only worth
the complexity once a project already has a combat/level design that actually needs
them. On a base template, they're just extra surface area with no clear payoff yet.

---

## 7. References

### Official Unity Cinemachine docs
- [Cinemachine Deoccluder — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html)
- [Cinemachine Decollider — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDecollider.html)
- [Cinemachine Confiner 3D — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineConfiner3D.html)
- [Camera control and transitions — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/concept-camera-control-transitions.html)
- [Class CinemachineBrain.UpdateMethod — API](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/api/Unity.Cinemachine.CinemachineBrain.UpdateMethod.html)

### Jitter / occlusion tuning (technical discussions)
- [Cinemachine: avoid camera clipping while allowing obstructions — Unity Discussions](https://discussions.unity.com/t/cinemachine-avoid-camera-clipping-while-allowing-obstructions/1573849)
- [What Cinemachine UpdateMethod can I use? — Unity Discussions](https://discussions.unity.com/t/what-cinemachiene-updatemethod-can-i-use/1628761)
- [Fix: Unity Cinemachine Camera Jitter and Stutter — Bugnet Blog](https://bugnet.io/blog/fix-unity-cinemachine-camera-jitter)

### Motion sickness / comfort / accessibility
- [The Last of Us Part II: Accessibility Features Detailed — Naughty Dog](https://www.naughtydog.com/blog/the_last_of_us_part_ii_accessibility_features_detailed)
- [If the game uses field of view, set an appropriate default — Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/if-the-game-uses-field-of-view-3d-engine-only-set-an-appropriate-default-for-the-expected-viewing-environment/)
- [How to Fix a Camera Field of View Causing Motion Sickness — Bugnet Blog](https://bugnet.io/blog/how-to-fix-camera-fov-causing-motion-sickness)
- [Alleviating motion sickness in first-person video games — Nicolas Busseneau](https://nicolas.busseneau.fr/en/blog/2020/09/alleviating-motion-sickness-in-first-person-video-games)

### Shake / juice in Cinemachine
- [Adding Camera Shake in Unity Using Cinemachine — Medium](https://medium.com/@wesley.n.campbell/adding-camera-shake-in-unity-using-cinemachine-80450b075f52)
- [Unity Camera Control Best Practices: Performance, Flexibility, and Feel](https://exoa.dev/blog/unity-camera-control-best-practices)

### Camera math fundamentals (optional, goes deeper)
- [Math for Game Programmers: Juicing Your Cameras With Math — Squirrel Eiserloh, GDC 2016 (YouTube)](https://www.youtube.com/watch?v=tu-Qe66AvtY)
