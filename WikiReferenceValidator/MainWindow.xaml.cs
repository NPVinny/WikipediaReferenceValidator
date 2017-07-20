using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace WikiReferenceValidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _URL = String.Empty;

        private bool _TextBlockVisible;

        public bool TextBlockVisible
        {
            get { return _TextBlockVisible; }
            set { _TextBlockVisible = value; }
        }

        private static ConcurrentDictionary<string, string> _MyDict;

        public static ConcurrentDictionary<string, string> MyDict
        {
            get { return _MyDict; }
            set { _MyDict = value; }
        }

        private static string _refsCounted;

        public static string refsCounted
        {
            get { return _refsCounted; }
            set { _refsCounted = value; }
        }

        private static string _totalTime = String.Empty;

        public static string totalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            BePatient.Visibility = Visibility.Hidden;
        }       

        public ConcurrentDictionary<string, string> SetupURLTest(string URLToTest)
        {


            ConcurrentDictionary<string, string> resultDict = new ConcurrentDictionary<string, string>();
            
            WebClient wc = new System.Net.WebClient();
            byte[] raw = wc.DownloadData(URLToTest);

            string webData = Encoding.UTF8.GetString(raw);

            int startIndex = webData.IndexOf(@"Edit section: References");

            string newWebData = webData.Substring(startIndex, webData.Length - startIndex);

            string re1 = "(<)"; // Any Single Character 1
            string re2 = "(.)"; // Any Single Character 2
            string re3 = "(o)"; // Any Single Character 3
            string re4 = "(l)"; // Any Single Character 4
            string re5 = "(>)";	// Any Single Character 5

            Regex r = new Regex(re1 + re2 + re3 + re4 + re5, RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

            return resultDict;
        }

        private ConcurrentDictionary<string, string> TestURLs(string rawText, int endIndex)
        {
            string returnString = String.Empty;
            string URL = String.Empty;
            int badURLIndex = 0;
            ConcurrentDictionary<string, string> referencesFinal = new ConcurrentDictionary<string, string>();
            var referencesRaw = rawText.Substring(0, endIndex);
            List<int> urlIndexes = StringExtensions.AllIndexesOf(referencesRaw, @"href=""");
            List<string> listOfURLs = new List<string>();
            string referenceFinder = String.Empty;
           
            for(int i = 0; i < urlIndexes.Count; i++)
            {
               referenceFinder = referencesRaw.Substring(urlIndexes[i] + 6, referencesRaw.Length - urlIndexes[i] - 7);
               URL = referenceFinder.Substring(0, referenceFinder.IndexOf('"'));               
                    
                   if (!String.IsNullOrEmpty(URL))
                   {


                       if (URL.StartsWith("/wiki"))
                       {
                           URL = "https://en.wikipedia.org" + URL;
                       }

                       if (!(URL.StartsWith("http") || URL.StartsWith("www")))
                       {
                           if (URL.IndexOf("www") < URL.IndexOf("http"))
                           {
                               badURLIndex = URL.IndexOf("www");
                           }
                           else
                           {
                               badURLIndex = URL.IndexOf("www");
                           }

                           if (badURLIndex >= 0)
                           {
                               URL = "http://" + URL.Substring(badURLIndex, URL.Length - badURLIndex);
                           }
                       }

                       if (!URL.StartsWith("#"))
                       {
                            if (!listOfURLs.Contains(URL))
                            {
                                listOfURLs.Add(URL);
                            }
                       }
                   }
               
           }

            referencesFinal = GetResponseFromURLList(listOfURLs);

            return referencesFinal;
        }

        public ConcurrentDictionary<string,string> GetResponseFromURLList(List<string> URLList)
        {
            Stopwatch testingTime = new Stopwatch();                       
            ConcurrentDictionary<string, string> referenceList = new ConcurrentDictionary<string, string>();
          
            testingTime.Start();

            referenceList = GetResponses(URLList);            

            testingTime.Stop();

            if (referenceList != null && referenceList.Count > 0)
            {
                TimeSpan ts = testingTime.Elapsed;

                _totalTime = ts.ToString("mm\\:ss\\.ff");
                _refsCounted = referenceList.Count.ToString();
            }

            return referenceList;
        }

        private ConcurrentDictionary<string, string> GetResponses(List<string> ListOfURLs)
        {
            int refCount = 0;
            WebRequest request;
            HttpWebResponse response;
            string URL = String.Empty;
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
                            if (!responseList.ContainsKey(URL))
                            {
                                responseList.TryAdd(ListOfURLs[i], "URL Returned With " + (int)response.StatusCode + " Response: " + ((HttpStatusCode)response.StatusCode).ToString());
                                //Console.Write(returnString + referencesFinal.Values.ElementAt(refCount) + Environment.NewLine + Environment.NewLine);
                                refCount++;
                            }
                        }
                    }
                }

                catch (WebException ex)
                {
                    responseList.TryAdd(ListOfURLs[i], "URL Returned With An Error - Response: " + ex.Message);
                    //Console.Write(returnString + referencesFinal.Values.ElementAt(refCount) + Environment.NewLine + Environment.NewLine);
                    refCount++;
                }

            });


            return responseList;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _URL = MyTextBox.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            Thread t = new Thread(() =>
            {

                this.Dispatcher.Invoke(() =>
                {
                    BePatient.Visibility = Visibility.Visible;
                    ValidateRefsButton.Content = "Loading Results... Please Wait";
                    ValidateRefsButton.IsEnabled = false;
                });

                if (!String.IsNullOrEmpty(_URL) && _URL.Contains("wikipedia"))
                {
                    if (!(_URL.StartsWith("http") || _URL.StartsWith("www")))
                    {
                        System.Windows.MessageBox.Show("Please enter in a full URL, starting with http(s) or www.", "Error");
                    }
                    else if (_URL.StartsWith("http") || _URL.StartsWith("www"))
                    {
                        _MyDict = SetupURLTest(_URL);
                    }
                }
                else
                {
                    if (!_URL.Contains("wikipedia"))
                    {

                        System.Windows.MessageBox.Show("Please enter in a URL from Wikipedia.", "Error");
                    }
                    if (String.IsNullOrEmpty(_URL))
                    {
                        System.Windows.MessageBox.Show("Please enter in a URL.", "Error");
                    }
                }

                if (MyDict != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        DG.ItemsSource = MyDict;
                        TimeTakenTB.Text = _totalTime;
                        RefsCountedTB.Text = _refsCounted;
                        ValidateRefsButton.Content = "Validate References";
                        ValidateRefsButton.IsEnabled = true;
                        BePatient.Visibility = Visibility.Hidden;
                    });
                }
            });
            t.Start();
            
        }

        private void DG_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)e.OriginalSource;
            Process.Start(link.NavigateUri.AbsoluteUri);
        }

    }

    
    
}
