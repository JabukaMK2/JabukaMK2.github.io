---
layout: page
---

# Advanced Repair Console (ARC-PE)

### Welcome!

The ARC application is a unique tool designed to bridge the gap between knowledge and execution.  Often, technicians lack software repair experience and in a repair environment, finding time to educate techs on a field with such nuance (software repairs) is not always possible.  This tool aims to help technicians automatically complete software repairs without any intervention by the tech.

The goal is a set it and forget it tool that tells the technician exactly what went wrong so they may effectively communicate that with the client while establishing a baseline software repair competence across multiple stores.  In the day to day often technicians in other locations will send clients to ours due to my software repair competencies, this tool means the easy problems are solved which makes up a vast majority of client OS repairs.

<br><br>

### ARC
The first artifact, the ARC launcher, shows software design and engineering.  I rebuilt it from a simple Java launcher that used the grid drop down menus and simply asked "what are we doing?" then completed tasks based on user input.  What I really wanted though, was a bootable toolkit because "you can't put out a fire inside the house" giving you an external access to the OS.  I needed something simpler that WinPE could understand and that relied on native tools within Windows.  For this I switched to C# on .NET 10, using the open-source Uno Platform so I could develop on Linux.  ARC collects device details and displays them for the technician, it also serves as a launcher for other apps like RepairIQ but functionally does none of the repair workflow itself.
![Alt Text](/pages/images/arc-landing.png)
<p align="center"><sub>Note: This is the landing page for ARC.exe, here the tech can switch between four pages.  Device info is grabbed using WMI toolkit system.management.</sub></p>

<br><br>

### RepairIQ
#### Main Console
The second is RepairIQ for algorithms and data structures.  It replaces a batch file that I started with, but it was clunky, and you still needed to know what you were doing.  I kept the principle of CMD files, allowing me to execute commands from the files independently.  I did this so in later versions I could implement selecting the step you'd like to complete.  Now it runs an automated algorithm, controlled by a C# class and executed in command files.  The command files return a value 0-9 and the class reads and interoperates that data into usable strings.  First step is Hardware test (CPU, RAM, and Storage) because if the hardware fails then there is no point in fixing the OS.  Next, the prompt uses MS Defender to scan files only using the scan, not live protection.  In the future I would like to implement some external antivirus but the core logic is implemented.  I also had it update its definitions before running.  Last the OS repair command cleans up temp files, runs DISM, SFC, and chckdsk.  Originally it forced a restart after giving a y/n prompt so that chckdsk could properly run.  Each step reports in simple language so the technician can relay information directly to the client.
![Alt Text](/pages/images/repair-iq.png)
<p align="center"><sub>Note: This is the testing page where progress is updated with an advanced section at the bottom that shows the terminal in action.</sub></p>

<br><br>

#### Scan Summary
The third artifact, the RepairIQ database, was written in C# and implemented using SQLite.  The scan automatically fills the database with the results of the repair.  Those results are saved upon scan completion, then retrieved at the end of the scan to show a comprehensive overview.  Its four tables use the STRICT type, CHECK constraints with DEFAULTS "Unknown" so if the data isn't updated, null doesn't return.  This saves null for other errors giving me another path.  Once the results are posted to the app, it gives a copy button so the technician can put the results into work order notes.  Right now, this doesn't make much sense, as the user is on the clients device not a work device.  However, in the later adaptations of the app, I intend to use the work order prompted for before RepairIQ opens to retrieve scan data from the database, that way the tech can type in the work order on their work device and pull up results instantly.  Then copy and past into the work order notes.  This ensures consistency in OS repairs between technicians as the app _forces_ you to enter a work order.  Additional constraints can also be added later to further enforce that.  The design is meant to be open to an external source so later database implementation can be linked and uploaded for active scan history integrated directly into our work order system.
![Alt Text](/pages/images/results.png)
<p align="center"><sub>Note: This is the page that immediately opens after RepairIQ completes, it retrieves the data from the SQLite database then displays it.</sub></p>

<br><br>

### DataIQ (*not yet implemented*)
![Alt Text](/pages/images/data-iq.png)
<p align="center"><sub>Note: This will have an option to select a folder, then select a location to auto transfer user data for backup.  It is currently in progress.</sub></p>

Together all my apps follow a single repair flow, with heavy focus on the actual outcomes.  I have implemented much of my systems with feedback from techs in the workplace.  I have shown them progress and asked for their opinions.  The main thing is ease of access to the results, so they don't have to interoperate them directly.  While at the same time, other more experienced techs wanted _more_ information which lead to the advanced section during system scans.

<br><br>

## Enhancement Narratives

- [Software Design and Engineering (ARC Launcher)]({{ '/software-engineering/' | relative_url }})
- [Algorithms and Data Structures (RepairIQ)]({{ '/algorithms/' | relative_url }})
- [Databases]({{ '/databases/' | relative_url }})

*"This is an independent, open-source project and is not affiliated with, endorsed by, or associated with any commercial repair corporation."*
