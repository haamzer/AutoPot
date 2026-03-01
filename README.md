# AutoPot

A Dalamud plugin for FFXIV that automatically recommends and swaps the optimal potion on your hotbar when entering duties.

## Features

- **Ultimate raids**: Selects the cheapest potion that caps your stats at the sync level
- **Savage raids**: Auto-detected via game data — uses the highest available potion
- **Normal raids**: Same as Savage — highest available potion
- **Hotbar auto-swap**: Finds the potion on your hotbar and replaces it with the recommended one
- **Chat notification**: Prints a message in chat when a potion is swapped
- **Test mode**: Simulate any duty to preview recommendations

## Supported Ultimates

| Duty | Sync | Optimal Tier |
|------|------|-------------|
| UCoB | i345 | Grade 3 Tincture |
| UWU | i375 | Grade 4 Tincture |
| TEA | i475 | Grade 6 Tincture |
| DSR | i600 | Grade 8 Tincture |
| TOP | i635 | Grade 1 Gemdraught |
| FRU | i665 | Grade 4 Gemdraught |

Savage and Normal raids are detected automatically from game data — no hardcoded lists needed.

## Installation

1. Open Dalamud settings with `/xlsettings`
2. Go to the **Experimental** tab
3. Add the custom plugin repo URL
4. Save, then open `/xlplugins` and search for **AutoPot**

## Usage

Type `/autopot` in-game to open the plugin window.

Put any potion on your hotbar, enter a duty, and AutoPot will swap it to the optimal one.
