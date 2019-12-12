using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using InstaSharper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Logger;
using TweetSharp;
using Places;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Serilog;
namespace Scrapetelligence
{
    class Scraper
    {
        private static Scraper instance = null;

        private static readonly object padLock = new object();

        private List<User> users = new List<User>();
        #region InstagramScraping
        UserSessionData user;
        IInstaApi api;
        #endregion

        private static string customer_key = "4ydG7SejWktAQwYJwYxD4gVEg";
        private static string customer_key_secret = "UFWeovrVFArT95QPCljMDK988I0B78YjuTjZcjUsnneTBw4Rq6";
        private static string access_token = "165402658-KgiGEI7tqQO2Z73G1DW2XhgqnUAu1sVT7dnh8Kz5";
        private static string access_token_secret = "0yAbuFUD6cxkvjdLaFcOPsLWgPeYijeDpeH4AxQqA5eV9";

        private static TwitterService service = new TwitterService(customer_key, customer_key_secret, access_token, access_token_secret);

        private static string googleApiKey = "AIzaSyAvMZJ9DAki7R2TDZWiZbGVLeFDvRNYbw0";
        public static Scraper Instance
        {
            get
            {
                lock(padLock)
                {
                    if(instance == null)
                    {
                        instance = new Scraper();
                    }

                    return instance;
                }
            }
        }


        Scraper()
        {

        }

        public void Scrape(ScrapeDetails detail)
        {
            Log.Information("Scraping started");
            if(detail.type == Miscellaneous.Type.TYPE_INSTAGRAM_FOLLOWERS)
            {
                Log.Information("Scraping instagram user followers -- Started");
                user = new UserSessionData();
                user.UserName = detail.userName;
                user.Password = detail.password;

                Login(detail.url);
            }
            else if(detail.type == Miscellaneous.Type.TYPE_TWITTER_KEYWORD)
            {
                Log.Information("Scraping twitter by keywords -- Started");
                ScrapeTwitter(detail.url);
            }
            else if(detail.type == Miscellaneous.Type.TYPE_YELP)
            {
                Log.Information("Scraping YELP by keywords -- Started");
                // Scrape the yelp buisness directory
                ScrapeYelp(detail.url, detail.location);
            }
            else if(detail.type == Miscellaneous.Type.TYPE_GOOGLE)
            {
                Log.Information("Scraping GOOGLE by keywords -- Started");
                ScrapeGoogle(detail.url);
            }
            else if(detail.type == Miscellaneous.Type.TYPE_INSTAGRAM_HASHTAG_FOLLOWERS)
            {
            }
            else if(detail.type == Miscellaneous.Type.TYPE_FACEBOOK_GROUP_FOLLOWERS)
            {

            }
            
        }

        public List<User> GetScrapeOutput()
        {
            Log.Debug("Get scraped users");
            return users;
        }

        private async Task<Response> SearchResults(string latitude, string longitude, string searchText, string apiKey)
        {
            Response results;

            if ((string.IsNullOrEmpty(latitude) || string.IsNullOrEmpty(longitude))) //if we dont have a lon/lat .. search txt
            {
                var task = Places.Api.TextSearch(searchText, apiKey);
                results = task.GetAwaiter().GetResult();
            }
            else
            {
                var task = Places.Api.SearchPlaces(Convert.ToDouble(latitude), Convert.ToDouble(longitude), searchText, apiKey);
                results = task.GetAwaiter().GetResult();
            }

            return results;
        }

        private async Task<Response> GetNextPage(string nextToken, string apiKey)
        {
            try
            {
                HttpClient client = new HttpClient();
                var task = client.GetAsync(String.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?key={0}&sensor=true&pagetoken={1}", apiKey, nextToken));
                //var resp_pass1 = await Task.WhenAll(task);
                var resp = task.GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject(await resp.Content.ReadAsStringAsync(), typeof(Response)) as Response;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error requesting next page 'YELP': {0}", e.Message);
                return null;
            }
        }

        private static async Task<string> GetDetailsPass1(string placeId, string apiKey)
        {
            HttpClient client = new HttpClient();
            try
            {
                var task = client.GetAsync(String.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key={1}", placeId, apiKey));
                //var resp_pass1 = await Task.WhenAll(task);
                var resp = task.GetAwaiter().GetResult();

                string response = await client.GetStringAsync(String.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key={1}", placeId, apiKey));
                JObject jObject = JObject.Parse(response);
                string formatted_phone_number = (string)jObject["result"]["formatted_phone_number"];
                return formatted_phone_number;
                //dynamic dynObject = JsonConvert.DeserializeObject();
                //var records = 
                //string formatted_phone_number = 
                if (resp.IsSuccessStatusCode)
                {
                    //return (JsonConvert.DeserializeObject(await resp.Content.ReadAsStringAsync(), typeof(Response)) as Response).Detail;
                    //return resp.Content.ToString();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error during GetDetailsPass1 : {0}", e.Message);
                return null;
            }
        }

        private void ScrapeGoogle(string keyword)
        {
            // Scrape the address and phone number from google places
            var placeList = new List<Place>();
            string latitude = "", longitude = "";

            var task = SearchResults(latitude, longitude, keyword, googleApiKey);
            Response results = task.GetAwaiter().GetResult();
            Thread.Sleep(2000);
            foreach (var place in results.Places)
            {
                placeList.Add(place);
            }

            //if there are more than one 'page' of results...
            while (results.Next != null)
            {
                // Use new function GetNextPage
                var task5 = GetNextPage(results.Next, googleApiKey);
                results = task5.GetAwaiter().GetResult();
                Thread.Sleep(2000);
                //get the next lot of results
                //results = await Places.Api.GetNext(results.Next, apiKey);

                foreach (var place in results.Places)
                {
                    placeList.Add(place);
                }
            }

            foreach (var place in placeList)
            {
                //var placeDetails = await Places.Api.GetDetails(place.PlaceId, apiKey);
                //var str2 = await GetDetailsPass1(place.PlaceId, apiKey);
                /*var task2 = PlaceDetails(place.PlaceId, apiKey);
                var res = await Task.WhenAll(task2);
                Detail placeDetails = task2.Result;*/

                var task2 = GetDetailsPass1(place.PlaceId, googleApiKey);
                string placeDetails = task2.GetAwaiter().GetResult();
                Thread.Sleep(2000);
                //do stuff with your place and placeDetails                
                string name = place.Name;
                string address = place.Address;
                string phone = "";
                if (placeDetails != null)
                {
                    phone = placeDetails;
                    Log.Debug("GOOGLE - Name : {0}, Phone : {1}, Address : {2}",name, phone, address);
                    User usr = new User(name, "", "", phone, address);
                    users.Add(usr);
                    //phone = placeDetails.Phone;
                }
                //......
                //Console.WriteLine("Place Name : {0}, Address : {1}, Phone : {2}", name, address, phone);
            }

        }

        private void ScrapeYelp(string keyword, string findLocation)
        {
            try{ 
            string key = keyword.Replace(" ", "%20");
            string find_location = "";

            if (!string.IsNullOrEmpty(findLocation))
            {
                find_location = "&find_loc =" + (findLocation.Replace(" ", "%20")).Replace(",", "%2C");
            }
            else
                find_location = "";

            bool bContinueSearch = true;
            int numberResultsCurrentPage = 0;
            int numberResultsTotal = 0;
            while (bContinueSearch)
            {

                numberResultsCurrentPage = 0;
                string currentPage = "&start=" + numberResultsTotal.ToString();

                // URL to scrape for YELP
                string url = "https://www.yelp.com/search?find_desc=" + key + find_location + currentPage;
                Log.Debug("Scraping URL : {0}", url);
                HtmlDocument doc = new HtmlDocument();
                HtmlWeb hw = new HtmlWeb();
                doc = hw.Load(url);

                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//ul[@class = 'lemon--ul__373c0__1_cxs undefined list__373c0__2G8oH']/li[@class = 'lemon--li__373c0__1r9wz border-color--default__373c0__2oFDT']");

                if (nodes != null && nodes.Count > 0)
                {
                    foreach (HtmlNode node in nodes)
                    {
                        string address = "";
                        string countryCode = "";
                        HtmlNode buisnessNode = node.SelectSingleNode(".//h3/span/a");
                        HtmlNode phoneNode = node.SelectSingleNode(".//div[@class = 'lemon--div__373c0__1mboc border-color--default__373c0__2oFDT']/p[@class = 'lemon--p__373c0__3Qnnj text__373c0__2pB8f text-color--normal__373c0__K_MKN text-align--right__373c0__3ARv7']");
                        HtmlNode addressNode = node.SelectSingleNode(".//address/div/div/div[@class = 'lemon--div__373c0__1mboc border-color--default__373c0__2oFDT']/p[@class = 'lemon--p__373c0__3Qnnj text__373c0__2pB8f text-color--normal__373c0__K_MKN text-align--right__373c0__3ARv7']");
                        HtmlNode countryNode = node.SelectSingleNode(".//div[@class = 'lemon--div__373c0__1mboc u-space-b1 border-color--default__373c0__2oFDT']/div/div/div[@class = 'lemon--div__373c0__1mboc border-color--default__373c0__2oFDT']/p");

                        if (buisnessNode != null)
                        {
                            if (addressNode != null)
                            {
                                address = addressNode.InnerText;
                            }
                            if (countryNode != null)
                            {
                                countryCode = countryNode.InnerText;
                            }
                            if (phoneNode != null)
                            {
                                string completeAddress = address + "," + countryCode;
                                // Save the details because it is valid node
                                User usr = new User(buisnessNode.InnerText, "", "", phoneNode.InnerText, completeAddress);
                                users.Add(usr);
                            }
                            ++numberResultsCurrentPage;
                            ++numberResultsTotal;
                        }
                    }
                }

                if (numberResultsCurrentPage < 10)
                {
                    bContinueSearch = false;
                    break;
                }
            }
        }
            catch(Exception e)
            {
                Log.Error("Error during scraping YELP : {0}",e.Message);
            }
        }

        private void ScrapeTwitter(string keyword)
        {
            string[] keywords = keyword.Split(',');
            foreach(string kewd in keywords)
            {
                if (string.IsNullOrEmpty(kewd))
                    continue;

                ScrapeTwitter_Pass1(keyword);
            }
        }

        private void ScrapeTwitter_Pass1(string keyword)
        {
            try
            {
                var options = new SearchOptions { Q = keyword };
                var tweets = service.Search(options);

                foreach (var tweet in tweets.Statuses)
                {
                    string name = "";
                    string userName = "";
                    string phone = "";
                    string email = "";
                    string countryCode = "";

                    name = tweet.User.Name;
                    userName = tweet.User.ScreenName;
                    countryCode = tweet.User.Location;
                    phone = GetPhoneNumber(tweet.User.Description);
                    email = GetEmail(tweet.User.Description);

                    User usr = new User(name, email, userName, phone, countryCode);
                    users.Add(usr);
                }
            }
            catch(Exception e)
            {
                Log.Error("Error during scrape Twitter, {0}",e.Message);
            }
        }

        private string GetEmail(string txt)
        {
            string eMail = "";
            try
            {
                
                const string MatchEmailPattern =
    @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
    + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
     + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
    + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
                Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                MatchCollection matches = rx.Matches(txt);
                int noOfMatches = matches.Count;

                foreach (Match match in matches)
                {
                    if (string.IsNullOrEmpty(eMail))
                    {
                        eMail = match.Value.ToString();
                    }
                    else
                    {
                        eMail += "," + match.Value.ToString();
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error("Error during GetEmail, {0}", e.Message);
            }
            return eMail;
        }

        public async void Login(string username)
        {
            api = InstaApiBuilder.CreateBuilder()
                    .SetUser(user)
                    .UseLogger(new DebugLogger(LogLevel.All))
                    .Build();

            //var loginRequest = await api.LoginAsync();
            var task = api.LoginAsync();
            var loginRequest = task.GetAwaiter().GetResult();
            Thread.Sleep(2000);
            if (loginRequest.Succeeded)
            {
                Log.Debug("Instagram login successful!");
                //Console.WriteLine("Login successful");
                PullUserPosts(username);
            }
            else
            {
                Log.Error("Error loggin in to Instagram. Please check username and password");
                //Console.WriteLine("Error logging in");
            }
        }

        public async void PullUserPosts(string username)
        {
            //IResult<InstaUserShortList> userFollowers = await api.GetUserFollowersAsync(username, PaginationParameters.MaxPagesToLoad(5));
            var task = api.GetUserFollowersAsync(username, PaginationParameters.MaxPagesToLoad(5));
            IResult<InstaUserShortList> userFollowers = task.GetAwaiter().GetResult();
            Thread.Sleep(2000);
            for (int i = 0; i < userFollowers.Value.Count; i++)
            {
                //Console.WriteLine($"\n\t{userFollowers.Value[i].UserName}");
                ScrapeUser(userFollowers.Value[i].UserName);
            }
        }

        public async void ScrapeUser(string username)
        {
            //followers.Add(username);
            //Console.WriteLine("############################## USER {0} ###################################", username);
            //IResult<InstaUser> userSearch = await api.GetUserAsync(username);
            var task = api.GetUserAsync(username);
            IResult<InstaUser> userSearch = task.GetAwaiter().GetResult();
            Thread.Sleep(2000);
            //Console.WriteLine($"USER: {userSearch.Value.FullName}\n\tUserName: {userSearch.Value.UserName}\n\tFollowers: {userSearch.Value.FollowersCount}\n\tVerified: {userSearch.Value.IsVerified}\n\tBio: {userSearch.Value.FollowersCount}");

            //IResult<InstaUserInfo> userInfo = await api.GetUserInfoByUsernameAsync(username);
            var task1 = api.GetUserInfoByUsernameAsync(username);
            IResult<InstaUserInfo> userInfo = task1.GetAwaiter().GetResult();
            Thread.Sleep(2000);
            //Console.WriteLine($"BioGraphy: {userInfo.Value.Biography}");
            string phone = "", eMail = "";
            foreach (var line in userInfo.Value.Biography.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                string phoneNumber = GetPhoneNumber(line);
                phone = phoneNumber;

                const string MatchEmailPattern =
@"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
+ @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
 + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
+ @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
                Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                MatchCollection matches = rx.Matches(line);
                int noOfMatches = matches.Count;

                foreach (Match match in matches)
                {
                    if(string.IsNullOrEmpty(eMail))
                    {
                        eMail = match.Value.ToString();
                    }
                    else
                    {
                        eMail += "," + match.Value.ToString();
                    }
                }
            }
            if (!string.IsNullOrEmpty(eMail))
            {
                User usr = new User(userSearch.Value.FullName, eMail, userSearch.Value.UserName, phone);
                users.Add(usr);
            }
        }

        public string GetPhoneNumber(string text)
        {
            int minimumDigits = 0;
            string phoneFinal = "";
            string phoneNumber = "";
            foreach (char c in text)
            {
                if (Char.IsNumber(c) || c == ' ' || c == '+')
                {
                    phoneNumber = phoneNumber + c;
                    minimumDigits++;
                }
                else
                {
                    if (minimumDigits >= 9)
                    {
                        // phone number is detected and then end of phone number
                        if (!string.IsNullOrEmpty(phoneFinal))
                        {
                            // append ',' after 1st phone number
                            phoneNumber = ',' + phoneNumber;
                        }

                        phoneFinal = phoneFinal + phoneNumber;
                        minimumDigits = 0; // reset counter
                    }
                    else
                    {
                        phoneNumber = "";
                    }
                    minimumDigits = 0;

                }
            }

            return phoneFinal;
        }
    }
}
