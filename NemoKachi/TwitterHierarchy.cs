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
        public ObservableCollection<Tweet> TweetList { get; set; }
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

        public ColumnData()
        {
            MaxRemainTime = new TimeSpan(0, 10, 0);
            MinTweetCount = 20;
            timer.Tick += timer_Tick;
            TimelineDatas = new ObservableCollection<ITimelineData>();
            TweetList = new ObservableCollection<Tweet>();
            timer.Start();
        }

        void timer_Tick(object sender, object e)
        {
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
            Tweet lastTweet = LastOrNull();
            while (lastTweet != null && TweetList.Count > MinTweetCount && DateTime.Now - house.GetTweetRegisteredTime(lastTweet.Id, this) > MaxRemainTime)
            {
                TweetList.Remove(lastTweet);
                house.UnregisterTweet(this, lastTweet.Id);
                lastTweet = LastOrNull();
            }
        }

        Tweet LastOrNull()
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
        
        public ColumnData(params ITimelineData[] tlDatas)
        {
            MaxRemainTime = new TimeSpan(0, 1, 0);
            MinTweetCount = 20;
            timer.Tick += timer_Tick;
            TimelineDatas = new ObservableCollection<ITimelineData>(tlDatas);
            TweetList = new ObservableCollection<Tweet>();
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
                DateTime itemTime = TweetList[i].CreatedAt;
                if (itemTime < publishedTime)
                {
                    TweetList.Insert(i, twt);
                    return;
                }
                else if (itemTime == publishedTime)
                {
                    Boolean same = false;
                    if (TweetList[i].Id == twt.Id)
                    {
                        same = true;
                    }
                    else
                    {
                        for (Int32 i2 = i + 1; i2 < TweetList.Count; i2++)
                        {
                            if (TweetList[i2].CreatedAt == publishedTime)
                            {
                                if (TweetList[i2].Id == twt.Id)
                                {
                                    same = true;
                                    break;
                                }
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
            TweetList.Add(twt);
        }

        public void Dispose()
        {
            timer.Stop();
        }
    }
}
