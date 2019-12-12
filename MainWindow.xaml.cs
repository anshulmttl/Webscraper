using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using Serilog;
using Serilog.Sinks;
using PaddleSDK;
using PaddleSDK.Checkout;
using PaddleSDK.Product;
using Microsoft.Win32;
using System.IO;

namespace Scrapetelligence
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _showInstagramFollowers;

        private List<ScrapeDetails> scrapeList= new List<ScrapeDetails>();

        BackgroundWorker myWorker = new BackgroundWorker();

        bool ScrapeRunning = false;

        Dictionary<int, string> comboBoxItemMap = new Dictionary<int, string> {
            { 0, "Instagram user followers"},
            { 1, "Instagram hash tag followers"},
            { 2, "Facebook group followers"},
            { 3, "Facebook hash tag"},
            { 4, "Twitter by keywords"},
            { 5, "Yelp by keywords"},
            { 6, "Google by keywords"} };

        #region PaddleSdkConfig
        string paddle_vendorId = "49371";
        string paddle_productId = "572660";
        string paddle_apiKey = "524606fd07c43bd529975c72b8d298c4";
        #endregion

        private void AddButtonHandler(object sender, RoutedEventArgs e)
        {
            // Code behind to add event handler
            if ((cboType.SelectedIndex == 5 || cboType.SelectedIndex == 4 || cboType.SelectedIndex == 6) && string.IsNullOrEmpty(url.Text))
                return;
            else if ((cboType.SelectedIndex == 0 || cboType.SelectedIndex == 1) && (string.IsNullOrEmpty(url.Text) || string.IsNullOrEmpty(username.Text) || string.IsNullOrEmpty(password.Text)))
                return;

            int index = cboType.SelectedIndex;

            ScrapeDetails details = new ScrapeDetails((Miscellaneous.Type)index, url.Text, username.Text, password.Text, location.Text);
            scrapeList.Add(details);
            Console.Text += "Scrape " + comboBoxItemMap[index] + "\n";
        }
        private void HelpClick(object sender, RoutedEventArgs e)
        {
            Log.Debug("Help button click");
            System.Diagnostics.Process.Start("https://www.scrapetelligence.com/p/contact.html");
        }

        private void ButtonFileSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = "";

                List<User> scrapeOutput = new List<User>();
                Scraper obj = Scraper.Instance;
                scrapeOutput = obj.GetScrapeOutput();

                foreach (User user in scrapeOutput)
                {
                    string output = string.Format("{0},{1},{2},{3},{4} \n", user.name, user.userName, user.phoneNumber, user.eMail, user.countryCode);
                    text += output;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName, text);
            }
            catch(Exception err)
            {
                Log.Error("Error saving file : {0}",err.Message);
            }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            Log.Debug("Start button click");
            if (false == ScrapeRunning)
            {
                // Start scraping
                myWorker.RunWorkerAsync();
                //StartButton.Background = Brushes.IndianRed;
                ScrapeRunning = true;
                StartButton.Content = "Stop";
                Console.Text += "Started scraping... \n";
            }
            else
            {
                StartButton.Content = "Start";
                //StartButton.Background = Brushes.DarkSeaGreen;
                ScrapeRunning = false;
                Console.Text += "Scraping interrupted \n";
            }
        }

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            Log.Debug("Start scraping in background thread");
            Scraper obj = Scraper.Instance;

            foreach(ScrapeDetails detail in scrapeList)
            {
                obj.Scrape(detail);
            }
        }

        private void BackgroundCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<User> scrapeOutput = new List<User>();
            Scraper obj = Scraper.Instance;
            scrapeOutput = obj.GetScrapeOutput();

            foreach(User user in scrapeOutput)
            {
                string output = string.Format("Name: {0}, UserName: {1}, Phone: {2}, Email: {3}, Address {4} \n",user.name, user.userName, user.phoneNumber, user.eMail, user.countryCode);
                Console.Text += output;
            }

            Console.Text += "Scraping completed... \n";
        }

        private void WebsiteHandler(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 1)
            {
                Log.Debug("Click on website label");
                var label = (Label)sender;
                Keyboard.Focus(label.Target);
                System.Diagnostics.Process.Start("https://www.scrapetelligence.com/");
            }
        }
        private void Paddle_TransactionCompleteEvent(object sender, TransactionCompleteEventArgs e)
        {

        }

        private void Paddle_TransactionErrorEvent(object sender, TransactionErrorEventArgs e)
        {

        }

        private void Paddle_TransactionBeginEvent(object sender, TransactionBeginEventArgs e)
        {

        }

        public MainWindow()
        {
            InitializeComponent();

            var productInfo = new PaddleProductConfig { ProductName = "Scraptelligence", VendorName = "Scraptelligence" };
            Paddle.Configure(paddle_apiKey, paddle_vendorId, paddle_productId, productInfo);
            Paddle.Instance.TransactionCompleteEvent += Paddle_TransactionCompleteEvent;
            Paddle.Instance.TransactionErrorEvent += Paddle_TransactionErrorEvent;
            Paddle.Instance.TransactionBeginEvent += Paddle_TransactionBeginEvent;

            PaddleProduct product = PaddleProduct.CreateProduct(paddle_productId);

            product.Refresh((success) =>
            { 
                if(success)
                {
                    if(!product.Activated)
                    {
                        Paddle.Instance.ShowProductAccessWindowForProduct(product);
                    }
                }
                else
                {
                    Paddle.Instance.ShowProductAccessWindowForProduct(product);
                }
            });
            // Initialize logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs\\Log.log")
                .CreateLogger();
            
            Log.Information("Starting Scraptelligence");
            _showInstagramFollowers = false;
            myWorker.DoWork += BackgroundWork;
            myWorker.RunWorkerCompleted += BackgroundCompleted;
            StartButton.Background = Brushes.DarkSeaGreen;
            
            string copyrightText = string.Format("Copyright © {0}, Scrapetelligence",DateTime.Now.Year.ToString());
            CopyrightLabel.Content = copyrightText;
        }

        public bool ShowInstagramFollowers
        {
            get { return _showInstagramFollowers; }
        }
    }
}
