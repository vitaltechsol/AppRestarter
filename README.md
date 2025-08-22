# AppRestarter

AppRestarter is a Windows Forms application designed to allow you to remotely restart applications on other PCs via a client/server model.

## Features

* View, add, edit, and delete application configurations.
* Start or stop applications remotely or locally.
* Restart applications from a web browser.
* Keep logs of actions performed.
* Lightweight and easy to deploy.

## Requirements

* Windows 10 and newer
* Application must be installed and accessible from the specified path.
* To use remote restart, the client machine must run this app and listen on port 2024.

## Installation & Setup

### 1. Download

Download the ZIP and extract it. https://github.com/vitaltechsol/AppRestarter/releases


### 2. Deployment

You need to run **AppRestarter** on both:

* The **remote PC** (which has the applications you want to restart).
* The **local PC** (from where you want to initiate remote restarts).

### 3. Run AppRestarter as Administrator

To ensure you can start/stop applications that require elevated (admin) permissions, you must run **AppRestarter.exe as Administrator**.

To do this:

* Right-click the EXE or shortcut
* Go to **Properties → Compatibility** and check **"Run this program as administrator"**

### 4. Windows Defender
Allow the app to communicate over the network when prompted, or:
1. Open "Windows Defender Firewall with Advanced Security".
2. Go to **Inbound Rules** > **New Rule**.
3. Choose **Port**, then click Next.
4. Select **TCP**, and enter port **2024**.
5. Allow the connection, name the rule `AppRestarter`, and finish the wizard.


### 5. Configure New Applications

Use the UI to add applications.
**You don't need to add the applications on both Server and Client.** Only add the applications to the Server

Click the *Add New* Button

* **Name:** Friendly name
* **Process Name:** e.g. `notepad` (without `.exe`) (See more instructions bellow)
* **Restart Path:** Full path to the `.exe` of the app. IF the app is remote, should be the path in the remote PC.
* **Client IP (Optional):** If the application is located on a different PC. Add the IP address.
* **Auto-start app after:** Will automatically start the application after that many seconds when AppRestarter starts
* **Auto-start minimized:** Will start the application minimized
* **Don't Warn when restarting:** Will not show the confirmation modal when restarting the apps.

**To find the process name:**

1. Press **Ctrl + Shift + Esc** to open **Task Manager**
2. Go to the **Details** tab
3. Look at the **Name** column (e.g. `FlightSimulator.exe`, `notepad.exe`)
4. Enter just the name without `.exe` in the app configuration

### 5. Edit / Remove Applications
After entering the applications, you can edit it by right-clicking the button and selecting `Edit`

### 6. Restart or Stop Applications
Click the button to restart an application. You can also right-click and select `Stop` to stop the application.

### 7. Using a web browser
* Enter the PC's IP with port 8080 to view the web version. 
  For example `http://192.168.1.123:8080`.
* Note that AppRestarter must run as administrator for this work.
* You can change the web port in applicantions.xml


## Notes

* The application uses XML to store app configurations.
* You can edit or delete entries directly from the Add/Edit form.
* Logs are displayed line by line in the UI.

---

For any issues or suggestions, please open an issue.
