namespace RoboMigratorGUI
{
    internal class DirCompare
    {
        public void CompareDirectories(string sourceDirectory, string destinationDirectory, string logDirectory)
        {
            var dirSourceDirectories = Directory.GetDirectories(sourceDirectory);
            var dirSourceFiles = Directory.GetFiles(sourceDirectory);
            var dirSourceStructure = dirSourceDirectories.Concat(dirSourceFiles).ToList();
            List<string> sourceCleaned = new List<string>();

            foreach (var line in dirSourceStructure)
            {
                sourceCleaned.Add(line.Replace(sourceDirectory, "PATH"));
            }
            var dirDestinationDirectories = Directory.GetDirectories(destinationDirectory);
            var dirDestinationFiles = Directory.GetFiles(destinationDirectory);
            var dirDestinationStructure = dirDestinationDirectories.Concat(dirDestinationFiles).ToList();
            
            List<string> destCleaned = new List<string>();

            foreach (string line in dirDestinationStructure)
            {
                destCleaned.Add(line.Replace(destinationDirectory, "PATH"));

            }
            using (TextWriter tw = new StreamWriter(logDirectory + @"\differences.txt"))
            {
                tw.WriteLine("DIRECTORY DIFFERENCES");
                var firstNotSecond = sourceCleaned.Except(destCleaned).ToList();
                var secondNotFirst = destCleaned.Except(sourceCleaned).ToList();
                tw.WriteLine("------IN SOURCE BUT NOT DESTINATION------");
                foreach (var line in firstNotSecond)
                {
                    tw.WriteLine(line);
                }
                tw.WriteLine("------IN DESTINATION BUT NOT SOURCE------");
                foreach (var line in secondNotFirst)
                {
                    tw.WriteLine(line);
                }
            }
                }
            }

}

