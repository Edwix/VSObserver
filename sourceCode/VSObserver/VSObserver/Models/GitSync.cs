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

        private string _shaKey;
        private Repository _repository;
        private bool _gitRepoIsOk;
        private Network _network;

        public GitSync(string shaKey)
        {
            this._shaKey = shaKey;

            try
            {
                string repoPath = ConfigurationManager.AppSettings[CONFKEY_REPO_PATH];
                string url = ConfigurationManager.AppSettings[CONFKEY_URL_REPO];

                if (Directory.Exists(repoPath))
                {
                    _repository = new Repository(repoPath);
                }
                else
                {
                    Repository.Clone(url, repoPath);
                    _repository = new Repository(repoPath);
                }

                _network = _repository.Network;
                _gitRepoIsOk = true;
            }
            catch (Exception e)
            {
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

                    repoStatus.Where(x => (x.State == FileStatus.NewInIndex || x.State == FileStatus.ModifiedInIndex || x.State == FileStatus.Conflicted));

                    ObservableCollection<StatusEntry> addedFiles = new ObservableCollection<StatusEntry>(repoStatus.Added);

                    if (addedFiles.Count > 0)
                    {
                        _repository.Stage("*");
                    }

                    if (repoStatus.IsDirty)
                    {
                        _repository.Stage("*");
                        Commit commited = _repository.Commit("VSObserver coloring files modification !");

                        int NbErrors = 0;

                        PushOptions pushOptions = new PushOptions();
                        pushOptions.CredentialsProvider = (url,user,cred) => new UsernamePasswordCredentials { Username="edwix", Password="" };
                        pushOptions.OnPushStatusError = error =>
                                                        {
                                                            NbErrors++;
                                                        };

                        Remote remote = _network.Remotes["origin"];
                        var pushRefSpec = _repository.Branches["master"].CanonicalName;
                        _network.Push(remote, pushRefSpec, pushOptions);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error GITSYNC : \n" + e.ToString());
                }
            }            
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
                        _repository.Stage("*");
                        Commit commited = _repository.Commit("VSObserver coloring files modification !");

                        int NbErrors = 0;

                        PullOptions pullOptions = new PullOptions();
                        pullOptions.MergeOptions = merge => new MergeOptions() 
                                                            { 
                                                                FastForwardStrategy = FastForwardStrategy.Default, 
                                                                FileConflictStrategy = CheckoutFileConflictStrategy.Merge 
                                                            };

                        _network.Pull();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error GITSYNC : \n" + e.ToString());
                }
            }   
        }
    }
}
