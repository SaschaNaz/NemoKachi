﻿using System;
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
        public TwitterUser AccountInformation
        {
            get { return (TwitterUser)GetValue(AccountImageUriProperty); }
            set { SetValue(AccountImageUriProperty, value); }
        }

        public static readonly DependencyProperty AccountNameProperty =
            DependencyProperty.Register("AccountName",
            typeof(String),
            typeof(AccountToken),
            new PropertyMetadata(null));

        public static readonly DependencyProperty AccountImageUriProperty =
            DependencyProperty.Register("AccountInformation",
            typeof(TwitterUser),
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

        public Exception TwitterExceptionParse(System.Net.HttpStatusCode errorcode, Windows.Data.Json.JsonObject errorobject)
        {
            IJsonValue errors;
            if (errorobject.TryGetValue("errors", out errors))
            {
                if (errors.ValueType == JsonValueType.String)
                {
                    return new TwitterParameterStringException(errorcode, errors.GetString());
                }
                else if (errors.ValueType == JsonValueType.Array)
                {
                    return TwitterParameterException.Parse(errorcode, errors.GetArray()[0].GetObject());
                }
                else
                {
                    return new Exception(
                        String.Format(
                            "App cannot understand Twitter's error message. If you screenshot this screen and send it to the developer (see Option tab), the person will thank you very much!\r\n{0}",
                            errorobject.Stringify()));
                }
            }
            else
            {
                return TwitterParameterProtectedException.Parse(errorcode, errorobject);
            }
        }


        public async Task<Tweet> StatusesUpdateAsync(AccountToken aToken, StatusesUpdateParameter tweetQuery)
        {
            return await StatusesUpdateAsync(aToken, tweetQuery, null);
        }

        /// <summary>
        /// 일반 트윗, 또는 멘션 등의 트윗을 보냅니다.
        /// </summary>
        /// <param name="status">트윗에 넣을 텍스트입니다</param>
        /// <returns>리퀘스트에 대한 HTTP Response 메시지를 반환합니다</returns>
        public async Task<Tweet> StatusesUpdateAsync(AccountToken aToken, StatusesUpdateParameter tweetQuery, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = tweetQuery;
            HttpCompletionOption completionOption;
            if (getstatus != null)
            {
                twtRequest.MergeGetStatusParameter(getstatus);
                completionOption = HttpCompletionOption.ResponseContentRead;
            }
            else
                completionOption = HttpCompletionOption.ResponseHeadersRead;
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1.1/statuses/update.json", twtRequest, null, completionOption))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (getstatus != null)
                        return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                    else
                        return null;
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet> StatusesShowAsync(AccountToken aToken, StatusesShowParameter tweetQuery, UInt64 Id)
        {
            return await StatusesShowAsync(aToken, tweetQuery, Id, null);
        }

        public async Task<Tweet> StatusesShowAsync(AccountToken aToken, StatusesShowParameter tweetQuery, UInt64 Id, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = tweetQuery;
            if (getstatus == null)
                throw new Exception("StatusesShowAsync needs valid GetStatusParameter parameter.");
            twtRequest.MergeGetStatusParameter(getstatus);
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Get,
                String.Format("https://api.twitter.com/1.1/statuses/show/{0}.json", Id), twtRequest))
            {
                if (response.IsSuccessStatusCode)
                {
                    return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet> StatusesRetweetAsync(AccountToken aToken, UInt64 Id)
        {
            return await StatusesRetweetAsync(aToken, Id, null);
        }

        public async Task<Tweet> StatusesRetweetAsync(AccountToken aToken, UInt64 Id, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = new TwitterParameter();
            HttpCompletionOption completionOption;
            if (getstatus != null)
            {
                twtRequest.MergeGetStatusParameter(getstatus);
                completionOption = HttpCompletionOption.ResponseContentRead;
            }
            else
                completionOption = HttpCompletionOption.ResponseHeadersRead;
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                String.Format("https://api.twitter.com/1.1/statuses/retweet/{0}.json", Id), twtRequest, completionOption))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (getstatus != null)
                        return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                    else
                        return null;
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet> StatusesDestroyAsync(AccountToken aToken, UInt64 Id)
        {
            return await StatusesDestroyAsync(aToken, Id, null);
        }

        public async Task<Tweet> StatusesDestroyAsync(AccountToken aToken, UInt64 Id, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = new TwitterParameter();
            HttpCompletionOption completionOption;
            if (getstatus != null)
            {
                twtRequest.MergeGetStatusParameter(getstatus);
                completionOption = HttpCompletionOption.ResponseContentRead;
            }
            else
                completionOption = HttpCompletionOption.ResponseHeadersRead;
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                String.Format("https://api.twitter.com/1.1/statuses/destroy/{0}.json", Id), twtRequest, completionOption))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (getstatus != null)
                        return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                    else
                        return null;
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet> FavoriteCreateAsync(AccountToken aToken, UInt64 Id)
        {
            return await FavoriteCreateAsync(aToken, Id, null);
        }

        public async Task<Tweet> FavoriteCreateAsync(AccountToken aToken, UInt64 Id, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = new TwitterParameter(
                new TwitterParameter.QueryKeyValue("id", Id.ToString(), TwitterParameter.RequestType.Type1));
            HttpCompletionOption completionOption;
            if (getstatus != null)
            {
                twtRequest.MergeGetStatusParameter(getstatus);
                completionOption = HttpCompletionOption.ResponseContentRead;
            }
            else
                completionOption = HttpCompletionOption.ResponseHeadersRead;
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1.1/favorites/create.json", twtRequest, completionOption))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (getstatus != null)
                        return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                    else
                        return null;
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet> FavoriteDestroyAsync(AccountToken aToken, UInt64 Id)
        {
            return await FavoriteDestroyAsync(aToken, Id, null);
        }

        public async Task<Tweet> FavoriteDestroyAsync(AccountToken aToken, UInt64 Id, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = new TwitterParameter(
                new TwitterParameter.QueryKeyValue("id", Id.ToString(), TwitterParameter.RequestType.Type1));
            HttpCompletionOption completionOption;
            if (getstatus != null)
            {
                twtRequest.MergeGetStatusParameter(getstatus);
                completionOption = HttpCompletionOption.ResponseContentRead;
            }
            else
                completionOption = HttpCompletionOption.ResponseHeadersRead;
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1.1/favorites/destroy.json", twtRequest, completionOption))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (getstatus != null)
                        return new Tweet(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                    else
                        return null;
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet[]> StatusesHometimelineAsync(AccountToken aToken, StatusesHometimelineParameter tweetQuery, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = tweetQuery;
            if (getstatus == null)
                throw new Exception("StatusesHometimelineAsync needs valid GetStatusParameter parameter.");
            twtRequest.MergeGetStatusParameter(getstatus);
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1.1/statuses/home_timeline.json", twtRequest))
            {
                if (response.IsSuccessStatusCode)
                {
                    List<Tweet> tweets = new List<Tweet>();
                    JsonArray jary = JsonArray.Parse(await response.Content.ReadAsStringAsync());
                    foreach (JsonValue jo in jary)
                    {
                        tweets.Add(new Tweet(jo.GetObject()));
                    }
                    return tweets.ToArray();
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        public async Task<Tweet[]> StatusesMentionsAsync(AccountToken aToken, StatusesMentionsParameter tweetQuery, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = tweetQuery;
            if (getstatus == null)
                throw new Exception("StatusesMentionsAsync needs valid GetStatusParameter parameter.");
            twtRequest.MergeGetStatusParameter(getstatus);
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Post,
                "https://api.twitter.com/1.1/statuses/mentions.json", twtRequest))
            {
                if (response.IsSuccessStatusCode)
                {
                    List<Tweet> tweets = new List<Tweet>();
                    JsonArray jary = JsonArray.Parse(await response.Content.ReadAsStringAsync());
                    foreach (JsonValue jo in jary)
                    {
                        tweets.Add(new Tweet(jo.GetObject()));
                    }
                    return tweets.ToArray();
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

        /// <summary>
        /// 타임라인을 리프레시합니다.
        /// </summary>
        /// <returns>리퀘스트에 대한 HTTP Response 메시지를 반환합니다. 리프레시된 트윗들이 컨텐트로 포함됩니다.</returns>
        public async Task<Tweet[]> RefreshAsync(AccountToken aToken, TwitterWrapper.ITimelineData tlData)
        {
            List<Tweet> tweets = new List<Tweet>();
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Get,
                tlData.RestURI.OriginalString, tlData.GetRequest()))
            {
                String str = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine(str);
                JsonArray jary = JsonArray.Parse(str);
                foreach (JsonValue jo in jary)
                {
                    tweets.Add(new Tweet(jo.GetObject()));
                }
                return tweets.ToArray();
            }
        }

        //public async Task<HttpResponseMessage> RefreshStream(AccountToken aToken, String url, TwitterParameter requestQuery)
        //{
        //    return await OAuthSocket(
        //        aToken, HttpMethod.Get,
        //        url, requestQuery);//new RefreshQuery() { include_TwitterEntities = true, include_rts = true }
        //}

        public async Task<TwitterUser> UsersShowAsync(AccountToken aToken, UsersShowParameter tweetQuery, GetStatusParameter getstatus)
        {
            TwitterParameter twtRequest = tweetQuery;
            if (getstatus == null)
                throw new Exception("UsersShowAsync needs valid GetStatusParameter parameter.");
            twtRequest.MergeGetStatusParameter(getstatus);
            using (HttpResponseMessage response = await OAuthRequestAsync(
                aToken, HttpMethod.Get,
                "https://api.twitter.com/1.1/users/show.json", twtRequest))
            {
                if (response.IsSuccessStatusCode)
                {
                    return new TwitterUser(JsonObject.Parse(await response.Content.ReadAsStringAsync()));
                }
                else
                {
                    String message = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(message);
                    throw TwitterExceptionParse(response.StatusCode, Windows.Data.Json.JsonObject.Parse(message));
                }
            }
        }

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

        //public class NoStreamerException : Exception
        //{
        //    public NoStreamerException()
        //    {

        //    }

        //    public NoStreamerException(String message)
        //        : base(message)
        //    {

        //    }

        //    public NoStreamerException(String message, Exception innerException)
        //        : base(message, innerException)
        //    {

        //    }
        //}

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

        public async Task<HttpResponseMessage> OAuthRequestAsync(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest)
        {
            return await OAuthRequestAsync(aToken, reqMethod, baseUrl, twRequest, null, HttpCompletionOption.ResponseContentRead);
        }

        public async Task<HttpResponseMessage> OAuthRequestAsync(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest, String callbackUri)
        {
            return await OAuthRequestAsync(aToken, reqMethod, baseUrl, twRequest, callbackUri, HttpCompletionOption.ResponseContentRead);
        }

        public async Task<HttpResponseMessage> OAuthRequestAsync(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest, HttpCompletionOption completionOption)
        {
            return await OAuthRequestAsync(aToken, reqMethod, baseUrl, twRequest, null, completionOption);
        }

        public String CreateOAuthHeader(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest, String callbackUri)
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

            String authHeader = String.Join(", ", headerStringList);

            System.Diagnostics.Debug.WriteLine(authHeader);

            return authHeader;
        }

        public async Task<HttpResponseMessage> OAuthRequestAsync(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest, String callbackUri, HttpCompletionOption completionOption)
        {
            String authHeader = CreateOAuthHeader(aToken, reqMethod, baseUrl, twRequest, callbackUri);

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

            httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", authHeader);
            httpRequestMessage.Headers.UserAgent.Add(UserAgent);
            using (HttpClient httpClientTemp = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false }))//{ Timeout = new TimeSpan(0, 0, 10) }
            {
                return await httpClientTemp.SendAsync(httpRequestMessage, completionOption);
            }
        }

        //public async Task TestMethod1(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest)
        //{
        //    #region authorization
        //    String authHeader = CreateOAuthHeader(aToken, reqMethod, baseUrl, twRequest, null);
        //    {
        //        String querytotal = twRequest.GetQueryStringTotal();
        //        if (querytotal != "")
        //        {
        //            baseUrl += '?' + querytotal;
        //        }
        //    }
        //    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl);
        //    String postquery = twRequest.GetPostQueryString();
        //    if (postquery != "")
        //    {
        //        httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
        //    }
        //    httpRequestMessage.Headers.Add("Authorization", authHeader);
        //    httpRequestMessage.Headers.UserAgent.Add(UserAgent);
        //    #endregion

        //    System.Diagnostics.Debug.WriteLine("Connection start");
        //    await TestMethod2(httpRequestMessage);

        //    System.Diagnostics.Debug.WriteLine("Stream successfully cancelled");
        //}


        //public async Task TestMethod2(HttpRequestMessage httpRequestMessage)
        //{
        //    using (HttpClient httpClient = new HttpClient())
        //    {
        //        using (HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
        //        {
        //            using (var stream = await response.Content.ReadAsStreamAsync())
        //            {
        //                //do something with the stream
        //            }
        //        }
        //    }
        //}

        //https://dev.twitter.com/docs/streaming-apis/connecting
        public IAsyncActionWithProgress<Object> OAuthStreamConnectAsync(AccountToken aToken, HttpMethod reqMethod, String baseUrl, TwitterParameter twRequest)
        {
            return System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run(async delegate(System.Threading.CancellationToken cancellationToken, IProgress<Object> progress)
            {
                Int32 Timeout = 5000;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Boolean ConnectionFailed = false;
                    try
                    {
                        #region authorization
                        String authHeader = CreateOAuthHeader(aToken, reqMethod, baseUrl, twRequest, null);
                        {
                            String querytotal = twRequest.GetQueryStringTotal();
                            if (querytotal != "")
                            {
                                baseUrl += '?' + querytotal;
                            }
                        }
                        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(reqMethod, baseUrl))
                        {
                            String postquery = twRequest.GetPostQueryString();
                            if (postquery != "")
                            {
                                httpRequestMessage.Content = new StringContent(postquery, Encoding.UTF8, "application/x-www-form-urlencoded");
                            }
                            httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", authHeader);
                            httpRequestMessage.Headers.UserAgent.Add(UserAgent);
                        #endregion

                            System.Diagnostics.Debug.WriteLine("Connection start");
                            await ConnectSocket(httpRequestMessage, new TimeSpan(0, 0, 5), cancellationToken, progress);
                        }
                    }
                    catch (System.IO.IOException)//stream.ReadAsync failed in action
                    {
                        System.Diagnostics.Debug.WriteLine("Stream disconnected: served data stream is suddenly cut");
                    }
                    catch (HttpRequestException)//이 때는 GET 자체가 실패한 것
                    {
                        ConnectionFailed = true;
                        System.Diagnostics.Debug.WriteLine("Stream connection failed");
                    }
                    catch (TaskCanceledException)//stream.ReadAsync could not be started
                    {
                        ConnectionFailed = true;
                        System.Diagnostics.Debug.WriteLine("Stream disconnected: cannot read the stream further."); 
                    }
                    //TimeoutException이 나면, 다시 연결 시도하려고 하지 말고 나중에 다시 연결 시도하도록 설득하기. Boolean false로 return값 보내기 (IAsyncOperation으로 수정)

                    if (ConnectionFailed)
                    {
                        if (Timeout <= 15000)
                        {
                            Timeout *= 2;
                        }
                        else
                            Timeout = 30000;
                    }
                    else
                        Timeout = 5000;

                    System.Diagnostics.Debug.WriteLine(String.Format("Wait {0} seconds and reconnect if not cancelled", Timeout / 1000));
                    await Task.Delay(Timeout);
                }

                System.Diagnostics.Debug.WriteLine("Stream successfully cancelled");
            });
        }

        async Task ConnectSocket(HttpRequestMessage httpRequestMessage, TimeSpan timeout, System.Threading.CancellationToken cancellationToken, IProgress<Object> progress)
        {
            
            using (HttpClient httpClient = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }) { Timeout = timeout })
            {
                using (HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                        return;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        List<Char> list = new List<Char>();
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            Byte[] buffer = new Byte[1000];
                            await stream.ReadAsync(buffer, 0, 1000);
                            foreach (Byte b in buffer)
                            {
                                Char ch = (Char)b;
                                switch (ch)
                                {
                                    case '\r':
                                        {
                                            if (list.Count > 0)
                                            {
                                                String str = new String(list.ToArray());
                                                try
                                                {
                                                    progress.Report(JsonObject.Parse(str));
                                                }
                                                catch
                                                {
                                                    System.Diagnostics.Debug.WriteLine("ERROR", str);
                                                    //textBlock1.Text += "UserStream: ERROR Ocurred";
                                                }
                                                list.Clear();
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

                        //bug - dispose won't end until the connection destroyed
                    }

                    System.Diagnostics.Debug.WriteLine("Stream reading finished");
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
        //    TwitterParameter.MakeRequest()))
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
