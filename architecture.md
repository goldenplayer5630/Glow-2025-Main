# 🧩 Architecture Diagrams

## Project 1 – Controller Architecture

### High-Level Overview

```text
┌────────────────────────────┐
│        Touchscreen         │
│    (User Interaction)      │
└─────────────┬──────────────┘
              │
┌─────────────▼──────────────┐
│  Avalonia UI (.NET 8)      │
│  - Show selection          │
│  - Playback controls       │
│  - Status & logging        │
└─────────────┬──────────────┘
              │
┌─────────────▼──────────────┐
│   Application Core         │
│  - Timeline engine         │
│  - Command scheduling     │
│  - Priority handling      │
└─────────────┬──────────────┘
              │
┌─────────────▼──────────────┐
│  Transport Layer           │
│  SerialPortStream          │
│  - Line-based frames       │
│  - Async read/write        │
│  - Linux native binaries   │
└─────────────┬──────────────┘
              │ USB
┌─────────────▼──────────────┐
│ USB → RS-485 Converter     │
└─────────────┬──────────────┘
              │ RS-485 Bus
┌─────────────▼──────────────┐
│  Flowers (1..N)             │
│  - Big flowers              │
│  - Small flowers            │
└────────────────────────────┘
```

---

### Key Design Choices

**Avalonia UI**

* Cross-platform
* Touch-friendly
* Same UI runs on Windows & Raspberry Pi

**Transport Layer**

* Clean abstraction (`ITransport`)
* Serial framing handled in one place
* Easy to add Modbus / TCP / relay transport later

**RS-485 Bus**

* Multi-drop (many flowers, one controller)
* Robust over long cable distances
* Ideal for outdoor festival setups

---

### Platform Differences

| Platform     | Notes                                          |
| ------------ | ---------------------------------------------- |
| Windows      | Plug & play, no native deps                    |
| Linux (Pi)   | Requires `SerialPortStream.Native.linux-arm64` |
| Raspberry Pi | Runs fullscreen, auto-start on boot            |

---

## Project 2 – Flower Firmware Architecture

![Image](https://hackster.imgix.net/uploads/attachments/1122521/half_duplex_WfXR2hmoK6.jpg)

![Image](https://maker.pro/storage/yrdzjPx/yrdzjPxPGRGDazX6YS36VRrIDq7zFd3lSsWt0nZi.png)

![Image](https://europe1.discourse-cdn.com/arduino/original/4X/f/5/7/f57c1a02416a5220da68a141dd9ab07647f4adba.png)

### Generic Flower Node

```text
┌────────────────────────────┐
│        RS-485 Bus          │
│  (Shared with other nodes) │
└─────────────┬──────────────┘
              │
┌─────────────▼──────────────┐
│ RS-485 Transceiver         │
│ (e.g. MAX485)              │
└─────────────┬──────────────┘
              │ UART
┌─────────────▼──────────────┐
│ Arduino MCU                │
│ - Command parser           │
│ - State machine            │
│ - Timing & ramps           │
└───────┬─────────┬──────────┘
        │         │
┌───────▼───┐ ┌───▼─────────┐
│ LEDs       │ │ Motors /    │
│ (PWM / RGB)│ │ Relays      │
└────────────┘ └─────────────┘
```

---

## Firmware Variant Differences

### 1️⃣ Big Flower (Serial Only)

```text
Controller
   │
   │ Serial commands
   ▼
Big Flower Arduino
   ├─ Motor control
   ├─ LED rings
   └─ Timed animations
```

* Fully remote-controlled
* No physical interaction
* Designed for synchronized shows

---

### 2️⃣ Small Flower (Serial Only)

```text
Controller
   │
   │ Serial commands
   ▼
Small Flower Arduino
   ├─ Simpler LEDs
   ├─ Reduced power usage
   └─ Mass-deployable
```

* Lightweight
* Cheap to replicate
* Same protocol, less hardware

---

### 3️⃣ Button-Enabled Variant (Hybrid)

```text
          ┌──────────────┐
          │ Button Input │
          │ (Pin short)  │
          └──────┬───────┘
                 │
Controller ──► Arduino ◄── Manual Input
```

* Serial **and** physical input
* Buttons created by shorting pins
* Useful for:

  * Debugging
  * Standalone interaction
  * Fallback mode if bus is down

---

## Why This Architecture Works Well for Glow

✅ Scales from **5 to 100+ flowers**
✅ Easy on-site debugging
✅ Windows dev ≠ Linux deployment issues isolated
✅ Hardware failures don’t crash the controller
✅ Clear separation between **show logic** and **hardware logic**

---
