using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices;
using System.Web;
using System.Web.Configuration;

namespace DTMF.Models.Authentication
{
    public class LoginModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DisplayName("Remember Me")]
        public bool RememberMe { get; set; } = true;

        public bool HasValidUsernameAndPassword => ValidateLogin(Username, Password);

        private static bool ValidateLogin(string userName, string pwd)
        {
            //prevent running twice
            if (HttpContext.Current.Items.Contains("ValidateLoginResult"))
                return (bool)HttpContext.Current.Items["ValidateLoginResult"];

            //always return true when active directory domain is empty
            if (WebConfigurationManager.AppSettings["ActiveDirectoryPath"] == "")
                return true;
            try
            {
                var fullusername = WebConfigurationManager.AppSettings["ActiveDirectoryDomain"] + "\\" + userName;
                var enTry = new DirectoryEntry(WebConfigurationManager.AppSettings["ActiveDirectoryPath"], fullusername, pwd, AuthenticationTypes.Secure & AuthenticationTypes.FastBind);
                var mySearcher = new DirectorySearcher(enTry) { Filter = "(&(objectClass=*)(samAccountName=" + userName + "))" };
                mySearcher.PropertiesToLoad.Add("samAccountName");
                mySearcher.PropertiesToLoad.Add("organizationalUnit");
                var result = mySearcher.FindOne();

                if (result == null) return false;
                if (!result.Path.Contains("OU=" + WebConfigurationManager.AppSettings["ActiveDirectoryOu"])) return false;

                var ldapResult = result.Properties["samAccountName"][0].ToString();
                HttpContext.Current.Items.Add("ValidateLoginResult", !string.IsNullOrEmpty(ldapResult));
                return !string.IsNullOrEmpty(ldapResult);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}