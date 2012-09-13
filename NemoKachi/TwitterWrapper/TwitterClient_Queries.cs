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
    //    public ITwitterParameterQuery RequestQuery { get; private set; }
    //    public Uri StreamUrl { get; private set; }
    //    public ITwitterParameterQuery StreamQuery { get; private set; }
    //    public String DistinctName { get; private set; }

    //    public ClientLine(String distinctName, Uri requestUrl, ITwitterParameterQuery requestQuery, Uri streamUrl, ITwitterParameterQuery streamQuery)
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

    //    public TwitterParameter MakeTwitterParameter()//type:옛 RefreshQuery는 새 UserTimelineQuery와 같다
    //    {
    //        return TwitterParameter.MakeRequest(
    //            new TwitterParameter.QueryKeyValue[]
    //            {
    //                new TwitterParameter.QueryKeyValue("include_entities", "true", TwitterParameter.RequestType.Type1),
    //                new TwitterParameter.QueryKeyValue("include_rts", "true", TwitterParameter.RequestType.Type1)
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

    //    public ITwitterParameterQuery MakeTwitterParameter()
    //    {
    //        return TwitterParameter.MakeQuery(
    //            new TwitterParameter.QueryKeyValue[]
    //            {
    //                new TwitterParameter.QueryKeyValue("include_entities", "true", TwitterParameter.RequestType.Type1),
    //                new TwitterParameter.QueryKeyValue("include_rts", "true", TwitterParameter.RequestType.Type1)
    //            });
    //    }
    //}


    //메소드 모두에 적용한 뒤에 OAuth에서 ITwitterParameterQuery 직접 받도록 수정. typeof로 타입 인식할 수 있을 듯
    //GetQueries니 GetPostQueries니 다 필요없고 GetQueryString GetPostQueryString이면 한방에 끝나는걸 왜이러고있지....!!!!!!
    //if문으로 문자열에 더하고 더하고 하는것도 좋지만 지금 하는것처럼 리스트에 넣고 한방에 Join 돌리는 것이 더 괜찮을 듯하다.
    //GetQueryString의 경우 쿼리가 두 부분으로 나뉘므로 주의

    public class TwitterParameter
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

        public TwitterParameter(params QueryKeyValue[] keyvalues)
        {
            Query1 = new SortedDictionary<String, String>();
            Query2 = new SortedDictionary<String, String>();
            PostQuery = new SortedDictionary<String, String>();
            AddValues(keyvalues);
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

        public void MergeGetStatusParameter(GetStatusParameter getstatus)
        {
            if (!getstatus.include_entities)
            {
                AddValues(
                       new TwitterParameter.QueryKeyValue(
                           "include_entities",
                           "false",
                           TwitterParameter.RequestType.Type1));
            }
            if (getstatus.trim_user)
            {
                AddValues(
                       new TwitterParameter.QueryKeyValue(
                           "trim_user",
                           "true",
                           TwitterParameter.RequestType.Type2));
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

    public class GetStatusParameter
    {
        public Boolean include_entities = true;
        public Boolean trim_user;
    }

    public class StatusesUpdateParameter
    {
        //queries type 1
        public Boolean display_coordinates = true;
        public Nullable<UInt64> in_reply_to_status_id;
        public Nullable<UInt64> lattitude;
        public Nullable<UInt64> longitude;

        //posts
        public String place_id;
        public String status;

        public static implicit operator TwitterParameter(StatusesUpdateParameter r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            if (r.status == null)
                throw new InvalidTwitterAPIParameterException(r.GetType(), "status");

            #region querys type 1
            if (r.display_coordinates)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "display_coordinates",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.in_reply_to_status_id != null)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "in_reply_to_status_id",
                        r.in_reply_to_status_id.Value.ToString(),
                        TwitterParameter.RequestType.Type1));
            }
            if (r.lattitude != null)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "lat",
                        r.lattitude.Value.ToString(),
                        TwitterParameter.RequestType.Type1));
            }
            if (r.longitude != null)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "long",
                        r.longitude.Value.ToString(),
                        TwitterParameter.RequestType.Type1));
            }
            #endregion
            #region posts
            if (r.place_id != null)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "place_id",
                        r.place_id,
                        TwitterParameter.RequestType.Post));
            }
            paramList.Add(
                new TwitterParameter.QueryKeyValue(
                    "status",
                    TwitterClient.AdditionalEscape(Uri.EscapeDataString(r.status)),
                    TwitterParameter.RequestType.Post));
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public class StatusesShowParameter
    {
        //queries type 1
        public Boolean include_my_retweet = true;

        public static implicit operator TwitterParameter(StatusesShowParameter r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            #region querys type 1
            if (r.include_my_retweet)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "include_my_retweet",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public class UsersShowParameter
    {
        //queries type 2
        /// <summary>
        /// Screen name. If you specify it then you should not specify user_id parameter.
        /// </summary>
        public String screen_name;
        /// <summary>
        /// User ID. If you specify it then you should not specify screen_name parameter.
        /// </summary>
        public Nullable<UInt64> user_id;

        public static implicit operator TwitterParameter(UsersShowParameter r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();

            if (r.screen_name == null && !r.user_id.HasValue)
                throw new InvalidTwitterAPIParameterException(r.GetType(), "screen_name", "user_id");

            #region querys type 2
            if (r.screen_name != null)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "screen_name",
                        r.screen_name,
                        TwitterParameter.RequestType.Type2));
            }
            else if (r.user_id.HasValue)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "user_id",
                        r.user_id.Value.ToString(),
                        TwitterParameter.RequestType.Type2));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public class StatusesHometimelineParameter
    {
        //queries type 1
        public Boolean contributor_details;
        public Nullable<Int32> count;
        public Boolean exclude_replies;
        public Nullable<UInt64> max_id;

        //queries type 2
        public Nullable<UInt64> since_id;

        public static implicit operator TwitterParameter(StatusesHometimelineParameter r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            #region querys type 1
            if (r.contributor_details)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "contributor_details",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.count.HasValue)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "count",
                           r.count.Value,
                           TwitterParameter.RequestType.Type1));
            }
            if (r.exclude_replies)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "exclude_replies",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.max_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterParameter.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.since_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterParameter.RequestType.Type2));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public class StatusesMentionsParameter
    {
        //queries type 1
        public Boolean contributor_details;
        public Nullable<Int32> count;
        public Nullable<UInt64> max_id;

        //queries type 2
        public Nullable<UInt64> since_id;

        public static implicit operator TwitterParameter(StatusesMentionsParameter r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            #region querys type 1
            if (r.contributor_details)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "contributor_details",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.count.HasValue)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "count",
                           r.count.Value,
                           TwitterParameter.RequestType.Type1));
            }
            if (r.max_id.HasValue)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterParameter.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.since_id.HasValue)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterParameter.RequestType.Type2));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public class InvalidTwitterAPIParameterException : Exception
    {
        public Type ParameterType;
        /// <summary>
        /// The attributes that must be specified at least one.
        /// </summary>
        public String[] AttributeNames;
        public InvalidTwitterAPIParameterException(Type parameter, params String[] attribute) : base()
        {
            ParameterType = parameter;
            AttributeNames = attribute;
        }
    }

    public class TwitterParameterException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; private set; }
        public Int32 ErrorNumber { get; private set; }

        public TwitterParameterException(System.Net.HttpStatusCode statuscode, Int32 errornumber, String message)
            : base(message)
        {
            StatusCode = statuscode;
            ErrorNumber = errornumber;
        }

        public static TwitterParameterException Parse(System.Net.HttpStatusCode errorcode, Windows.Data.Json.JsonObject errorobject)
        {
            return new TwitterParameterException(
                errorcode, (Int32)errorobject.GetNamedNumber("code"), errorobject.GetNamedString("message"));
        }
    }

    public class TwitterParameterStringException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; private set; }

        public TwitterParameterStringException(System.Net.HttpStatusCode statuscode, String message)
            : base(message)
        {
            StatusCode = statuscode;
        }
    }

    public class TwitterParameterProtectedException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; private set; }
        public String Request { get; private set; }

        public TwitterParameterProtectedException(System.Net.HttpStatusCode statuscode, String request, String message)
            : base(message)
        {
            StatusCode = statuscode;
            Request = request;
        }

        public static TwitterParameterProtectedException Parse(System.Net.HttpStatusCode errorcode, Windows.Data.Json.JsonObject errorobject)
        {
            return new TwitterParameterProtectedException(
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

        public static implicit operator TwitterParameter(LocalRefreshRequest r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            #region querys type 1
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterParameter.RequestType.Type1));
            }
            if (r.include_rts)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "include_rts",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.max_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterParameter.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.since_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterParameter.RequestType.Type2));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
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

        public static implicit operator TwitterParameter(SpecificUserRefreshRequest r)
        {
            List<TwitterParameter.QueryKeyValue> paramList = new List<TwitterParameter.QueryKeyValue>();
            #region querys type 1
            if (r.include_entities)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "include_entities",
                           "true",
                           TwitterParameter.RequestType.Type1));
            }
            if (r.include_rts)
            {
                paramList.Add(
                    new TwitterParameter.QueryKeyValue(
                        "include_rts",
                        "true",
                        TwitterParameter.RequestType.Type1));
            }
            if (r.max_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "max_id",
                           r.max_id.Value,
                           TwitterParameter.RequestType.Type1));
            }
            #endregion
            #region querys type 2
            if (r.screen_name != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "screen_name",
                           r.screen_name,
                           TwitterParameter.RequestType.Type2));
            }
            if (r.since_id != null)
            {
                paramList.Add(
                       new TwitterParameter.QueryKeyValue(
                           "since_id",
                           r.since_id.Value,
                           TwitterParameter.RequestType.Type2));
            }
            #endregion

            return new TwitterParameter(paramList.ToArray());
        }
    }

    public interface ITimelineData
    {
        Uri RestURI { get; }
        Uri StreamURI { get; }
        UInt64 AccountID { get; set; }
        TwitterParameter GetRequest();
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

        public TwitterParameter GetRequest()
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

        public TwitterParameter GetRequest()
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

        public TwitterParameter GetRequest()
        {
            return RestOption;
        }
    }
}
