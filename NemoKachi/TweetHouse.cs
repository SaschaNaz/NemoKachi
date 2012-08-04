using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemoKachi.TwitterWrapper;
using NemoKachi.TwitterWrapper.TwitterDatas;

namespace NemoKachi
{
    public class TweetHouse
    {
        Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer() { Interval = new TimeSpan(0, 1, 0) };
        List<TweetLog> TweetList = new List<TweetLog>();
        public TimeSpan MaxRemainTime { get; set; }

        public TweetHouse(TimeSpan maxRemainTime)
        {
            timer.Tick += timer_Tick;
            MaxRemainTime = maxRemainTime;
        }

        void timer_Tick(object sender, object e)
        {
            TweetLog lastTweet = LastOrNull();
            while (lastTweet != null && DateTime.Now - lastTweet.LastLoadedTime > MaxRemainTime)
            {
                TweetList.Remove(lastTweet);
                lastTweet = LastOrNull();
            }
        }
        
        TweetLog LastOrNull()
        {
            if (TweetList.Count > 0)
            {
                return TweetList.Last();
            }
            else
            {
                return null;
            }
        }

        public void AttachTweets(params Tweet[] twts)
        {
            for (Int32 i = twts.Length - 1; i >= 0; i--)
            {
                TweetList.Insert(0, new TweetLog() { Stored = twts[i], LastLoadedTime = DateTime.Now });
            }
        }

        public Tweet LoadTweet(AccountToken aToken, UInt64 Id)
        {
            return TweetList.Find((TweetLog tLog) =>
            {
                if (tLog.Stored.Id == Id)
                    return true;
                else
                    return false;
            }).Stored;
            //찾지 못했을 때 클라이언트를 이용해 새로 받아오는 코드
        }

        public Boolean IsManaging
        {
            get
            {
                return timer.IsEnabled;
            }
        }

        public void ManageStart()
        {
            timer.Start();
        }
        public void ManageStop()
        {
            timer.Stop();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1">The base class type</typeparam>
    /// <typeparam name="T2">The attribute type you want to add</typeparam>
    public class TweetLog
    {
        public Tweet Stored;
        public DateTime LastLoadedTime;
    }
}
