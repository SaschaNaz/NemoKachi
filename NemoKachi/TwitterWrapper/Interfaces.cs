using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemoKachi.TwitterWrapper.TwitterDatas;

namespace NemoKachi.TwitterWrapper
{
    //public enum TweetState
    //{
    //    Normal, Retweeted, Favorited, Deleted
    //}

    public interface ITwitterStreamer : IDisposable
    {
        Boolean IsActivated { get; }
        void AddTweetInStream(Tweet twt);
        Task ActivateAsync(AccountToken aToken);
        ITwitterColumnQuery ColumnQuery { get; }
    }

    public interface ITwitterViewer // for TweetView in NemoKachi, for code independency of TwitterClient class
    {
        List<ITwitterColumnQuery> ColumnQueries { get; } //쿼리 삭제할 때 쿼리로 스트림 먼저 삭제해 주는 것 잊지 않기. 잊으면 스트림 삭제할 방법이 없게 되어 계속 돌아간다 ㄷㄷ
        //List<ITweetContainer>
        void GiveTweet(Tweet twt);
        //void GiveEvent(KnownDeleteEvent kde);
        void GiveEvent(DeleteEvent de);
        void GiveEvent(FavoriteEvent fe);
        void GiveEvent(UserEvent ue);
        void GiveEvent(TextEvent te);
    }

    //public interface IGlobalTweetContainer
    //{
    //    ITweetElement[] GetTweetElementsWithId(UInt64 Id);
    //}

    public interface ITweetElement : ITwitterContent
    {
        //TweetState State { get; set; }
        Tweet TweetData { get; }
    }

    public interface ITwitterColumnQuery
    {
        TwitterClient Client { get; }
        Uri RequestUrl { get; }
        Uri StreamUrl { get; }//리스트, 유저타임라인 등 할 땐 Uri만 갖고는 안 될 거 같은데..
        ITwitterRequestQuery GetRequestQuery();
        //Boolean IsStreamActivated { get; set; }
    }

    public interface ITwitterRequestQuery
    {
        String GetQueryStringPart1();
        String GetQueryStringPart2();
        String GetQueryStringTotal();
        String GetPostQueryString();
    }

    public interface ITwitterContent
    {
        //String GetJsonMessage();//삭제대상, TwitterData로 옮길 것..인데 그냥 놔둘까? 메시지에서도 제이슨메시지 받아야 하니까 음. - 는 전체 삭제대상. ITwitterData도 딱히 필요없을듯
        Nullable<DateTime> GetPublishedTime();
    }
}

