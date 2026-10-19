# Voice wave icon

Created with the built-in image_gen tool on 2026-10-19. Master: voice-wave-master.png.
Replaces the borrowed development placeholder across all existing logo/tile/splash assets, including the legacy backup file. Asset dimensions are preserved; playback control icons are unchanged.

Final generation prompt:
Use case: logo-brand. Create one original Windows voice pitch training application icon. Square production asset, no mockup, no lettering. Full bleed solid deep indigo #202441 background. Centered bold mint green abstract continuous vocal waveform that rises into a smooth pitch contour, with a small warm ivory circular endpoint. Extremely simple, polished flat vector-like silhouette, thick rounded strokes, balanced asymmetric rhythm, strong readability at 24 pixels, generous 18 percent safe margin. No microphone, no pink and blue split background, no zigzag dividing line, no recognizable existing app logo, no gradients, shadows, borders or text. Output a single square icon.

## Final selected revision: trans pride voice wave
Master: trans-voice-wave-master.png. Generated with the built-in image_gen tool. The earlier mint master is an unused concept.
Prompt: Use case: logo-brand. One finished square Windows application icon for transgender voice training. Make transgender identity immediately recognizable through the transgender pride flag's EXACT five stripe order light blue #5BCEFA, pink #F5A9B8, white, pink #F5A9B8, light blue #5BCEFA. Design a single bold flowing voice waveform ribbon with these five parallel colored bands flowing along its entire shape. The waveform ribbon has two smooth upward humps and one deep valley, rounded terminals, strong simple silhouette readable small. Centered on a full-bleed flat dark midnight navy #202441 square background. Waveform occupies 78 percent width and 55 percent height, centered, no lettering, no microphone, no medical cross, no extra marks, no flag pole, no zigzag divider, no original Voice Tools icon composition. Crisp polished flat vector-style icon, no 3D or mockup. Trans pride colors must be prominent, equal stripe widths.

## Current selected icon: trans microphone
Master: trans-microphone-master.png. Generated using the built-in image_gen tool. Replaces both earlier concepts in all application icon resources.
Final prompt: Use case: logo-brand. Create one finished square flat app icon for a transgender voice training Windows app. Visual direction: recognizable centered microphone and transgender pride light blue pink white colors, but a fresh independent composition. Full bleed solid very light ivory #FAF9F6 background, no corner rounding. Center a large deep midnight indigo microphone silhouette: rounded capsule head with THREE short ivory horizontal grille slots, open U shaped cradle below, short stem and broad rounded foot. Behind microphone place TWO large overlapping solid circular disks, light blue at upper left and pastel pink at lower right, overlapping as white lens. Disks contained with 12 percent outer safe margin. Add two small dark indigo curved sound resonance arcs on each side of microphone, balanced, minimal. Microphone is strong readable shape occupying central 45 percent height, background disks occupy 75 percent square. Restrained crisp flat geometric vector-like rendering, no textures, no gradients, no shadows, no lettering. Do not use a pink upper half blue lower half divided by a white zigzag. Do not reproduce an existing branded logo. One icon only, no mockup, no presentation.

## Original third-party icon backup
`icon_old.png` is an unmodified copy of the original development placeholder supplied in Desktop/Workgroup/icon.png. It is third-party Voice Tools artwork, not generated artwork and not covered by a license granted by this project. Keeping it here is not a grant of permission from its rights holder.

The active manifest continues to use the new icon. To select the old resources, edit Package.appxmanifest and add `_old` before `.png` in its seven image references:
- Assets\StoreLogo.png → Assets\StoreLogo_old.png
- Assets\Square150x150Logo.png → Assets\Square150x150Logo_old.png
- Assets\Square44x44Logo.png → Assets\Square44x44Logo_old.png
- Assets\Wide310x150Logo.png → Assets\Wide310x150Logo_old.png
- Assets\SmallTile.png → Assets\SmallTile_old.png
- Assets\LargeTile.png → Assets\LargeTile_old.png
- Assets\SplashScreen.png → Assets\SplashScreen_old.png

Matching `_old.scale-*` and `_old.targetsize-*` assets preserve Windows resource resolution and are included in the project. Rebuild and redeploy after switching. Restore the reference names to select the new icon again. Old assets are included in distributed packages even when inactive; a user's decision to switch does not remove the project's responsibility for distributing third-party artwork.
