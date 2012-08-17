using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using NemoKachi.TwitterWrapper;
using NemoKachi.TwitterWrapper.TwitterDatas;
using Windows.UI.Xaml;

namespace NemoKachi
{
    public class ColumnData : DependencyObject, IDisposable
    {
        Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer() { Interval = new TimeSpan(0, 1, 0) };
        public ObservableCollection<ITimelineData> TimelineDatas { get; set; }
        public ObservableCollection<object> TweetList { get; set; }
        public String ColumnTitle
        {
            get { return (String)GetValue(ColumnTitleProperty); }
            set { SetValue(ColumnTitleProperty, value); }
        }
        public TimeSpan MaxRemainTime { get; set; }
        public Int32 MinTweetCount { get; set; }

        public static readonly DependencyProperty ColumnTitleProperty =
            DependencyProperty.Register("ColumnTitle",
            typeof(String),
            typeof(ColumnData),
            new PropertyMetadata("New Column"));

        public Tweet FindTweetWithId(UInt64 Id)
        {
            if (TweetList.Count > 0)
            {
                return TweetList.First(delegate(Object twt)
                {
                    if (twt.GetType() == typeof(Tweet) && (twt as Tweet).Id == Id)
                        return true;
                    else return false;
                }) as Tweet;
            }
            else
                return null;
        }

        public ColumnData()
        {
            MaxRemainTime = new TimeSpan(0, 10, 0);
            MinTweetCount = 20;
            timer.Tick += timer_Tick;
            TimelineDatas = new ObservableCollection<ITimelineData>();
            TweetList = new ObservableCollection<object>();
            timer.Start();
        }

        void timer_Tick(object sender, object e)
        {
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
            Object lastitem = LastOrNull();
            while (lastitem != null)
            {
                Type itemtype = lastitem.GetType();
                if (itemtype == typeof(Tweet))
                {
                    Tweet lastTweet = lastitem as Tweet;
                    if (TweetList.Count > MinTweetCount && DateTime.Now - house.GetTweetRegisteredTime(lastTweet.Id, this) > MaxRemainTime)
                    {
                        TweetList.Remove(lastTweet);
                        house.UnregisterTweet(this, lastTweet.Id);
                        lastitem = LastOrNull();
                    }
                    else
                        break;
                }
                else if (itemtype == typeof(TimeGapEnd))
                {
                    while (TweetList.Count > 0 && TweetList.Last().GetType() != typeof(TimeGapStart))
                        TweetList.Remove(TweetList.Last());
                    if (TweetList.Count > 0)
                        TweetList.Remove(TweetList.Last());
                }
            }
        }

        Object LastOrNull()
        {
            if (TweetList.Count > 0)
            {
                return TweetList.Last(); // to be fixed because the last object may not be Tweet rather TimeGap
            }
            else
            {
                return null;
            }
        }
        
        public ColumnData(params ITimelineData[] tlDatas)
        {
            MaxRemainTime = new TimeSpan(0, 1, 0);
            MinTweetCount = 20;
            timer.Tick += timer_Tick;
            TimelineDatas = new ObservableCollection<ITimelineData>(tlDatas);
            TweetList = new ObservableCollection<object>();
            timer.Start();
        }


        public void AttachTweets(params Tweet[] twts)
        {
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
            for (Int32 i = twts.Length - 1; i >= 0; i--)
            {
                AttachTweet(twts[i]);
                house.RegisterTweets(this, twts[i].Id);
            }
        }

        void AttachTweet(Tweet twt)
        {
            DateTime publishedTime = twt.CreatedAt;
            for (Int32 i = 0; i < TweetList.Count; i++)
            {
                Type itemtype = TweetList[i].GetType();
                if (itemtype == typeof(Tweet))
                {
                    Tweet tweetitem = TweetList[i] as Tweet;
                    DateTime itemTime = tweetitem.CreatedAt;
                    if (itemTime < publishedTime)
                    {
                        TweetList.Insert(i, twt);
                        return;
                    }
                    else if (itemTime == publishedTime)
                    {
                        Boolean same = false;
                        if (tweetitem.Id == twt.Id)
                        {
                            same = true;
                        }
                        else
                        {
                            for (Int32 i2 = i + 1; i2 < TweetList.Count; i2++)
                            {
                                if (TweetList[i2].GetType() == typeof(Tweet))
                                {
                                    Tweet tweetitem2 = TweetList[i2] as Tweet;
                                    if (tweetitem2.CreatedAt == publishedTime)
                                    {
                                        if (tweetitem2.Id == twt.Id)
                                        {
                                            same = true;
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }
                                else
                                    break;
                            }
                        }
                        if (!same)
                            TweetList.Insert(i, twt);
                        return;
                    }
                }
                else if (itemtype == typeof(TimeGapStart))
                {
                    TimeGapStart startitem = TweetList[i] as TimeGapStart;
                    if (startitem.FirstGapTweetID < twt.Id)
                        TweetList.Insert(i, twt);
                    else if (startitem.FirstGapTweetID == twt.Id)
                        return;
                }
                else if (itemtype == typeof(TimeGapEnd))
                {
                    TimeGapEnd enditem = TweetList[i] as TimeGapEnd;
                    if (enditem.LastGapTweetID < twt.Id)
                        TweetList.Insert(i, twt);
                    else if (enditem.LastGapTweetID == twt.Id)
                        return;
                }
            }
            TweetList.Add(twt);
        }

        public void Dispose()
        {
            timer.Stop();
        }
    }

    class TimeGapStart
    {
        public UInt64 FirstGapTweetID { get; set; }
    }

    class TimeGapEnd
    {
        public UInt64 LastGapTweetID { get; set; }
    }
}
