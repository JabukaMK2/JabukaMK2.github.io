---
layout: page
title: Algorithms and Data Structures
permalink: /algorithms/
---

# Algorithms and Data Structure<br>(RepairIQ)

This artifact is the core logic behind the automated repair cycle component of my finished product.  The original was a simple batch file to run automated repair scripts, requiring a right click and run as admin to use it properly.  The idea was to automate the OS repair workflow to the point that I didn’t need to intervene.  I made it along with the other portions of my system as separate batch files or applications.  I find that this artifact sufficiently shows my progress with algorithms and data structures.  Not only is the whole RepairIQ function a series of separate, encapsulated command files, the commands all execute a large number of OS repair tasks without any intervention.

## Automated Workflow

The app is separated in logic by 4 unique parts all with specific tasks they complete:

1. *Hardware* which uses built in hard drive functions to test for SMART values on the host hard drive, it then hands that over to Prime95 which stress tests the CPU and RAM.  These 3 devices make up a vast majority of failures so it tests them first.  After all, there’s no point in repairing the OS if the device is physically failing.
2. *Antivirus* portion uses windows own defender program to run a quick scan on the host device, even if the host device has another antivirus installed, it will run this without real time protection, preventing issues.  Before starting it updates its virus definitions.
3. *Bitlocker* tool hosts logic for disabling encryption before rebooting to ARC-PE as external media cannot boot to device while the drive is encrypted.  Right now this is a TODO as ARC-PE was not completed as part of this capstones scope.
4. *OS Repair* runs checks on the devices temp files (clears them), runs DISM restore and component cleanup, SFC to repair files integrity, and chckdsk to validate the drives integrity, then repair violations.  Chckdsk runs last specifically because it requires a reboot to complete, but the task itself doesn't need to inform the technician.

## Orchestration

The original app used main.cmd to run each command independently.  This only allowed me to use return codes between 0 and 9 like I did for each command file.  This meant that logic errors or fails were all on the command line to report and would give the user very little control over the automated test flow.  I used 0 to 9 return for all command files and wrote a translation matrix within mainpage.xaml.cs to return codes into data the core program could use.

In the final product I created a new system that uses C# code to manage states and run each scan.  I created an automatic repair flow that live reported the status of the repair while it ran.  This also allows me to create an exclusion workflow later in the product development.

## Data Structure

The final app uses three different data sources to manage active program execution:

- States
- Strings
- Codes

Command prompts use exit codes due to certain conditions that then get translated into string that are fed into the relational database.

As those strings go to the database they are also passed to states that update the status of the repair to the technician.

## Reflection

I feel I have met all the planned outcomes I intended and then some.  I was able to implement far more using windows commands than I initially anticipated.  In the final project I intend to further integrate each feature with one another.  I also am unsure at this time if my final project will include a WinPE version.  Eventually, that is my goal, but I don’t plan to integrate that until after completion of this course.  I am also unsure if DataIQ is necessary (the file manager) considering windows tools already available.  If anything, I may add some functionality where the application can launch other useful apps from the launcher like file manager, revo uninstaller, etc..

This part of the project hosted a whole new gambit of problems that required a lot of adaptation.  For one, many of the scripts I had to run were Windows-only commands which are impossible to test on a Linux device.  To remedy this, I reinstalled windows to an older laptop I have and am using that as a testing bench for my applications.  Fortunately, git allows me to move between the devices pretty easily, and failing that, I can publish the app to a USB for testing.  I learned a lot about Windows command line, including echo, pause, net, and extension support for functions like if, for, or while.  Once I found out I could use loops my life became a whole lot easier.  I believe one of the scripts uses loop:, an old WIN-DOS command for loop functions and finding a more modern way to handle it made life a lot better.  Even more, I learned how to use .NET to publish apps into .exe files for later use.  Fortunately, IDE’s make it pretty easy nowadays, as I was expecting an excruciatingly painful process in order to publish each app.  I am also grateful to my past self for encapsulating each program separately, as it has made this project much easier to approach.

[← Back to Home]({{ '/' | relative_url }})
