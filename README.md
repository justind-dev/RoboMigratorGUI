# RoboMigratorGUI
GUI for RoboMigrator which uses RoboSharp (A wrapper for RoboCopy) to mirror a destination with a provided source directory.

This application will parse the source directory creating a robocopy job for each folder in the source directory and then run those in parallel. 
Ideally, this is a directory which has many subdirectories in it with possibly many inside those as well. It will run at most 8 parallel robocopy jobs with 16 threads per job. 
You may get away with higher numbers depending on storage backing, but I just followed the advice in the PowerShell script that this is based off of. 

That Powershell Script can be found here: https://support.zadarastorage.com/hc/en-us/articles/213024806-How-to-Run-Robocopy-in-Parallel

Under the hood for the logic and Robocopy interfacing it uses the RoboSharp library, specifically the (currently) unmerged branch / fork located here: 
https://github.com/RFBomb/RoboSharp/tree/RoboCommandList

Which is a revision of the main RoboSharp library located at: https://github.com/tjscience/RoboSharp

## USAGE
 - Input the source directory from which you would like to copy into the text box labeled 'Source'. 

 - Input the destination directory to which you would like to copy into the text box labeled 'Destination'

 - Input the directory for which you would like to use to store the robocopy logs and the directory comparison output into the text box labeled 'Logs'.

 - Click migrate to begin the RoboMigration. (I use the word migration but it does not move anything from source, only copies)
 
### DISCLAIMER!

##### It is configured to mirror the directories. Anything at source and not at destination gets copied.

##### Anything at destination but not source gets removed.

#### YOU WILL LOSE DATA IF YOU IN CORRECTLY SUPPLY THE WRONG DIRECTORIES. I CANNOT BE HELD RESPONSIBLE FOR THIS SOFTWARE AND IT IS PROVIDED AS IS. Just saying.


## To-Do

- 
- Clean up the user interface / user experience
- More checks and error catching where I can (escaping inputs, verifying directories, preventing hitting migration twice, etc...)
- Some method of tracking progress without enabling the progress flag on the robocopy command itself (To increase performance) 
I am guessing it will be building a directory list, and then comparing that list to finished jobs. 
If nothing else, just something that says what it is currently doing so you dont have to watch the log directory to know.
- Remove any uncessary code and refactor what is currently there.
