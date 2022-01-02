using RoboSharp;
using System.Diagnostics;

namespace RoboMigratorGUI
{
    class BackupJob
    {
        private string _sourceDirectory;
        private string _destinationDirectory;
        private int _maxJobs;
        private Dictionary<string, string> _jobs;
        private List<RoboCommand> _jobList;
        private List<RoboSharp.Results.RoboCopyResults> _jobResults;
        private Stopwatch copyTime = new Stopwatch();
        public string logPath;
        public string status;
        public int totalJobs = 0;
        public int completedJobs = 0;
        public int runningJobs = 0;
        public BackupJob(string sourceDirectory, string destinationDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _destinationDirectory = destinationDirectory;
            _maxJobs = 8;
            _jobs = new Dictionary<string, string>();
            _jobList = new List<RoboCommand>();
            _jobResults = new List<RoboSharp.Results.RoboCopyResults>();
            logPath = @"";
            status = "No Status";
            CreateJobs();
        }

        public void Start()
        {
            totalJobs = _jobList.Count;
            runningJobs = GetRunningJobs();
            copyTime.Start();
            if (_jobList.Count > 0)
            {
                while (_jobResults.Count < totalJobs)
                {
                    foreach (RoboCommand job in _jobList)
                    {
                        if (!job.IsRunning & !job.IsPaused)
                        {
                            status = "Migrating: " + job.CopyOptions.Source.ToString();
                            asyncRun(job);
                            runningJobs = GetRunningJobs();
                            while (runningJobs >= _maxJobs && runningJobs > 0)
                            {
                                Thread.Sleep(2000);
                                runningJobs = GetRunningJobs();
                                status = "Jobs Running: " + runningJobs.ToString();
                            }
                        }
                    }
                }

                copyTime.Stop();
                string message = "Migration completed in:\n" +
                                 "Hours: " + copyTime.Elapsed.Hours.ToString() + "\n" +
                                 "Seconds: " + copyTime.Elapsed.Seconds.ToString() + "\n" +
                                 "Milliseconds: " + copyTime.Elapsed.Milliseconds.ToString();

                string caption = "Migration Completed";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }


        }
        private static async Task asyncRun(RoboCommand copyJob)
        {
            await copyJob.Start();
        }
        private void CreateJobs()
        {
            foreach (var sourceSubDirectory in ProcessDirectory(_sourceDirectory))
            {
                if (sourceSubDirectory != null){
                    string[] dirpath = sourceSubDirectory.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                    string current_folder_name = dirpath.Last();
                    string destinationSubDirectory = _destinationDirectory + "\\" + current_folder_name;
                    _jobs.Add(sourceSubDirectory, destinationSubDirectory);
                }
            }

            //this creates a robocommand for the root directory files (excluding subdirs) using different parameters.
            //AddRootDirectoryJob(_sourceDirectory, _destinationDirectory);

            //This adds the rest of the sub directories as RoboCommand jobs.
            foreach (var job in _jobs)
            {
                AddJob(job.Key, job.Value);
            }
            string message = "TOTAL JOBS: "+ _jobList.Count + "\n";
            foreach (RoboCommand job in _jobList)
            {
                message = message + job.CopyOptions.Source.ToString() + "\n";
            }
            string caption = "DEBUG MESSAGE - JOBS IN _jobs";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        public int GetRunningJobs()
        {
            int runningJobs = 0;
            foreach (RoboCommand job in _jobList)
            {
                //if it is running, add it to the running total
                if (job.IsRunning | job.IsPaused)
                    {
                    runningJobs++;
                    }
            }
            return runningJobs;
        }

        public void AddJob(string sourceDirectory, string destinationDirectory)
        {
            RoboCommand backup = new RoboCommand();
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
        public void AddRootDirectoryJob(string sourceDirectory, string destinationDirectory)
        {
            RoboCommand backup = new RoboCommand();
            backup.OnCommandCompleted += OnBackupCommandCompletion;
            backup.OnFileProcessed += OnBackupFileProcessed;
            backup.OnCopyProgressChanged += Backup_OnCopyProgressChanged;

            // copy options
            backup.CopyOptions.Source = sourceDirectory;
            backup.CopyOptions.Destination = destinationDirectory;
            backup.CopyOptions.CopySubdirectories = false;
            backup.CopyOptions.CopySubdirectoriesIncludingEmpty = false;
            backup.CopyOptions.Depth = 1;
            backup.CopyOptions.UseUnbufferedIo = true;
            backup.CopyOptions.Mirror = true;
            backup.CopyOptions.EnableRestartMode = true;
            backup.CopyOptions.CopyFlags = "DAT";
            backup.CopyOptions.MultiThreadedCopiesCount = 16;

            //logging options
            backup.LoggingOptions.NoProgress = true;
            backup.LoggingOptions.NoDirectoryList = true;
            backup.LoggingOptions.NoFileList = true;
            backup.LoggingOptions.ReportExtraFiles = true;
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
                completedJobs++;
                var logFileName = ParseLogFileName(e.Results.LogLines);
                //Set our log file name to directory copied name + current year, month, day, hour, and minute
                string logFile = logPath + "\\" + logFileName + "_" + DateTime.Now.Year.ToString()
                + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".log";

                status = "Migrated: " + logFile;
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

        public void ShowJobResults()
        {
            foreach(RoboSharp.Results.RoboCopyResults result in _jobResults)
            {
                Console.WriteLine(result.ToString());
            }
        }

        public List<RoboSharp.Results.RoboCopyResults> GetJobResults()
        {
            List<string> jobResults = new List<string>();
            return _jobResults;
        }
        private static string[] ProcessDirectory(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
            {
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);

                return subdirectoryEntries;
            }
            else
            {
                Console.WriteLine("Directory is not reachable or does not exist: " + targetDirectory);
                return null;
            }
        }

        private static string ParseLogFileName(string[] jobResults)
        {
           string dir = jobResults[7];
           var dirname = dir.Replace(" ", "").Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList().Last();

           return dirname;
        }
    }
}