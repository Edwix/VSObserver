using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using System.IO;
using System.Configuration;

namespace VSObserver.Models
{
    public class GitSync
    {
        private const string CONFKEY_REPO_PATH = "RepositoryPath";
        private const string CONFKEY_URL_REPO = "URLRepository";

        private string _shaKey;
        private Repository _repository;
        private bool _gitRepoIsOk;

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

                _gitRepoIsOk = true;
            }
            catch (Exception e)
            {
                _gitRepoIsOk = false;
            }
        }

        public void pushContent()
        {
            GitObject obj = _repository.Lookup(_shaKey);
            Network net = _repository.Network;
            _repository.Commit("VSObserver coloring files modification !");
            net.Push(_repository.Branches["origin/master"]);
            
        }

        public void pullContent()
        {

        }
    }
}
