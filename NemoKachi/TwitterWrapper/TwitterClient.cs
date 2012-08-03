using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Net.Http;
using Windows.Data.Json;
using NemoKachi.TwitterWrapper.TwitterDatas;

namespace NemoKachi.TwitterWrapper
{
    public class AccountToken : DependencyObject
    {
        public String oauth_token;
        public String oauth_token_secret;

        public UInt64 AccountId;
        public String AccountName
        {
            get { return (String)GetValue(AccountNameProperty); }
            set { SetValue(AccountNameProperty, value); }
        }
        public Uri AccountImageUri
        {
            get { return (Uri)GetValue(AccountImageUriProperty); }
            set { SetValue(AccountImageUriProperty, value); }
        }

        public static readonly DependencyProperty AccountNameProperty =
            DependencyProperty.Register("AccountName",
            typeof(String),
            typeof(AccountToken),
            new PropertyMetadata(null));

        public static readonly DependencyProperty AccountImageUriProperty =
            DependencyProperty.Register("AccountImageUri",
            typeof(Uri),
            typeof(AccountToken),
            new PropertyMetadata(null));
    }

    public partial class TwitterClient
    {
        /// <summary>
        /// 어떤 클라이언트인지 알리는 토큰입니다.
        /// </summary>
        readonly String oauth_consumer_key;
        readonly String oauth_consumer_secret;
        readonly System.Net.Http.Headers.ProductInfoHeaderValue UserAgent;

        public static DateTime ConvertToDateTime(String TimeString)
        {
            return DateTime.ParseExact(TimeString, "ddd MMM dd HH:mm:ss zzz yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
        //SortedDictionary로 OAuth 패러미터들 집어넣기
        //OAuth 클래스 만들어서 속성으로 패러미터 넣기? 는 속성 너무 많이 생길 듯
        //OAuth용 클래스 만들어서 속성으로 토큰 넣고 지금처럼 메소드는 OAuth 그대로 쓰기 - 괜찮은듯
        //SendTweet("트윗내용"); 이렇게만 하는 게 가능하도록

        /// <summary>
        /// 유저가 누구인지 알리는 토큰입니다.
        /// </summary>
        //String oauth_token, oauth_token_secret;

        /// <summary>
        /// 트위터 서비스에 필요한 작업을 알아서 해 주는 클래스입니다
        /// </summary>
        /// <param name="_oauth_consumer_key">클라이언트의 oauth_consumer_key를 주세요</param>
        /// <param name="_oauth_consumer_secret">클라이언트의 oauth_consumer_secret을 주세요</param>
        /// <param name="textBlock">디버깅용입니다</param>
        public TwitterClient(String consumer_key, String consumer_secret, System.Net.Http.Headers.ProductInfoHeaderValue userAgent)
        {
            //InitializeComponent();
            oauth_consumer_key = consumer_key;
            oauth_consumer_secret = consumer_secret;
            UserAgent = userAgent;
        }

        //public TwitterClient(String consumer_key, String consumer_secret, String token, String token_secret, UInt64 Id, String Name)
        //{
        //    //InitializeComponent();
        //    oauth_consumer_key = consumer_key;
        //    oauth_consumer_secret = consumer_secret;
        //    oauth_token = token;
        //    oauth_token_secret = token_secret;
        //    AccountId = Id;
        //    AccountName = Name;
        //}

        /// <summary>
        /// UriEscape는 몇 가지 덜 처리돼서 추가로 처리
        /// </summary>
        /// <param name="status">처리할 텍스트</param>
        /// <returns>Escape 완료된 텍스트를 반환합니다</returns>
        public static String AdditionalEscape(string status)
        {
            const String blockedChars = @"!()*'";

            String[] returner = new String[status.Length];

            Parallel.For(0, status.Length, (i) =>
            {
                Parallel.ForEach(blockedChars.ToCharArray(), (b) =>
                {
                    if (status[i] == b)
                    {
                        returner[i] = "%" + String.Format("{0:X}", Convert.ToInt32(b));
                    }
                });
                if (returner[i] == null)
                {
                    returner[i] = status[i].ToString();
                }
            });

            return String.Join("", returner);
        }

        static Dictionary<String, String> HTTPQuery(String query)
        {
            Dictionary<String, String> returner = new Dictionary<String, String>();
            String[] tempparams1 = query.Split('&');
            for (Int32 i = 0; i < tempparams1.Length && tempparams1[i].Length != 0; i++)
            {
                String[] tempparams2 = tempparams1[i].Split('=');
                returner.Add(tempparams2[0], tempparams2[1]);
            }
            return returner;
        }

        /// <summary>
        /// 일반 트윗, 또는 멘션 등의 트윗을 보냅니다.
        /// </summary>
        /// <param name="status">트윗에 넣을 텍스트입니다</param>
        /// <returns>리퀘스트에 대한 HTTP Response 메시지를 반환합니다</returns>
        public async Task<Tweet> SendTweetAsync(AccountToken aToken, SendTweetRequest tweetQuery)
        {
            HttpResponseMessage response = await OAuthStream(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1/statuses/update.json", tweetQuery, null);
            return new Tweet(JsonObject.Parse(await ConvertStreamAsync(response.Content)));
        }

        public async Task<HttpResponseMessage> SendRetweet(AccountToken aToken, UInt64 id)
        {
            return await OAuthStream(
                aToken, HttpMethod.Post,
                String.Format("https://api.twitter.com/1/statuses/retweet/{0}.json", id),
                TwitterRequest.MakeRequest(new TwitterRequest.QueryKeyValue("include_entities", "true", TwitterRequest.RequestType.Type1)), null);
        }

        public async Task<HttpResponseMessage> Destroy(AccountToken aToken, UInt64 id)
        {
            return await OAuthStream(
                aToken, HttpMethod.Post,
                String.Format("https://api.twitter.com/1/statuses/destroy/{0}.json", id),
                TwitterRequest.MakeRequest(new TwitterRequest.QueryKeyValue("include_entities", "true", TwitterRequest.RequestType.Type1)), null);
        }

        /// <summary>
        /// 타임라인을 리프레시합니다.
        /// </summary>
        /// <returns>리퀘스트에 대한 HTTP Response 메시지를 반환합니다. 리프레시된 트윗들이 컨텐트로 포함됩니다.</returns>
        public async Task<Tweet[]> RefreshAsync(AccountToken aToken, ITimelineData tlData)
        {
            List<Tweet> tweets = new List<Tweet>();
            HttpResponseMessage response = await OAuthStream(
                aToken, HttpMethod.Get,
                tlData.RestURI.OriginalString, tlData.GetRequest(), null);
            String str = await ConvertStreamAsync(response.Content);
            System.Diagnostics.Debug.WriteLine(str);
            JsonArray jary = JsonArray.Parse(str);
            foreach (JsonValue jo in jary)
            {
                tweets.Add(new Tweet(jo.GetObject()));
            }
            return tweets.ToArray();
        }

        public async Task<HttpResponseMessage> RefreshStream(AccountToken aToken, String url, TwitterRequest requestQuery)
        {
            return await OAuthSocket(
                aToken, HttpMethod.Get,
                url, requestQuery);//new RefreshQuery() { include_entities = true, include_rts = true }
        }

        public async Task<HttpResponseMessage> GetUserInformation(AccountToken aToken, UInt64 Id)
        {
            return await OAuthStream(
                aToken, HttpMethod.Get,
                "https://api.twitter.com/1/users/show.json",
                TwitterRequest.MakeRequest(
                    new TwitterRequest.QueryKeyValue("include_entities", "true", TwitterRequest.RequestType.Type1),
                    new TwitterRequest.QueryKeyValue("user_id", Id.ToString(), TwitterRequest.RequestType.Type2)), null);//new RefreshQuery() { include_entities = true, include_rts = true }
        }

        public async Task<HttpResponseMessage> GetUserProfileImage(String ScreenName)
        {
            return await NonAuthStream(
                HttpMethod.Get,
                "https://api.twitter.com/1/users/profile_image",
                TwitterRequest.MakeRequest(
                    new TwitterRequest.QueryKeyValue("screen_name", ScreenName, TwitterRequest.RequestType.Type2),
                    new TwitterRequest.QueryKeyValue("size", "bigger", TwitterRequest.RequestType.Type2)));//new RefreshQuery() { include_entities = true, include_rts = true }
        }

        //public async Task<HttpResponseMessage> MentionRefresh(String lastId)
        //{
        //    SortedDictionary<String, String> querys = new SortedDictionary<String, String>()
        //    {
        //        { "include_entities", "true" },
        //        { "include_rts", "true" }
        //    };
        //    if (lastId != "")
        //    {
        //        querys.Add("since_id", lastId);
        //    }
        //    return await OAuth(
        //            new SortedDictionary<String, String>(),
        //        new SortedDictionary<String, String>(),
        //        HttpMethod.Get,
        //        "https://api.twitter.com/1/statuses/mentions.json", querys, textBlock1);
        //}

        /// <summary>
        /// 문자열과 키를 받아서 해시값을 Base64 형태로 내보냅니다
        /// </summary>
        /// <param name="toHashed">해시할 문자열을 받습니다</param>
        /// <param name="key">해시에 쓰일 키를 받습니다</param>
        /// <returns>Base64 형태의 해시값입니다</returns>
        String HMAC_SHA1Hasher(String toHashed, String key)
        {
            //http://msdn.microsoft.com/en-us/library/windows/apps/hh464979(v=vs.85).aspx
            //http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/b541a08a-d3cd-4e21-8d21-7ed80749cb23
            // Open the SHA1 hash provider.
            //foreach (string abc in HashAlgorithmProvider.EnumerateAlgorithms())
            //{
            //    textBlock1.Text += Environment.NewLine + abc;
            //}
            MacAlgorithmProvider Algorithm = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            Windows.Storage.Streams.IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            CryptographicKey cryptoKey = Algorithm.CreateKey(keyMaterial);

            // Create a buffer and fill it with the data to be hashed.
            Windows.Storage.Streams.IBuffer vectorData = CryptographicBuffer.ConvertStringToBinary(
                   toHashed, BinaryStringEncoding.Utf8);

            // Hash the data and save it in a new buffer.
            Windows.Storage.Streams.IBuffer hashValue = CryptographicEngine.Sign(cryptoKey, vectorData);

            // Encode the hash to a hexadecimal string.
            return CryptographicBuffer.EncodeToBase64String(hashValue);
        }

        //oauthParam을 두가지로 나누어 넣는 메소드
        //public static async Task<HttpResponseMessage> OAuth(String oauth_token, String oauth_token_secret, String oauth_consumer_key, String oauth_consumer_secret, String[,] sigParam, String[,] postParam, String[,] oauthParam, HttpMethod reqMethod, Uri baseUrl, Boolean useOAuthToken)
        //{
        //    List<String[]>
        //        oauthParam1 = new List<String[]>(),
        //        oauthParam2 = new List<String[]>();
        //    int x = sigParam.GetUpperBound(0);
        //    for (Int32 i = 0; i <= sigParam.GetUpperBound(0); i++)
        //    {
        //        switch (sigParam[i, 0])
        //        {
        //            case "oauth_callback":
        //                {
        //                    oauthParam1.Add(new String[] { sigParam[i, 0], sigParam[i, 1] });
        //                    break;
        //                }
        //            case "oauth_token":
        //                {
        //                    oauthParam2.Add(new String[] { sigParam[i, 0], sigParam[i, 1] });
        //                    break;
        //                }
        //            default:
        //                {
        //                    oauthParam2.Add(new String[] { sigParam[i, 0], sigParam[i, 1] });
        //                    break;
        //                }
        //        }
        //    }
        //    return await OAuth(oauth_token, oauth_token_secret, oauth_consumer_key, oauth_consumer_secret, sigParam, postParam, oauthParam1, oauthParam2, reqMethod, baseUrl, useOAuthToken);
        //    //---sigParam을 앞에 넣는 sigParam1과 sigParam2로 나누고 바로 OAuth 불러온다
        //}


        /* URL 패러미터는 따로 넣어주는 게 나을 거 같기도 하다, URL엔 나중에 결합시키고.
         * URL에 넣을 패러미터들은 리프레시, 트윗, 이런것들마다 종류가 다르기 때문에 대책이 필요할듯
         * enum Tweet, Refresh 라든가
         * 이 때 멘션 리프레시는 트윗 리프레시와 같으니 뭐 신경쓸 필요 없을지도?
         * 
         * 또는 각각의 클래스 (TweetParams, RefreshParams) 등을 만들어서 각 클래스에 필요한 패러미터들을 넣게함?
         * 우선은 리트윗이나 만들자
         */
        /// <summary>
        /// OAuth 리퀘스트를 만들어 보낸 후 반응 메시지를 반환합니다
        /// </summary>
        /// <param name="reqMethod">리퀘스트 메소드입니다</param>
        /// <param name="baseUrl">어느 URL로 리퀘스트를 보낼 지 정합니다</param>
        /// <param name="twtQuery">트위터 쿼리를 만들어 보냅니다</param>
        /// <returns>리퀘스트에 대한 HTTP Response 메시지를 반환합니다</returns>
        //public async Task<HttpResponseMessage> OAuth(HttpMethod reqMethod, String baseUrl, ITwitterRequestQuery twtQuery)
        //{
        //    const String oauth_version = "1.0";
        //    const String oauth_signature_method = "HMAC-SHA1";
        //    String oauth_nonce = Convert.ToBase64String(new UTF8Encoding().GetBytes(DateTime.Now.Ticks.ToString()));
        //    TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        //    String oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();

        //    List<String> baseStringList = new List<String>();
        //    baseStringList.Add("oauth_consumer_key=" + oauth_consumer_key);
        //    baseStringList.Add("oauth_nonce=" + oauth_nonce);
        //    baseStringList.Add("oauth_signature_method=" + oauth_signature_method);
        //    baseStringList.Add("oauth_timestamp=" + oauth_timestamp);
        //    if (oauth_token != null)
        //    {
        //        baseStringList.Add("oauth_token=" + oauth_token);
        //    }
        //    baseStringList.Add("oauth_version=" + oauth_version);


        //    String baseString = String.Concat(reqMethod, "&", Uri.EscapeDataString(baseUrl));
        //    {
        //        String AddString = "";
        //        {
        //            String query1 = twtQuery.GetQueryStringPart1();
        //            if (query1 != "")
        //            {
        //                AddString += query1;
        //            }
        //        }
        //        if (AddString != "")
        //        {
        //            AddString += '&';
        //        }
        //        AddString += String.Join("&", baseStringList);
        //        {
        //            String postquery = twtQuery.GetPostQueryString();
        //            if (postquery != "")
        //            {
        //                AddString += '&' + postquery;
        //            }
        //        }
        //        {
        //            String query2 = twtQuery.GetQueryStringPart2();
        //            if (query2 != "")
        //            {
        //                AddString += '&' + query2;
        //            }
        //        }
        //        baseString += '&' + Uri.EscapeDataString(AddString);
        //    }
        //    textBlock1.Text += baseString + Environment.NewLine;

        //    String compositeKey = Uri.EscapeDataString(oauth_consumer_secret) + "&";
        //    if (oauth_token_secret != null)
        //    {
        //        compositeKey += Uri.EscapeDataString(oauth_token_secret);
        //    }

        //    String oauth_signature = HMAC_SHA1Hasher(baseString, compositeKey);

        //    List<String> headerStringList = new List<String>();
        //    headerStringList.Add(String.Format("oauth_nonce=\"{0}\"", Uri.EscapeDataString(oauth_nonce)));
        //    headerStringList.Add(String.Format("oauth_signature_method=\"{0}\"", Uri.EscapeDataString(oauth_signature_method)));
        //    headerStringList.Add(String.Format("oauth_timestamp=\"{0}\"", Uri.EscapeDataString(oauth_timestamp)));
        //    headerStringList.Add(String.Format("oauth_consumer_key=\"{0}\"", Uri.EscapeDataString(oauth_consumer_key)));
        //    if (oauth_token != null)
        //    {
        //        headerStringList.Add("oauth_token" + String.Format("=\"{0}\"", Uri.EscapeDataString(oauth_token)));
        //    }
        //    headerStringList.Add(String.Format("oauth_signature=\"{0}\"", Uri.EscapeDataString(oauth_signature)));
        //    headerStringList.Add(String.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(oauth_version)));

        //    String authHeader = "OAuth " + String.Join(", ", headerStringList);

        //    textBlock1.Text += authHeader + Environment.NewLine;

        //    {
        //        String querytotal = twtQuery.GetQueryStringTotal();
        //        if (querytotal != "")
        //        {
        //            baseUrl += '?' + querytotal;
        //        }
        //    }

        //    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl);
        //    {
        //        String postquery = twtQuery.GetPostQueryString();
        //        if (postquery != "")
        //        {
        //            httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
        //        }
        //    }

        //    httpRequestMessage.Headers.Add("Authorization", authHeader);
        //    httpRequestMessage.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("NemoKachi", "Alpha"));
        //    using (HttpClient httpClientTemp = new HttpClient()) //{ Timeout = new TimeSpan(0, 0, 10) })
        //    {
        //        HttpResponseMessage response = await httpClientTemp.SendAsync(httpRequestMessage);
        //        DisplayTextResult(response, textBlock1);
        //        return response;
        //    }
        //}

        public static async Task<String> ConvertStreamAsync(HttpContent content)
        {
            List<Char> list = new List<Char>();
            await Task.Run(async delegate
            {
                while (list.Count != content.Headers.ContentLength)
                {
                    Byte[] buffer = new Byte[1000];
                    await (await content.ReadAsStreamAsync()).ReadAsync(buffer, 0, 1000);
                    foreach (Byte b in buffer)
                    {
                        Char ch = Convert.ToChar(b);
                        switch (ch)
                        {
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
            });
            return new String(list.ToArray());
        }

        public class NoStreamerException : Exception
        {
            public NoStreamerException()
            {

            }

            public NoStreamerException(String message)
                : base(message)
            {

            }

            public NoStreamerException(String message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        //public async Task AddStreamer(TweetPanel twtPanel, String DistinctName)//DistinctName을 enum 형식으로 바꾸기?
        //{
        //    if (ReturnStreamer(DistinctName) == null)
        //    {
        //        switch (DistinctName)
        //        {
        //            case "UserStream":
        //                {
        //                    ITwitterStreamer UserStream;
        //                    //StreamingState = UserStreamingState.Streaming;
        //                    using (UserStream = new TwitterClient.UserStreamer(twtPanel, this)) // 나중엔 리스트에 넣고 확인하는 방식으로 교체
        //                    {
        //                        TwitterStreamers.Add(UserStream);
        //                        try
        //                        {
        //                            await UserStream.ActivateAsync();
        //                        }
        //                        finally
        //                        {
        //                            //auto dispose
        //                            UserStream = null;
        //                            TwitterStreamers.Remove(UserStream);
        //                        }
        //                    }
        //                    //StreamingState = UserStreamingState.None;
        //                    break;
        //                }
        //        }
        //    }
        //    else
        //    {
        //        throw new StreamerDuplicatedException();
        //    }
        //}

        //public ITwitterStreamer ReturnStreamer(String DistinctName)
        //{
        //    foreach (ITwitterStreamer its in TwitterStreamers)
        //    {
        //        if (its.DistinctName == DistinctName)
        //        {
        //            return its;
        //        }
        //    }
        //    return null;
        //}

        //public void StopStreamer(String DistinctName)
        //{
        //    ITwitterStreamer delete = ReturnStreamer(DistinctName);
        //    if (delete != null)
        //    {
        //        delete.Dispose();
        //        TwitterStreamers.Remove(delete);
        //    }
        //    else
        //    {
        //        throw new NoStreamerException();
        //    }
        //}

        public async Task<HttpResponseMessage> NonAuthStream(HttpMethod reqMethod, String baseUrl, TwitterRequest twRequest)
        {
            {
                String querytotal = twRequest.GetQueryStringTotal();
                if (querytotal != "")
                {
                    baseUrl += '?' + querytotal;
                }
            }

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl);
            {
                String postquery = twRequest.GetPostQueryString();
                if (postquery != "")
                {
                    httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
            }

            httpRequestMessage.Headers.UserAgent.Add(UserAgent);
            using (HttpClient httpClientTemp = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false }))//{ Timeout = new TimeSpan(0, 0, 10) }
            {
                return await httpClientTemp.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            }
        }

        public async Task<HttpResponseMessage> OAuthStream(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterRequest twRequest, String callbackUri)
        {
            const String oauth_version = "1.0";
            const String oauth_signature_method = "HMAC-SHA1";
            String oauth_nonce = Convert.ToBase64String(new UTF8Encoding().GetBytes(DateTime.Now.Ticks.ToString()));
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            String oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();

            List<String> baseStringList = new List<String>();
            if (callbackUri != null)
            {
                baseStringList.Add("oauth_callback=" + Uri.EscapeDataString(callbackUri));
            }
            baseStringList.Add("oauth_consumer_key=" + oauth_consumer_key);
            baseStringList.Add("oauth_nonce=" + oauth_nonce);
            baseStringList.Add("oauth_signature_method=" + oauth_signature_method);
            baseStringList.Add("oauth_timestamp=" + oauth_timestamp);
            if (aToken.oauth_token != null)
            {
                baseStringList.Add("oauth_token=" + aToken.oauth_token);
            }
            baseStringList.Add("oauth_version=" + oauth_version);


            String baseString = String.Concat(reqMethod, "&", Uri.EscapeDataString(baseUrl));
            {
                String AddString = "";
                {
                    String query1 = twRequest.GetQueryStringPart1();
                    if (query1 != "")
                    {
                        AddString += query1;
                    }
                }
                if (AddString != "")
                {
                    AddString += '&';
                }
                AddString += String.Join("&", baseStringList);
                {
                    String postquery = twRequest.GetPostQueryString();
                    if (postquery != "")
                    {
                        AddString += '&' + postquery;
                    }
                }
                {
                    String query2 = twRequest.GetQueryStringPart2();
                    if (query2 != "")
                    {
                        AddString += '&' + query2;
                    }
                }
                baseString += '&' + Uri.EscapeDataString(AddString);
            }
            System.Diagnostics.Debug.WriteLine(baseString);

            String compositeKey = Uri.EscapeDataString(oauth_consumer_secret) + "&";
            if (aToken.oauth_token_secret != null)
            {
                compositeKey += Uri.EscapeDataString(aToken.oauth_token_secret);
            }

            String oauth_signature = HMAC_SHA1Hasher(baseString, compositeKey);

            List<String> headerStringList = new List<String>();
            if (callbackUri != null)
            {
                headerStringList.Add("oauth_callback" + String.Format("=\"{0}\"", Uri.EscapeDataString(callbackUri)));
            }
            headerStringList.Add(String.Format("oauth_nonce=\"{0}\"", Uri.EscapeDataString(oauth_nonce)));
            headerStringList.Add(String.Format("oauth_signature_method=\"{0}\"", Uri.EscapeDataString(oauth_signature_method)));
            headerStringList.Add(String.Format("oauth_timestamp=\"{0}\"", Uri.EscapeDataString(oauth_timestamp)));
            headerStringList.Add(String.Format("oauth_consumer_key=\"{0}\"", Uri.EscapeDataString(oauth_consumer_key)));
            if (aToken.oauth_token != null)
            {
                headerStringList.Add("oauth_token" + String.Format("=\"{0}\"", Uri.EscapeDataString(aToken.oauth_token)));
            }
            headerStringList.Add(String.Format("oauth_signature=\"{0}\"", Uri.EscapeDataString(oauth_signature)));
            headerStringList.Add(String.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(oauth_version)));

            String authHeader = "OAuth " + String.Join(", ", headerStringList);

            System.Diagnostics.Debug.WriteLine(authHeader);

            {
                String querytotal = twRequest.GetQueryStringTotal();
                if (querytotal != "")
                {
                    baseUrl += '?' + querytotal;
                }
            }

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl);
            {
                String postquery = twRequest.GetPostQueryString();
                if (postquery != "")
                {
                    httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
            }

            httpRequestMessage.Headers.Add("Authorization", authHeader);
            httpRequestMessage.Headers.UserAgent.Add(UserAgent);
            using (HttpClient httpClientTemp = new HttpClient())//{ Timeout = new TimeSpan(0, 0, 10) }
            {
                return await httpClientTemp.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            }
        }

        public async Task<HttpResponseMessage> OAuthSocket(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterRequest twRequest)
        {
            const String oauth_version = "1.0";
            const String oauth_signature_method = "HMAC-SHA1";
            String oauth_nonce = Convert.ToBase64String(new UTF8Encoding().GetBytes(DateTime.Now.Ticks.ToString()));
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            String oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();

            List<String> baseStringList = new List<String>();
            baseStringList.Add("oauth_consumer_key=" + oauth_consumer_key);
            baseStringList.Add("oauth_nonce=" + oauth_nonce);
            baseStringList.Add("oauth_signature_method=" + oauth_signature_method);
            baseStringList.Add("oauth_timestamp=" + oauth_timestamp);
            if (aToken.oauth_token != null)
            {
                baseStringList.Add("oauth_token=" + aToken.oauth_token);
            }
            baseStringList.Add("oauth_version=" + oauth_version);


            String baseString = String.Concat(reqMethod, "&", Uri.EscapeDataString(baseUrl));
            {
                String AddString = "";
                {
                    String query1 = twRequest.GetQueryStringPart1();
                    if (query1 != "")
                    {
                        AddString += query1;
                    }
                }
                if (AddString != "")
                {
                    AddString += '&';
                }
                AddString += String.Join("&", baseStringList);
                {
                    String postquery = twRequest.GetPostQueryString();
                    if (postquery != "")
                    {
                        AddString += '&' + postquery;
                    }
                }
                {
                    String query2 = twRequest.GetQueryStringPart2();
                    if (query2 != "")
                    {
                        AddString += '&' + query2;
                    }
                }
                baseString += '&' + Uri.EscapeDataString(AddString);
            }
            System.Diagnostics.Debug.WriteLine(baseString);

            String compositeKey = Uri.EscapeDataString(oauth_consumer_secret) + "&";
            if (aToken.oauth_token_secret != null)
            {
                compositeKey += Uri.EscapeDataString(aToken.oauth_token_secret);
            }

            String oauth_signature = HMAC_SHA1Hasher(baseString, compositeKey);

            List<String> headerStringList = new List<String>();
            headerStringList.Add(String.Format("oauth_nonce=\"{0}\"", Uri.EscapeDataString(oauth_nonce)));
            headerStringList.Add(String.Format("oauth_signature_method=\"{0}\"", Uri.EscapeDataString(oauth_signature_method)));
            headerStringList.Add(String.Format("oauth_timestamp=\"{0}\"", Uri.EscapeDataString(oauth_timestamp)));
            headerStringList.Add(String.Format("oauth_consumer_key=\"{0}\"", Uri.EscapeDataString(oauth_consumer_key)));
            if (aToken.oauth_token != null)
            {
                headerStringList.Add("oauth_token" + String.Format("=\"{0}\"", Uri.EscapeDataString(aToken.oauth_token)));
            }
            headerStringList.Add(String.Format("oauth_signature=\"{0}\"", Uri.EscapeDataString(oauth_signature)));
            headerStringList.Add(String.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(oauth_version)));

            String authHeader = "OAuth " + String.Join(", ", headerStringList);

            System.Diagnostics.Debug.WriteLine(authHeader);

            {
                String querytotal = twRequest.GetQueryStringTotal();
                if (querytotal != "")
                {
                    baseUrl += '?' + querytotal;
                }
            }

            //Windows.Networking.Sockets.StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();

            //System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
            //cts.CancelAfter(5000);


            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl);
            {
                String postquery = twRequest.GetPostQueryString();
                if (postquery != "")
                {
                    httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
            }

            httpRequestMessage.Headers.Add("Authorization", authHeader);
            httpRequestMessage.Headers.UserAgent.Add(UserAgent);
            using (HttpClient httpClientTemp = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }) { Timeout = new TimeSpan(0, 0, 5) })
            {
                while (true)
                {
                    HttpResponseMessage response = null;
                    try
                    {
                        response = await httpClientTemp.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch (TimeoutException)
                    {
                        Int32 Timeout = httpClientTemp.Timeout.Seconds * 2;
                        if (Timeout < 270)
                        {
                            if (Timeout < 30)
                            {
                                httpClientTemp.Timeout = new TimeSpan(0, 0, 30);
                            }
                            else
                            {
                                httpClientTemp.Timeout = new TimeSpan(0, 0, httpClientTemp.Timeout.Seconds * 2);
                            }
                        }
                        else
                        {
                            httpClientTemp.Timeout = new TimeSpan(0, 4, 30);
                        }
                    }
                    //(await response.Content.ReadAsStreamAsync()).ReadTimeout = 90000;
                    return response;
                }
            }
        }

        //public enum UserStreamingState
        //{
        //    None, Connecting, Streaming
        //}

        //public UserStreamingState StreamingState
        //{
        //    get
        //    {
        //        return (UserStreamingState)GetValue(StreamingProperty);
        //    }
        //    private set
        //    {
        //        SetValue(StreamingProperty, value);
        //    }
        //}

        //public async Task<WebView> Login()
        //{
        //    oauth_token = oauth_token_secret = null;
        //    using (HttpResponseMessage response = await OAuthStream(
        //    HttpMethod.Post,
        //    "https://api.twitter.com/oauth/request_token",
        //    TwitterRequest.MakeRequest()))
        //    {
        //        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            Dictionary<String, String> loginparams = HTTPQuery(await TwitterClient.ConvertStreamAsync(response.Content));

        //            if (loginparams["oauth_callback_confirmed"] == "true")
        //            {
        //                WebView webView1 = new WebView() { Margin = new Thickness() { Bottom = 50, Left = 50, Right = 50, Top = 50 } };
        //                webView1.Navigate(new Uri("https://api.twitter.com/oauth/authenticate?oauth_token=" + loginparams["oauth_token"]));

        //                webView1.LoadCompleted += new Windows.UI.Xaml.Navigation.LoadCompletedEventHandler(webView1_LoadCompleted);
        //                //oauth_token = loginparams[0, 1];
        //                oauth_token_secret = loginparams["oauth_token_secret"];
        //                return webView1;
        //            }
        //            else
        //            {
        //                throw new Exception("Login Failed, oauth_callback is not confirmed.");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("Login Failed");
        //        }
        //    }
        //}


        //private void DisplayTextResult(HttpResponseMessage response, TextBlock output)
        //{
        //    //string responseBodyAsText;

        //    output.Text += "Status: " + response.StatusCode + Environment.NewLine;
        //    output.Text += "Reason: " + response.ReasonPhrase + Environment.NewLine;
        //    //responseBodyAsText = response.Content.ReadAsString();
        //    //responseBodyAsText = responseBodyAsText.Replace("<br>", Environment.NewLine); // Insert new lines
        //    //output.Text += Environment.NewLine + responseBodyAsText;
        //    output.Text += response.Content.ReadAsString() + Environment.NewLine;
        //}
    }
}
