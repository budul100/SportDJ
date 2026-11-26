# SportDJ

A simple, fast sample player for PA announcers and DJs at sports events. Every sound sits on its own key, one press starts it, and nothing gets in the way when things get busy.

<!-- Screenshot: main window -->

## Download

Get the latest version from the [Releases](https://github.com/budul100/SportDJ/releases) page. Two files are offered:

| File | What it does |
|------|--------------|
| `SportDJ-Setup-x.y.z.exe` | Installs SportDJ with a Start menu entry and an uninstaller. |
| `SportDJ-Portable-x.y.z.exe` | Only unpacks SportDJ into a folder of your choice and leaves nothing behind on your system. |

You need Windows 10 or 11 (64-bit). Nothing else has to be installed.

> **Windows shows "Windows protected your PC"?** The files are not code signed yet. Click **More info**, then **Run anyway**.

## Quick start

1. **Start SportDJ** and open the settings on the right (gear button in the title bar).
2. **Choose your audio output** and, if you use one, your **MIDI controller**.
3. **Assign sounds to a key.** Open the settings of a key and pick one or more audio files, a whole folder, or a playlist (`.m3u`).
4. **Assign a button** to the key, either a computer keyboard key or a MIDI note. SportDJ listens for the next press, you do not have to type anything.
5. **Press the key** to play the sound.

<!-- Screenshot: key settings -->

## What you can do

- **Keys with sounds.** Each key plays a file, a folder of files or a playlist. Keys can loop.
- **Overlay keys.** A sound effect (horn, siren) plays on top of the running music without stopping it.
- **Tabs.** Organise your keys in tabs, for example *Warm-up*, *Game* and *End*, and switch between them with the keyboard.
- **Control keys** for the moments that matter: pause the output, lower the music while you talk, and switch the display to full screen.
- **Title bar display** with clock, current title and countdown, readable from a distance.
- **Keyboard and MIDI.** Use the number pad, a MIDI pad controller, or both.
- **Audio interfaces.** Play through ASIO or normal Windows audio devices.

<!-- Screenshot: title bar and control keys -->

### What the key colours tell you

A **pulsing yellow border** on a key means it is still loading or no audio output is available yet. Wait a moment, or check the output in the settings. A pulsing yellow gear button means the settings are just opening.

## Your sets

Everything you set up, keys, tabs and options, is saved in one settings file (`Settings.xml`). In the settings you can **load**, **save** and **start a new, empty session**. SportDJ asks before it overwrites another file.

**Taking a set to another computer:** keep the settings file together with your audio files in one folder, for example

```
MySet\
  Settings.xml
  Beginn\
  Spiel\
  Ende\
```

SportDJ stores the sound locations relative to the settings file, so you can copy the whole folder to another PC, a USB stick or a cloud drive and everything is still found.

## Troubleshooting

| Problem | What to try |
|---------|-------------|
| **No sound** | Open the settings and check the output device. Keys stay yellow as long as no output is available. |
| **ASIO device is not listed or does not start** | ASIO drivers come from the manufacturer of your audio interface. Make sure the driver is installed and that no other program is using the device. |
| **A key plays nothing** | The file may have moved. Open the key's settings and select it again. |
| **A MIDI controller is not found** | Make sure it is connected, then reload the controller list in the settings. |

## Feedback

Found a problem or have an idea? Please open an [issue](https://github.com/budul100/SportDJ/issues).
