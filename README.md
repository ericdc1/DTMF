DTMF
====

A deployment tool for ASP.Net websites

Check out the blog post about this project at http://sharpcoders.org/post/DTMF-Deployment-Tool 

To get started, edit the settings in web.config for your environment. For testing your can leave the Active Directory Path empty to disable security. For production use you will need to configure the Active Directory authentication settings.

Create one XML configuration for each project. An example configuration is here: https://github.com/ericdc1/DTMF/blob/master/Source/DTMF.Website/App_Data/Configurations/Template.xml.temp

Create one web.config transform for each project named [projectname].web.config. An example transform is here: https://github.com/ericdc1/DTMF/blob/master/Source/DTMF.Website/App_Data/Transforms/Web.Template.config.temp

The IIS app pool needs to run as an account with access to target systems. 
