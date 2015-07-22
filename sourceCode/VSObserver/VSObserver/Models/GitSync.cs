using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using System.IO;
using System.Configuration;
using System.Collections.ObjectModel;

namespace VSObserver.Models
{
    public class GitSync
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Error :" + e.ToString());
                _gitRepoIsOk = false;
            }
        }

        public void pushContent()
        {
            if (_gitRepoIsOk)
            {
                try
                {                    
                    RepositoryStatus repoStatus = _repository.RetrieveStatus();
                    string pathClonedRepo = "Cloned" + _repoPath;

                    if (Directory.Exists(pathClonedRepo))
                        DeleteDirectory(pathClonedRepo);

                    Repository clonedRepo = new Repository(Repository.Clone(_url, pathClonedRepo));

                    if (repoStatus.IsDirty)
                    {
                        _repository.Stage("*");
                        Commit commited = _repository.Commit("VSObserver coloring files modification !");
                    }

                    //Si les deux référence du répertoire cloné et du répertoir courant ne sont pas les mêmes
                    //cela signifie qu'il y a eu un changment sur le serveur donc on fait un pull.
                    if (!clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier.Equals(
                        _repository.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier))
                    {
                        pullContent();
                        _repository.Stage("*");
                        _repository.Commit("Conflict: The initial files on the server have been kept");
                    }

                    //Suprression du repository cloné
                    DeleteDirectory(pathClonedRepo);

                    PushOptions pushOptions = new PushOptions()
                                                {
                                                    OnPushStatusError = OnPushStatusError,
                                                    OnPushTransferProgress = (current, total, bytes) =>
                                                    {
                                                        Console.WriteLine(string.Format("Transfer Progress {0} out of {1}, Bytes {2}", current, total, bytes));
                                                        return true;
                                                    }
                                                };
                    pushOptions.CredentialsProvider = (url,user,cred) => new UsernamePasswordCredentials { Username="edwix", Password="" };

                    Remote remote = _network.Remotes["origin"];
                    var pushRefSpec = _repository.Branches["master"].CanonicalName;
                    _network.Push(remote, pushRefSpec, pushOptions);               
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error GITSYNC : \n" + e.ToString());
                }
            }            
        }

        public void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        private void OnPushStatusError(PushStatusError pushStatusErrors)
        {
            Console.WriteLine("Failed to update reference '{0}': {1}",
                pushStatusErrors.Reference, pushStatusErrors.Message);
        }

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
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error PULL : \n" + e.ToString());
                }
            }   
        }
    }
}
