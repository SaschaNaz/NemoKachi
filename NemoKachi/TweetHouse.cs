using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using NemoKachi.TwitterWrapper;
using NemoKachi.TwitterWrapper.TwitterDatas;

namespace NemoKachi
{
    public class TweetStorage
    {
        Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer() { Interval = new TimeSpan(0, 1, 0) };
        List<TweetLog> TweetList = new List<TweetLog>();
        public TimeSpan MaxRemainTime { get; set; }

        public TweetStorage(TimeSpan maxRemainTime)
        {
            timer.Tick += timer_Tick;
            MaxRemainTime = maxRemainTime;
        }

        void timer_Tick(object sender, object e)
        {
            TweetLog lastTweet = LastOrNull();
            while (lastTweet != null && DateTime.Now - lastTweet.StorageTimestamp > MaxRemainTime)
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
                AttachTweetLog(new TweetLog() { LoggedTweet = twts[i], StorageTimestamp = DateTime.Now });
            }
        }

        public async Task<Tweet> LoadTweet(AccountToken aToken, UInt64 Id)
        {
            TweetLog storedLog = FindStoredLog(Id);
            if (storedLog != null)
            {
                if (!storedLog.IsLocked)
                    return storedLog.LoggedTweet;
                else
                    throw new TweetStorageLockedException(storedLog.StorageTimestamp, storedLog.LoggedTweet);
            }
            else
            {
                TwitterWrapper.TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient;

                return await MainClient.ShowTweetAsync(aToken, new ShowTweetRequest(), Id);
            }
            //찾지 못했을 때 클라이언트를 이용해 새로 받아오는 코드 추가
        }

        public class TweetStorageLockedException : Exception
        {
            public DateTime UnlockTime { get; private set; }
            public Tweet LockedTweet { get; private set; }

            public TweetStorageLockedException(DateTime unlockTime, Tweet locked) : base() { UnlockTime = unlockTime; LockedTweet = locked; }
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

        public void LockTweet(UInt64 Id, DateTime UnlockTime)
        {
            TweetLog locktwt = FindStoredLog(Id);
            TweetList.Remove(locktwt);
            //TweetList.
            //단순 FindStoredLog만 쓰면 리스트에 트윗이 없을 때는 대응할 수 없다 - 어차피 리스트에 트윗 있을 때만 나오지 않나
            /* FindStoredLog를 써서 리스트를 검색한 다음에 Remove를 쓰면 리스트를 또 검색할 텐데 이렇게 안 하고 인덱스를 쓰면
             * 밀려서 오류가 발생할 수 있으니 그냥 써야겠다
             * 트윗창고에서 트윗이 사라지면 칼럼에서도 지우는 방법?
             */
            locktwt.StorageTimestamp = UnlockTime;
            AttachTweetLog(locktwt);
        }

        void AttachTweetLog(TweetLog log)
        {
            DateTime newLogTime = log.StorageTimestamp;
            for (Int32 i = 0; i < TweetList.Count; i++)
            {
                DateTime itemTime = TweetList[i].StorageTimestamp;
                if (itemTime <= newLogTime)
                {
                    TweetList.Insert(i, log);
                    return;
                }
            }
            TweetList.Add(log);
        }

        TweetLog FindStoredLog(UInt64 Id)
        {
            return TweetList.Find((TweetLog tLog) =>
            {
                if (tLog.LoggedTweet.Id == Id)
                    return true;
                else
                    return false;
            });
        }
    }

    public class TweetLog
    {
        public Tweet LoggedTweet;
        public DateTime StorageTimestamp;

        public Boolean IsLocked
        {
            get
            {
                return (DateTime.Now < StorageTimestamp);
            }
        }
    }
}
