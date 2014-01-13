using System.Management.Automation;
using System.Text;

namespace DTMF.Logic
{
    public class SyncLogic
    {
 
        public string ExecuteCode(string code)
        {
            // We use a string builder ton create our result text
            var builder = new StringBuilder();

            // Initialize PowerShell engine
            var shell = PowerShell.Create();

            // Add the script to the PowerShell object
            shell.Commands.AddScript(code);

            // Execute the script
            var results = shell.Invoke();

            // display results, with BaseObject converted to string
            // Note : use |out-string for console-like output
            if (results.Count > 0)
            {
                foreach (var psObject in results)
                {
                    // Convert the Base Object to a string and append it to the string builder.
                    // Add \r\n for line breaks
                    builder.Append(psObject.BaseObject + "\r\n");
                }
            }

            if (shell.HadErrors)
            {
                foreach (var err in shell.Streams.Error)
                {
                    builder.Append(err + "\r\n");
                }
            }

            if (builder.ToString().Length == 0)
                builder.AppendLine("Ok");
            return builder.ToString();
        }
          
    }
}