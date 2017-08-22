using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikiReferenceValidator
{
    class ParsingLogic
    {        
        public string TotalReferencesCounted
        { get; set; }

        public string TotalReferenceResponseTime
        { get; set; }

        public string ErrorMessage
        { get; set; }


        public ConcurrentDictionary<string, string> SetupURLTest(string URLToTest)
        {
            ConcurrentDictionary<string, string> resultDict = new ConcurrentDictionary<string, string>();

            WebClient wc = new System.Net.WebClient();
            byte[] raw = new byte[0];
            string webData = String.Empty;

            try
            {
                raw = wc.DownloadData(URLToTest);
            }
            catch (WebException ex)
            {
                ErrorMessage = ex.Message;
            }

            if (raw != null && raw.Count() > 0)
            {
                webData = Encoding.UTF8.GetString(raw);

                int startIndex = webData.IndexOf(@"id=""References"">References");

                if (startIndex >= 0)
                {
                    string newWebData = String.Empty;

                    try
                    {
                        newWebData = webData.Substring(startIndex, webData.Length - startIndex);
                    }

                    catch (IndexOutOfRangeException ex)
                    {
                        ErrorMessage = ex.Message;
                    }


                    string findReferencesSection = "(<)(.)(o)(l)(>)";
                    Regex r = new Regex(findReferencesSection, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    Match m = r.Match(newWebData);

                    if (m.Success)
                    {

                        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                               | SecurityProtocolType.Tls11
                                                               | SecurityProtocolType.Tls12
                                                               | SecurityProtocolType.Ssl3;

                        resultDict = TestURLs(newWebData, m.Index);
                    }
                }
                else
                {
                    ErrorMessage = "The references section cannot be found for this article.";
                }
            }
            return resultDict;
        }

        public ConcurrentDictionary<string, string> TestURLs(string rawText, int endIndex)
        {     
            int badURLIndex = 0;
            ConcurrentDictionary<string, string> referencesFinal = new ConcurrentDictionary<string, string>();
            var referencesRaw = rawText.Substring(0, endIndex);
            List<int> urlIndexes = StringExtensions.AllIndexesOf(referencesRaw, @"href=""");
            List<string> listOfURLs = new List<string>();

            for (int i = 0; i < urlIndexes.Count; i++)
            {
                string referenceFinder = referencesRaw.Substring(urlIndexes[i] + 6, referencesRaw.Length - urlIndexes[i] - 7);
                string url = referenceFinder.Substring(0, referenceFinder.IndexOf('"'));

                if (!String.IsNullOrEmpty(url))
                {
                    if (!url.StartsWith("/w/index.php"))
                    {

                        if (url.StartsWith("/wiki"))
                        {
                            url = "https://en.wikipedia.org" + url;
                        }

                        if (!(url.StartsWith("http") || url.StartsWith("www")))
                        {
                            if (url.IndexOf("www") < url.IndexOf("http"))
                            {
                                badURLIndex = url.IndexOf("www");
                            }
                            else
                            {
                                badURLIndex = url.IndexOf("www");
                            }

                            if (badURLIndex >= 0)
                            {
                                url = "http://" + url.Substring(badURLIndex, url.Length - badURLIndex);
                            }
                        }

                        if (!url.StartsWith("#"))
                        {
                            if (!listOfURLs.Contains(url))
                            {
                                listOfURLs.Add(url);
                            }
                        }
                    }
                }

            }

            referencesFinal = GetResponseFromURLList(listOfURLs);

            return referencesFinal;
        }

        public ConcurrentDictionary<string, string> GetResponseFromURLList(List<string> URLList)
        {
            ConcurrentDictionary<string, string> referenceList = new ConcurrentDictionary<string, string>();
            Stopwatch testingTime = Stopwatch.StartNew();

            referenceList = GetResponses(URLList);

            testingTime.Stop();

            if (referenceList != null && referenceList.Count > 0)
            {
                TimeSpan ts = testingTime.Elapsed;

                TotalReferenceResponseTime = ts.ToString("mm\\:ss\\.ff");
                TotalReferencesCounted = referenceList.Count.ToString();
            }

            return referenceList;
        }

        public ConcurrentDictionary<string, string> GetResponses(List<string> ListOfURLs)
        {
            int refCount = 0;
            WebRequest request;
            HttpWebResponse response;
            string url = String.Empty;
            ConcurrentDictionary<string, string> responseList = new ConcurrentDictionary<string, string>();

            ParallelOptions parallelOps = new ParallelOptions();
            parallelOps.MaxDegreeOfParallelism = Environment.ProcessorCount;

            Parallel.For(0, ListOfURLs.Count, parallelOps, i =>
            {
                request = WebRequest.Create(ListOfURLs[i]);

                try
                {
                    using (response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response != null)
                        {                            
                            responseList.TryAdd(ListOfURLs[i], "url Returned With " + (int)response.StatusCode + " Response: " + ((HttpStatusCode)response.StatusCode).ToString());
                            refCount++;                            
                        }
                    }
                }

                catch (WebException ex)
                {
                    responseList.TryAdd(ListOfURLs[i], "url Returned With An Error - Response: " + ex.Message);
                    refCount++;
                }

            });

            return responseList;
        }
    }
}
