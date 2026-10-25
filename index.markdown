---
layout: home
title: About Me
---

## Welcome!

My name is Eric Schooler and I am currently a technician for a prominent device repair company.  I have a strong repair skill-set in troubleshooting and physical repairs.  One day, I decided that I wanted to grow my skill set from IT and technical experience to CS with programming with my focus.  When I started my degree, I was working at a major retailer as a store manager and found the job unrewarding, lacking creative challenge.  Then one day, my partner approached me after a rough day during the holidays and said "if you don't like it, change it, get a degree!" and thus I began my journey at SNHU.

As I continued my time at SNHU, I made the determination to switch to Linux from Windows 11.  I did this for couple reasons, the first, as an OS repair technician I saw an incredible amount of vulnerabilities and errors with Windows due to outdated architecture such as the NTFS file system.  In addition Microsoft as of Windows 11 24H2 had removed a local account bypass in an effort to force the user to sign in, this was so they could more efficiently show the user ads within the start menu and take telemetry data via cloud.  Finally, a well known game platform innovation company named Valve announced that due to extreme limitations of Windows and the forced "walled garden" architecture they would be making an open source Linux build based on Arch and develop a gaming compatibility layer based on Wine called Proton.  Because of this, I had no reason to stay on Windows, and I felt I could learn more by giving myself one rule: I would never go back to Windows, instead I would find a way.  I used Linux as an opportunity to learn OS architecture along side my computer science program.

Many times during my studies I ran into hiccups like in **CS-330: Graphics and Visualization** where the OpenGL project we worked on was project file built with the MSVC which only works on Windows  In order to render the application, I had to quickly learn about virtualization with GPU acceleration capabilities on Linux.  Virtualization software support *specifically* for GPU acceleration was required due to my build.  VMWare was the first step, but installation required deep system level access and longer setup which at the time I could not afford.  Next, I attempted to use my DE (desktop environment) native VM called Gnome Boxes.  That also did not support my needs, despite having incredible system integration.  Last I tried VirtualBox which supported my needs perfectly.  Now I use WinBoat to run individual applications off a hidden Windows 11 VM, running it headless is more convenient and more efficient.

In my future I see me as either a full stack developer, reverse software engineer, or software developer.

<br><br>
## Collaborating in a team environment
**CS-465: Full Stack Development:** We worked entirely out of git, creating a branch for each module as we worked.  The work we completed in that course is within my repository here.  In the course we created a website for travlr, a travel agency, using MEAN stack development.  We were a representative from our organization who worked on the web app along with simulated tasks being completed between modules by another programmer.  Git was used following best practices (like creating a new branch) each time to prevent git errors during push or pull.

In my workplace we use work orders to signify each device and often must pass tasks to one another.  It's important to have good documentation so no one is caught off guard.  Ensuring we are all on the same page also keeps work order completion timely, as most will just not touch your work if you have already started it.  This is also why ARC uses work orders assigned at the start of RepairIQ to ensure the order can be opened by another technician later, allowing them to view past results.

<br><br>
## Communicating with stakeholders
**IT-266: Communications in STEM:** We went over the communications side from the perspective of an interested party designing the application, but not implementing.  This course spent no time on actual code and instead leaned into the deliverables and stakeholder communications.  It consisted of a mock client looking to have an application named 1Latte created that was a social media platform with an open source design.

<br><br>
## Data Structures
**CS-320: JUnit Testing:** We focused on engineering an automated JUnit testing system that would execute test cases one by one.  Here we learned about test coverage and exception handling, although no other language makes it quite as easy as Java's native JUnit system.  My own code in this program was C# and was being implemented in a Linux environment.  I had less to work with, but I was able to test by hand on multiple devices to gain as much understanding as possible.  Over time more data will become available as more devices get tested.

<br><br>
## Software Engineering and Databases
**CS-340: Server Development:** A focus on MongoDB and database queries where CRUD principles were established.  Here I discovered that a large number of helper functions is far preferable to repeating yourself in code.  After building a create, return, update, delete system, you can call on them when querying the database.  Doing so is far more preferable to entering queries directly into MongoDB and here we both had to do it manually, then with the help of functions we made.

**Intro to Databases:** Focused on MySQL and was my first exposure to database systems.  This course was a learning experience for sure.  Database queries in the console we used did not allow recalling previous queries, so say a query failed, that meant you had to go back and redo the entire query.  I definitely improved my typing accuracy after suffering this course!

<br><br>
## Security
**CS-305: Software Security:** Was a class focused on Maven projects using testing methods for security vulnerabilities.  We used centralized vulnerability scans through Maven and worked as a security consultant company for Artemis Financial who were creating a new system for financial data.  The class wasn't about storing the data, but rather about handling the data in a safe way that avoided vulnerabilities.  We learned about SHA-256 hash checksum, and RSA certificate handling.  We also discussed common methods of data protection such as AES using hidden and public keys.

---
<br>
## Featured Project: Advanced Repair Console (ARC)

The three artifacts in this portfolio are not separate class assignments, instead I built this from scratch based on an idea and USB toolkit's I have used in the past.  I created them because I needed something like it and I missed the tools I used to have.  Thus, I used the ARC project across all three deliverables as the toolkit could easily match all three core areas.  Together they, my tools follow the path of a single repair, from start to finish.  This is not the finished product, only the first draft.

**[View the ARC project →]({% link pages/arc-landing.md %})**
