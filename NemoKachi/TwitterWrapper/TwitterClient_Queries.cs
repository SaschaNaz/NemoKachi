using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemoKachi.TwitterWrapper
{
    public partial class TwitterClient
    {
        public class ClientLine
        {
            public TwitterClient Client { get; private set; }
            public Uri RequestUrl { get; private set; }
            public ITwitterRequestQuery RequestQuery { get; private set; }
            public Uri StreamUrl { get; private set; }
            public ITwitterRequestQuery StreamQuery { get; private set; }
            public String DistinctName { get; private set; }
            
            public ClientLine(String distinctName, Uri requestUrl, ITwitterRequestQuery requestQuery, Uri streamUrl, ITwitterRequestQuery streamQuery)
            {
                DistinctName = distinctName;
                RequestUrl = requestUrl;
                RequestQuery = requestQuery;
                StreamUrl = streamUrl;
                StreamQuery = streamQuery;
            }
        }
        public class AccountLine : ITwitterColumnQuery
        {
            public TwitterClient Client { get; set; }
            public String Type { get; set; } //"Timeline" or "Mentions"
            public Uri RequestUrl
            {
                get
                {
                    if (Type == "Follow")
                    {
                        return new Uri("https://api.twitter.com/1/statuses/home_timeline.json");
                    }
                    else if (Type == "Mentions")
                    {
                        return new Uri("https://api.twitter.com/1/statuses/mentions.json");
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            public Uri StreamUrl
            {
                get { return new Uri("https://userstream.twitter.com/2/user.json"); }
            }

            public ITwitterRequestQuery GetRequestQuery()//type:옛 RefreshQuery는 새 UserTimelineQuery와 같다
            {
                return NormalQuery.MakeQuery(
                    new NormalQuery.QueryKeyValue[]
                    {
                        new NormalQuery.QueryKeyValue("include_entities", "true", NormalQuery.QueryType.Type1),
                        new NormalQuery.QueryKeyValue("include_rts", "true", NormalQuery.QueryType.Type1)
                    });
            }
        }

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

        //    public ITwitterRequestQuery GetRequestQuery()
        //    {
        //        return NormalQuery.MakeQuery(
        //            new NormalQuery.QueryKeyValue[]
        //            {
        //                new NormalQuery.QueryKeyValue("include_entities", "true", NormalQuery.QueryType.Type1),
        //                new NormalQuery.QueryKeyValue("include_rts", "true", NormalQuery.QueryType.Type1)
        //            });
        //    }
        //}


        //메소드 모두에 적용한 뒤에 OAuth에서 ITwitterRequestQuery 직접 받도록 수정. typeof로 타입 인식할 수 있을 듯
        //GetQueries니 GetPostQueries니 다 필요없고 GetQueryString GetPostQueryString이면 한방에 끝나는걸 왜이러고있지....!!!!!!
        //if문으로 문자열에 더하고 더하고 하는것도 좋지만 지금 하는것처럼 리스트에 넣고 한방에 Join 돌리는 것이 더 괜찮을 듯하다.
        //GetQueryString의 경우 쿼리가 두 부분으로 나뉘므로 주의

        public struct NormalQuery : ITwitterRequestQuery
        {
            public enum QueryType
            {
                Type1, Type2, Post
            }
            public struct QueryKeyValue
            {
                public String Key;
                public Object Value;
                public QueryType Type;
                public QueryKeyValue(String key, Object value, QueryType type)
                {
                    Key = key;
                    Value = value;
                    Type = type;
                }
            }
            //normal queries
            KeyValuePair<String, String>[] Query1;
            KeyValuePair<String, String>[] Query2;
            KeyValuePair<String, String>[] PostQuery;

            //posts

            public static NormalQuery MakeQuery(params QueryKeyValue[] keyvalues)
            {
                NormalQuery nquery = new NormalQuery();
                SortedDictionary<String, String> query1 = new SortedDictionary<String, String>();
                SortedDictionary<String, String> query2 = new SortedDictionary<String, String>();
                SortedDictionary<String, String> postquery = new SortedDictionary<String, String>();
                foreach (QueryKeyValue qkv in keyvalues)
                {
                    switch (qkv.Type)
                    {
                        case QueryType.Type1:
                            {
                                query1.Add(qkv.Key, qkv.Value.ToString());
                                break;
                            }
                        case QueryType.Type2:
                            {
                                query2.Add(qkv.Key, qkv.Value.ToString());
                                break;
                            }
                        case QueryType.Post:
                            {
                                postquery.Add(qkv.Key, qkv.Value.ToString());
                                break;
                            }
                    }
                }
                nquery.Query1 = query1.ToArray();
                nquery.Query2 = query2.ToArray();
                nquery.PostQuery = postquery.ToArray();
                return nquery;
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

        public struct SendTweetQuery : ITwitterRequestQuery
        {
            //normal queries
            public Boolean include_entities;
            public Nullable<UInt64> in_reply_to_status_id;

            //posts
            public String status;

            public String GetQueryStringPart1()
            {
                List<String> returnList = new List<String>();
                if (in_reply_to_status_id != null)
                {
                    returnList.Add("in_reply_to_status_id=" + in_reply_to_status_id.Value.ToString());
                }
                if (include_entities)
                {
                    returnList.Add("include_entities=true");
                }

                return String.Join("&", returnList);
            }

            public String GetQueryStringPart2()
            {
                return "";//&랑 concat하면 흐에에되잖아...null이 나을듯
            }

            public String GetQueryStringTotal()
            {
                List<String> returnList = new List<String>();
                if (in_reply_to_status_id != null)
                {
                    returnList.Add("in_reply_to_status_id=" + in_reply_to_status_id.Value.ToString());
                }
                if (include_entities)
                {
                    returnList.Add("include_entities=true");
                }

                return String.Join("&", returnList);
            }

            public String GetPostQueryString()
            {
                if (status != null)
                {
                    return "status=" + AdditionalEscape(Uri.EscapeDataString(status));
                }
                else
                {
                    throw new Exception("There is no status message. Status message must be in this query.");
                }
            }
        }

        public class RefreshQuery : ITwitterRequestQuery
        {
            //normal queries
            public Boolean include_entities;
            public Nullable<UInt64> since_id;
            public Boolean include_rts;
            public String screen_name;

            //posts
            public String GetQueryStringPart1()
            {
                List<String> returnList = new List<String>();
                if (include_entities)//Boolean은 False일 땐 안 넣으니 Nullable 빼고 false일 때 안 넣기만 하면 될듯. 값은 무조건 true 넣게 하고
                {
                    returnList.Add("include_entities=true");
                }
                if (include_rts)
                {
                    returnList.Add("include_rts=true");
                }

                return String.Join("&", returnList);
            }

            public String GetQueryStringPart2()
            {
                List<String> returnList = new List<String>();
                if (screen_name != null)
                {
                    returnList.Add("screen_name=" + screen_name.ToString());
                }
                if (since_id != null)
                {
                    returnList.Add("since_id=" + since_id.Value);
                }

                return String.Join("&", returnList);
            }

            public String GetQueryStringTotal()
            {
                List<String> returnList = new List<String>();
                if (include_entities)
                {
                    returnList.Add("include_entities=true");
                }
                if (include_rts)
                {
                    returnList.Add("include_rts=true");
                }
                if (screen_name != null)
                {
                    returnList.Add("screen_name=" + screen_name.ToString());
                }
                if (since_id != null)
                {
                    returnList.Add("since_id=" + since_id.Value);
                }

                return String.Join("&", returnList);
            }

            public String GetPostQueryString()
            {
                return "";
            }
        }

        private enum RequestType
        {
            Tweet = 0, Retweet, Destroy = 1, Refresh = 2
        }

        public static TweetColumnQuery TimelineUrl
        {
            get
            {
                return new TweetColumnQuery()
                {
                    twitterUrl = "https://api.twitter.com/1/statuses/home_timeline.json",
                    queries = new RefreshQuery()
                    {
                        include_entities = true,
                        include_rts = true,
                    }
                };
            }
        }
        public static TweetColumnQuery MentionUrl
        {
            get
            {
                return new TweetColumnQuery()
                {
                    twitterUrl = "https://api.twitter.com/1/statuses/mentions.json",
                    queries = new RefreshQuery()
                    {
                        include_entities = true,
                        include_rts = true,
                    }
                };
            }
        }
        public static TweetColumnQuery UserUrl(String screen_name)
        {
            return new TweetColumnQuery()
            {
                twitterUrl = "http://api.twitter.com/1/statuses/user_timeline.json",
                queries = new RefreshQuery()
                {
                    include_entities = true,
                    include_rts = true,
                    screen_name = screen_name
                }
                //new SortedDictionary<String, String>()
                //{
                //    { "include_entities", "true" },
                //    { "include_rts", "true" },
                //    { "screen_name", screen_name } 
                //}
            };
        }

        public struct TweetColumnQuery
        {
            public String twitterUrl;
            public RefreshQuery queries;
        }
    }
}
