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
        #region Class Variables
        private string _URL = String.Empty;
        #endregion

        #region Properties
        public string TotalReferencesCounted
        {  get; set;  }

        public string TotalReferenceResponseTime
        {  get; set;  }
        
        public ConcurrentDictionary<string, string> ReferenceResponses
        {  get; set;  }
        #endregion

        #region Methods
        public MainWindow()
        {
            InitializeComponent();
            BePatient.Visibility = Visibility.Hidden;
        }               

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _URL = MyTextBox.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            Thread t = new Thread(() =>
            {
                ParsingLogic wikiUrlTester = new ParsingLogic();

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
                        
                        ReferenceResponses = wikiUrlTester.SetupURLTest(_URL);
                        TotalReferenceResponseTime = wikiUrlTester.TotalReferenceResponseTime;
                        TotalReferencesCounted = wikiUrlTester.TotalReferencesCounted;
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

                if (String.IsNullOrEmpty(wikiUrlTester.ErrorMessage))
                {
                    if (ReferenceResponses != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            DG.ItemsSource = ReferenceResponses;
                            TimeTakenTB.Text = TotalReferenceResponseTime;
                            RefsCountedTB.Text = TotalReferencesCounted;
                            ValidateRefsButton.Content = "Validate References";
                            ValidateRefsButton.IsEnabled = true;
                            BePatient.Visibility = Visibility.Hidden;
                        });
                    }
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(wikiUrlTester.ErrorMessage, "Error Has Occurred While Processing");
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
    #endregion


}
