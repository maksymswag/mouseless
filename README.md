# Mouse Grid Utility

## Overview

Mouse Grid is a high-performance, keyboard-driven mouse navigation utility for Windows, written in C#. It allows for rapid, precise mouse pointer positioning without ever touching the mouse.

The application runs silently in the system tray. When activated, it overlays a visual grid on the screen, which the user can navigate with the `WASD` keys to "zoom in" on a target location. After a few zooms, the tool switches to a fine-grained "nudge" mode for pixel-perfect adjustments.

This tool is designed for maximum performance and a "native" feel, using a layered window for its overlay to provide clean, anti-aliased visuals with zero artifacts.

## Features

- **Keyboard-Only Activation**: Trigger the grid with a single press of the `CapsLock` key.
- **Custom Grid Layout**: A unique layout with top/bottom bars and a split middle section for intuitive navigation.
- **Real-time Visual Feedback**: A dynamic trail of red lines is drawn from the cursor to predicted target points, following the mouse in real-performance, keyboard-driven mouse navigation utility for Windows, written in C#. It allows for rapid, precise mouse pointer positioning without ever touching the mouse.

The application runs silently in the system tray. When activated, it overlays a visual grid on the screen, which the user can navigate with the `WASD` keys to "zoom in" on a target location. After a few zooms, the tool switches to a fine-grained "nudge" mode for pixel-perfect adjustments.

This tool is designed for maximum performance and a "native" feel, using a layered window for its overlay to provide clean, anti-aliased visuals with zero artifacts.

## Features

- **Keyboard-Only Activation**: Trigger the grid with a single press of the `CapsLock` key.
- **Custom Grid Layout**: A unique layout with top/bottom bars and a split middle section for intuitive navigation.
- **Real-time Visual Feedback**: A dynamic trail of red lines is drawn from the cursor to predicted target points, following the mouse in real-time.
- **Variable Line Thickness**: Lines for the immediate next move are drawn twice as thick, clearly indicating the primary targets.
- **Line Fading**: Visuals automatically fade to 10% opacity after 100ms of inactivity to be less intrusive.
- **Multi-Stage Navigation**:
    1.  **Grid Mode**: Quickly traverse large screen areas.
    2.  **Nudge Mode**: After 5 grid moves, automatically switch to a mode where `WASD` moves the cursor by 25 pixels for fine adjustments.
- **Inactivity Timeout**: The grid automatically deactivates after 2 seconds of no input to prevent it from staying active unintentionally.
- **System Tray Integration**: The application runs in the system tray, providing a clean "Exit" option.
- **Highly Optimized**: Uses a layered window for perfect transparency and a high-frequency timer for smooth, low-latency visuals without unnecessary CPU usage.

---

## User Guide

### Installation

1.  Navigate to the build output directory (e.g., `bin/Release/net6.0-windows/`).
2.  Ensure the following files are in the same folder:
    *   `MouseGrid.exe`
    *   `app.ico`
    *   `cursor.wav`
3.  Double-click `MouseGrid.exe` to run the application. An icon will appear in your system tray.

### How to Use

The application is controlled entirely by the keyboard once running.

| Key(s)           | Action                                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------------------ |
| **`CapsLock`**   | Toggles the Mouse Grid **on** or **off**.                                                              |
| **`W`**          | **In Grid Mode**: Selects the top region. <br> **In Nudge Mode**: Moves the cursor up 25 pixels.         |
| **`A`**          | **In Grid Mode**: Selects the left region. <br> **In Nudge Mode**: Moves the cursor left 25 pixels.       |
| **`S`**          | **In Grid Mode**: Selects the bottom region. <br> **In Nudge Mode**: Moves the cursor down 25 pixels.      |
| **`D`**          | **In Grid Mode**: Selects the right region. <br> **In Nudge Mode**: Moves the cursor right 25 pixels.    |
| **`Ctrl` / `Esc`** | Immediately deactivates the grid and returns to normal operation.                                     |

### Exiting the Application

Right-click the Mouse Grid icon in the system tray and select **"Exit"**.

---

## Developer Guide

### Prerequisites

-   [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) or newer.

### Project Structure
