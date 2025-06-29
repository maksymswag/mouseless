[Logo](https://raw.githubusercontent.com/maksymswag/mouseless/refs/heads/main/app.ico)
It's a high-performance Windows utility for keyboard-only mouse control. Activate with `CapsLock` to overlay a predictive grid on your screen. Use `WASD` to rapidly navigate the grid, which automatically switches to a fine-grained "nudge" mode for pixel-perfect positioning.

## How to Use

1.  Run `MouseGrid.exe`. An icon will appear in your system tray.
2.  Press `CapsLock` to toggle the grid on and off.

| Key(s)           | Action                                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------------------ |
| **`CapsLock`**   | Toggles the Mouse Grid **on** or **off**.                                                              |
| **`W / A / S / D`** | **In Grid Mode**: Selects a region. <br> **In Nudge Mode**: Moves the cursor by 25 pixels.          |
| **`Ctrl` / `Esc`** | Immediately deactivates the grid.                                                                      |

To quit the application, right-click the tray icon and select **"Exit"**.

---

## For Developers

### Build

-   **Prerequisites**: .NET 6 SDK or newer.
-   **Command**: Run `dotnet build -c Release` in the project's root directory.

### How It Works

The application uses three core components for its performance and functionality:
1.  A **global keyboard hook** (`WH_KEYBOARD_LL`) captures input system-wide.
2.  The visual overlay is a transparent **layered window** (`WS_EX_LAYERED`), which allows for perfect anti-aliased graphics without artifacts.
3.  A **high-frequency timer** drives a custom drawing loop, which only redraws the screen when the mouse moves. All graphics objects (`Pen`) are cached to prevent stutter from garbage collection.
