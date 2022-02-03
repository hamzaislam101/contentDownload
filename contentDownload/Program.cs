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
    public class Link
    {
        public string uri { get; set; }
        public DateTime DateTime { get; set; }
    }
    class Program
    {


        public static List<string> GetMostRecentDirectoriesAsync(string rootDirectory, List<string> matchPattern)
        {
            List<string> uris = new List<string>();
            List<string> anchors = new List<string>();

            WebClient client = new WebClient();
            //var rs = client.OpenRead(new Uri(rootDirectory));
            Stream rs = File.OpenRead(rootDirectory);
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
            for(int i = 0;i<anchorMatches.Count-1;i++)
            {
                totalList.Add(new Link() { uri = anchorMatches[i + 1].Value, DateTime = datetimes[i] });
            }


            foreach(var pattern in matchPattern)
            {
                var list = totalList.Where(x => x.uri.Contains(pattern));
                anchors.Add(list.OrderByDescending(x => x.DateTime).First().uri);
            }

            foreach(var anchor in anchors)
            {
                // extract href attribute value
                string href = Regex.Match(anchor, "([\"\"'](?<url>.*?)[\"\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase).Value;
                href = href.Replace("\"", "");
                uris.Add(rootDirectory+"/"+ href);
            }
            

            return uris;
        }

        public static void DownloadAllFiles(List<string> uris,string directory)
        {
            foreach(var uri in uris)
            {
                DownloadRecursive(uri);
            }
        }

        public static List<string> ExtractAnchors(string html)
        {
            Regex anchorRegEx = new Regex("<a[\\s]+([^>]+)>((?:.(?!\\<\\/a\\>))*.)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            MatchCollection anchorMatches = anchorRegEx.Matches(html);

            List<string> links = new List<string>();
            foreach(Match match in anchorMatches)
            {
                // extract href attribute value
                string href = Regex.Match(match.Value, "([\"\"'](?<url>.*?)[\"\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase).Value;
                href = href.Replace("\"", "");
                links.Add(href);
            }

            return links;

        }

        public static void DownloadRecursive(string uri)
        {
            WebClient client = new WebClient();
            var rs = client.OpenRead(new Uri(uri));
            //Stream rs = File.OpenRead("dir.txt");
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
                    string fileToGet = uri+ href;

                    WebClient downloadClient = new WebClient();
                    var xarr = fileToGet.Split('/').Skip(3);
                    var filepath = string.Join("/", xarr);
                    var path = string.Join("/", xarr.SkipLast(1));
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    downloadClient.DownloadFile(fileToGet, filepath);
                    Console.WriteLine(fileToGet);
                    // file download code here...
                }
                else if(href != "../")
                {
                    DownloadRecursive(uri + href);
                }
            }


        }

        static void Main(string[] args)
        {


            //WebClient client = new WebClient();
            //client.DownloadProgressChanged += Client_DownloadProgressChanged;
            //client.DownloadFileCompleted += Client_DownloadFileCompleted;
            //client.BaseAddress = "https://hamzaislam101.github.io";
            //await client.DownloadFileTaskAsync("", "apnifile");


            string urlBase = "http://apptest.net";
            //string filesFolderUrl = urlBase + "/DirLister";

            string filepath = "strWebDirectory.txt";

            //var uris = GetMostRecentDirectoriesAsync(filepath, new List<string>() { "Word", "Control Panel","Core" });

            DownloadRecursive("https://www.vanucci.com/files/16:9-resimler/HIGH_LINE/BOLOGNA/");


            //HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(filesFolderUrl);
            //HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(filepath);

            //HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            //if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            //{
            //    //Stream rs = httpWebResponse.GetResponseStream();
            //    Stream rs = File.OpenRead(filepath);
            //    StreamReader sr = new StreamReader(rs);
            //    StringBuilder sb = new StringBuilder();

            //    char[] read = new Char[256];
            //    int count = sr.Read(read, 0, 256);

            //    while (count > 0)
            //    {
            //        sb.Append(read, 0, count);
            //        count = sr.Read(read, 0, 256);
            //    }
            //    sr.Close();
            //    httpWebResponse.Close();

            //    // Extract <a href=... </a> from html
            //    Regex regEx = new Regex("<a[\\s]+([^>]+)>((?:.(?!\\<\\/a\\>))*.)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);


            //    // Extract dates from html
            //    Regex dateRegEx = new Regex(@"(\d{2}([.\-/])[A-Z][a-z][a-z]([.\-/])\d{4} \d{2}:\d{2})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            //    MatchCollection dateMatches = dateRegEx.Matches(sb.ToString());


            //    foreach (var dateMatch in dateMatches)
            //    {
            //        var x = Convert.ToDateTime(dateMatch.ToString());
            //    }

            //    MatchCollection matches = regEx.Matches(sb.ToString());

            //    foreach (Match match in matches)
            //    {
            //        // extract href attribute value
            //        string href = Regex.Match(match.Value, "([\"\"'](?<url>.*?)[\"\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase).Value;
            //        href = href.Replace("\"", "");

            //        // foldernames start and end with / but files only start with /
            //        if (href.StartsWith("/") && !href.EndsWith("/"))
            //        {
            //            string fileToGet = urlBase + href;

            //            Console.WriteLine(fileToGet);
            //            // file download code here...
            //        }
            //    }
            //}

            //Console.WriteLine("Hello World!");
        }

        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine("File downloaded successfully");
        }

        private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("    downloaded {0} of {1} bytes. {2} % complete...", e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
