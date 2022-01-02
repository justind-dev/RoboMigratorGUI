using System.Diagnostics;

namespace RoboMigratorGUI
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void DisplayCopyInformation(Stopwatch copyTimer)
        {
            var message = "Migration completed in:\n" +
                          "Hours: " + copyTimer.Elapsed.Hours + "\n" +
                          "Seconds: " + copyTimer.Elapsed.Seconds + "\n" +
                          "Milliseconds: " + copyTimer.Elapsed.Milliseconds;

            var caption = "Migration Completed";
            var buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            var backupJob = new BackupJob(SourceTextBox.Text, DestinationTextBox.Text, LogPathText.Text);
            backupJob.Start();
            statusLabel.Text = backupJob.Status;
            DisplayCopyInformation(backupJob.CopyTime);
            

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var compare = new DirCompare();
            compare.CompareDirectories(SourceTextBox.Text, DestinationTextBox.Text);
        }
    }
}