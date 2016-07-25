using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Configuration;
using System.ServiceModel.Syndication;


namespace SaveFeedContent
{
    /*
     **************************************************************************************************
     
     Console app to read BBC News Feed and save to JSON files, hourly
    
     REFERENCES:
     
            To build console app, need 2 references added:
     
                System.ServiceModel.Web
                System.XML
     
     REQUIREMENTS:
    
          <AppDirectory> is defined in app.config ('SaveFeedContent' can be renamed if desired)
          'feed' folder for saved JSON files already exists, in <AppDirectory> (see app.config)
    
     NOTES:
    
          log.txt file is written to <AppDirectory> (created/appended as required)
          Title/Link/Description in header taken from spec rather than 'feed' object which doesn't provide necessary values
          The check for an existing item looks at both "title" and "pub. date"; worst-case scenario could see same "title"
              reused. Don't need other 2 properties to provide the key for this check.
    
    
     ************************************************************************************************** 
     */


    class Program
    {
        static void Main(string[] args)
        {
            string strURL = ConfigurationSettings.AppSettings["RSS_URL"];
            SyndicationFeed feed = null;

            try
            {
                XmlReader reader = XmlReader.Create(strURL);
                feed = SyndicationFeed.Load(reader);
                reader.Close();
            }
            catch (Exception ex)
            {
                // Log error simply, flag failure for clarity in Scheduled Task
                Logger.Log(string.Format("*** ERROR *** Failed to read feed at {0}, {1}", strURL, ex.Message));
                System.Environment.Exit(1);
            }

            try
            {
                JSON_FileMgr json = new JSON_FileMgr();
                // Title/Link/Description would be passed in here if 'feed' provided useful values - spec used instead
                if (json.CreateFile().Equals(false))
                {
                    // 'File already exists' logged in method if appropriate
                    System.Environment.Exit(1);
                };

                foreach (SyndicationItem item in feed.Items)
                {
                    // Let class worry about validity, skip as necessary
                    json.WriteItem(item.Title.Text, item.Summary.Text, item.Links[0].Uri.ToString(), item.PublishDate.DateTime.ToString("r"));
                }

                json.WriteEndOfFile();
                json = null;
            }
            catch (Exception ex)
            {
                // Log error simply, flag failure for clarity in Scheduled Task
                Logger.Log(string.Format("*** ERROR *** Failed to create JSON file for feed at {0}, {1}", strURL, ex.Message));
                System.Environment.Exit(1);
            }

            Logger.Log(string.Format("Feed at {0} processed", strURL));

            // Standard 'success' value for Scheduled Tasks
            System.Environment.Exit(0);
        }
    }
}