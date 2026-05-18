# SlotMachineAssignment
# Jackpot Rush

## Play Online
🔗 https://osdern.github.io/SlotMachineAssignment/

Jackpot Rush is a 2D pixel-art slot machine game developed in Unity using C#.  
The game combines classic casino mechanics with upgrade systems, probability manipulation, animated rewards, and a short story-driven progression.

## Features

- 🎰 Fully animated slot machine system
- 🍒 4 unique reel symbols:
  - Cherry
  - Bell
  - Bar
  - Seven
- 📈 Luck upgrade system that dynamically changes reel probabilities
- 💰 Reward multiplier system for 2-match and 3-match combinations
- 🪙 Animated coin reward effects
- 🔊 Audio system with adjustable master volume
- 💾 Save and load system using PlayerPrefs
- 🧠 Probability-driven stopping logic
- ⏸ Pause menu and settings menu
- 📖 Intro dialogue/story sequence
- 🌐 WebGL support for browser play
- ✨ DOTween-powered UI and reel animations

---

# Gameplay

The player starts with limited money and must use the slot machine to earn enough coins to survive increasing debt.

Each reel symbol has its own probability:
- Cherry → Common
- Bell → Uncommon
- Bar → Rare
- Seven → Legendary

Players can upgrade their luck to:
- decrease low-tier symbol probability
- increase high-tier symbol probability
- improve overall winnings

Winning combinations reward coins based on:
- symbol rarity
- match count
- current gambling amount

---

# Upgrade System

The game includes progression mechanics such as:

- Luck upgrades
- Reward multiplier upgrades
- Gambling amount scaling
- Spin duration upgrades

Luck upgrades dynamically rebalance probabilities:
- Seven gains the highest increase
- Bar gains moderate increase
- Bell decreases moderately
- Cherry decreases the most

---

# Technical Overview

## Core Systems

### Reel System
The reel movement system uses DOTween sequences for:
- smooth reel motion
- pause/resume logic
- symbol spawning
- controlled stopping behavior

### Luck System
Each reel item evaluates probability independently after the spin timer expires.

### Winning Evaluation
The game checks:
- 2-match rewards
- 3-match rewards
- dominant symbol payout logic

### Save System
Persistent data includes:
- balance
- upgrades
- luck values
- audio settings
- progression state

---

# Built With

- Unity
- C#
- DOTween
- TextMeshPro

---

# WebGL Build

This project supports WebGL browser builds.

To run locally:
1. Build the project for WebGL
2. Open the generated `index.html`
3. Serve using a local server

---

# Scripts Overview

| Script | Responsibility |
|---|---|
| AudioManager | Handles all game audio |
| CoinAnimation | Coin reward visual effects |
| DialogueController | Intro story dialogue system |
| HandleController | Slot machine handle interaction |
| MenuManager | Pause/settings/menu transitions |
| MoneyManager | Money balance and animation |
| ReelMover | Reel movement and stopping logic |
| SaveManager | Save/load persistence |
| SlotGameConfig | Luck values and gameplay config |
| SoundSliderController | Audio slider integration |
| UpgradeButtonController | Upgrade panel animations |
| WinningCondition | Match detection and rewards |

---

# Probability System

Default symbol probabilities:

| Symbol | Probability |
|---|---|
| Cherry | 45% |
| Bell | 22% |
| Bar | 15% |
| Seven | 8% |

Luck upgrades modify these values dynamically during gameplay.

---

# Author

Developed by Osdern
