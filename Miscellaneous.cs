using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrapetelligence
{
    class User
    {
        public string name;

        public string eMail;

        public string userName;

        public string phoneNumber;

        public string countryCode;

        public User(string Name, string EMail, string UserName, string PhoneNumber, string CountryCode = "")
        {
            name = Name;
            eMail = EMail;
            userName = UserName;
            phoneNumber = PhoneNumber;
            countryCode = CountryCode;
        }
    }

    class ScrapeDetails
    {
        public Miscellaneous.Type type;

        public string url;

        public string userName;

        public string password;

        public string location;

        public ScrapeDetails() { }

        public ScrapeDetails(Miscellaneous.Type t, string user, string un, string pass, string loc = "")
        {
            type = t;
            url = user;
            userName = un;
            password = pass;
            location = loc;
        }
    }
    static class Miscellaneous
    {
        public enum Type
        {
            TYPE_INSTAGRAM_FOLLOWERS,
            TYPE_INSTAGRAM_HASHTAG_FOLLOWERS,
            TYPE_FACEBOOK_GROUP_FOLLOWERS,
            TYPE_FACEBOOK_HASHTAG_FOLLOWERS,
            TYPE_TWITTER_KEYWORD,
            TYPE_YELP,
            TYPE_GOOGLE,
            TYPE_LINKEDIN_HASHTAG_FOLLOWERS
        }
    }
}
