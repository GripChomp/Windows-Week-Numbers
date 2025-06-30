# Windows Week Number Display

A lightweight Windows application that displays the current ISO 8601 week number either as an overlay above the taskbar or as a system tray icon.

## Demo

![Beschrijving van de gif](./windows_week_numbers_demo_1.gif)

## Buy me a coffee
[![Buy Me A Coffee 8-bit](./support_me_with_coffees.png)](https://betaalverzoek.rabobank.nl/betaalverzoek/?id=MmEGapIPSKSZTf9NOuXw1Q)


## Features

- Display the current week number as an overlay or in the system tray
- Multiple themes to choose from:
  - **Standard**: Clean off-white with black text
  - **Indigo**: Modern purple theme
  - **Dark**: Dark mode with light text
  - **Retro 95**: Windows 95-inspired theme with pixel art and animations
- Automatically updates at midnight
- Remembers position and theme preferences
- Draggable overlay window
- Option to start automatically with Windows
- Minimal resource usage
- Automatically hides during fullscreen applications

## Requirements

- Windows 10/11
- .NET 6.0 Runtime or later

## Installation

1. Download the latest release from the Releases page
2. Extract the ZIP file to a location of your choice
3. Run `WeekNumberTrayOverlay.exe`

## Usage

- Right-click on the overlay to access the menu
- Choose between different visual themes:
  - Standard, Indigo, Dark, and Retro 95
- Access settings from the right-click menu:
  - Switch between overlay and tray icon modes
  - Choose tray icon color (white or black text)
  - Enable/disable automatic startup with Windows
- Left-click and drag to reposition the overlay
- The application remembers your position and theme preferences

## Building from Source

1. Clone this repository
2. Open the solution in Visual Studio 2022 or later
3. Build the solution
4. The compiled application will be in the `bin/Release/net6.0-windows` directory

## License

This project is licensed under the MIT License - see the LICENSE file for details. 