using System.ComponentModel;

namespace DTMF.Models
{
    public class AppInfoExtended : AppInfo
    {
        [DisplayName("Latest")]
        public string LatestVersion { get; set; }

        [DisplayName("Destination")]
        public string DestinationVersion { get; set; }

        public string BackupVersion { get; set; }

        public bool IsValid { get; set; }
        public string InvalidMessage { get; set; }
        public string BuildOutputBasePathTemp
        {
            get
            {
                if (BuildOutputBasePath.EndsWith("\\"))
                    BuildOutputBasePath = (BuildOutputBasePath.Length > -1)
                        ? BuildOutputBasePath.Substring(0, BuildOutputBasePath.Length)
                        : BuildOutputBasePath;
                return BuildOutputBasePath + "_temp";
            }
        }

        public SyncInfo SyncInfo
        {
            get
            {
                var syncinfo = new SyncInfo();
                var versionsmatch = (LatestVersion == DestinationVersion);
                if (LatestVersion == null) versionsmatch = false;
                var candeploy = Utilities.CanDeploy;
                var pendingrequest = !string.IsNullOrWhiteSpace(PendingRequest);

                if (candeploy)
                {
                    syncinfo.Action = "Sync";
                    syncinfo.ClassName = "btn-primary";
                    syncinfo.Text = "Sync";
                    syncinfo.Title = "Sync " + AppName;

                    if (versionsmatch)
                    {
                        syncinfo.ClassName = "btn-default";
                        syncinfo.Text = "Sync";
                        syncinfo.Title = "Resync " + AppName;
                    }
                    if (pendingrequest)
                    {
                        syncinfo.ClassName = "btn-danger";
                        syncinfo.Text = "Sync";
                        syncinfo.Title = PendingRequest;
                    }
                    return syncinfo;
                }

                syncinfo.Action = "RequestSync";
                syncinfo.ClassName = "btn-primary";
                syncinfo.Text = "Request Sync";
                syncinfo.Title = "Request Sync";

                if (versionsmatch)
                {
                    syncinfo.ClassName = "btn-default";
                    syncinfo.Text = "Request Sync";
                    syncinfo.Title = "Request Resync";
                }
                if (pendingrequest)
                {
                    syncinfo.ClassName = "btn-default";
                    syncinfo.Text = "Resend Request";
                    syncinfo.Title = PendingRequest;
                }
                return syncinfo;
            }
        }
    }

}