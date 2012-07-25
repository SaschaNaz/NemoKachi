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
    }
}
