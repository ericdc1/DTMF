using System;
using System.IO;
using System.Linq;
using System.Text;
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
            StringBuilder runlog,
            string gitUrl,
            string releaseBranchName, 
            string repositoryPath,
            string version)
        {
            if (string.IsNullOrWhiteSpace(GitUsername)) return;
            if (string.IsNullOrWhiteSpace(GitEmail)) return;
            if (string.IsNullOrWhiteSpace(GitPassword)) return;

            if (string.IsNullOrWhiteSpace(gitUrl)) return;
            if (string.IsNullOrWhiteSpace(releaseBranchName)) return;
            if (string.IsNullOrWhiteSpace(repositoryPath)) return;

            try
            {
                Utilities.AppendAndSend(runlog, "Run git merge", Utilities.WrapIn.H3);

                Utilities.AppendAndSend(runlog, "Cloning: " + gitUrl + " to: " + repositoryPath + "...", Utilities.WrapIn.Pre);
                CloneIfNeeded(gitUrl, repositoryPath);

                Utilities.AppendAndSend(runlog, "Checking out latest..." + repositoryPath, Utilities.WrapIn.Pre);
                CheckoutBranchIfNeeded(releaseBranchName, repositoryPath);

                Utilities.AppendAndSend(runlog, "Merging master changes to branch..." + repositoryPath, Utilities.WrapIn.Pre);
                PullChangesFromMaster(repositoryPath, version);

                Utilities.AppendAndSend(runlog, "Pushing master changes to branch..." + repositoryPath, Utilities.WrapIn.Pre);
                PushChanges(repositoryPath);

                Utilities.AppendAndSend(runlog, "Git merge done." + repositoryPath, Utilities.WrapIn.Pre);
            }
            catch (Exception e)
            {
                Utilities.AppendAndSend(runlog, "Exception!", Utilities.WrapIn.H3);
                Utilities.AppendAndSend(runlog, e.ToString(), Utilities.WrapIn.Pre);
            }
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

        private static void PullChangesFromMaster(string path, string version)
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

                var signature = new Signature(GitUsername, GitEmail, DateTimeOffset.Now);

                repo.Merge(repo.Branches["origin/master"],
                    signature,
                    new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.NoFastForward,
                        CommitOnSuccess = false
                    });

                if (repo.RetrieveStatus().IsDirty)
                {
                    repo.Commit("Deployed version: " + version, signature, signature);
                }
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