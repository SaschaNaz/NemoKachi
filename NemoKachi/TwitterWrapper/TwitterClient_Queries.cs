using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemoKachi.TwitterWrapper
{
    //public class ClientLine
    //{
    //    public TwitterClient Client { get; private set; }
    //    public Uri RequestUrl { get; private set; }
    //    public ITwitterRequestQuery RequestQuery { get; private set; }
    //    public Uri StreamUrl { get; private set; }
    //    public ITwitterRequestQuery StreamQuery { get; private set; }
    //    public String DistinctName { get; private set; }

    //    public ClientLine(String distinctName, Uri requestUrl, ITwitterRequestQuery requestQuery, Uri streamUrl, ITwitterRequestQuery streamQuery)
    //    {
    //        DistinctName = distinctName;
    //        RequestUrl = requestUrl;
    //        RequestQuery = requestQuery;
    //        StreamUrl = streamUrl;
    //        StreamQuery = streamQuery;
    //    }
    //}
    //public class AccountLine : ITwitterColumnQuery
    //{
    //    public TwitterClient Client { get; set; }
    //    public String Type { get; set; } //"Timeline" or "Mentions"
    //    public Uri RequestUrl
    //    {
    //        get
    //        {
    //            if (Type == "Follow")
    //            {
    //                return new Uri("https://api.twitter.com/1/statuses/home_timeline.json");
    //            }
    //            else if (Type == "Mentions")
    //            {
    //                return new Uri("https://api.twitter.com/1/statuses/mentions.json");
    //            }
    //            else
    //            {
    //                return null;
    //            }
    //        }
    //    }
    //    public Uri StreamUrl
    //    {
    //        get { return new Uri("https://userstream.twitter.com/2/user.json"); }
    //    }

    //    public TwitterRequest MakeTwitterRequest()//type:옛 RefreshQuery는 새 UserTimelineQuery와 같다
    //    {
    //        return TwitterRequest.MakeRequest(
    //            new TwitterRequest.QueryKeyValue[]
    //            {
    //                new TwitterRequest.QueryKeyValue("include_entities", "true", TwitterRequest.RequestType.Type1),
    //                new TwitterRequest.QueryKeyValue("include_rts", "true", TwitterRequest.RequestType.Type1)
    //            });
    //    }
    //}

    //public struct UserTimelineQuery : ITwitterColumnQuery
    //{
    //    TwitterClient _Client;
    //    public TwitterClient Client
    //    {
    //        get { return _Client; }
    //        set { _Client = value; }
    //    }
    //    public String ScreenName;
    //    public Uri RequestUrl
    //    {
    //        get { return new Uri("http://api.twitter.com/1/statuses/user_timeline.json"); }
    //    }
    //    public Uri StreamUrl
    //    {
    //        get { return null; }//(...)
    //    }

    //    public ITwitterRequestQuery MakeTwitterRequest()
    //    {
    //        return TwitterRequest.MakeQuery(
    //            new TwitterRequest.QueryKeyValue[]
    //            {
    //                new TwitterRequest.QueryKeyValue("include_entities", "true", TwitterRequest.RequestType.Type1),
    //                new TwitterRequest.QueryKeyValue("include_rts", "true", TwitterRequest.RequestType.Type1)
    //            });
    //    }
    //}


    //메소드 모두에 적용한 뒤에 OAuth에서 ITwitterRequestQuery 직접 받도록 수정. typeof로 타입 인식할 수 있을 듯
    //GetQueries니 GetPostQueries니 다 필요없고 GetQueryString GetPostQueryString이면 한방에 끝나는걸 왜이러고있지....!!!!!!
    //if문으로 문자열에 더하고 더하고 하는것도 좋지만 지금 하는것처럼 리스트에 넣고 한방에 Join 돌리는 것이 더 괜찮을 듯하다.
    //GetQueryString의 경우 쿼리가 두 부분으로 나뉘므로 주의

    public class TwitterRequest
    {
        public enum RequestType
        {
            /// <summary>
            /// Takes place in front of the OAuth string.
            /// </summary>
            Type1,
            /// <summary>
            /// Takes place in rear of the OAuth string.
            /// </summary>
            Type2,
            /// <summary>
            /// The post query of the OAuth string.
            /// </summary>
            Post
        }
        public struct QueryKeyValue
        {
            public String Key;
            public Object Value;
            public RequestType Type;
            public QueryKeyValue(String key, Object value, RequestType type)
            {
                Key = key;
                Value = value;
                Type = type;
            }
        }
        //normal queries
        SortedDictionary<String, String> Query1;
        SortedDictionary<String, String> Query2;
        SortedDictionary<String, String> PostQuery;

        //posts

        public TwitterRequest(params QueryKeyValue[] keyvalues)
        {
            Query1 = new SortedDictionary<String, String>();
            Query2 = new SortedDictionary<String, String>();
            PostQuery = new SortedDictionary<String, String>();
            foreach (QueryKeyValue qkv in keyvalues)
            {
                switch (qkv.Type)
                {
                    case RequestType.Type1:
                        {
                            Query1.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                    case RequestType.Type2:
                        {
                            Query2.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                    case RequestType.Post:
                        {
                            PostQuery.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                }
            }
        }

        public void AddValues(params QueryKeyValue[] keyvalues)
        {
            foreach (QueryKeyValue qkv in keyvalues)
            {
                switch (qkv.Type)
                {
                    case RequestType.Type1:
                        {
                            Query1.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                    case RequestType.Type2:
                        {
                            Query2.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                    case RequestType.Post:
                        {
                            PostQuery.Add(qkv.Key, qkv.Value.ToString());
                            break;
                        }
                }
            }
        }

        public String GetQueryStringPart1()
        {
            List<String> returner = new List<String>();
            foreach (KeyValuePair<String, String> pair in Query1)
            {
                returner.Add(pair.Key + '=' + pair.Value);
            }
            return String.Join("&", returner);
        }

        public String GetQueryStringPart2()
        {
            List<String> returner = new List<String>();
            foreach (KeyValuePair<String, String> pair in Query2)
            {
                returner.Add(pair.Key + '=' + pair.Value);
            }
            return String.Join("&", returner);
        }

        public String GetQueryStringTotal()
        {
            SortedDictionary<String, String> dict = new SortedDictionary<String, String>();
            foreach (KeyValuePair<String, String> pair in Query1)
            {
                dict.Add(pair.Key, pair.Value);
            }
            foreach (KeyValuePair<String, String> pair in Query2)
            {
                dict.Add(pair.Key, pair.Value);
            }
            List<String> returner = new List<String>();
            foreach (KeyValuePair<String, String> pair in dict)
            {
                returner.Add(pair.Key + '=' + pair.Value);
            }
            return String.Join("&", returner);
        }

        public String GetPostQueryString()
        {
            List<String> returner = new List<String>();
            foreach (KeyValuePair<String, String> pair in PostQuery)
            {
                returner.Add(pair.Key + '=' + pair.Value);
            }
            return String.Join("&", returner);
        }
    }

    public class SendTweetRequest
    {
        //queries type 1
        public Boolean display_coordinates = true;
        public Nullable<UInt64> in_reply_to_status_id;
        public Boolean include_entities = true;
        public Nullable<UInt64> lattitude;
        public Nullable<UInt64> longitude;

        //posts
        public String place_id;
        public String status;

        //queries type 2
        public Boolean trim_user;

        public static implicit operator TwitterRequest(SendTweetRequest r)
        {
            List<TwitterRequest.QueryKeyValue> paramList = new List<TwitterRequest.QueryKeyValue>();
            #region querys type 1
            if (r.display_coordinates)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "display_coordinates",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            if (r.in_reply_to_status_id != null)
            {
                paramList.Add(
                    new TwitterRequest.QueryKeyValue(
                        "in_reply_to_status_id",
                        r.in_reply_to_status_id.Value.ToString(),
                        TwitterRequest.RequestType.Type1));
            }
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            if (r.lattitude != null)
            {
                paramList.Add(
                    new TwitterRequest.QueryKeyValue(
                        "lat",
                        r.lattitude.Value.ToString(),
                        TwitterRequest.RequestType.Type1));
            }
            if (r.longitude != null)
            {
                paramList.Add(
                    new TwitterRequest.QueryKeyValue(
                        "long",
                        r.longitude.Value.ToString(),
                        TwitterRequest.RequestType.Type1));
            }
            #endregion
            #region posts
            if (r.place_id != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "place_id",
                           r.place_id,
                           TwitterRequest.RequestType.Post));
            }
            if (r.status != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "status",
                           TwitterClient.AdditionalEscape(Uri.EscapeDataString(r.status)),
                           TwitterRequest.RequestType.Post));
            }
            #endregion
            #region querys type 2
            if (r.trim_user)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "trim_user",
                           "true",
                           TwitterRequest.RequestType.Type2));
            }
            #endregion

            return new TwitterRequest(paramList.ToArray());
        }
    }

    public class ShowTweetRequest
    {
        //queries type 1
        public Boolean include_entities = true;
        public Boolean include_my_retweet = true;

        //queries type 2
        public Boolean trim_user = false;

        public static implicit operator TwitterRequest(ShowTweetRequest r)
        {
            List<TwitterRequest.QueryKeyValue> paramList = new List<TwitterRequest.QueryKeyValue>();
            #region querys type 1
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            if (r.include_my_retweet)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "include_my_retweet",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.trim_user)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "trim_user",
                           "true",
                           TwitterRequest.RequestType.Type2));
            }
            #endregion

            return new TwitterRequest(paramList.ToArray());
        }
    }

    public class TwitterRequestException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; private set; }
        public Int32 ErrorNumber { get; private set; }

        public TwitterRequestException(System.Net.HttpStatusCode statuscode, Int32 errornumber, String message)
            : base(message)
        {
            StatusCode = statuscode;
            ErrorNumber = errornumber;
        }

        public static TwitterRequestException Parse(System.Net.HttpStatusCode errorcode, Windows.Data.Json.JsonObject errorobject)
        {
            return new TwitterRequestException(
                errorcode, (Int32)errorobject.GetNamedNumber("code"), errorobject.GetNamedString("message"));
        }
    }

    public class TwitterRequestProtectedException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; private set; }
        public String Request { get; private set; }

        public TwitterRequestProtectedException(System.Net.HttpStatusCode statuscode, String request, String message)
            : base(message)
        {
            StatusCode = statuscode;
            Request = request;
        }

        public static TwitterRequestProtectedException Parse(System.Net.HttpStatusCode errorcode, Windows.Data.Json.JsonObject errorobject)
        {
            return new TwitterRequestProtectedException(
                errorcode, errorobject.GetNamedString("request"), errorobject.GetNamedString("error"));
        }
    }

    public class LocalRefreshRequest
    {
        //queries type 1
        public Boolean include_entities = true;
        public Boolean include_rts = true;
        public Nullable<UInt64> max_id;

        //queries type 2
        public Nullable<UInt64> since_id;

        public static implicit operator TwitterRequest(LocalRefreshRequest r)
        {
            List<TwitterRequest.QueryKeyValue> paramList = new List<TwitterRequest.QueryKeyValue>();
            #region querys type 1
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            if (r.include_rts)
            {
                paramList.Add(
                    new TwitterRequest.QueryKeyValue(
                        "include_rts",
                        "true",
                        TwitterRequest.RequestType.Type1));
            }
            if (r.max_id != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterRequest.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.since_id != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterRequest.RequestType.Type2));
            }
            #endregion

            return new TwitterRequest(paramList.ToArray());
        }
    }

    public class SpecificUserRefreshRequest
    {
        //queries type 1
        public Boolean include_entities = true;
        public Boolean include_rts = true;
        public Nullable<UInt64> max_id;

        //queries type 2
        public String screen_name;
        public Nullable<UInt64> since_id;

        public static implicit operator TwitterRequest(SpecificUserRefreshRequest r)
        {
            List<TwitterRequest.QueryKeyValue> paramList = new List<TwitterRequest.QueryKeyValue>();
            #region querys type 1
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterRequest.RequestType.Type1));
            }
            if (r.include_rts)
            {
                paramList.Add(
                    new TwitterRequest.QueryKeyValue(
                        "include_rts",
                        "true",
                        TwitterRequest.RequestType.Type1));
            }
            if (r.max_id != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterRequest.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.screen_name != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "screen_name",
                           r.screen_name,
                           TwitterRequest.RequestType.Type2));
            }
            if (r.since_id != null)
            {
                paramList.Add(
                       new TwitterRequest.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterRequest.RequestType.Type2));
            }
            #endregion

            return new TwitterRequest(paramList.ToArray());
        }
    }

    public interface ITimelineData
    {
        Uri RestURI { get; }
        Uri StreamURI { get; }
        UInt64 AccountID { get; set; }
        TwitterRequest GetRequest();
        /// <summary>
        /// 마지막으로 불러들인 트윗 ID를 기억
        /// </summary>
        Nullable<UInt64> LoadedLastTweetID { get; set; }
        Nullable<UInt64> LoadedFirstGapTweetID { get; set; }
    }

    public class FollowingTweetsData : ITimelineData
    {
        public Uri RestURI
        {
            get { return new Uri("https://api.twitter.com/1/statuses/home_timeline.json"); }
        }
        public Uri StreamURI
        {
            get { return new Uri("https://userstream.twitter.com/2/user.json"); }
        }
        public UInt64 AccountID { get; set; }
        public LocalRefreshRequest RestOption { get; private set; }
        public Nullable<UInt64> LoadedLastTweetID
        {
            get { return RestOption.since_id + 1; }
            set { RestOption.since_id = value - 1; }
        }
        public Nullable<UInt64> LoadedFirstGapTweetID
        {
            get { return RestOption.max_id; }
            set { RestOption.max_id = value; }
        }

        public FollowingTweetsData(UInt64 accountID, LocalRefreshRequest restOption)
        {
            AccountID = accountID;
            RestOption = restOption;
        }

        public TwitterRequest GetRequest()
        {
            return RestOption;
        }
    }

    public class MentionTweetsData : ITimelineData
    {
        public Uri RestURI
        {
            get { return new Uri("https://api.twitter.com/1/statuses/mentions.json"); }
        }
        public Uri StreamURI
        {
            get { return new Uri("https://userstream.twitter.com/2/user.json"); }
        }
        public UInt64 AccountID { get; set; }
        public LocalRefreshRequest RestOption { get; private set; }
        public Nullable<UInt64> LoadedLastTweetID
        {
            get { return RestOption.since_id + 1; }
            set { RestOption.since_id = value - 1; }
        }
        public Nullable<UInt64> LoadedFirstGapTweetID
        {
            get { return RestOption.max_id; }
            set { RestOption.max_id = value; }
        }

        public MentionTweetsData(UInt64 accountID, LocalRefreshRequest restOption)
        {
            AccountID = accountID;
            RestOption = restOption;
        }

        public TwitterRequest GetRequest()
        {
            return RestOption;
        }
    }

    public class SpecificUserTweetsData : ITimelineData
    {
        public Uri RestURI
        {
            get { return new Uri("https://api.twitter.com/1/statuses/home_timeline.json"); }
        }
        public Uri StreamURI
        {
            get { return new Uri("https://userstream.twitter.com/2/user.json"); }//to be changed
        }
        public UInt64 AccountID { get; set; }
        public SpecificUserRefreshRequest RestOption { get; private set; }
        public Nullable<UInt64> LoadedLastTweetID
        {
            get { return RestOption.since_id + 1; }
            set { RestOption.since_id = value - 1; }
        }
        public Nullable<UInt64> LoadedFirstGapTweetID
        {
            get { return RestOption.max_id; }
            set { RestOption.max_id = value; }
        }

        public SpecificUserTweetsData(UInt64 accountID, SpecificUserRefreshRequest restOption)
        {
            AccountID = accountID;
            RestOption = restOption;
        }

        public TwitterRequest GetRequest()
        {
            return RestOption;
        }
    }

    //private enum RequestType
    //{
    //    Tweet = 0, Retweet, Destroy = 1, Refresh = 2
    //}

    //public static TweetColumnQuery TimelineUrl
    //{
    //    get
    //    {
    //        return new TweetColumnQuery()
    //        {
    //            twitterUrl = "https://api.twitter.com/1/statuses/home_timeline.json",
    //            queries = new RefreshQuery()
    //            {
    //                include_entities = true,
    //                include_rts = true,
    //            }
    //        };
    //    }
    //}
    //public static TweetColumnQuery MentionUrl
    //{
    //    get
    //    {
    //        return new TweetColumnQuery()
    //        {
    //            twitterUrl = "https://api.twitter.com/1/statuses/mentions.json",
    //            queries = new RefreshQuery()
    //            {
    //                include_entities = true,
    //                include_rts = true,
    //            }
    //        };
    //    }
    //}
    //public static TweetColumnQuery UserUrl(String screen_name)
    //{
    //    return new TweetColumnQuery()
    //    {
    //        twitterUrl = "http://api.twitter.com/1/statuses/user_timeline.json",
    //        queries = new RefreshQuery()
    //        {
    //            include_entities = true,
    //            include_rts = true,
    //            screen_name = screen_name
    //        }
    //        //new SortedDictionary<String, String>()
    //        //{
    //        //    { "include_entities", "true" },
    //        //    { "include_rts", "true" },
    //        //    { "screen_name", screen_name } 
    //        //}
    //    };
    //}

    //public struct TweetColumnQuery
    //{
    //    public String twitterUrl;
    //    public RefreshQuery queries;
    //}
    
}
