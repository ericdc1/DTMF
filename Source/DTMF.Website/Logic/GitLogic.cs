using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace DTMF.Logic
{
    public class GitLogic
    {
        private static readonly string GitUsername = System.Configuration.ConfigurationManager.AppSettings["GitUsername"];
        private static readonly string GitEmail = System.Configuration.ConfigurationManager.AppSettings["GitEmail"];
        private static readonly string GitPassword = System.Configuration.ConfigurationManager.AppSettings["GitPassword"];

        private static UsernamePasswordCredentials Credentials => new UsernamePasswordCredentials
        {
            Username = GitUsername,
            Password = GitPassword
        };

        public static void PushToReleaseBranchIfNeeded(
            string gitUrl,
            string releaseBranchName, 
            string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(GitUsername)) return;
            if (string.IsNullOrWhiteSpace(GitEmail)) return;
            if (string.IsNullOrWhiteSpace(GitPassword)) return;

            if (string.IsNullOrWhiteSpace(gitUrl)) return;
            if (string.IsNullOrWhiteSpace(releaseBranchName)) return;
            if (string.IsNullOrWhiteSpace(repositoryPath)) return;

            CloneIfNeeded(gitUrl, repositoryPath);
            CheckoutBranchIfNeeded(releaseBranchName, repositoryPath);
            PullChangesFromMaster(repositoryPath);
            PushChanges(repositoryPath);
        }

        private static void CloneIfNeeded(string cloneUrl, string path)
        {
            if (Directory.Exists(path)) return;

            var cloneOptions = new CloneOptions
            {
                CredentialsProvider =
                    (url, user, cred) => Credentials,
                CertificateCheck = (certificate, valid, host) => true
            };

            Repository.Clone(cloneUrl, path, cloneOptions);
        }

        private static void CheckoutBranchIfNeeded(string branchName, string path)
        {
            using (var repo = new Repository(path))
            {
                var branch = repo.Branches[branchName];
                if (branch == null) throw new Exception("Cannot find branch");

                if (repo.Head.FriendlyName == "local-" + branchName)
                {
                    return;
                }

                var mostRecentBranchCommit = branch.Commits.First();
                var localReleaseBranch = repo.CreateBranch("local-" + branchName, mostRecentBranchCommit);
                repo.Branches.Update(localReleaseBranch, updater =>
                {
                    updater.Remote = branch.RemoteName;
                    updater.UpstreamBranch = branch.UpstreamBranchCanonicalName;
                });
                Commands.Checkout(repo, localReleaseBranch);
            }
        }

        private static void PullChangesFromMaster(string path)
        {
            var fetchOptions = new FetchOptions
            {
                CredentialsProvider = (url, usernameFromUrl, types) => Credentials,
                CertificateCheck = (certificate, valid, host) => true
            };

            using (var repo = new Repository(path))
            {
                foreach (var remote in repo.Network.Remotes)
                {
                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, "");
                }

                repo.Merge(repo.Branches["origin/master"],
                    new Signature(GitUsername, GitEmail, DateTimeOffset.Now),
                    new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
            }
        }

        private static void PushChanges(string path)
        {
            var pushOptions = new PushOptions
            {
                CredentialsProvider = (url, usernameFromUrl, types) => Credentials,
                CertificateCheck = (certificate, valid, host) => true
            };

            using (var repo = new Repository(path))
            {
                repo.Network.Push(repo.Head, pushOptions);
            }
        }
    }
}