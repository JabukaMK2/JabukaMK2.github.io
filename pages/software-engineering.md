---
layout: default
title: Software Design and Engineering
permalink: /software-engineering/
---

# Software Design and Engineering<br>(ARC Launcher)

My artifact was created about 3 months ago but only as a simple java based menu system that I planned to integrate later.  It is called ARC or the Advanced Repair Console.  I am currently a technician at a phone and computer repair franchise where there is a great lack of experience for software repairs.  

Before this job I worked for a large electronics retailer’s repair service, whose techs had an in-house USB toolkit with automated repairs and WinPE boot.  I was shocked to find that my current shop had no such tool, and while I wasn’t expecting something as robust as my previous job, I figured they must have something.

I included this project in my ePortfolio as a means to show my problem solving and software engineering skills.  This project has been a great undertaking and I almost regret not just improving upon course content instead, but I know I can make something great.

## Encapsulation

So far I have made a large number of changes from the original.  First, java was out almost right away.  I prefer working in java, but what I need is something that runs native in all Windows environments.  WinPE is a very minimalist version of the OS, and if I can properly integrate my app with it then it should run native on any other device.  For this purpose I have selected .NET v10 as the scaffolding in my project.  .NET is simple, written in C#, and runs off of native elements that would be present in any windows environment.

I structured the project by encapsulating it into 4 core components:

- ARC.exe application that runs the launcher.
- RepairIQ.exe that houses the repair flow (see more in algorithms).
- DataIQ.exe for file transfers (yet to be integrated).
- ArcShared which hosts all needed shared files between each.  

In order to communicate the state between each app I used a session.json file which is written by ARC and read by RepairIQ.  I purposely separated the session context control from other apps, establishing a rule that only one app would have editing capabilities (single-writer).  Doing this was strong separation of concerns and allows each component to function independent of the main app.  RepairIQ saves data separately into a SQLite database (see databases).  For a cohesive theme I used a single file linked to each project.  I often changed my mind on colors used so I created once place where I could change all of them at will.

## Linux/Windows Integration

Next, I had a massive problem.  I use Linux instead of Windows and don’t have another computer powerful enough to compile something as complex as this.  I could have wiped my computer and reinstalled Windows, but I made myself a promise when I started my CS courses.  I would NOT go back to Windows, I would take every issue and hiccup as an opportunity to learn.

What I needed was a flexible interface that could allow my computer to run the program native.  I found a fully open source (Apache 2.0 license) product called the Uno Platform.  The Uno Platform was designed as a multi-purpose implementation for development of runtime across Windows, Linux, and Mac platforms.  The Uno Platform fortunately has project build capabilities that you can directly integrate to VS Code.

After I recompiled my code, I ended up with a working platform from which to build.  From there I used basic .NET resources to create a simple console with buttons.  I am not yet sure how it will run in a Windows environment as I have not yet got to that step beyond virtualization.

## Licensing

Next, one major focus was ensuring that nothing I used would violate licenses of the product when used in a commercial environment.  I became much more familiar with the types of licenses used in code development such as:

- MIT used by the Phosphor icon pack I used and my own project.
- Apache 2.0 used by Uno (only for framework).
- Revo Uninstaller which allowed commercial use under a Freeware license, meaning it could be used but cannot be changed or integrated in any way.

Many tools use open source licenses which only allow unlimited use for non-commercial applications.  If I was to present this tool as an option it had to abide by any license ahead of time.  Thus, as MIT and Apache 2.0 require, I have the license stored along side the application.

## Reflection

I had hopped to integrate WinPE within this phase, but I may have to scrap that capability due to its complexity.  At the moment I believe my project as it stands meets the course outcomes.  As I delve further into this project, I may need to further simplify but I would assess that I have completed a major portion of the finished product.  What I have learned so far is the difficulty of resolving errors created by early adaption.  The initial project before I began this capstone project was a simple scaffold created by AI tools or Uno/.NET project files.  While it helped with .NET integration, AI is very difficult to work with, and any tweaking or errors need to be caught by the user.  It took far too long to undo much of the generated content, though to its credit, most of the code was either from .NET or the Uno Platform documentation.  It even went as far as to put documentation notes on certain components within some of the code which helped significantly with resolving conflicts.  I am confident that I have resolved any issues the original code base created, although there is a non-zero chance that some redundant code still exists that was unnecessarily generated.  As with most programmers, I am a little too superstitious to remove anything if it’s working right now and there is no way I would trust any AI agent to go through and remove anything without breaking everything.
