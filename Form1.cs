using System.Diagnostics;
using RoboSharp;

namespace RoboMigratorGUI;

public partial class Form1 : Form
{
    //Stores the Job Results in a results list object.
    private RoboSharp.Results.RoboCopyResultsList JobResults = new RoboSharp.Results.RoboCopyResultsList();
    //Initialize the RoboQueue object which will handle the multijob logic.
    RoboSharp.RoboQueue roboQueue = new RoboSharp.RoboQueue();
    //Creating an overall copy time log. This may be already implemented in RoboQueue Results and will explore that.
    Stopwatch copyTime = new Stopwatch();


    public Form1()
    {
        InitializeComponent();
    }

    //Parses log file names so that log file name matches the directory that was copied.
    private static string ParseLogFileName(string[] jobResults)
    {
        var dir = jobResults[7];
        var dirname = dir.Replace(" ", "").Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList().Last();

        return dirname;
    }

    //called from the generated RoboCommand below upon Command completion.
    void Backup_OnBackupCommandCompletion(object sender, RoboCommandCompletedEventArgs e)
    {


        var results = e.Results;
        Console.WriteLine("Files copied: " + results.FilesStatistic.Copied);
        Console.WriteLine("Directories copied: " + results.DirectoriesStatistic.Copied);
        JobResults.Add(e.Results);
        //write the log file
        var logFileName = ParseLogFileName(e.Results.LogLines);

        //Set our log file name to directory copied name + current year, month, day, hour, and minute
        var logFile = LogPathText.Text + "\\" + logFileName + "_" + DateTime.Now.Year.ToString()
                      + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                      + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".log";

        using TextWriter tw = new StreamWriter(logFile);
        foreach (var line in e.Results.LogLines)
        {
            tw.WriteLine(line);
        }
    }

    //This generates a robocommand backup job with desired config and returns it.
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
        if (jobSourceDirectory == SourceTextBox.Text && jobDestinationDirectory == DestinationTextBox.Text)
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

    //This takes the source directory, and splits it into RoboCommand copy jobs for each underlying directory.
    private void CreateJobs()
    {
        var _jobs = new Dictionary<string, string>();
        List<RoboCommand> _jobList = new List<RoboCommand>();
        RoboSharp.RoboQueue roboQueue = new RoboSharp.RoboQueue();
        foreach (var sourceSubDirectory in ProcessDirectory(SourceTextBox.Text))

        {
            if (sourceSubDirectory == null) continue;
            var directoryPaths = sourceSubDirectory.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var currentFolderName = directoryPaths.Last();
            var destinationSubDirectory = DestinationTextBox.Text + @"\" + currentFolderName;
            if (!roboQueue.IsRunning)
            {
                roboQueue.AddCommand(GetCommand(false, sourceSubDirectory, destinationSubDirectory));
            }

        }
        // add job for root directory files
        if (!roboQueue.IsRunning)
        {
            roboQueue.AddCommand(GetCommand(false, SourceTextBox.Text, DestinationTextBox.Text));
        }
    }

    //Processes a given directory, returning sub-directory names
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
    
    //Really only for debugging copy times.
    private void DisplayCopyInformation(Stopwatch copyTimer)
    {
        var message = "Migration completed in:\n" +
                      "Hours: " + copyTimer.Elapsed.Hours + "\n" +
                      "Seconds: " + copyTimer.Elapsed.Seconds + "\n" +
                      "Milliseconds: " + copyTimer.Elapsed.Milliseconds + "\n" +
                      "Time Completed: " + DateTime.Now.ToString();

        var caption = "Migration Completed";
        var buttons = MessageBoxButtons.OK;
        MessageBox.Show(message, caption, buttons);
    }

    private async void StartMigration()
    {
        copyTime.Start();
        await roboQueue.StartAll();
        copyTime.Stop();
    }

    //this Button needs renamed to button_StartMigration
    private void button1_Click(object sender, EventArgs e)
    {
        roboQueue.MaxConcurrentJobs = 8;
        if (!roboQueue.IsRunning)
        {
            if (SourceTextBox.Text != "" | DestinationTextBox.Text != "")
            {
                CreateJobs();
                StartMigration();
            }
            else
            {
                MessageBox.Show("Please enter a value for both source and destination.");
            }
        }
        else
        {
            MessageBox.Show("There is already a migration in progress,\nplease wait until that completes.");
        }


    }

    //Place holder for once we work out how to display status / currently running jobs / or copy results.
    private void label3_Click(object sender, EventArgs e)
    {

    }
    //This needs renamed to buttom_CompareDirectories
    private void button2_Click(object sender, EventArgs e)
    {
        var compare = new DirCompare();
        compare.CompareDirectories(SourceTextBox.Text, DestinationTextBox.Text);
    }


    private void Form1_Load(object sender, EventArgs e)
    {

    }


}
