using RoboSharp;
using System.Diagnostics;

namespace RoboMigratorGUI
{
    public class BackupJob
    {
        private string _sourceDirectory;
        private string _destinationDirectory;
        private int _maxJobs;
        private Dictionary<string, string> _jobs;
        private List<RoboCommand> _jobList;
        private List<RoboSharp.Results.RoboCopyResults> _jobResults;
        private Stopwatch copyTime = new Stopwatch();
        private string _logDirectory;
        private string _status;
        private int _totalJobs = 0;
        private int _completedJobs = 0;
        private int _runningJobs = 0;
        public BackupJob(string sourceDirectory, string destinationDirectory, string logDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _destinationDirectory = destinationDirectory;
            _logDirectory = logDirectory;
            _maxJobs = 8;
            _jobs = new Dictionary<string, string>();
            _jobList = new List<RoboCommand>();
            _jobResults = new List<RoboSharp.Results.RoboCopyResults>();
            _status = "No Status";
            CreateJobs();
        }

        public void Start()
        {
            _totalJobs = _jobList.Count;
            _runningJobs = GetRunningJobs();
            copyTime.Start();
            if (_jobList.Count <= 0) return;
            
            while (_jobResults.Count < _totalJobs)
            {
                foreach (var job in _jobList.Where(job => !job.IsRunning & !job.IsPaused))
                {
                    _status = "Migrating: " + job.CopyOptions.Source;
                    asyncRun(job);
                    _runningJobs = GetRunningJobs();
                    while (_runningJobs >= _maxJobs && _runningJobs > 0)
                    {
                        Thread.Sleep(2000);
                        _runningJobs = GetRunningJobs();
                        _status = "Jobs Running: " + _runningJobs;
                    }
                }
            }

            copyTime.Stop();
            var message = "Migration completed in:\n" +
                          "Hours: " + copyTime.Elapsed.Hours + "\n" +
                          "Seconds: " + copyTime.Elapsed.Seconds + "\n" +
                          "Milliseconds: " + copyTime.Elapsed.Milliseconds;

            var caption = "Migration Completed";
            var buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);


        }
        private static async Task asyncRun(RoboCommand copyJob)
        {
            await copyJob.Start();
        }
        private void CreateJobs()
        {
            foreach (var sourceSubDirectory in ProcessDirectory(_sourceDirectory))
            {
                if (sourceSubDirectory == null) continue;
                string[] dirpath = sourceSubDirectory.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                string current_folder_name = dirpath.Last();
                string destinationSubDirectory = _destinationDirectory + "\\" + current_folder_name;
                _jobs.Add(sourceSubDirectory, destinationSubDirectory);
            }

            //this creates a robocommand for the root directory files (excluding subdirs) using different parameters.
            //AddRootDirectoryJob(_sourceDirectory, _destinationDirectory);

            //This adds the rest of the sub directories as RoboCommand jobs.
            foreach (var job in _jobs)
            {
                AddJob(job.Key, job.Value);
            }
            var message = "TOTAL JOBS: "+ _jobList.Count + "\n";
            foreach (RoboCommand job in _jobList)
            {
                message = message + job.CopyOptions.Source + "\n";
            }
            var caption = "DEBUG MESSAGE - JOBS IN _jobs";
            var buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        private int GetRunningJobs()
        {
            return _jobList.Count(job => job.IsRunning | job.IsPaused);
        }

        private void AddJob(string sourceDirectory, string destinationDirectory)
        {
            var backup = new RoboCommand();
            backup.OnCommandCompleted += OnBackupCommandCompletion;
            backup.OnFileProcessed += OnBackupFileProcessed;
            backup.OnCopyProgressChanged += Backup_OnCopyProgressChanged;

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

        private void Backup_OnCopyProgressChanged(object sender, CopyProgressEventArgs e)
        {
            //Do we need to do something here?
            var CurrentFileProgress = e.CurrentFileProgress;
            
        }

        private void OnBackupFileProcessed(object sender, FileProcessedEventArgs e)
        {
            //Do we need to do something here?

        }

        private void OnBackupCommandCompletion(object sender, RoboCommandCompletedEventArgs e)
        {
                RoboSharp.Results.RoboCopyResults results = e.Results;
                _jobResults.Add(results);
                _completedJobs++;
                var logFileName = ParseLogFileName(e.Results.LogLines);
                //Set our log file name to directory copied name + current year, month, day, hour, and minute
                string logFile = _logDirectory + "\\" + logFileName + "_" + DateTime.Now.Year.ToString()
                + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".log";

                _status = "Migrated: " + logFile;
                using (TextWriter tw = new StreamWriter(logFile))
                {
                    tw.WriteLine("------OUTPUT OF e.Results.LogLines------");
                    foreach (var line in e.Results.LogLines)
                    {
                        tw.WriteLine(line);
                    }
                    tw.WriteLine("------------------------------------------------------------------------------");
                    tw.WriteLine("------COPY TIME------");
                    tw.WriteLine("Hours: ");
                    tw.WriteLine(copyTime.Elapsed.Hours.ToString());
                    tw.WriteLine("Minutes: ");
                    tw.WriteLine(copyTime.Elapsed.Minutes.ToString());
                    tw.WriteLine("Seconds: ");
                    tw.WriteLine(copyTime.Elapsed.Seconds.ToString());
                    tw.WriteLine("Millieconds: ");
                    tw.WriteLine(copyTime.Elapsed.Milliseconds.ToString());
                
                }
        }

        private static string[] ProcessDirectory(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
            {
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);

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
}