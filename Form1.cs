namespace RoboMigratorGUI
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            BackupJob backupJob = new BackupJob(SourceTextBox.Text, DestinationTextBox.Text);
            backupJob.logPath = LogPathText.Text;
            backupJob.Start();
            
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            DirCompare compare = new DirCompare();
            compare.CompareDirectories(SourceTextBox.Text, DestinationTextBox.Text);
        }
    }
}