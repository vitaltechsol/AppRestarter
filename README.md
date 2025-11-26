# AppRestarter

AppRestarter is a Windows Forms application designed to allow you to remotely restart applications on other PCs via a client/server model. 
It also allows you to shut down or restart remote PCs.

AppRestarter gracefully stops the target application before restarting it, ensuring a clean restart process.  
If the application is frozen or unresponsive, AppRestarter can forcefully terminate it.

The application includes **Apps** and **PCs** navigation tabs, allowing you to manage applications and remote PCs separately.

---

## Features

* Start or stop applications remotely or locally.
* Restart applications using a web browser (Web UI).
* Create groups to start/stop groups of applications.
* Add remote PCs and perform **Shutdown** and **Restart** commands.
* Keep logs of actions performed.
* Lightweight and easy to deploy.
* Supports optional auto-start with Windows.
* Supports start-minimized mode.

---

## Requirements

* Windows 10 or newer
* Application must be installed and accessible from the specified path.
* To use **remote restart**, **shutdown**, or **restart PC** features, the remote machine must also be running AppRestarter.

---

## Installation & Setup

### 1. Download
Download the ZIP and extract it:  
https://github.com/vitaltechsol/AppRestarter/releases

### 2. Deployment
You must run **AppRestarter** on:

* The **remote PC** (where the applications actually run)
* The **local PC** (the controller)

### 3. Run AppRestarter as Administrator
To stop/start elevated applications or send shutdown/restart commands:

1. Right-click the EXE  
2. Choose **Properties → Compatibility**  
3. Check **Run this program as administrator**

### 4. Windows Defender Firewall

Allow inbound TCP on AppRestarter's port (default **2024**):

1. Open *Windows Defender Firewall with Advanced Security*
2. Select **Inbound Rules → New Rule**
3. Choose **Port**, then *Next*
4. Select **TCP**, enter **2024**
5. Allow the connection
6. Name it **AppRestarter**

---

## The New Navigation System

AppRestarter provides two main tabs on the left sidebar:

### **Apps**
This tab shows:
- Your configured applications  
- Group buttons  
- App/group restart and stop actions

This is where you manage **application-level operations**.

---

### **PCs**
This tab shows:
- All remote PCs you’ve added  
- A special **[All PCs]** button that can shut down or restart every PC at once  
- Per-PC shutdown/restart options  
- Context menu options (right-click)

This tab handles **PC-level power control**.

---

## Adding Remote PCs

To send shutdown/restart commands or restart remote applications, you must add PCs to AppRestarter.

### To Add a PC
1. Switch to the **PCs** tab  
2. Click **Add New PC**  
3. Enter:
   - **PC Name** (e.g., “Gaming Rig”, “Sim Machine”)
   - **PC IP Address**
4. Save

This creates an entry under the `Systems` node in `applications.xml`.

### Editing or Removing a PC
Right-click any PC button:
- **Edit** – change name or IP  
- **Delete** – remove the PC from the config  
- **Shutdown**  
- **Restart**

### PC Power Controls
From the **PCs** tab:

| Action | How |
|--------|-----|
| Shutdown a PC | Click the PC button |
| Restart a PC | Right-click → Restart |
| Shutdown ALL PCs | Click **[All PCs]** or right-click |
| Restart ALL PCs | Right-click **[All PCs]** |

A confirmation modal always appears to prevent accidental shutdowns.

---

## Adding & Configuring Applications

In the **Apps** tab:

### Configure New Applications

Click **Add New App**.

Fields:

| Field | Description |
|--------|-------------|
| **Name** | Friendly name |
| **Restart Path** | Full path to `.exe` (local or remote!) |
| **Process Name (Optional)** | Needed only if the app runs in background or path is unknown |
| **Client IP** | Select the remote PC (leave this pc selected if local) |
| **Auto-start app after X seconds** | Delay before auto-start on AppRestarter launch |
| **Auto-start minimized** | Starts app minimized |
| **Don't warn** | Skips confirmation modal |
| **Group** | Assigns app to a group |

---

## Process Name (Optional)

Process Name is **optional** and used only when:
- The application runs as a background process  
- The executable path isn’t enough to identify the process  
- Multiple paths share similar names  

How to find Process Name:

1. Press **Ctrl + Shift + Esc**
2. Go to **Details**
3. Locate the process (e.g. `notepad.exe`)
4. Enter only **notepad** in the Process Name field

---

## Groups

AppRestarter supports application grouping for batch operations.

### Creating a Group

1. Add or edit an application
2. Open the **Manage Groups** dialog
3. Add new group name(s)

### Assigning Applications to a Group

- When adding/editing an app, select a group from the dropdown  
- Choose **None** to leave unassigned  

### Using Groups

- Clicking a group button restarts **all apps in that group**  
- Right-click → **Stop** stops them all  
- Groups also appear in the **web interface**

---

## Using the Web Interface

AppRestarter includes a built-in web UI.

Access it at: http://<local-ip>:<WebPort>
For example `http://192.168.1.123:8090`.
* Note that AppRestarter must run as administrator for this work.
* You can change the web port in Settings

## Settings

- **App Port**  
  The TCP port used by the AppRestarter server for communication between local and remote clients.  
  Default: `2024`

- **Web Port**  
  The port the built-in web UI version runs on.  
  Access the web UI by visiting `http://<your-ip>:<WebPort>` in a browser.  
  Default: `8090`

- **Auto Start with Windows**  
  If set to `true`, AppRestarter will configure itself to run automatically when Windows starts.  
  This is done using a Windows Scheduled Task so that the program can also run with administrator rights if required.  
  Default: `true`
	- 
- **Start Minimized**  
  If set to `true`, AppRestarter will start minimized when launched.  
  This is useful when running at startup so the application doesn't pop up in front of the user.  
  Default: `false`


## Notes

* The application uses XML to store app configurations.
* You can edit or delete entries directly from the Add/Edit form.
* Logs are displayed line by line in the UI.

---

For any issues or suggestions, please open an issue.
