using System;
using System.IO;
using System.Security.Permissions;

namespace VSObserver.Models
{
    class FileWatcher
    {
        private IFileChange fileChange;
        private string path;

        public FileWatcher(string path)
        {
            this.path = path;
            Run();
        }

        public void setFileChangeListener(IFileChange fileChange)
        {
            this.fileChange = fileChange;

            if (fileChange != null)
            {
                fileChange.loadXMLRule(Path.GetFullPath(path));
            }
        }

        public void Run()
        {
            bool fileExist = File.Exists(path);

            if (Directory.Exists(path) || fileExist)
            {
                string completePath;

                if (fileExist)
                {
                    completePath = Path.GetDirectoryName(path);
                }
                else
                {
                    completePath = path;
                }

                //On regarde uniquement sur les fichier XML
                FileSystemWatcher watcher = new FileSystemWatcher();

                watcher.Path = Path.GetFullPath(completePath);

                //ICi on vérifie le fichier en fonction de son nom
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                watcher.Filter = "*.xml";

                //Puis à chaque changmement du fichier on déclenche l'évènement
                watcher.Changed += new FileSystemEventHandler(OnChanged);

                //Démmarage du watcher (°o°)
                watcher.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("=== WATCHER === File: " + e.Name + " " + e.ChangeType);

            if (fileChange != null)
            {
                fileChange.loadXMLRule(e.FullPath);
            }
        }
    }
}
