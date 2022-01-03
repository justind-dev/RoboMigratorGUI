using System.Diagnostics;
using RoboSharp;

namespace RoboMigratorGUI;

public partial class Form1 : Form
{

    private RoboSharp.Results.RoboCopyResultsList JobResults = new RoboSharp.Results.RoboCopyResultsList();
    RoboSharp.RoboQueue roboQueue = new RoboSharp.RoboQueue();
    Stopwatch copyTime = new Stopwatch();


    public Form1()
    {
        InitializeComponent();
    }
    void Backup_OnBackupCommandCompletion(object sender, RoboCommandCompletedEventArgs e)
    {


        var results = e.Results;
        Console.WriteLine("Files copied: " + results.FilesStatistic.Copied);
        Console.WriteLine("Directories copied: " + results.DirectoriesStatistic.Copied);
        JobResults.Add(e.Results);
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

    private void label3_Click(object sender, EventArgs e)
    {

    }

    private void button2_Click(object sender, EventArgs e)
    {
        var compare = new DirCompare();
        compare.CompareDirectories(SourceTextBox.Text, DestinationTextBox.Text);
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }


}
