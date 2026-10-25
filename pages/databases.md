---
layout: page
title: Databases
permalink: /databases/
---

# Databases

The final of the 3 main implementations was databases and I built this as part of the larger plan of the ARC toolkit.  So far it has a RepairIQ system that does advanced OS repair tasks and hardware testing.  The database is a method for storing and accessing data written during the RepairIQ process for proper logging and to give a simple one page report at the end of the testing cycle.

## MongoDB to Relational

I started thinking from a non relational perspective.  I soon realized that MongoDB documents was not engineered for the type of data I needed.  Add to that, MongoDB was too complex for something that integrates into RepairIQs core logic. Instead I thought of it through the technicians eyes.

What questions do they need to answer?

- Did the RAM, CPU, and storage pass?
- Was OS corruption found?
- Were viruses found and were they automatically resolved?
- What exactly failed in a way explainable to a customer?

For questions like these JSON files were not the stronger option.  I needed many states saved to specific statuses that would always be the same regardless of what device was tested.  I decided to keep using JSON for what it does well, sharing current states between processes, and added a SQLite database into RepairIQ for scan history.

## SQLite

DOTNET 10 does not have a built in SQLite implementation but fortunately does support Microsoft.Data.Sqlite NuGet package which works in both Windows and Linux.  This meant that large amounts of data can be saved to a single database file, built and managed locally by the app and prepares the data to be uploaded to an external source if such integration is needed in the future.

<img width="569" height="325" alt="image" src="https://github.com/user-attachments/assets/ad476c02-981a-48a1-992f-b6ba5dc2e381" />


This plan was adapted and changed multiple times since its creation but the core logic is the same.  First, the device table was removed from the database entirely and was instead managed by ARC using the session.json file.  Serial number was avoided as a primary or foreign key in an attempt to avoid natural keys that could repeat.

Information about the device hosted in the Device table is now all in the Scan table as info about the device being scanned.  Moving device info out of the SQLite database meant I could use the session file to give the user information about the device on the ARC launcher.  It still adds to the database via the scan table, but instead of going straight to the database, it lives on JSON and only grabs it when the scan starts.

I planned integrity in the code in the database, using the SQLite CHECK feature to limit the statuses of known values.  For instance, every status entry in all charts has limited known values that can be assigned to it.  In addition, I used STRICT data type enforcement and foreign key process using ScanID (for uniqueness) and serial number where possible.

## Reflection

The biggest thing I learned was how to think relational.  My database experience is mostly with MongoDB, and my first instinct was to store a scan as one document with its results embedded in an array.  My first relational schema showed that this instinct didn’t carry over.  I had a CPU table keyed by the CPU’s model name, with CPU’s pass/fail status in the same row.  When I walked through two laptops with the same processor, one failing and one passing, I saw that the second scan would overwrite the first, because a model name identifies a kind of part, not a specific part in a specific test.  That lead me to separate facts that never change from facts that change with every test.

Choosing keys was the next challenge.  I first tried to identify scan by the device’s serial number, then by its data and time.   Both fail in realistic situations, the same laptop for a second visit/rework or a dead CMOS battery resets the clock so two machines report the same timestamp.  I ended up with a surrogate key, which exists only to be unique, and I now understand why that is different from a natural key such as a serial number.  I also corrected a misunderstanding about foreign keys.  A foreign key is not just any important column, its a column whose value points to another tables primary key.

One of my favorite things I learned was on STRICT table use.  On the SQLite documentation I found a quirk exists where if you set a primary key as TEXT instead of INT, the program can and will set the primary key to NULL.  Obviously this is far from what you want from a key that’s whole purpose is to EXIST.  The funny part was, the error wasn’t caught until after the release, and by then it was too late as it had been implemented.  Fortunately, a later update added the STRICT argument that would ensure that the primary key was unique and could not be NULL.

[← Back to Home]({{ '/' | relative_url }})
