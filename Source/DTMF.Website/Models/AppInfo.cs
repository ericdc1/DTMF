using System.Collections.Generic;
using System.ComponentModel;

namespace DTMF.Models
{
    public class AppInfo
    {
        public AppInfo()
        {
            BuildOutputDatabases = new List<DatabaseInfo>();
            DestinationPaths = new List<string>();
        }

        [DisplayName("Application Name")]
        public string AppName { get; set; }
        public string BuildOutputBasePath { get; set; }
        public string BuildOutputRelativeWebPath { get; set; }
        public List<DatabaseInfo> BuildOutputDatabases { get; set; }
        public List<string> DestinationPaths { get; set; }
        public string Powershell { get; set; }

        private string _lastDeployed;
        [DisplayName("Deployed")]
        public string LastDeployed
        {
            get
            {
                if (string.IsNullOrEmpty(_lastDeployed)) return "Never";
                return _lastDeployed;
            }
            set { _lastDeployed = value; }
        }

        public string PendingRequest { get; set; }
        public string RobocopyExcludedFiles { get; set; }
        public string RobocopyExcludedFolders { get; set; }

        [DisplayName("Fast App Offline")]
        public bool FastAppOffline { get; set; }

        [DisplayName("TeamCity Build Configuration ID")]
        public string BuildConfigurationID { get; set; }

        public string HipChatRoomID { get; set; }
        public string SlackRoomID { get; set; }

        public string GitUrl { get; set; }
        public string ReleaseBranchName { get; set; }
        public string RepositoryPathName { get; set; }

    }
}