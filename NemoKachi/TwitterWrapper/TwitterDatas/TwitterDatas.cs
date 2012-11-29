using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NemoKachi.TwitterWrapper.TwitterDatas
{
    public interface ITimeAttached
    {
        //String JsonMessage { get; }
        DateTime GetPublishedTime();
    }

    //...이거 쓰지 말고 그냥 각각 파싱할 때 에러처리 해 줘도 되는데... 어차피 이거 써도 에러처리 해야 되고 
    //public class TwitterDataParseException : Exception
    //{
    //    public JsonObject JsonData { get; private set; }

    //    public TwitterDataParseException(JsonObject data)
    //        : base()
    //    {
    //        JsonData = data;
    //    }
    //}

    //public struct KnownDeleteEvent//이걸로 여러 개 만든 뒤에 TextMessage constructor 여러 개 만들기
    //{
    //    public readonly TwitterUser User;
    //    public readonly String Status;
    //    public KnownDeleteEvent(Tweet twt)
    //    {
    //        User = twt.GetUser();
    //        Status = twt.GetText();
    //    }
    //}

    //public struct DeleteEvent
    //{
    //    public readonly UInt64 UserId;//이거갖고 팔로우 리스트에서 찾으면 쓸모가 있을듯. 팔로우에 없는 것들이야 뭐...몰라
    //    public readonly UInt64 StatusId;
    //    public DeleteEvent(JsonObject jsob)
    //    {
    //        UserId = Convert.ToUInt64(jsob.GetNamedString("user_id_str"));
    //        StatusId = Convert.ToUInt64(jsob.GetNamedString("id_str"));
    //    }
    //}

    //public struct FavoriteEvent
    //{
    //    public enum EventType
    //    {
    //        Favorited, Unfavorited
    //    }
    //    public readonly EventType Type;
    //    public readonly TwitterUser OfUser;
    //    public readonly String Status;
    //    public readonly TwitterUser FromUser;
    //    public readonly DateTime EventCreated;
    //    public FavoriteEvent(JsonObject jsob, EventType type)
    //    {
    //        Type = type;
    //        FromUser = new TwitterUser(jsob.GetNamedObject("source"));
    //        Tweet twt = new Tweet(jsob.GetNamedObject("target_object"));
    //        OfUser = twt.GetUser();
    //        Status = twt.GetText();
    //        EventCreated = TwitterClient.ConvertToDateTime(jsob.GetNamedString("created_at"));
    //    }//이거 다 만들고 AddStreamedData에 집어넣기. 으앙
    //}

    //public struct UserEvent
    //{
    //    public enum EventType
    //    {
    //        Follow, Block
    //    }
    //    public readonly EventType Type;
    //    public readonly TwitterUser TargetUser;
    //    public readonly TwitterUser FromUser;
    //    public readonly DateTime EventCreated;
    //    public UserEvent(JsonObject jsob, EventType type)
    //    {
    //        Type = type;
    //        TargetUser = new TwitterUser(jsob.GetNamedObject("target"));
    //        FromUser = new TwitterUser(jsob.GetNamedObject("source"));
    //        EventCreated = TwitterClient.ConvertToDateTime(jsob.GetNamedString("created_at"));
    //    }
    //}

    //public struct TextEvent
    //{
    //    public String Title;
    //    public String Content;
    //    public Windows.UI.Color TextColor;
    //}

    public class Tweet// : ITimeAttached
    {
        //readonly JsonObject JsonData;
        //public String JsonMessage
        //{
        //    get { return JsonData.Stringify(); }
        //}

        public TwitterUser User { get; private set; }
        public Tweet RetweetedStatus { get; private set; }
        public TwitterEntities AttachedEntities { get; private set; }
        public String Text { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public UInt64 Id { get; private set; }
        public String Source { get; private set; }
        public Nullable<UInt64> ReplyId { get; private set; }

        public Tweet(JsonObject jo)
        {
            User = new TwitterUser(jo.GetNamedObject("user"));
            {
                IJsonValue value;
                if (jo.TryGetValue("retweeted_status", out value))
                {
                    RetweetedStatus = new Tweet(value.GetObject());
                }
            }
            AttachedEntities = new TwitterEntities(jo.GetNamedObject("entities"));
            Text = Windows.Data.Html.HtmlUtilities.ConvertToText(Uri.UnescapeDataString(jo.GetNamedString("text")));
            CreatedAt = TwitterClient.ConvertToDateTime(jo.GetNamedString("created_at"));
            Id = Convert.ToUInt64(jo.GetNamedString("id_str"));
            Source = GetClientName(jo.GetNamedString("source"));
            {
                IJsonValue value;
                if (jo.TryGetValue("in_reply_to_user_id_str", out value) && value.ValueType == JsonValueType.String)
                {
                    ReplyId = GetReplyId(value.GetString());
                }
            }
        }

        public String GetClientName(String ParsedString)
        {
            if (ParsedString[0] == '<')//The required conditon to check if it can be parsed.
            {
                System.Xml.Linq.XElement xelm = System.Xml.Linq.XElement.Parse(ParsedString);
                return xelm.Value + " : " + xelm.Attribute(System.Xml.Linq.XName.Get("href")).Value;
            }
            else
            {
                return ParsedString;
            }
        }
        public Nullable<UInt64> GetReplyId(String ParsedString)
        {
            try { return Convert.ToUInt64(ParsedString); }
            catch { return null; }
        }

        //static UIElement embeddedMedia(Uri uri)
        //{
        //    switch (uri.Host)
        //    {
        //        case "yfrog.com":
        //            {
        //                return new TextBlock() { Text = "yfrog image" };
        //            }
        //        case "twitpic.com":
        //            {
        //                return new TextBlock() { Text = "twitpic image" };
        //            }
        //        case "lockerz.com":
        //            {
        //                return new TextBlock() { Text = "lockerz image" };
        //            }
        //        case "youtube.com":
        //            {
        //                return new TextBlock() { Text = "youtube video" };
        //            }
        //        //case "twitter.com":
        //        //    {
        //        //        uri.
        //        //    }
        //        //twitter.com? 아, 여기서 twitter.com이면 그냥 트윗 링크고 미디어 링크는 다른 패러미터로 온다
        //        default: return null;
        //    }
        //}

    }

    public struct TwitterEntities //https://dev.twitter.com/docs/tweet-TwitterEntities
    {
        public readonly Hashtag[] Hashtags;
        public readonly UserMention[] UserMentions;
        public readonly Url[] Urls;
        public readonly TweetMedia[] Media;

        public TwitterEntities(JsonObject jsob)
        {
            JsonArray jary = jsob.GetNamedArray("hashtags");
            Hashtags = new Hashtag[jary.Count];
            for (Int32 i = 0; i < Hashtags.Length; i++)
            {
                Hashtags[i] = new Hashtag(jary[i].GetObject());
            }
            jary = jsob.GetNamedArray("user_mentions");
            UserMentions = new UserMention[jary.Count];
            for (Int32 i = 0; i < UserMentions.Length; i++)
            {
                UserMentions[i] = new UserMention(jary[i].GetObject());
            }
            jary = jsob.GetNamedArray("urls");
            Urls = new Url[jary.Count];
            for (Int32 i = 0; i < Urls.Length; i++)
            {
                Urls[i] = new Url(jary[i].GetObject());
            }

            {
                IJsonValue value;
                if (jsob.TryGetValue("media", out value))
                {
                    Media = new TweetMedia[value.GetArray().Count];
                    for (Int32 i = 0; i < Media.Length; i++)
                    {
                        Media[i] = new TweetMedia(value.GetArray()[i].GetObject());
                    }
                }
                else
                {
                    Media = null;
                }
            }
        }
    }

    public struct Hashtag
    {
        public readonly Int32[] Indices;
        public readonly String Text;

        public Hashtag(JsonObject jsob)
        {
            Indices = new Int32[2];
            Text = jsob.GetNamedString("text");
            JsonArray jary = jsob.GetNamedArray("indices");
            for (Int32 i = 0; i < 2; i++)
            {
                Indices[i] = (Int32)jary[i].GetNumber();
            }
        }
    }

    public struct UserMention
    {
        public readonly Int32[] Indices;
        public readonly String Name;
        public readonly String ScreenName;
        public readonly UInt64 Id;

        public UserMention(JsonObject jsob)
        {
            Indices = new Int32[2];
            Id = Convert.ToUInt64(jsob.GetNamedString("id_str"));
            Name = Uri.UnescapeDataString(jsob.GetNamedString("name"));
            ScreenName = jsob.GetNamedString("screen_name");
            JsonArray jary = jsob.GetNamedArray("indices");
            for (Int32 i = 0; i < 2; i++)
            {
                Indices[i] = (Int32)jary[i].GetNumber();
            }
        }
    }

    public struct TweetMedia
    {
        public enum MediaType
        {
            Photo, Unknown
        }
        public readonly MediaType Type;
        public readonly String ExpandedUrl;
        //public readonly String Url;
        public readonly String MediaUrl;//Use HTTPS :)
        public readonly UInt64 Id;
        public readonly Int32[] Indices;
        public readonly String DisplayUrl;
        public readonly TweetMediaSizes MediaSizes;

        public TweetMedia(JsonObject jsob)
        {
            Indices = new Int32[2];
            {
                String type = jsob.GetNamedString("type");
                switch (type)
                {
                    case "photo":
                        {
                            Type = MediaType.Photo;
                            break;
                        }
                    default:
                        {
                            Type = MediaType.Unknown;
                            break;
                        }
                }
            }
            ExpandedUrl = jsob.GetNamedString("expanded_url");
            MediaUrl = jsob.GetNamedString("media_url_https");
            DisplayUrl = jsob.GetNamedString("display_url");
            Id = Convert.ToUInt64(jsob.GetNamedString("id_str"));
            MediaSizes = new TweetMediaSizes(jsob.GetNamedObject("sizes"));
            JsonArray jary = jsob.GetNamedArray("indices");
            for (Int32 i = 0; i < 2; i++)
            {
                Indices[i] = (Int32)jary[i].GetNumber();
            }
        }

        public struct TweetMediaSizes
        {
            public readonly MediaSize Small;
            public readonly MediaSize Medium;
            public readonly MediaSize Large;
            public readonly MediaSize Thumb;

            public TweetMediaSizes(JsonObject jsob)
            {
                Small = new MediaSize(jsob.GetNamedObject("small"));
                Medium = new MediaSize(jsob.GetNamedObject("medium"));
                Large = new MediaSize(jsob.GetNamedObject("large"));
                Thumb = new MediaSize(jsob.GetNamedObject("thumb"));
            }
        }

        public struct MediaSize
        {
            public enum ResizeStatus
            {
                Fit, Crop
            }
            public readonly ResizeStatus SizeState;
            public readonly Windows.Foundation.Size MediumSize;

            public MediaSize(JsonObject jsob)
            {
                MediumSize = new Windows.Foundation.Size(jsob.GetNamedNumber("w"), jsob.GetNamedNumber("h"));
                {
                    String state = jsob.GetNamedString("resize");
                    switch (state)
                    {
                        case "fit":
                            {
                                SizeState = ResizeStatus.Fit;
                                break;
                            }
                        case "crop":
                            {
                                SizeState = ResizeStatus.Crop;
                                break;
                            }
                        default:
                            {
                                throw new Exception("Cannot recogsize the resize status of medium.");
                            }
                    }
                }
            }
        }
    }
    public struct Url
    {
        public readonly Int32[] Indices;
        public readonly String LinkUrl;
        public readonly String DisplayUrl;
        public readonly String ExpandedUrl;
        public Url(JsonObject jsob)
        {
            Indices = new Int32[2];
            try
            {
                DisplayUrl = jsob.GetNamedString("display_url");
                ExpandedUrl = jsob.GetNamedString("expanded_url");
                LinkUrl = null;
            }
            catch //before-tco tweets have no DisplayUrl or ExpandedUrl
            {
                DisplayUrl = null;
                ExpandedUrl = null;
                LinkUrl = jsob.GetNamedString("url");
            }
            JsonArray jary = jsob.GetNamedArray("indices");
            for (Int32 i = 0; i < 2; i++)
            {
                Indices[i] = (Int32)jary[i].GetNumber();
            }
        }
    }

    //https://dev.twitter.com/docs/platform-objects/users
    public class TwitterUser
    {
        public TwitterUser(JsonObject jo)
        {
            CreatedAt = TwitterClient.ConvertToDateTime(jo.GetNamedString("created_at"));
            FavouritesCount = (UInt32)jo.GetNamedNumber("favourites_count");
            Id = Convert.ToUInt64(jo.GetNamedString("id_str"));
            Name = jo.GetNamedString("name");
            ProfileImageUrl = new Uri(jo.GetNamedString("profile_image_url_https"));
            ScreenName = jo.GetNamedString("screen_name");
        }

        //public Boolean IsContributorsEnabled;
        public DateTime CreatedAt { get; set; }
        //public Boolean IsDefaultProfile;
        //public Boolean IsDefaultProfileImage;
        //public String Description;
        //public TwitterEntities AttachedEntities { get; private set; }
        public UInt32 FavouritesCount { get; set; }
        //public Nullable<Boolean> IsFollowRequestSent;
        //public UInt32 FollowersCount;
        //public UInt32 FriendsCount;
        //public Boolean IsGeoEnabled;
        public UInt64 Id { get; set; }
        //public Boolean IsTranslator;
        //public String Language;
        //public UInt32 ListedCount;
        //public String Location;
        public String Name { get; set; }
        //public Windows.UI.Xaml.Media.Color ProfileBackgroundColor;
        //public Uri ProfileBackgroundImageUrl;//plz get HTTPS uri
        //public Boolean IsProfileBackgroundTile;
        //public String ProfileBannerUrl;
        public Uri ProfileImageUrl { get; set; }//HTTPS too
        //public Windows.UI.Xaml.Media.Color ProfileLinkColor;
        //public Windows.UI.Xaml.Media.Color ProfileSidebarBorderColor;
        //public Windows.UI.Xaml.Media.Color ProfileSidebarFillColor;
        //public Windows.UI.Xaml.Media.Color ProfileTextColor;
        //public Boolean IsProfileUseBackgroundImage;
        //public Boolean IsProtected;
        public String ScreenName { get; set; }
        //public Boolean ShowAllInlineMedia;
        //public Tweet Status;
        //public UInt32 StatusesCount;
        //public String TimeZone;
        //public Uri Url;
        //public Int32 UtcOffset;
        //public Boolean IsVerified;
        //public String[] WithheldInCountries;
        //public TwitterScope WithheldScope;//TwitterScope is 'status' or 'user'
    }
}
