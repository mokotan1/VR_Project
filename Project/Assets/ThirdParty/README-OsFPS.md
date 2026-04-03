# OsFPS + UnityTK (ThirdParty)

Source: [OsFPS](https://github.com/kennux/OsFPS) and [UnityTK](https://github.com/kennux/UnityTK), MIT License.

The upstream project targets **Unity 2018** and is **archived**. These folders are vendored for reference and gradual porting; they may not compile cleanly in Unity 6 until APIs are updated.

## Playable desktop FPS in this repo

For a **working** desktop loop (movement, jump, hitscan, magazine, reload, HUD, targets), use the menu:

**VR Project → Scenes → Create OsFPS-Inspired Desktop Sandbox**

That scene uses fresh code under `Assets/_Project/Presentation/OsFpsInspired/` that follows OsFPS-style gameplay (hitscan, ammo) without requiring the full OsFPS entity stack.

## Submodule note

`git submodule add` for OsFPS defaults UnityTK to **SSH**; if clone fails, use HTTPS for UnityTK:

`git clone https://github.com/kennux/UnityTK.git` inside the OsFPS `UnityTK` path.
