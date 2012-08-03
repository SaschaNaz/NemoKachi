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
    public class ColumnData : DependencyObject
    {
        public ObservableCollection<ITimelineData> TimelineDatas { get; set; }
        public ObservableCollection<Tweet> TweetList { get; set; }
        public String ColumnTitle
        {
            get { return (String)GetValue(ColumnTitleProperty); }
            set { SetValue(ColumnTitleProperty, value); }
        }

        public static readonly DependencyProperty ColumnTitleProperty =
            DependencyProperty.Register("ColumnTitle",
            typeof(String),
            typeof(ColumnData),
            new PropertyMetadata("New Column"));

        public ColumnData()
        {
            TimelineDatas = new ObservableCollection<ITimelineData>();
            TweetList = new ObservableCollection<Tweet>();
        }
        
        public ColumnData(params ITimelineData[] tlDatas)
        {
            TimelineDatas = new ObservableCollection<ITimelineData>(tlDatas);
            TweetList = new ObservableCollection<Tweet>();
        }

        public void AttachTweet(Tweet twt)
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
                    if (TweetList[i].Id != twt.Id)
                    {
                        TweetList.Insert(i, twt);
                    }
                    return;
                }
            }
            TweetList.Insert(TweetList.Count, twt);
        }
    }
}
