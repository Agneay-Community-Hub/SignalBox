# Signal Box

*A short first-person horror game for **GitHub Game Off 2026**. Theme: **SHIFT**.*

> **14 November 1981.** The 06110 Kalvadi Mail is sent down a closed branch line and off a broken river bridge. 212 people drown.
> The signalman, Velan, is found dead at his desk. The register says the Mail was set for the branch, in handwriting nobody can identify.
>
> **14 November, present day.** You are the new night signalman at Kalvadi Halt. The last one, S. Anand, has not been seen for a week.

Answer the control phone. Set the points. Clear the signal. Write it in the register. Survive until the relief arrives at five.

Some of tonight's trains are not in the timetable.

---

## How it uses the theme

- **A night shift:** 22:30 to 05:00, alone in a signal cabin.
- **Shifting the points:** the black lever decides where every train goes, and which ending you reach.
- **A shifting reality:** the phone, the register, the levers and the world itself slowly shift into the night of the crash.

## Features

- A voiced, cinematic **1981 prologue**, then the present-day shift (about 25 minutes for a first playthrough).
- **6 endings** (plus a bonus death), tracked on the title screen.
- Three ghosts with a story behind every scare: Velan on the line, Anand in the cabin, and the 212 drowned passengers.
- Set pieces: a man walking in front of a train, a body that is not there, a hanging man, faces at the window, a radio emergency bulletin and a minute of **"do not open the door"**, the red world of the crash, and the 3:10.
- Full-screen jump scares, voiced phone calls, procedural horror audio.
- **Checkpoints** and **save / continue**, pause menu, options (volume, mouse sensitivity, invert mouse, fullscreen).
- PS1-style low-resolution look: fog, grain, vignette.

## Controls

| Action | Control |
|---|---|
| Move | WASD / arrow keys |
| Run | Left Shift |
| Look | Mouse |
| Interact / answer the phone / levers | E or left click |
| Torch | F |
| Scroll the register | Mouse wheel or W / S |
| Pause menu | Esc |

**The job:** the black lever is Points No. 1 (NORMAL = main line, REVERSE = the closed branch). The red lever is the home signal (pull to clear, push back to restore to danger). The points are locked while the signal is clear, like a real interlocking.

Read the note pinned by the cabin door. It will help.

<details>
<summary><b>Endings (spoilers)</b></summary>

| Ending | How |
|---|---|
| **Relieved** | Send the Kalvadi Mail down the branch, then hold the 3:10 at danger until 05:00 |
| **Same Name** | Send the Kalvadi Mail along the main line, then hold the 3:10 until 05:00 |
| **The Junction** | Clear the 3:10 onto the main line |
| **The Shift Never Ends** | Clear the 3:10 down the branch |
| **Going Home** | Board the 3:10 |
| **Inquiry** | Send a train with living passengers down the branch |
| *Struck* (bonus death) | Stand on the line when a train passes |

</details>

## Play

- **Browser:** WebGL build (see Releases / itch.io page).
- **Windows:** download the Windows build from Releases and run `SignalBox.exe`.

## Building from source

1. Install **Unity 6000.3.7f1** with **Windows Build Support** and **Web Build Support** (Unity Hub > Installs > Add modules).
2. Unity Hub > **Add > Add project from disk** and choose this folder.
3. On first open, an editor script creates `Assets/Scenes/SignalBox.unity` and the base materials.
4. Open `Assets/Scenes/SignalBox.unity` and press Play.
5. Build from the **Signal Box** menu: **Build Windows** or **Build WebGL (itch.io)**. Output goes to `Builds/`.

Notes:
- Uses the Universal Render Pipeline and the Input System.
- `com.unity.collections` is pinned to **2.6.8** in `Packages/manifest.json`, because the version URP pulls in by default does not compile for WebGL.
- The world, cabin, trains, UI and most audio are generated at runtime from code (`Assets/Scripts/Runtime`). Characters and textures come from the CC0 / CC BY packs listed below.

### Voices

Voiced lines live in `Assets/Resources/Voice`. They are generated from the text in the scripts with the Windows built-in speech voices, then processed in code (phone line, radio, ghost, crowd). After changing any line, run **Signal Box > Generate Voices** (Windows only). The script is in `Assets/Scripts/Editor/VoiceGen.cs`.

### Custom sounds

Drop `.wav` / `.ogg` / `.mp3` files into `Assets/Resources/Audio/` with the names listed in the README there, and they replace the generated sounds.

## Project layout

```
Assets/
  Scenes/SignalBox.unity        bootstrap scene (the game builds itself at runtime)
  Scripts/Runtime/              game code
    GameBoot.cs                 entry point
    Director.cs                 the night: prologue, calls, trains, checkpoints, endings
    Scares.cs                   set pieces and ghosts
    World.cs                    cabin, track, signal, levers, radio
    Train.cs, Route.cs          trains
    Hud.cs, JumpScare.cs        UI, menus, jump scares
    Synth.cs, Voice.cs          procedural audio, voice clips
    Actors.cs, Checkpoint.cs    animated characters, save data and options
  Scripts/Editor/               setup, build menu, voice generator
  Resources/
    ThirdParty/                 character models, textures (see credits)
    Voice/                      generated voice clips
    Audio/                      optional custom sound overrides
```

## Credits

Made by **Chetan Kumar G**, [GitHub](https://github.com/Chetan-Kumar-G) · [chetankumarg.tech](https://chetankumarg.tech)

Third-party assets:

| Asset | Author | License |
|---|---|---|
| Zombie Apocalypse Kit (characters, animations) | [Quaternius](https://quaternius.com/packs/zombieapocalypsekit.html) | CC0 1.0 |
| PSX Horror Entity | [puck_psx](https://puszke.itch.io/psx-horror-entity-free) | CC BY (credit: puck_psx) |
| Textures (rusty metal, plaster, wood, gravel, forest ground, corrugated iron) | [Poly Haven](https://polyhaven.com) | CC0 |
| Train Kit | [Kenney](https://kenney.nl/assets/train-kit) | CC0 |

Each pack's folder under `Assets/Resources/ThirdParty/` contains its license note.

Kalvadi Halt, its trains, its people and the 1981 accident are fictional.

## License

The game's code is released under the [MIT License](LICENSE). Third-party assets keep their own licenses, listed above.
