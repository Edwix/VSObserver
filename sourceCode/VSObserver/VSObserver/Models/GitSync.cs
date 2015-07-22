using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using System.IO;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Threading;

namespace VSObserver.Models
{
    public class GitSync : IFileChange
    {
        private const string CONFKEY_REPO_PATH = "RepositoryPath";
        private const string CONFKEY_URL_REPO = "URLRepository";
        private const string CONFKEY_LOGIN = "GitHubLogin";
        private const string CONFKEY_PASSWORD = "GitHubPassword";
        private const string CONFKEY_NAME = "Name";
        private const string CONFKEY_EMAIL = "Email";

        private Repository _repository;
        private bool _gitRepoIsOk;
        private Network _network;
        private Signature _signature;
        private string _url;
        private string _repoPath;
        private string _login;
        private string _password;
        private string _name;
        private string _email;

        //File watcher to see if there are some modification on git repository
        private FileWatcher _fileWatcher;
        
        //Timer qui va réaliser le pull continuellement
        private Timer _pullTimer;

        public GitSync()
        {
            try
            {
                _url = ConfigurationManager.AppSettings[CONFKEY_URL_REPO];
                _login = ConfigurationManager.AppSettings[CONFKEY_LOGIN];
                _password = ConfigurationManager.AppSettings[CONFKEY_PASSWORD];
                _name = ConfigurationManager.AppSettings[CONFKEY_NAME];
                _email = ConfigurationManager.AppSettings[CONFKEY_EMAIL];

                _repoPath = _url.Replace("https://gist.github.com/", "").Replace(".git", "");

                _signature = new Signature(_name, _email, DateTimeOffset.Now);

                //On supprime le dossier existant et on clone
                //Cela permet de modifier de repository si jamais quelqu'un à modifier la clé git
                if (!Directory.Exists(_repoPath))
                {
                    Repository.Clone(_url, _repoPath);
                }

                _repository = new Repository(_repoPath);

                _network = _repository.Network;
                _gitRepoIsOk = true;

                _fileWatcher = new FileWatcher(_repoPath);
                _fileWatcher.setFileChangeListener(this);

                TimerCallback tcb = pullTimerElapsed;
                _pullTimer = new Timer(tcb);
                _pullTimer.Change(0, 1000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error :" + e.ToString());
                _gitRepoIsOk = false;
            }
        }

        public string getRepositoryPath()
        {
            return _repoPath;
        }

        /// <summary>
        /// Utilise la commande Git push
        /// Avant de faire le push cette méthode réalise un pull et un commit
        /// </summary>
        public void pushContent()
        {
            if (_gitRepoIsOk)
            {
                try
                {                    
                    RepositoryStatus repoStatus = _repository.RetrieveStatus();

                    pullContent();

                    if (repoStatus.IsDirty)
                    {
                        _repository.Stage("*");
                        Commit commited = _repository.Commit("VSObserver coloring files modification !");
                    }

                    PushOptions pushOptions = new PushOptions()
                                                {
                                                    OnPushStatusError = OnPushStatusError,
                                                    OnPushTransferProgress = (current, total, bytes) =>
                                                    {
                                                        Console.WriteLine(string.Format("Transfer Progress {0} out of {1}, Bytes {2}", current, total, bytes));
                                                        return true;
                                                    }
                                                };
                    pushOptions.CredentialsProvider = (url,user,cred) => new UsernamePasswordCredentials { Username=_login, Password=_password };

                    Remote remote = _network.Remotes["origin"];
                    var pushRefSpec = _repository.Branches["master"].CanonicalName;
                    _network.Push(remote, pushRefSpec, pushOptions);

                    Console.WriteLine("PUSH OK !");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error GITSYNC : \n" + e.ToString());
                }
            }            
        }

        /// <summary>
        /// Méthode appellée à chaque période du timer
        /// </summary>
        /// <param name="stateInfo"></param>
        private void pullTimerElapsed(Object stateInfo)
        {
            pullContent();
        }

        private void OnPushStatusError(PushStatusError pushStatusErrors)
        {
            Console.WriteLine("Failed to update reference '{0}': {1}",
                pushStatusErrors.Reference, pushStatusErrors.Message);
        }

        /// <summary>
        /// Méthode qui réalise la commande pull de Git
        /// Cette méthode prend en priorité le fichier sur le serveur
        /// en cas de conflit
        /// </summary>
        public void pullContent()
        {
            if (_gitRepoIsOk)
            {
                try
                {
                    RepositoryStatus repoStatus = _repository.RetrieveStatus();

                    if (!repoStatus.IsDirty)
                    {
                        PullOptions pullOptions = new PullOptions()
                        {
                            MergeOptions = new MergeOptions() 
                                            { 
                                                FastForwardStrategy = FastForwardStrategy.Default, 
                                                FileConflictStrategy = CheckoutFileConflictStrategy.Theirs
                                            }
                        };

                        _network.Pull(_signature, pullOptions);

                        Console.WriteLine("PULL OK !");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error PULL : \n" + e.ToString());
                }
            }   
        }

        /// <summary>
        /// When we load a XML rule we push because the file has been changed
        /// </summary>
        /// <param name="path"></param>
        public void loadXMLRule(string path)
        {
            pushContent();
        }
    }
}
