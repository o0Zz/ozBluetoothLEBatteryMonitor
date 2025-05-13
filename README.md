# 🔋Oz's Bluetooth Battery Monitor

![Banner Gif of Tray](https://i.imgur.com/r18xM3j.png)

### Introduction
The Bluetooth Battery Monitor adds a small and simple battery display to your system tray. These battery displays allow you to see the life of Bluetooth devices connected to your computer.

**Notifications**   
Whenever a device drops below 20%, a notification will play if activated.  
**Auto Startup**  
When activated, the application will start automatically at system startup.  
**Adjustable Refresh Rate**  
You can change how often the system looks for and updates the battery charge levels.

### Quick Start Guide
Follow the directions below to get started!

**Step 1.** Download BluetoothLEBatteryMonitor.exe. [Latest Release](https://github.com/o0Zz/ozBluetoothLEBatteryMonitor/releases)

**Step 2.** Double click *BluetoothLEBatteryMonitor.exe*

**Step 3.** Open the system tray balloon and locate the battery icon with the Bluetooth symbol in the center. Move this to the main tray.

**Step 5.** Right-click the icon and select exit to shut down the program. Select setting to adjust settings.

*Start on startup is not activated by default. This must be switched on in the setting or it will need to be manually started every time.*

# ⚙️Settings and Configuration⚙️
![Settings Banner Photo](https://i.imgur.com/HuXtQqF.jpeg)
### How to Access Settings
When the application is powered up you will find a battery with a Bluetooth icon in the system tray. Right-click this and select *Settings*.

### Settings Breakdown 

- [ ] Launch application on startup
  - *[Off by Default]* When activated it will automatically boot up this program when Windows starts up.

- [x] Enable Notifications
  - When the battery of one of your Bluetooth devices drops below 20%, it will display a notification.

- [x] Automatically detect new devices (If unchecked, detect device only during startup)
  - By default, the system will continuously search for new Bluetooth devices to monitor. If you want it to only search once when the computer first starts then disable this option.

- [x] Refresh [5 min Default]
  - This option allows you to adjust how often the application will pull and update the battery displays.

  # 🔧Troubleshooting🔧

- Battery icon not in system tray
  - Check in the system tray pop-up 
  - Manually start the application again
    > On the first startup, the battery icon will go into the pop-up menu in the system tray, not the main tray. If you can't find it anywhere it's possible there was an issue starting the application. Manually starting the program can help restart it.

- Battery shows -1%
  - Allow for the refresh timer to pass and check again 
  - Exit and restart the application
    > The application only samples the battery charge level every 5 min by default. This time is adjustable in the settings. If you add a device it will not display a charge level until it has been sampled. If you give it time and it still does not show then exiting and restarting the program should help.

- Not auto-starting on system startup
  - Check to make sure this feature is activated in the application settings
  - Check it is enabled on the system startup page
    > By default this feature is disabled. If you want the application to power up on startup then it needs to be enabled in the setting of the application. See the setting section on how to do this. If it still won't start on startup then go into task manager and move to the start up tab. Ensure it is enabled here as well.

- Not sending low-battery notifications
  - Check that the notifications are enabled in the settings
  - Ensure notifications are not disabled for Windows or application 
  - Exit and restart the application if the problem persists
    >If you are not getting the low power notifications then you should first check that they are enabled in the application settings. If you still have no notifications, check that they are not disabled in the Windows settings. If you give it time and it still does not show then exiting and restarting the program should help.

- Duplicate Battery Icons
  - Exit out of all and manually restart application
    > If the application is opened more than once it can produce multiple icons as it is running multiple instances in the background. Right clicking adn exiting all of them and then manually starting the application again should fix this.

### References
- https://github.com/MUedsa/BluetoothLEBatteryMonitor/
