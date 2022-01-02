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
    private readonly int _maxJobs;
        
    private Dictionary<string, string> _jobs;
    private List<RoboCommand> _jobList;
    private List<RoboSharp.Results.RoboCopyResults> _jobResults;
    private int _totalJobs;
    private int _completedJobs;
    private int _runningJobs;

    public BackupJob(string sourceDirectory, string destinationDirectory, string logDirectory, int maxJobs = 8)
    {
        _sourceDirectory = sourceDirectory;
        _destinationDirectory = destinationDirectory;
        _logDirectory = logDirectory;
        _maxJobs = maxJobs;
            
        CreateJobs();
    }

    public void Start()
    {
        Status = new string("");
        CopyTime = new Stopwatch();
        _jobResults = new List<RoboSharp.Results.RoboCopyResults>();
        _totalJobs = _jobList.Count;
        _runningJobs = GetRunningJobs();
        
        CopyTime.Start();
        if (_jobList.Count <= 0) return;
        while (_jobResults.Count < _totalJobs)
        {
            foreach (var job in _jobList.Where(job => !job.IsRunning & !job.IsPaused))
            {
                Status = "Migrating: " + job.CopyOptions.Source;
                RunJobAsync(job);
                _runningJobs = GetRunningJobs();
                while (_runningJobs >= _maxJobs && _runningJobs > 0)
                {
                    Thread.Sleep(2000);
                    _runningJobs = GetRunningJobs();
                    Status = "Jobs Running: " + _runningJobs;
                }
            }
        }
        CopyTime.Stop();
    }
        
    private async Task RunJobAsync(IRoboCommand roboCommand)
    {
        await roboCommand.Start();
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
            AddJob(sourceSubDirectory, destinationSubDirectory);
        }
    }

    private int GetRunningJobs()
    {
        return _jobList.Count(job => job.IsRunning | job.IsPaused);
    }

    private void AddJob(string sourceDirectory, string destinationDirectory)
    {
        var backup = new RoboCommand();
        backup.OnCommandCompleted += Backup_OnBackupCommandCompletion;

        // copy options
        backup.CopyOptions.Source = sourceDirectory;
        backup.CopyOptions.Destination = destinationDirectory;
        backup.CopyOptions.CopySubdirectories = true;
        backup.CopyOptions.UseUnbufferedIo = false;
        backup.CopyOptions.Mirror = true;
        backup.CopyOptions.EnableRestartMode = true;
        backup.CopyOptions.MultiThreadedCopiesCount = 16;
        backup.CopyOptions.CopyFlags = "DAT";
            
        //logging options
        backup.LoggingOptions.NoProgress = true;
        backup.LoggingOptions.NoDirectoryList = true;
        backup.LoggingOptions.NoFileList = true;
        backup.LoggingOptions.NoFileClasses = true;

        // select options
        backup.SelectionOptions.OnlyCopyArchiveFilesAndResetArchiveFlag = false;

        // retry options
        backup.RetryOptions.RetryCount = 1;
        backup.RetryOptions.RetryWaitTime = 2;
        _jobList.Add(backup);
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

        Console.WriteLine("Directory is not reachable or does not exist: " + targetDirectory);
        return null;
    }

    private static string ParseLogFileName(string[] jobResults)
    {
        var dir = jobResults[7];
        var dirname = dir.Replace(" ", "").Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList().Last();

        return dirname;
    }
}