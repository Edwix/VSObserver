using System;
using System.IO;
using System.Security.Permissions;

namespace VSObserver
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
        }

        public void Run()
        {
            if(File.Exists(path))
            {
                //On regarde uniquement sur les fichier XML
                FileSystemWatcher watcher = new FileSystemWatcher();

                watcher.Path = Path.GetDirectoryName(Path.GetFullPath(path));

                //ICi on vérifier le fichier en fonction de son nom
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
