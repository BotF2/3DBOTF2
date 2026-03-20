A Unity-based space strategy game built with C# and .NET Standard 2.1.

🚀 Quick Start for Developers
Prerequisites
Required Software:

Unity Hub (latest version)
git clone https://github.com/BotF2/3DBOTF2.git cd 3DBOTF2
*Unity 6, (Once you clone from GitHub check In your Project Folder for the folder ProjectSettings holding the ProjectVersion.txt file for exact version)
Visual Studio 2022+
Recommended: Visual Studio Community 2026 (18.4+)
Git (2.0+)
.NET Standard 2.1 SDK
Recommended Hardware:

16GB+ RAM
SSD with at least 10GB free space
GPU with DirectX 11/12 support
🎨 Coding Standards
### C# Settings

C# Version: 9.0
Target: .NET Standard 2.1
**### The Unity 'MonoBehaviour' Manager/Controller/Data class code structure:

**# Manager class roles: (A static single 'instance' class in the game, see CivManager, FleetManager, ShipManager...)

Act as a Factory
 A factory is the class responsible to create a new object / instantiate a new gameObject of a specific type
Maintain a list of ALL the game objects that has been instantiated.
provide a method to get a specific game object from the list.
will be used to saved the data component (the data class) attached to the controller of each game object. For example, the fleet manager contain a list of all the fleet game object of the game. Each fleet game object has a fleet controller component. Each fleet controller has a fleet data component.
(The fleet data is basically a copy of the fleet controller properties that will be needed when we will be saving the game and loading the game.)
**#Controller class roles:

the controller is the one containing the logic for a game object. Example: fleetController has a method MoveToDestination(GameObject destination GO)
(The MoveToDestinationGo() is called to command the fleet game object to move to a certain point.)
**#Data class roles:

the primary role of the data class is serialization. We want to be able to save the progress of a fleet/ civilization faction/ star system during gameplay, we want to save for example, the hit points of a fleet, its current destination and position, etc. The controller and the data class are working closely together. When we save a game, the data is saved as the state of the game object. For example the fleet A.
When we load a game, we instantiate a generic fleet prefab game object, then we will immediately over ride the fleet controller of that prefab with the saved data of fleet A.
**### Key Conventions (from .github/copilot-instructions.md)

Naming: Use descriptive names; avoid abbreviations except industry-standard (e.g., UI, SFX)
ScriptableObjects: Use for data configuration (see SoundData.cs and ShipSO.cs examples)
Pooling: Use Unity Object Pools for frequently spawned objects (Like Audio, SoundData)
**### Unity Documentation

Unity Manual
Unity Scripting API
**### Third-Party Assets

Mirror Networking Docs
DOTween Documentation
**### namespace
BOTF3D.Core → Managers, Data classes, Game systems
BOTF3D.UI → UI Controllers(menu, HUD, panels)
BOTF3D.GamePlay → Gameplay controllers(Fleet, Ship, System)
BOTF3D.Audio
BOTF3D.SpaceCombat
BOTF3D.UI
📦 Detailed Setup Instructions
✅ Unity Hub + correct Unity version
Install Unity Hub https://unity.com/developer-tools?clickref=1101lD8rVNaT&utm_source=partnerize&utm_medium=affiliate&utm_campaign=unity_affiliate&gad_source=1&gad_campaignid=22883287084&gbraid=0AAAABA_4ouI15_4SCtDw8WOxKVduer4Vg&gclid=Cj0KCQjwmunNBhDbARIsAOndKpnvjJgI7IPc_Xjj41UrSvMvz0UfGzAGuS_ZYIc33XpFJ3UB9PSPZgAaApgiEALw_wcB
Windows and possibly Mac / not Android or iOS build support
✅ Visual Studio setup (The free Community version is more than adequate )
Install Microsoft Visual Studio with: .NET desktop development and Game development with Unity workload. In the Visual Studio Installer Modify/Workload check the Game development with Unity. I have added the extension GitHubTestProjectVisualSutdio17Extension.

 Configure Visual Studio Integration
Back In Unity: Edit → Preferences → External Tools (connects Unity to VS)

Set: External Script Editor → Visual Studio to your current version Visual Studio (2022+)

Check all Generate .csproj files options

Click Regenerate project files
This fixes common issues like: Missing IntelliSense and Red squiggles on valid code

📦 Get the project: If using Git, Install Git and: git clone or GitHub to clone.
Open Project in Unity
1. Open Unity Hub
2. Click Add → Add project from disk
3. Navigate to the cloned 3DBOTF2 or whatever you named it folder
4. Select the folder and click Open
5. Unity will import all assets (first import takes 5-15 minutes)
4) ⚙️ Let Unity import everything: Open the project through Unity Hub and Wait for: Asset import, Package restore, Script compilation
⚠️ First load can take a long time — that’s normal
The project uses Unity Package Manager. Packages should auto-install, but if there are issues here is a list of what is currently in Package Manager/In Project:
2D Sprite, Addressables, AI Navigation, Analytics, Analytics Library, Authentication, Burst, Collections, Custom NUnit, Development, Deployment API, Input System, JetBrains Rider Editor, Localization, Mathematics, Mirror, Mono Cecil, Multiplayer Center, Multiplayer Center Quickstart Content, Multiplayer Play Mode, Multiplayer Services, Multiplayer Tools, Netcode for GameObjects, Newtonsoft Json, Performance testing API, Qos, Scriptable Build Pipeline, Scriptable Render Pipeline Core, Searcher, Services Core, Setting Manager, Shader Graph, Timeline, Unity Profiling Core API, Unity Transport, Unity UI, Universal Render Pipeline, Universal Render Pipeline Config, Visual Effects Graph, Visual Scripting, Visual Studio Editor, and Wire.
Key Dependencies:

Mirror Networking - Multiplayer framework (included in Assets)
TextMesh Pro - UI text rendering (included)
DOTween - Animation tweening (included in Plugins)
CameraMultiTarget - Camera system (included)
If packages are missing:

Window → Package Manager
Check for errors and resolve dependencies
(! Once it loads in Unity the main window may show the default blue gray horizon: If so, go to the Project window and click on the 'Scenes' folder. Now double click the PersistentScene to load it in the Hierarchy window. Now right click the MainMenuScene and click the 'Open Scene Additive' option. In the main view window, on the Game tab you should now see the opening menu. Click the Play button, top center.
🧪 Verify scripts compile: Before coding: Check Unity Console for errors and Fix any missing dependencies.
🐞 When you want to Enable debugging In Visual Studio look in the Unity app and double a script C# file to open Visual Studio: Set Attach and use in VS: Debug → Attach Unity Debugger to select the active project.
🧼 Recommended settings (quality of life) In Unity: Enable Enter Play Mode Options (faster iteration, optional) and Set correct scripting runtime (.NET version)
In Visual Studio: Enable: Full solution analysis and IntelliSense, Disable unnecessary workloads to keep it fast.
🚨 Common beginner issues (and fixes)
❌ No IntelliSense - Regenerate project files and Make sure Visual Studio Tools for Unity is installed
❌ Missing references / errors - Wrong Unity version, Packages didn’t restore
❌ Build errors on first open - Usually resolves after reimport Or delete Library/ and reopen (last resort)

🐛 Troubleshooting
    Common Issues
Unity won't open project:

Verify Unity version matches project
Delete Library/ folder and re-open
Script compilation errors:

Assets → Reimport All
Regenerate .csproj files in Visual Studio settings
Git merge conflicts in .meta files:

Carefully resolve—these link assets to Unity's internal database
When in doubt, reimport the asset
Audio not playing:

Check AudioManager is in scene
Verify SoundData assets are assigned in AudioManager's sound library
Use SoundData's right-click → "Preview Sound" to test clips
📧 Contact
Repository: https://github.com/BotF2/3DBOTF2
Issues: Use GitHub Issues for bug reports and feature requests
🧠 What a new team member might ask for

✅ Unity version

✅ Repo URL, GitHub...

✅ Branch to work on

✅ Coding standards (naming, architecture)
