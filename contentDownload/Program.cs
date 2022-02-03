using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace contentDownload
{
    class Program
    {

        static void Main(string[] args)
        {
            string rootDirectoryToExtract = "strWebDirectory.txt";
            string rootDirectoryToSave = "";
            var uris = GetMostRecentDirectoriesAsync(rootDirectoryToExtract, new List<string>() { "Word", "Control Panel","Core" });

            DownloadAllFiles(uris,rootDirectoryToSave);
        }



        public static List<string> GetMostRecentDirectoriesAsync(string rootDirectory, List<string> matchPattern)
        {
            List<string> uris = new List<string>();
            List<string> anchors = new List<string>();

            WebClient client = new WebClient();
            var rs = client.OpenRead(new Uri(rootDirectory));
            //Stream rs = File.OpenRead(rootDirectory);
            StreamReader sr = new StreamReader(rs);
            StringBuilder sb = new StringBuilder();

            char[] read = new Char[256];
            int count = sr.Read(read, 0, 256);

            while (count > 0)
            {
                sb.Append(read, 0, count);
                count = sr.Read(read, 0, 256);
            }
            sr.Close();

            // Extract <a href=... </a> from html
            Regex anchorRegEx = new Regex("<a[\\s]+([^>]+)>((?:.(?!\\<\\/a\\>))*.)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            MatchCollection anchorMatches = anchorRegEx.Matches(sb.ToString());

            // Extract dates from html
            Regex dateRegEx = new Regex(@"(\d{2}([.\-/])[A-Z][a-z][a-z]([.\-/])\d{4} \d{2}:\d{2})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            MatchCollection dateMatches = dateRegEx.Matches(sb.ToString());

            List<DateTime> datetimes = new List<DateTime>();
            foreach (var dateMatch in dateMatches)
            {
                datetimes.Add(Convert.ToDateTime(dateMatch.ToString()));
            }

            List<Link> totalList = new List<Link>();

            //attaching the dates with their links
            for (int i = 0; i < anchorMatches.Count - 1; i++)
            {
                totalList.Add(new Link() { uri = anchorMatches[i + 1].Value, DateTime = datetimes[i] });
            }

            //finding the most recent link for the given pattern
            //donot need to use asterik like word*. only 'Word' would be fine
            foreach (var pattern in matchPattern)
            {
                var list = totalList.Where(x => x.uri.Contains(pattern));
                anchors.Add(list.OrderByDescending(x => x.DateTime).First().uri);
            }

            //filtering only the required parts and attaching with the rootdirectory path
            foreach (var anchor in anchors)
            {
                // extract href attribute value
                string href = Regex.Match(anchor, "([\"\"'](?<url>.*?)[\"\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase).Value;
                href = href.Replace("\"", "");
                uris.Add(rootDirectory + "/" + href);
            }


            return uris;
        }

        public static void DownloadAllFiles(List<string> uris, string directory)
        {
            foreach (var uri in uris)
            {
                DownloadRecursive(uri, directory);
            }
        }

        //to extract the anchor tags from the html string
        public static List<string> ExtractAnchors(string html)
        {
            Regex anchorRegEx = new Regex("<a[\\s]+([^>]+)>((?:.(?!\\<\\/a\\>))*.)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            MatchCollection anchorMatches = anchorRegEx.Matches(html);

            List<string> links = new List<string>();
            foreach (Match match in anchorMatches)
            {
                // extract href attribute value
                string href = Regex.Match(match.Value, "([\"\"'](?<url>.*?)[\"\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase).Value;
                href = href.Replace("\"", "");
                links.Add(href);
            }

            return links;

        }

        public static void DownloadRecursive(string uri, string rootDirectory)
        {
            WebClient client = new WebClient();
            var rs = client.OpenRead(new Uri(uri));
            //Stream rs = File.OpenRead("dir.txt");

            //reading the html and storing in a string
            StreamReader sr = new StreamReader(rs);
            StringBuilder sb = new StringBuilder();

            char[] read = new Char[256];
            int count = sr.Read(read, 0, 256);

            while (count > 0)
            {
                sb.Append(read, 0, count);
                count = sr.Read(read, 0, 256);
            }
            sr.Close();

            // Extract links from html
            var links = ExtractAnchors(sb.ToString());

            foreach (var href in links)
            {
                // foldernames end with /
                if (!href.EndsWith("/"))
                {
                    string fileToGet = uri + href;

                    WebClient downloadClient = new WebClient();

                    //this whole splitting part is done because we just want to create the folder 
                    //that is after the root address 
                    //e.g for  https://rootaddress.com/controlpanel/filename , we would just need the controlpanel and filename parts
                    //
                    //it could be modified for different setups like what if files are not being delivered over http address
                    //what if it is a ip address. In that case the values of skipping the address parts would be changed
                    //also if root directory has more slashes than the expected root directory like https://rootaddress.com/unwanteddirectory/controlpanel/filename
                    var xarr = fileToGet.Split('/').Skip(3);
                    var filepath = string.Join("/", xarr);
                    var path = string.Join("/", xarr.SkipLast(1));
                    if (!Directory.Exists(Path.Combine(rootDirectory, path)))
                    {
                        Directory.CreateDirectory(Path.Combine(rootDirectory, path));
                    }

                    downloadClient.DownloadFile(fileToGet, Path.Combine(rootDirectory, filepath));

                }
                //ignoring the return to parent link and following the other links
                else if (href != "../")
                {
                    DownloadRecursive(uri + href, rootDirectory);
                }
            }


        }




        //the following methods can used as event handlers if we want to add the percentage downloaded part
        //but that would be a bit tricky because we are moving in the directories recursively so we donot know the 
        //total or actual size of the files that we would need to download
        //
        //it can be solved such as we gather the size info and links of all the files first and then download them
        //after getting the information

        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine("File downloaded successfully");
        }

        private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("    downloaded {0} of {1} bytes. {2} % complete...", e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }

    //data structure to manage url and datetime so that the latest one is used
    public class Link
    {
        public string uri { get; set; }
        public DateTime DateTime { get; set; }
    }
}
