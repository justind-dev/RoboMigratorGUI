using RoboSharp;
using System.Diagnostics;
using RoboSharp.Interfaces;

namespace RoboMigratorGUI;

public class BackupJob
{
    public Stopwatch CopyTime { get; private set; }
    public string Status;

    private readonly string _sourceDirectory;
    private readonly string _destinationDirectory;
    private readonly string _logDirectory;
        
    private Dictionary<string, string> _jobs;
    private List<RoboCommand> _jobList;
    private List<RoboSharp.Results.RoboCopyResults> _jobResults;
    private int _totalJobs;
    private int _completedJobs;
    private int _runningJobs;
    private RoboSharp.RoboQueue roboQueue = new RoboSharp.RoboQueue();

    public BackupJob(string sourceDirectory, string destinationDirectory, string logDirectory, int maxJobs = 8)
    {
        _sourceDirectory = sourceDirectory;
        _destinationDirectory = destinationDirectory;
        _logDirectory = logDirectory;
        roboQueue.MaxConcurrentJobs = maxJobs;
            
        CreateJobs();
    }

    public async void Start()
    {

        CopyTime = new Stopwatch();
        CopyTime.Start();
        await roboQueue.StartAll();        
        CopyTime.Stop();
    }
        
    private void CreateJobs()
    {
        _jobs = new Dictionary<string, string>();
        _jobList = new List<RoboCommand>();
        
        foreach (var sourceSubDirectory in ProcessDirectory(_sourceDirectory))
        {
            if (sourceSubDirectory == null) continue;
            var directoryPaths = sourceSubDirectory.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var currentFolderName = directoryPaths.Last();
            var destinationSubDirectory = _destinationDirectory + @"\" + currentFolderName;
            if (!roboQueue.IsRunning)
            {
                roboQueue.AddCommand(GetCommand(false, sourceSubDirectory, destinationSubDirectory));
            }
            
        }
        // add job for root directory files
        if (!roboQueue.IsRunning)
        {
            roboQueue.AddCommand(GetCommand(false, _sourceDirectory, _destinationDirectory));
        }
    }

    private int GetRunningJobs()
    {
        return _jobList.Count(job => job.IsRunning | job.IsPaused);
    }

    private int GetCompletedJobs()
    {
        return _jobResults.Count(result => result.Status.ExitCode.ToString() == "FilesCopiedSuccessfully" |
                                           result.Status.ExitCode.ToString() == "NoErrorNoCopy");
    }
    private RoboCommand GetCommand(bool BindEvents, string jobSourceDirectory, string jobDestinationDirectory)
    {
        RoboCommand backup = new RoboCommand();
        if (BindEvents)
        {
            backup.OnCommandCompleted += Backup_OnBackupCommandCompletion;
        }

        // copy options
        backup.CopyOptions.Source = jobSourceDirectory;
        backup.CopyOptions.Destination = jobDestinationDirectory;
        backup.CopyOptions.Purge = true;
        //set different options for source directory so that it doesnt try to copy sub directories and only copies files in root of source.
        if (jobSourceDirectory == _sourceDirectory && jobDestinationDirectory == _destinationDirectory)
        {
            backup.CopyOptions.CopySubdirectoriesIncludingEmpty = false;
            backup.CopyOptions.CopySubdirectories = false;
            backup.CopyOptions.Depth = 1;
        }
        else
        {
            backup.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            backup.CopyOptions.CopySubdirectories = true;
            backup.CopyOptions.Depth = 0;
        }
        backup.CopyOptions.CopyFlags = "DAT";
        backup.CopyOptions.DirectoryCopyFlags = "DAT";
        backup.CopyOptions.EnableRestartMode = true;
        backup.CopyOptions.MultiThreadedCopiesCount = 16;

        
        //logging options
        backup.LoggingOptions.NoProgress = true;
        backup.LoggingOptions.NoDirectoryList = true;
        backup.LoggingOptions.NoFileList = true;
        backup.LoggingOptions.NoFileClasses = true;

        // retry options
        backup.RetryOptions.RetryCount = 1;
        backup.RetryOptions.RetryWaitTime = 2;

        //add job
        return backup;
    }

    private void Backup_OnBackupCommandCompletion(object sender, RoboCommandCompletedEventArgs e)
    {
        _jobResults.Add(e.Results);
        _completedJobs++;
            
        var logFileName = ParseLogFileName(e.Results.LogLines);
            
        //Set our log file name to directory copied name + current year, month, day, hour, and minute
        var logFile = _logDirectory + "\\" + logFileName + "_" + DateTime.Now.Year.ToString()
                      + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                      + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".log";

        Status = "Migrated: " + logFile;
            
        using TextWriter tw = new StreamWriter(logFile);
        foreach (var line in e.Results.LogLines)
        {
            tw.WriteLine(line);
        }
    }

    private static string[] ProcessDirectory(string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            var subdirectoryEntries = Directory.GetDirectories(targetDirectory);

            return subdirectoryEntries;
        }
        else
        {
            return null;
        }
    }

    private static string ParseLogFileName(string[] jobResults)
    {
        var dir = jobResults[7];
        var dirname = dir.Replace(" ", "").Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList().Last();

        return dirname;
    }
}