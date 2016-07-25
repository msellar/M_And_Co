using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.IO;

namespace SaveFeedContent
{
    public class JSON_FileMgr
    {
        // Compromise, constants match the spec exactly
        // RSS is UK only, and lookup of Link/Description isn't obvious so use these values as supplied
        const string cTitle = @"BBC News - Home";
        const string cLink = @"http://www.bbc.co.uk/news/#sa-ns_mchannel=rss&amp;ns_source=PublicRSS20-sa";
        const string cDescription = @"The latest stories from the Home section of the BBC News web site.";

        private string m_FileName;
        private bool m_NoItemsExist = true;
        private List<NewsItem> m_lstNewsItems;

        // Used to hold day's items in memory
        // Title and Pub. Date together act as key (no link/description required)
        // (Title alone may not be unique)
        public struct NewsItem
        {
            public string m_Title;
            public string m_pubDate;
        }

        public bool CreateFile()
        {
            // Create file and write header info
            // Better to store all News Item 'keys' for today once, than spend file I/O for each in turn
            // Can be run at any point in the hour, if repeated hourly
            string strJSONFilename = ConfigurationSettings.AppSettings["AppDirectory"] + @"\feed\" + string.Format("{0:yyyy-MM-dd-HH}", DateTime.Now) + @".json";

            try
            {
                // Early re-runs not permitted 
                if (File.Exists(strJSONFilename))
                {
                    Logger.Log(string.Format("*** ERROR *** File {0} cannot be created, already exists", strJSONFilename));
                    return false;
                }

                m_FileName = strJSONFilename;

                // Create in-memory list of news items already existing today
                CreateListOfItemsToday();

                // Know JSON file does not already exist
                WriteStartOfFile();
            }
            catch (Exception ex)
            {
                throw new Exception("Create File failed: " + ex.Message);
            }

            return true;
        }

        private void WriteStartOfFile()
        {
            StreamWriter sr = null;

            try
            {
                sr = new StreamWriter(m_FileName);

                // Constants used, feed object not ideal so used directly from spec
                sr.WriteLine("{");
                sr.WriteLine("\"title\": \"" + cTitle + "\",");
                sr.WriteLine("\"link\": \"" + cLink + "\","); 
                sr.Write("\"description\": \"" + cDescription + "\"");        // No newline
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return;
        }


        public void WriteEndOfFile()
        {
            StreamWriter sr = null;

            try
            {
                sr = new StreamWriter(m_FileName, true);   // Append = true

                sr.WriteLine();
                if (m_NoItemsExist.Equals(false))
                {
                    sr.WriteLine("]");
                }
                sr.WriteLine("}");

                m_NoItemsExist = false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return;
        }

        private void CreateListOfItemsToday()
        {
            int currentHour = DateTime.Now.Hour;
            m_lstNewsItems = new List<NewsItem>();

            try
            {
                for (int hour = 0; hour < currentHour; hour++)
                {
                    // Know each item across all hourly JSON files today will be unique
                    // (even if not, checking all means it wouldn't matter)
                    // File existence checked for each previous hour, no guarantees about previous runs and files
                    // ASSUMES each item holds "title" before "pubDate", and pubDate is last property. Fair, as we create the JSON files

                    string strJSONFilename = ConfigurationSettings.AppSettings["AppDirectory"] + @"\feed\" + string.Format("{0:yyyy-MM-dd}-{1:00}", DateTime.Now, hour) + @".json";
                    if (File.Exists(strJSONFilename))
                    {
                        string strCurrentTitle = "";
                        string strCurrentPubDate = "";

                        using (StreamReader reader = new StreamReader(strJSONFilename))
                        {
                            string strLineRead;
                            while ((strLineRead = reader.ReadLine()) != null)
                            {
                                if (strLineRead.Contains("\"title\":"))
                                {
                                    // If it's the header "BBC News" line (for this example), ignore
                                    if (strLineRead.Contains(cTitle).Equals(false))
                                    {
                                        // Strip off label and trailing quotes/comma. Skips leading TABs, but otherwise relies on format as per spec
                                        int idxKey = strLineRead.IndexOf("\"title\":");
                                        if (idxKey >= 0)
                                        {
                                            strCurrentTitle = strLineRead.Substring(10 + idxKey, strLineRead.Length - (12 + idxKey));
                                            strCurrentPubDate = "";       // Reach Pub. Date in a later line
                                        }
                                    }
                                }
                                else if (strLineRead.Contains("\"pubDate\":"))
                                {
                                    // Strip off label and trailing quotes/comma. Skipd leading TABs, but otherwise relies on format as per spec
                                    int idxKey = strLineRead.IndexOf("\"pubDate\":");
                                    if (idxKey >= 0)
                                    {
                                        strCurrentPubDate = strLineRead.Substring(12 + idxKey, strLineRead.Length - (13 + idxKey));

                                        NewsItem item = new NewsItem();
                                        item.m_Title = strCurrentTitle;
                                        item.m_pubDate = strCurrentPubDate;
                                        m_lstNewsItems.Add(item);

                                        // Reset for next news item
                                        strCurrentTitle = "";
                                        strCurrentPubDate = "";

                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // No further tidy-up required

            return;
        }

        private bool ItemExistsToday(string p_Title, string p_PubDate)
        {
            foreach (NewsItem item in m_lstNewsItems)
            {
                if (item.m_Title.Equals(p_Title) && item.m_pubDate.Equals(p_PubDate))
                {
                    return true;
                }
            }
            return false;
        }

        public void WriteItem(string p_Title, string p_Description, string p_Link, string p_pubDate)
        {
            if (ItemExistsToday(p_Title, p_pubDate))
            {
                return;
            }

            StreamWriter sr = null;

            try {
                sr = new StreamWriter(m_FileName, true);   // Append = true

                if (m_NoItemsExist)
                {
                    sr.WriteLine(",");
                    sr.WriteLine("\"items\":[");
                }
                else
                {
                    sr.WriteLine(",");
                }

                sr.WriteLine("\t{");
                sr.WriteLine("\t\t\"title\": \"" + p_Title + "\",");
                sr.WriteLine("\t\t\"description\": \"" + p_Description + "\",");
                sr.WriteLine("\t\t\"link\": \"" + p_Link + "\",");
                sr.WriteLine("\t\t\"pubDate\": \"" + p_pubDate + "\"");
                sr.Write("\t}");             // Following comma on same line (if more items exist)

                m_NoItemsExist = false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                } 
            }

            return;
        }

    }
}
