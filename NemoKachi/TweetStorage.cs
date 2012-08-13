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
        //Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer() { Interval = new TimeSpan(0, 1, 0) };
        SortedDictionary<UInt64, TweetLog> TweetLogList = new SortedDictionary<UInt64, TweetLog>();
        ColumnData InternalTweetDatas = new ColumnData() { MaxRemainTime = new TimeSpan(0, 10, 0), MinTweetCount = 0 };
        //public TimeSpan MaxRemainTime { get; set; }

        public TweetStorage()
        {
            //timer.Tick += timer_Tick;
            //MaxRemainTime = maxRemainTime;
        }

        //void timer_Tick(object sender, object e)
        //{
        //    TweetLog lastTweet = LastOrNull();
        //    while (lastTweet != null && DateTime.Now - lastTweet.StorageTimestamp > MaxRemainTime)
        //    {
        //        TweetList.Remove(lastTweet);
        //        lastTweet = LastOrNull();
        //    }
        //}
        
        //TweetLog LastOrNull()
        //{
        //    if (TweetList.Count > 0)
        //    {
        //        return TweetList.Last();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public void RegisterTweets(ColumnData clData, params UInt64[] twtids)
        {
            foreach (UInt64 twtid in twtids)
            {
                TweetLog twlog;
                if (TweetLogList.TryGetValue(twtid, out twlog))
                {
                    twlog.RegisterColumn(clData);
                }
                else
                {
                    twlog = new TweetLog();
                    twlog.RegisterColumn(clData);
                    TweetLogList.Add(twtid, twlog);
                }
            }
            //for (Int32 i = twts.Length - 1; i >= 0; i--)
            //{
            //    AttachTweetLog(new TweetLog() { LoggedTweet = twts[i], StorageTimestamp = DateTime.Now });
            //}
        }

        public void RegisterTweets(ColumnData clData, params Tweet[] twts)
        {
            foreach (Tweet twt in twts)
                RegisterTweets(clData, twt.Id);
        }

        public void UnregisterTweet(ColumnData clData, UInt64 twtid)
        {
            TweetLog twlog;
            if (TweetLogList.TryGetValue(twtid, out twlog))
            {
                twlog.UnregisterColumn(clData);
                if (twlog.IsEmpty)
                    TweetLogList.Remove(twtid);
            }
            else
                throw new Exception("No tweet with such ID.");
        }

        public async Task<Tweet> LoadTweet(AccountToken aToken, UInt64 Id)
        {
            Tweet twt = GetTweetInStorage(Id);
            if (twt != null)
            {
                return twt;
            }
            else
            {
                TwitterWrapper.TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient;

                twt = await MainClient.ShowTweetAsync(aToken, new ShowTweetRequest(), Id);
                InternalTweetDatas.AttachTweets(twt);
                return twt;
            }
        }

        public class TweetStorageLockedException : Exception
        {
            public DateTime UnlockTime { get; private set; }
            public Tweet LockedTweet { get; private set; }

            public TweetStorageLockedException(DateTime unlockTime, Tweet locked) : base() { UnlockTime = unlockTime; LockedTweet = locked; }
        }

        //public Boolean IsManaging
        //{
        //    get
        //    {
        //        return timer.IsEnabled;
        //    }
        //}

        //public void ManageStart()
        //{
        //    timer.Start();
        //}
        //public void ManageStop()
        //{
        //    timer.Stop();
        //}

        public void LockTweet(UInt64 Id, DateTime UnlockTime)
        {
            TweetLog storedLog;
            if (TweetLogList.TryGetValue(Id, out storedLog))
            {
                storedLog.Lock(UnlockTime);
            }
            else
                throw new Exception("No tweet with such ID to lock.");
        }

        //void AttachTweetLog(TweetLog log)
        //{
        //    DateTime newLogTime = log.StorageTimestamp;
        //    for (Int32 i = 0; i < TweetList.Count; i++)
        //    {
        //        DateTime itemTime = TweetList[i].StorageTimestamp;
        //        if (itemTime <= newLogTime)
        //        {
        //            TweetList.Insert(i, log);
        //            return;
        //        }
        //    }
        //    TweetList.Add(log);
        //}

        //TweetLog FindStoredLog(UInt64 Id)
        //{
        //    return TweetList.Find((TweetLog tLog) =>
        //    {
        //        if (tLog.LoggedTweet.Id == Id)
        //            return true;
        //        else
        //            return false;
        //    });
        //}

        Tweet GetTweetInStorage(UInt64 Id)
        {
            TweetLog storedLog;
            if (TweetLogList.TryGetValue(Id, out storedLog))
            {
                ColumnData sourceColumn = storedLog.GetStoredColumn();
                if (sourceColumn.TweetList.Count > 0)
                {
                    return sourceColumn.TweetList.First(delegate(Tweet twt)
                    {
                        if (twt.Id == Id)
                            return true;
                        else return false;
                    });
                }
            }
            return null;
        }

        public DateTime GetTweetRegisteredTime(UInt64 Id, ColumnData clData)
        {
            TweetLog storedLog;
            if (TweetLogList.TryGetValue(Id, out storedLog))
            {
                return storedLog.CheckStoredTime(clData);
            }
            else
                throw new Exception("No tweet with such ID to get.");
        }
    }

    class TweetLog
    {
        DateTime LockTimestamp;
        List<ColumnRegistration> RegisteredList = new List<ColumnRegistration>();

        public Boolean IsLocked
        {
            get
            {
                return (DateTime.Now < LockTimestamp);
            }
        }

        public void Lock(DateTime lockTime)
        {
            LockTimestamp = lockTime;
        }

        public void Unlock()
        {
            LockTimestamp = new DateTime();
        }

        public void RegisterColumn(ColumnData clData)
        {
            RegisteredList.Add(new ColumnRegistration() { StorageTimestamp = DateTime.Now, StoredColumn = clData });
        }

        public Boolean UnregisterColumn(ColumnData clData)
        {
            ColumnRegistration clreg = RegisteredList.Find(delegate(ColumnRegistration reg)
            {
                return (reg.StoredColumn == clData);
            });
            if (clreg != null)
            {
                RegisteredList.Remove(clreg);
                return true;
            }
            else
                return false;
        }

        public ColumnData GetStoredColumn()
        {
            if (RegisteredList.Count != 0)
                return RegisteredList[0].StoredColumn;
            else
                throw new Exception("There is no column data registered.");
        }

        public DateTime CheckStoredTime(ColumnData clData)
        {
            ColumnRegistration clreg = RegisteredList.Find(delegate(ColumnRegistration reg)
            {
                return (reg.StoredColumn == clData);
            });
            if (clreg != null)
                return clreg.StorageTimestamp;
            else
                throw new Exception("This column has not registered with this tweet ID.");
        }

        public Boolean IsEmpty
        {
            get
            {
                return (RegisteredList.Count == 0);
            }
        }
    }

    class ColumnRegistration
    {
        public DateTime StorageTimestamp;
        public ColumnData StoredColumn;
    }
}
