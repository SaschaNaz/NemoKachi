using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.Data.Json;
using NemoKachi.TwitterWrapper.TwitterDatas;

namespace NemoKachi.TwitterWrapper
{
    public partial class TwitterClient
    {
        public static void AddStreamedData(List<ITwitterViewer> Viewers, Tweet twt)//ITwitterData 중에서도 스트림에 추가되는 게 있고 영 안 되는 게 있는데 그건 어떻게 구분하지. 는 그냥 없애고 다 따로 만듦.
        {
            foreach (ITwitterViewer Viewer in Viewers)
            {
                Viewer.GiveTweet(twt);//TextMessage를 struct 형태로 바꾸고 이름을 TwitterEvent로 수정. 그리고 TwitterDatas.Tweet과 ITwitterMessage로 묶으면 될 듯.
            }
        }
        //static void AddStreamedData(List<ITwitterViewer> Viewers, KnownDeleteEvent kde)
        //{
        //    foreach (ITwitterViewer Viewer in Viewers)
        //    {
        //        Viewer.GiveEvent(kde);
        //    }
        //}
        static void AddStreamedData(List<ITwitterViewer> Viewers, DeleteEvent de)
        {
            foreach (ITwitterViewer Viewer in Viewers)
            {
                Viewer.GiveEvent(de);
            }
        }
        static void AddStreamedData(List<ITwitterViewer> Viewers, FavoriteEvent fe)
        {
            foreach (ITwitterViewer Viewer in Viewers)
            {
                Viewer.GiveEvent(fe);
            }
        }
        static void AddStreamedData(List<ITwitterViewer> Viewers, UserEvent ue)
        {
            foreach (ITwitterViewer Viewer in Viewers)
            {
                Viewer.GiveEvent(ue);
            }
        }
        static void AddStreamedData(List<ITwitterViewer> Viewers, TextEvent te)
        {
            foreach (ITwitterViewer Viewer in Viewers)
            {
                Viewer.GiveEvent(te);
            }
        }

        public class UserStreamer : ITwitterStreamer
        {
            String _DistinctName;//스트리밍 고유의 이름. 리스트면 리스트, 메인 스트림이면 메인 스트림(타임라인/멘션), DM이면 DM 등
            //DependencyProperty를 안 쓰는 이유는 영 버그 때문에 Consumer Preview 올라오면 그 때..
            public String DistinctName
            {
                get
                {
                    return _DistinctName;
                }
                private set
                {
                    _DistinctName = value;
                }
            }
            Boolean _IsActivated;
            public Boolean IsActivated
            {
                get
                {
                    return _IsActivated;
                }
                private set
                {
                    _IsActivated = value;
                }
            }
            HttpResponseMessage response;//스트리밍 멈추기용 기억장치
            //TwitterClient.UserStreamingState StreamState = UserStreamingState.None;
            TwitterClient.AccountLine _ColumnQuery;
            public UInt64[] Followings;
            public ITwitterColumnQuery ColumnQuery
            {
                get { return _ColumnQuery; }
            }
            public readonly List<ITwitterViewer> TimelineViews = new List<ITwitterViewer>();
            public readonly List<ITwitterViewer> MentionsViews = new List<ITwitterViewer>();
            //public readonly IGlobalTweetContainer AttachedContainer;

            public UserStreamer(TwitterClient.AccountLine Query)
            {
                //AttachedContainer = twtContainer;
                DistinctName = "UserStream";
                _ColumnQuery = Query;
                _ColumnQuery.Type = String.Empty;
                
                //타임라인+멘션함 용
                //Uri는 user.json
                //지금까지 MainPage.xaml.cs에서 하던 것 그대로 모두 처리
            }

            public void AddTweetInStream(Tweet twt)
            {
                //TweetBlock twtblock = new TweetBlock(twt);
                //UInt64 Id = twt.GetUser().GetId();
                //if (_ColumnQuery.Client.AccountId == Id || Followings.Contains(Id))
                //{
                //    AddStreamedData(TimelineViews, twt);
                //}
                //if (twtblock.MentionedNames != null && twtblock.MentionedNames.Contains("SaschaNaz"))
                //{
                //    AddStreamedData(MentionsViews, twt);
                //}

                //{
                //    //기타 다른 리트윗 같은 것들? 어떻게 넣지.
                //    //
                //    //AddStreamedData(MentionsViews, twt);
                //}
            }

            public void Dispose()
            {
                response.Dispose();
            }

            public class ActivatedException : Exception
            {
                public ActivatedException()
                {

                }

                public ActivatedException(String message)
                    : base(message)
                {

                }

                public ActivatedException(String message, Exception innerException)
                    : base(message, innerException)
                {

                }
            }

            public async Task ActivateAsync()//ActivateAsync 불러오는 메소드에서 TimeoutException 받으면 다시 액티베이팅하게 만들기
            {
                if (IsActivated)
                {
                    throw new ActivatedException("This streamer already has activated.");
                }
                IsActivated = true;
                //StreamState = UserStreamingState.Connecting;
                try
                {
                    response = await ColumnQuery.Client.RefreshStream(ColumnQuery.StreamUrl.AbsoluteUri, ColumnQuery.GetRequestQuery());
                    //if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    //{
                    //    StreamState = UserStreamingState.Streaming;
                    //}
                    //else
                    //{
                    //    StreamState = UserStreamingState.None;
                    //}
                }
                catch (Exception ex)
                {
                    //StreamState = UserStreamingState.None;
                    //IsActivated = false;
                    throw ex;
                }

                //response.Content.ContentReadStream.ReadTimeout = 10;
                if (response.IsSuccessStatusCode)
                {
                    AddStreamedData(TimelineViews, new TextEvent() { Title = "Userstream Started", Content = "Userstream is started successfully", TextColor = Windows.UI.Colors.Green });
                    List<Char> list = new List<Char>();
                    System.IO.Stream stream = await response.Content.ReadAsStreamAsync();
                    stream.ReadTimeout = 90000;

                    {//recieving user IDs - 나중에 아래것과 통합할까..
                        {
                            Boolean IsStopped = false;
                            //while (!streader.EndOfStream)
                            //    textBlock1.Text += Convert.ToChar(streader.Read());
                            while (!IsStopped)
                            {
                                Byte[] buffer = new Byte[1000];
                                try
                                {
                                    await stream.ReadAsync(buffer, 0, 1000);
                                }
                                //catch (System.Net.WebException)
                                //{
                                //    AddStreamedData(TimelineViews, new TextEvent() { Title = "Userstream Stopped", Content = "Userstream is stopped by user", TextColor = Windows.UI.Xaml.Media.Colors.Green });
                                //    return;
                                //}
                                catch (System.IO.IOException)
                                {
                                    AddStreamedData(TimelineViews, new TextEvent() { Title = "Userstream Stopped", Content = "Userstream is stopped by an accidental disconnection from server", TextColor = Windows.UI.Colors.Red });
                                    return;
                                }
                                foreach (Byte b in buffer)
                                {
                                    Char ch = Convert.ToChar(b);
                                    switch (ch)
                                    {
                                        case '\r':
                                            {
                                                if (list.Count > 0 && IsStopped != true)
                                                {
                                                    IsStopped = true;
                                                    //textBlock1.Text = new String(list.ToArray());
                                                    JsonObject json = JsonObject.Parse(new String(list.ToArray()));
                                                    //try
                                                    //{
                                                    //    json.Parse(new String(list.ToArray()));
                                                    //}
                                                    //catch (Exception ex)
                                                    //{
                                                    //    //textBlock1.Text += "UserStream: ERROR Ocurred";
                                                    //    AddStreamedData(TimelineViews, new TextEvent() { Title = "Error", Content = "UserStream: Loading Failed, stream stopped." + Environment.NewLine + "Message: " + ex.Message, TextColor = Windows.UI.Xaml.Media.Colors.Red });
                                                    //}
                                                    list.Clear();

                                                    JsonArray jary = json.GetNamedArray("friends");
                                                    Followings = new UInt64[jary.Count];
                                                    for (Int32 i = 0; i < jary.Count; i++)
                                                    {
                                                        Followings[i] = (UInt64)jary[i].GetNumber();
                                                    }
                                                }
                                                break;
                                            }
                                        case '\n':
                                        case '\0':
                                            {
                                                break;
                                            }
                                        default:
                                            {
                                                list.Add(ch);
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                    }

                    try
                    {//recieving tweet stream
                        while (true)
                        {
                            Byte[] buffer = new Byte[1000];
                            try
                            {
                                await stream.ReadAsync(buffer, 0, 1000);
                            }
                            catch (System.Net.WebException)
                            {
                                AddStreamedData(TimelineViews, new TextEvent() { Title = "Userstream Stopped", Content = "Userstream is stopped by user", TextColor = Windows.UI.Colors.Green });
                                return;
                            }
                            catch (System.IO.IOException)
                            {
                                AddStreamedData(TimelineViews, new TextEvent() { Title = "Userstream Stopped", Content = "Userstream is stopped by an accidental disconnection from server", TextColor = Windows.UI.Colors.Red });
                                return;
                            }
                            foreach (Byte b in buffer)
                            {
                                Char ch = Convert.ToChar(b);
                                switch (ch)
                                {
                                    case '\r':
                                        {
                                            if (list.Count > 0)
                                            {
                                                JsonObject json = JsonObject.Parse(new String(list.ToArray()));
                                                //Boolean jsonbl;
                                                //{
                                                //    try
                                                //    {
                                                //        json.Parse(new String(list.ToArray()));
                                                //        jsonbl = true;
                                                //    }
                                                //    catch (Exception ex)
                                                //    {
                                                //        //textBlock1.Text += "UserStream: ERROR Ocurred";
                                                //        AddStreamedData(TimelineViews, new TextEvent() { Title = "Error", Content = "UserStream: Wrong Response" + Environment.NewLine + "Message: " + ex.Message, TextColor = Windows.UI.Xaml.Media.Colors.Red });
                                                //        jsonbl = false;
                                                //    }
                                                //}
                                                list.Clear();
                                                //if (jsonbl)
                                                {
                                                    String responseType = null;
                                                    if (json.GetNamedString("source") != null)
                                                    {
                                                        responseType = "Status";
                                                    }
                                                    else if (json.GetNamedObject("delete") != null)
                                                    {
                                                        responseType = "Delete";
                                                    }
                                                    else
                                                    {
                                                        String eventstr = json.GetNamedString("event");
                                                        if (eventstr != null)
                                                        {
                                                            switch (eventstr)
                                                            {
                                                                case "favorite":
                                                                    {
                                                                        responseType = "Favorite";
                                                                        break;
                                                                    }
                                                                case "unfavorite":
                                                                    {
                                                                        responseType = "Unfavorite";
                                                                        break;
                                                                    }
                                                                case "follow":
                                                                    {
                                                                        responseType = "Follow";
                                                                        break;
                                                                    }
                                                                case "block":
                                                                    {
                                                                        responseType = "Block";
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                        else
                                                        {

                                                        }
                                                    }
                                                    //System.Diagnostics.Debug.WriteLine(json.Stringify());
                                                    switch (responseType)
                                                    {
                                                        case "Status":
                                                            {
                                                                //유저스트림 제어기에 주도록 나중에 수정
                                                                AddTweetInStream(new Tweet(json));
                                                                break;
                                                            }
                                                        case "Delete":
                                                            {
                                                                JsonObject del_status = json.GetNamedObject("delete").GetNamedObject("status");
                                                                //ITweetElement[] ites = AttachedContainer.GetTweetElementsWithId(Convert.ToUInt64(del_status.GetNamedString("id_str")));//MainPage.GetTweetWithId ~~
                                                                //if (ites.Length != 0)
                                                                //{
                                                                //    foreach (ITweetElement ite in ites)
                                                                //    {
                                                                //        //ite.State = TweetState.Deleted;
                                                                //    }
                                                                //    AddStreamedData(TimelineViews, new KnownDeleteEvent(ites[0].TweetData));
                                                                //}
                                                                AddStreamedData(TimelineViews, new DeleteEvent(del_status));
                                                                /* DeleteEvent를 연결된 각 트윗뷰어들에 넘겨주면서 각 트윗뷰어에서 알아서 지워진 트윗 찾아본 뒤에, 없으면 말고, 있으면 트윗뷰어 히스토리에 트윗 지워졌다는 결과 남김
                                                                 */

                                                                break;
                                                            }
                                                        case "Favorite":
                                                            {
                                                                AddStreamedData(TimelineViews, new FavoriteEvent(json, FavoriteEvent.EventType.Favorited));
                                                                //Tweet twt = new Tweet(json.GetNamedObject("target_object"));
                                                                //AttachedPanel.AddTextMessage(new TextMessage("Response: Favorite",
                                                                //        String.Format(
                                                                //        "User: {0}"
                                                                //        + Environment.NewLine + "Status: {1}"
                                                                //        + Environment.NewLine + "From: {2}", twt.GetUser().GetScreenName(), twt.GetText(), json.GetNamedObject("source").GetNamedString("screen_name")),
                                                                //        TwitterClient.ConvertToDateTime(json.GetNamedString("created_at")),
                                                                //        jsonString, Windows.UI.Xaml.Media.Colors.Yellow));
                                                                break;
                                                            }
                                                        case "Unfavorite":
                                                            {
                                                                AddStreamedData(TimelineViews, new FavoriteEvent(json, FavoriteEvent.EventType.Unfavorited));
                                                                //Tweet twt = new Tweet(json.GetNamedObject("target_object"));
                                                                //AttachedPanel.AddTextMessage(new TextMessage("Response: Unfavorite",
                                                                //        String.Format(
                                                                //        "User: {0}"
                                                                //        + Environment.NewLine + "Status: {1}"
                                                                //        + Environment.NewLine + "From: {2}", twt.GetUser().GetScreenName(), twt.GetText(), json.GetNamedObject("source").GetNamedString("screen_name")),
                                                                //        TwitterClient.ConvertToDateTime(json.GetNamedString("created_at")),
                                                                //        jsonString, Windows.UI.Xaml.Media.Colors.Yellow));
                                                                break;
                                                            }
                                                        case "Follow":
                                                            {
                                                                AddStreamedData(TimelineViews, new UserEvent(json, UserEvent.EventType.Follow));
                                                                //AttachedPanel.AddTextMessage(new TextMessage("Response: Follow",
                                                                //    String.Format(
                                                                //    "Followed: {0}"
                                                                //        + Environment.NewLine + "From: {1}",
                                                                //        json.GetNamedObject("target").GetNamedString("screen_name"),
                                                                //        json.GetNamedObject("source").GetNamedString("screen_name")),
                                                                //    TwitterClient.ConvertToDateTime(json.GetNamedString("created_at")),
                                                                //    jsonString, new Windows.UI.Xaml.Media.Color() { R = 0x7C, G = 0x90, B = 0xFF, A = 0xFF }));

                                                                break;
                                                            }
                                                        case "Block":
                                                            {
                                                                AddStreamedData(TimelineViews, new UserEvent(json, UserEvent.EventType.Block));
                                                                //AttachedPanel.AddTextMessage(new TextMessage("Response: Block",
                                                                //    String.Format(
                                                                //    "Blocked: {0}"
                                                                //        + Environment.NewLine + "From: {1}",
                                                                //        json.GetNamedObject("target").GetNamedString("screen_name"),
                                                                //        json.GetNamedObject("source").GetNamedString("screen_name")),
                                                                //    TwitterClient.ConvertToDateTime(json.GetNamedString("created_at")),
                                                                //    jsonString, Windows.UI.Xaml.Media.Colors.Purple));
                                                                break;
                                                            }
                                                        case null:
                                                            {
                                                                //AddStreamedData에 그대로 null 던지기.
                                                                AddStreamedData(TimelineViews, new TextEvent() { Title = "Response: Unknown", Content = "", TextColor = Windows.UI.Colors.Red });
                                                                break;
                                                            }
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    case '\n':
                                    case '\0':
                                        {
                                            break;
                                        }
                                    default:
                                        {
                                            list.Add(ch);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("아 뭐 어쩌라고");
                    }

                    //httpClient = new HttpClient(); 
                    //httpResponse = response;
                }
            }
        }

        //public class TwitterStreamer : ITwitterStreamer
        //{
        //    //여기서 바로 유저스트림 받아서 제어하면 되겠네. MainPage.xaml.cs에 있는 것 데려오자. RefreshStream은 얘가 이어받는다.
        //    //UserStreamState도 여기로 넘겨온다.
        //    //스트림 추가할 때 이 클래스 만들어서 등록하면 됨
        //    //트윗뷰는 어디서 추가하니 ㄷㄷㄷ

        //    String _DistinctName;//스트리밍 고유의 이름. 리스트면 리스트, 메인 스트림이면 메인 스트림(타임라인/멘션), DM이면 DM 등
        //    //DependencyProperty를 안 쓰는 이유는 영 버그 때문에 Consumer Preview 올라오면 그 때..
        //    public String DistinctName
        //    {
        //        get
        //        {
        //            return _DistinctName;
        //        }
        //        private set
        //        {
        //            _DistinctName = value;
        //        }
        //    }
        //    HttpResponseMessage response;//스트리밍 멈추기용 기억장치

        //    public readonly Action<TwitterDatas.ITwitterData> AddTweetInStream;
        //    public readonly Task StartStream;

        //    public TwitterStreamer(String distinctName, TweetView StreamView, TwitterClient twtClient, params Int64[] ids)
        //    {
        //        DistinctName = distinctName;
        //        AddTweetInStream = new Action<TwitterDatas.ITwitterData>(
        //            delegate(TwitterDatas.ITwitterData twtData)
        //            {

        //            });
        //        StartStream = new Task(async delegate
        //        {
        //            //ids 갖고 JSON 쿼리 넣어서 스트림 보내는 과정 필요
        //            response = await twtClient.RefreshStream();
        //        });
        //        //단일 트윗뷰용. 여러가지 용도. Uri는 없어도 되나?
        //        //Uri는 follow.json
        //        //처리는 일반트윗/리트윗 정도만 하면 될 듯
        //        //리스트에 빨리 추가한 뒤에 작업을 시작하면 좋겠는데 지금 상태로는 작업을 시작한 뒤에 리스트에 추가된다. 음.
        //        //RichTextBlock rtb = new RichTextBlock();
        //        //rtb.

        //        //예외 발생 그대로 시키기 - 예외 발생하면 외부에서 이 스트리머 지우도록
        //    }

        //    //나중에 만들 DM용..
        //    //public TwitterStreamer(DMView dmessage)
        //    //{
        //    //https://dev.twitter.com/docs/streaming-api/user-streams
        //    //여기에 따르면 그냥 DM도 메인스트림 따라 오는 거 같은데..;
        //    //}

        //    public void Dispose()
        //    {
        //        response.Dispose();
        //    }
        //}

        //public List<ITwitterStreamer> TwitterStreamers = new List<ITwitterStreamer>();
    }
}
