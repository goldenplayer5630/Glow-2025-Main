---

# 🌸 Glow Flower Installation – How It Works (Festival Edition)

![Image](https://newbiely.com/images/tutorial/raspberry-pi-touch-sensor-led-wiring-diagram.jpg)

![Image](https://petapixel.com/assets/uploads/2024/06/how-to-build-timelapse-controller-feat.jpg)

![Image](https://www.holidaycoro.com/v/vspfiles/assets/images/LOR-Controllers-Together-With-E1.31-Pixel-Controllers.gif)

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

## 🔌 The Cable Network (How flowers are connected)

![Image](https://www.researchgate.net/publication/333870484/figure/fig2/AS%3A771484674957312%401560947688913/Connection-diagram-of-RS485-Communication-Circuit.ppm)

![Image](https://res.utmel.com/Images/UEditor/31b6e9d7-153a-4b74-a160-bd3fa5de146a.jpg)

![Image](https://www.lorric.com/Files/Images/HowTo/what-is-rs485/what-is-rs485-Daisy-Chain-en-53.jpg)

* All flowers are connected using **one shared cable**
* This cable:

  * Runs long distances
  * Is very reliable outdoors
  * Connects many flowers at once

If one flower fails:
👉 **The rest keep working**

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
Controller plays timeline
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

