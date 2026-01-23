---

# 🌸 Glow Flower Installation – How It Works (Festival Edition)
## Big Picture

The Glow flower installation consists of **three simple layers**:

```text
[ Touchscreen Controller ]
            ↓
[ Central Flower Controller ]
            ↓
[ Multiple Mechanical Flowers ]
```

One controller, many flowers — all moving and lighting up **in sync**.

---

## 🎛️ The Controller (What the operator sees)

* A **touchscreen interface**
* Used to:

  * Select a show
  * Start / stop animations
  * Control lighting and movement
* Runs on a **Raspberry Pi**, hidden in a control box

💡 Think of it as the *DJ booth* for the flowers.

---

## 🧠 The Brain (What the controller does)

Inside the controller:

* The system:

  * Reads a show (timeline of events)
  * Sends commands at the right moment
* Commands include:

  * Light colors
  * Brightness
  * Fade-ins / fade-outs
  * Movement triggers

Everything is timed so **all flowers stay synchronized**.

---

## 🌷 The Flowers (What the audience sees)

Each flower:

* Has its **own small computer**
* Listens for commands
* Controls:

  * LEDs
  * Motors
  * Animations

Flowers do **not depend on each other** — they all listen to the controller.

---

## 🌼 Different Flower Types (Visually identical, internally different)

### 🌺 Big Flowers

* More powerful
* More movement
* Used as visual highlights

### 🌸 Small Flowers

* Simpler mechanics
* Used in larger numbers
* Same look, lighter hardware

### 👆 Interactive Flowers

* Have buttons or sensors
* Can be triggered manually
* Still work together with the show

---

## 🔁 What Happens During a Show?

```text
Operator presses START
        ↓
Controller plays show
        ↓
Commands sent over cable
        ↓
All flowers react together
```

* Lights fade in
* Colors change
* Flowers move
* Everything stays synchronized

---

## 🛠️ What If Something Breaks?

Designed for festivals, so:

✅ Controller can be restarted safely
✅ Flowers keep their last state
✅ One broken flower ≠ broken installation
✅ Cables can be unplugged / replugged
✅ Flowers can be reprogrammed quickly

---

## 🎯 Why This Setup Is Festival-Proof

* ✔ Minimal cabling
* ✔ No Wi-Fi dependency
* ✔ Works in rain & cold
* ✔ Easy to debug on site
* ✔ Fast replacement of parts
* ✔ Operator-friendly UI

---

## 🧑‍🤝‍🧑 Who Is This For?

* **Operators** → touchscreen only
* **Technicians** → plug & play hardware
* **Artists** → flexible animations
* **Audience** → smooth, magical experience ✨

---

