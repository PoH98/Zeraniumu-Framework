# Zeraniumu Framework
This is the framework which uses EmguCV, SharpAdb and WinAPI for controlling an emulator or a process in a user computer, performing "scripts" and "bots" actions
Zeraniumu which is in japanese, means Geranium flower. Geranium have the meaning of we met each other, accidental.

## Disclaimer
**This framework is NOT FOR SALE! Don't use any bot or script softwares that cost you money if they using this framework!**

### Documentation
> You have 2 options for using this framework. Either compile your own exe or run the console based application without any Visual Studio installed.

---
### Method 1: Direct run console
1. Write a script and save it as any file type. The script should start like sample below:

    ```
    public static void Run()
    {
        <your script code here>
    }
    ```

2. The script should be in C# format. About the function list please preview [Wiki](#wiki)
3. Here will having a special function. As we can't build classes in this method, the script supports `@include <another file>`. Its function is override the specific line with that specific file you defined. Hence you can reuse the part of code in anywhere.
4. After completed the script, go to the `Zeraniumu.exe` and open command prompt there by pressing `shift + right click`. Type `Zeraniumu <your script path>` and press `Enter`
5. The script will start execute now!
6. You can run multiple scripts once, they will run at the same time, with multi-threading.
---
### Method 2: Compile an exe
1. Use any C# coding program and create new project
2. Add reference to `Zeraniumu.exe`, add new class which implements interface `IScript`. For example: 
   ```
   class ImplementedScript : IScript 
   { 
       public void Run() 
       { 
           <your code here> 
        } 
    }
    ```
3. Code the script whatever you like, for function list please preview [Wiki](#wiki).
4. 
5. Compile your project, and just direct start your project. The script will be executed now!
---
> Don't come and judge me your account had been banned or affected by bot virus. This program is just for learning purpose. I can't control how the other developers! However this framework is under GNU license, which means whatever other programmers using this framework should open source their code!
---
---
# Wiki
Here is some code example and function list of the whole bot framework.
## Preparation
Before anything starts, we need to get a log file first. This log file will record almost all your method calls at which line, which function and etc. This will be helpful on debugging!
## Direct run console special method
The below functions is the methods you can use in direct run console method
### @include <script file name>
This line will replaced with the file name you mention and reusable. Put it at the line you need to be override with the code in another file.
> Example:
```
@include anotherScript.txt
```
### @using <namespace>
This line will be replaced with normal C# using, current limitation will be only supporting the dll which is same name with namespace.
Will be upgraded to more flexible for this, but need time as I'm not quite free
> Example：
```
@using System.Threading;
```
### SharedBag
This class should be used to share resources between different asynchronized running scripts. However use it carefully as this might making the scripts running into trouble! The data stored is with thread-safe dictionary provided by Microsoft hence you don't need to worry about thread problem, just need to make sure the scripts able to get what they need in the right time!
> Example:
```
SharedBag.SaveValue("PublicInteger", 10);//Create new value
SharedBag.SaveValue("PublicInteger", 30);//Modify Value
SharedBag.GetValue<int>("PublicInteger");//Get value as int
SharedBag.DeleteValue<int>("PublicInteger");//Delete value and receive it's last result
```

---
## Log
> Example:
```
var logger = new Log();
```
Here you can add a richtextbox for showing some 'non-private' logs to let users know what is going on in the script.
> Example:
```
var logger = new Log(richTextBox1);
```
** *now, we get our log file done. We have to attach this log file to controller before we really use it.* **

After attached, you can use the functions below!

### WriteLog
Write a public log which will shows in the richtextbox, if setted. If not setted it will shows in Console. The log will be written into log file too!
> Example:
```
logger.WriteLog("Some logs with green color!", Color.Green);

logger.WriteLog("Some logs with default color!");
```

### WritePrivateLog
Write a log to log file ONLY.
> Example:
```
logger.WritePrivateLog("You can only see me in file!");
```

> The logs will be written in a fixed format:\
[23:18:27]: [SetLogPath|60]: Log Path Setted: Profile\Bot\Log\2020_10_15_23_18_27.log\
which will be [time]: [caller|line number]: Log

---
# Controller
We need to prepare the `Controller` for controlling your target. Here we have two default controllers which is `EmulatorController` and `ProcessController`. 

Here are some shared functions which both controller have.

### PrepairOCR
Prepair OCR traineddata. As we use EmguCV, our OCR will use Tesseract. Here the trainneddata will be automatically downloaded from gitub.
> Example:
```
core.PrepairOCR("eng");
```
### Screenshot
We will need to get the screenshot for processing like find image or colors.
> Example:
```
var iimagedata = core.Screenshot();
```
---

## EmulatorController
> Example:
```
var core = new EmulatorController(logger); //Attach log to the emulator controller
```
You may add a panel for docking the emulator inside it to prevent user's dirty hand affected script running.
> Example:
```
var core = new EmulatorController(logger, panel1);//Attach log and dock panel to emulator controller
```
### StartEmulator
After attaching the controller, we can now start our detected emulator installed in PC. It will automatically find already started emulator too.
> Example:
```
core.StartEmulator();
```
### ConnectEmulator
While starting the emulator, we have to hook with our emulator, like adb, minitouch and etc.
> Example:
```
core.ConnectEmulator();
```

### Dock
If we had attached the dock panel above， we can now call this function to dock our emulator inside the panel.
> Example:
```
core.Dock();
```

### Undock
If we want to undock the emulator from panel, just call this function.
> Example:
```
core.UnDock();
```

### GameIsForeground
We can check our game is at foreground or not by calling this function. Return true if yes and false if no.
> Example:
```
var booleanValue = core.GameIsForeground("com.package.name");
```
### StartGame
If we checked our game is not at foreground, we might need to start the game. You can use this adb command to get the game's activity name and package name: `dumpsys window windows | find /I 'mCurrentFocus'`
> Example:
```
core.StartGame("com.package.name", "com.activity.name");
```

### KillGame
We might need to exit the game sometimes.
> Example:
```
core.KillGame("com.package.name");
```

### Tap
Sending tap to emulator. The point won't really accurate, it will contains some random to avoid bot detection.
> Example:
```
core.Tap(new Point(0,0));
```
### Swipe
Sending swipe from one location to another in emulator. The swipe will not be perfect to avoid bot detection.
> Example:
```
core.Swipe(new Point(0,0), new Point(100,100), 3000);
//Swipe from point 0,0 to 100, 100 in 3 seconds
```
### LongTap
Sending a long touch to emulator. The point won't be accurate to avoid bot detection.
> Example:
```
core.LongTap(new Point(0,0), 3000);
//Send a long tap on point 0,0 with 3 seconds
```
### SendText
Send a sentence or text to emulator. This function will simulate a person using somekind of dictionary keyboard typing to avoid bot detection.
```
core.SendText("A long long text!");
```

### Hide System Bar
Use this to hide the android's top system bar
```
core.CloseSystemBar();
```

### Show System Bar
Use this to show the android's top system bar back
```
core.OpenSystemBar();
```

### Open Play Store
Use this to open spefific application download page from playstore.
```
core.OpenPlayStore("com.package.name");
```

### Settings
Here is some settings can be used in EmulatorController
| Setting               | Default                             | Type                       | Decription                                                                                                                                       |
|-----------------------|-------------------------------------|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| ResizeScreenshot      | true                                | bool                       | Check if the framework should crop the captured image to prefered size. Usefull in different situation to switch crop the image or not doing so. |
| EmulatorCaptureMethod | EmulatorCaptureMethod.WinApiCapture | enum EmulatorCaptureMethod | Define how the framework capture screenshots. Change this if you get a black image.                                                              |
| TapScale              | 1                                   | double                     | Scale up or down the tap position. Will be usefull in some devices or emulators                                                                  |
| MinitouchPath  | adb\minitouch | string | The minitouch file location. By default this no need changes as the zip in release contains it.                              |
| AdbPath        | adb           | string | The adb file path. By default this no need changes as the zip in release contains it.                                        |
| KeepBackground | true          | bool   | Used to check if we need to kept the bot running on background. Used for multiple purpose like capturing screenshots and etc |

---
---
## ProcessController
Above controller will only communicate with android emulators. If we need communicate with other processes, we should use this. However `ProcessController` is always not recommanded as games or application able to scan what application is running in background, which might easily get caught!
> Example:
```
var core = new ProcessController(logger, "path to your process installed", "the process name", "optional startup arguments"); //Attach log to the process controller
```

### StartProcess
Start the process and attach it with our bot.
> Example:
```
core.StartProcess();
```
### KillProcess
Kill the attached process. If no process is attached, this will do nothing!
> Example:
```
core.KillProcess();
```

### Check Process Alive
Check if attached process is still responding or alive (Haven't get killed)
> Example:
```
var booleanValue = core.ProcessAlive();
```

### LeftClick
Send left click. This will 'hijack' our lovely user's mouse cursor! Will automatically add randoms to avoid bot detecton.
> Example:
```
core.LeftClick(new Point(location));
```

### RightClick
Send right click. This will 'hijack' our lovely user's mouse cursor! Will automatically add randoms to avoid bot detecton.
> Example:
```
core.RightClick(new Point(location));
```

### DoubleClick
Send double click (left click). This will 'hijack' our lovely user's mouse cursor! Will automatically add randoms to avoid bot detecton.
> Example:
```
core.DoubleClick(new Point(location));
```

### MoveMouse
Move mouse to specific location. This will 'hijack' our lovely user's mouse cursor! Will automatically add randoms to avoid bot detecton.
> Example:
```
core.MoveMouse(new Point(location));
```

### HoldLeft
Hold left button (Click down but no up). This have to use with MoveMouse as here you can't set the location to start hold left click.
> Example:
```
core.HoldLeft();
```

### ReleaseLeft
Release holded left button. You can add `Delay.Wait` and `MoveMouse` for dragging
> Example:
```
core.ReleaseLeft();
```

### HoldRight
Hold right button (Click down but no up). This have to use with MoveMouse as here you can't set the location to start hold right click.
> Example:
```
core.HoldRight();
```
### ReleaseRight
Release holded right button. You can add `Delay.Wait` and `MoveMouse` for dragging
> Example:
```
core.ReleaseRight();
```

### KeyboardPress
Press specific button but not releasing it.
> Example:
```
core.KeyboardPress(VirtualKeyCode.SPACE); //Press down spacebar button
```

### KeyboardRelease
Release keyboard press
> Example:
```
core.KeyboardRelease(VirtualKeyCode.SPACE); //Release previous spacebar button
```

### GetIntPtr
Get hWnd of process for setting up no mouse move clicks
> Example:
```
//Get default process.MainWindowHandle
var mainhWnd = core.GetIntPtr();
//Get deeper child hWnd from MainWindowHandle
mainhWnd = core.GetIntPtr("className", "string.Empty", main);
```

### GetChildrenPtrs
Get all child hWnd in the parent hWnd, return IEnumerable<IntPtr> 
> Example:
```
var listhWnd = core.GetChildrenPtrs(mainhWnd);
```

### SetIntPtr
Set the click will pass to the hWnd if ClickMethod is WinAPI
> Example:
```
core.SetIntPtr(mainhWnd);
```

### KeyboardType
Simulates keyboard typing
> Example:
```
core.KeyboardType("A sentence here");
```

### BlockInput
Lock user's mouse and keyboard. User can press `CTRL+ALT+DEL` to unlock
> Example:
```
core.BlockInput();
```

### Dispose
Unlock user's mouse and keyboard
> Example:
```
core.Dispose();
```

### Settings
Here is some settings can be used for the ProcessController to execute it's job.

| Setting       | Default                   | Type               | Decription                                                                                                                                                           |
|---------------|---------------------------|--------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ClickScale    | 1                         | double             | It is used to scale up while clicking. Some devices might need to scale up or down to get it's click at the right position                                           |
| CaptureMethod | CaptureMethod.GDIPlus     | enum CaptureMethod | It is used to define how the framework should capture a screenshot. Change this if you get a black screen!                                                           |
| ClickMethod   | ClickMethod.RealMouseMove | enum ClickMethod   | You can choose how the framework performs its clicks. Using WinAPI or the real mouse moving for clicks. WinAPI can used for background however won't works everytime |

---
---
## Screenshot
The images are all in class `ScreenShot` by default. It contains EmguCV Image processing inside which is now packed nicely for easy usage.

### FindImage
After we get our screenshot, we might need to find image template. 
> Example: 
```
var listofpoints = iimagedata.FindImage(new ScreenShot("path\to\template.png", logger, xmlFile: false), true, 0.8);
if(listofpoints.Count() > 1) //Image match found
{
    //Bla bla bla
}
else
{
    //Not found
}
```

### Crop
Crop the image to smaller. Returns IImageData, which actually is Sceeenshot
> Example:
```
iimagedata.Crop(new Rectangle(0, 0, 10, 10));
//Crop image from point 0, 0 with width 10 and height 10
```

### ColorMatch
Check if the point is specific color
> Example:
```
var booleanValue = iimagedata.ColorMatch(new Point(165, 49), Color.FromArgb(11,5,27), 0.9);
```

### GetColor
Get the color of the point
> Example:
```
var color = iimagedata.GetColor(new Point(30, 30));
//Get color from point 30, 30
```

### FindColor
Find color exist in specific area of the image
> Example:
```
var colorLocation = iimagedata.FindColor(new Rectangle(80, 80, 100, 100), Color.Red, 0.8);
//Check area starts from point 80, 80 with width 100 and height 100 have red color. Match radius 0.8
```

### SaveXml
Save the image to xml format file
> Example
```
iimagedata.SaveXml("image.xml");
```

### SaveFile
Save the image to normal image
> Example:
```
iimagedata.SaveFile("image.png");
```

### ToBitmap
Convert the image to bitmap
> Example:
```
var bmp = iimagedata.ToBitmap();
```

### OCR
Recoginize text from image. Make sure you cropped the image which left the text area first before entering here! You should `prepareOCR` first in the controller before you can use this function.
> Example:
```
var text = iimagedata.OCR(core);
```

### Moved
Detect if the image have something moved. Return true if yes, else false.
> Example:
```
var lastimage = core.Screenshot();
if((iimagedata as Screenshot).Moved(lastimage))
{
    //Something moved
}
```


---
# RoadMap

Current supported emulators:
- [x] MEmu
- [ ] Nox
- [ ] Bluestack
- [ ] Droid4X
- [ ] ITools

> If any other emulators needed to be supported, let me know!
