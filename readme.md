Nice, this deserves a clean, solid README 👍
Below is a **ready-to-drop-in README** structure for **both projects**, written as if it lives at the root of each repo. You can tweak tone later, but this is already “public-repo ready”.

---

# 🌸 Glow 2025 – Controller & Flower Firmware

This repository is part of the **Glow 2025 / 2026 interactive flower installation**.
The project consists of **two main codebases**:

1. **Controller application** – runs on a Raspberry Pi with touchscreen
2. **Flower firmware** – Arduino code running on the mechanical flowers

Each project is explained below.

---

## 📁 Project 1 – Flower Controller (Desktop / Raspberry Pi)

**Repository:**
👉 [https://github.com/goldenplayer5630/Glow-2025-Main.git](https://github.com/goldenplayer5630/Glow-2025-Main.git)

### 📌 Purpose

This project contains the **controller software** responsible for:

* Loading and playing light/motion shows
* Sending commands over a **serial bus (RS-485)** to multiple flowers
* Providing a **touch-friendly UI** for operators during the Glow festival

---

## 🧱 Tech Stack

* **.NET 8**
* **Avalonia UI** (cross-platform desktop UI)
* **Serial communication** via `SerialPortStream`
* Target platforms:

  * **Linux (Raspberry Pi)**
  * **Windows (development & testing)**

---

## 🖥️ How the Controller Runs

### Runtime Overview

* The app is a **desktop-style UI**, not a web app
* Runs fullscreen on a **Raspberry Pi with touchscreen**
* Communicates with flowers via **USB → RS-485 adapter**
* Sends **line-based serial commands** to all connected flowers

---

## 🧠 Serial Communication Explained

### Why Serial?

The flowers are connected using an **RS-485 bus**, which:

* Allows long cable runs
* Supports many devices on one bus
* Is robust for outdoor installations

### Implementation Details

We use:

```text
RJCP.SerialPortStream
```

Instead of the default .NET `SerialPort`, because:

* Better Linux support
* Async I/O
* More stable under load

The transport layer:

* Automatically appends `\n` (LF) to outgoing commands
* Reads incoming data as **frames split by newline**
* Raises events per received frame

---

## 🐧 Linux & Native Binaries (Important!)

On **Linux (Raspberry Pi)**, `SerialPortStream` depends on **native binaries**.

### What we did

* Explicitly include:
  * `RJCP.SerialPortStream.Native.linux-arm64`
* These binaries are **required** on the Pi
* Without them, serial ports will fail to open

This was a key learning point during development.

---

## 🪟 Running on Windows (Easy Mode™)

Good news:
👉 **On Windows, it “just works”**

* No native binaries required
* Standard USB-serial drivers handle everything
* Ideal for:

  * Development
  * Testing without hardware
  * Debugging UI & show logic

### Run locally

```bash
dotnet run
```

Or just open the solution in **Visual Studio** and hit ▶️.

---

## 🚀 Deployment (Raspberry Pi)

Typical setup:

* Raspberry Pi (Linux, ARM64)
* Touchscreen display
* USB → RS-485 adapter
* App runs:

  * Fullscreen
  * On boot (systemd / startup script)

Deployment is usually done by:

* Publishing a **self-contained .NET build**
* Copying it to the Pi
* Running it directly

---

---

# 🌷 Project 2 – Flower Firmware (Arduino)

**Repository:**
👉 [https://github.com/goldenplayer5630/Glow-2025-Small-Flower.git](https://github.com/goldenplayer5630/Glow-2025-Small-Flower.git)

This repository contains **all Arduino firmware** used by the flowers.

Each flower runs independently but listens on the same **serial bus**.

---

## 🌼 Firmware Variants

There are **three supported variants**:

---

### 1️⃣ Big Flower – Serial Controlled

📄 **Documentation:**
`readme-big-tulip.md`

**Features:**

* Accepts serial commands
* Controls:

  * Motors
  * LEDs
  * Animations
* Used for the large, mechanical “hero” flowers

**Typical commands:**

* LED intensity
* RGB color
* Ramp in / ramp out
* Motor movement

---

### 2️⃣ Small Flower – Serial Controlled

📄 **Documentation:**
`readme.md` (inside the small flower folder)

**Features:**

* Smaller hardware footprint
* Serial command driven
* Designed for large quantities
* Lower power usage

Used where:

* Many flowers are needed
* Simpler motion/lighting is sufficient

---

### 3️⃣ Button-Controlled Variant (Hybrid)

🆕 **Newest variant (created recently)**

**What’s different:**

* Adds **physical button input**
* Buttons are created by **shorting pins**
* Allows:

  * Manual triggering
  * Debugging without the controller
  * Interactive installations

This variant still supports serial commands but can also react locally.

---

## 🔁 Uploading / Re-Uploading Firmware

Reprogramming a flower is straightforward.

### What you need

* Arduino IDE
* USB cable
* Correct board selected (e.g. Arduino Nano)
* Correct COM / serial port

### Steps

1. Open Arduino IDE
2. Open the desired `.ino` file
3. Select:

   * **Board**
   * **Processor**
   * **Port**
4. Click **Upload**

That’s it 🚀

No special tooling required.

---

## 🔧 Customization

All variants are designed to be:

* Easy to tweak
* Well-commented
* Safe to re-flash multiple times

Common changes:

* Pin assignments
* LED limits
* Timing / ramp durations
* Button behavior

---

## 🌈 Final Notes

Together, these two projects form the backbone of the **Glow mechanical flower installation**:

* **Controller** → orchestration, shows, UI
* **Firmware** → real-time hardware control

They are intentionally:

* Modular
* Robust
* Easy to debug on-site during Glow


