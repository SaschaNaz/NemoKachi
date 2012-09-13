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


namespace NemoKachi.TwitterWrapper
{
    public partial class TwitterClient
    {
        public interface ILoginVisualizer
        {
            LoginPhase Phase { get; set; }
            /// <summary>
            /// An ILoginVisualizer has to make a WebView and some other UI things with Authorization URI to let the user go authorization page.
            /// The ILoginVisualizer must immediatly return the WebView after the settings are completed.
            /// </summary>
            /// <param name="AuthUri">An URI that a WebView will initially navigate to.</param>
            /// <returns></returns>
            WebView GetWebView();
            void SetWebView();
            /// <summary>
            /// Removes WebView from ILoginVisualizer
            /// </summary>
            void RemoveWebView();
            Boolean IsWebViewSet { get; }
            event RoutedEventHandler Closed;
        }
        public enum LoginPhase
        {
            /// <summary>
            /// Phase 1.
            /// </summary>
            WaitingOAuthCallback,
            /// <summary>
            /// Phase 2.
            /// </summary>
            AuthorizingApp,
            /// <summary>
            /// Phase 3.
            /// </summary>
            VerifyingTempToken,
            /// <summary>
            /// Phase 4.
            /// </summary>
            AccessingToken,
            /// <summary>
            /// Phase 5
            /// </summary>
            LoadingAccountInformation,
            /// <summary>
            /// Phase 6
            /// </summary>
            GettingAccountImageURI
        }


        public class LoginHandler
        {
            event LoginCompletedEventHandler LoginCompleted;
            delegate void LoginCompletedEventHandler(object sender, LoginCompletedEventArgs e);
            public class LoginCompletedEventArgs : EventArgs
            {
                public Boolean Succeed;
                public Exception InnerException;
                public AccountToken AuthedAccountToken;
            }

            public enum LoginMessage
            {
                Succeed, UserDenied, Failed
            }
            protected virtual void OnLoginCompleted(LoginCompletedEventArgs e)
            {
                if (LoginCompleted != null)
                {
                    LoginCompleted(this, e);
                }
            }

            readonly TwitterClient Client;
            readonly ILoginVisualizer Vis;
            readonly String CallbackUri;

            public LoginHandler(TwitterClient client, ILoginVisualizer vis, String callbackUri)
            {
                Client = client;
                Vis = vis;
                CallbackUri = callbackUri;
            }

            public Task<AccountToken> AccountLoginAsync()
            {
                var taskSource = new TaskCompletionSource<AccountToken>();

                LoginCompletedEventHandler completedHandler = null;
                RoutedEventHandler closedHandler = null;
                completedHandler = delegate(Object sender, LoginCompletedEventArgs e)
                {
                    LoginCompleted -= completedHandler;
                    Vis.Closed -= closedHandler;
                    if (e.Succeed)
                        taskSource.SetResult(e.AuthedAccountToken);
                    else
                    {
                        if (e.InnerException == null)
                            taskSource.SetCanceled();
                        else
                            taskSource.SetException(e.InnerException);
                    }
                };
                LoginCompleted += completedHandler;

                closedHandler = delegate(Object sender, RoutedEventArgs e)
                {
                    LoginCompleted -= completedHandler;
                    Vis.Closed -= closedHandler;
                    taskSource.SetCanceled();
                };
                Vis.Closed += closedHandler;

                LoginAsync(taskSource);

                return taskSource.Task;
            }

            public async void LoginAsync(TaskCompletionSource<AccountToken> tcs)
            {
                AccountToken Token = new AccountToken();
                //"Recieving OAuth callback...";
                Vis.Phase = LoginPhase.WaitingOAuthCallback;
                using (HttpResponseMessage response = await Client.OAuthRequestAsync(
                    Token,
                    HttpMethod.Post,
                    "https://api.twitter.com/oauth/request_token",
                    new TwitterParameter(), CallbackUri, HttpCompletionOption.ResponseContentRead))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Dictionary<String, String> loginparams = TwitterClient.HTTPQuery(await response.Content.ReadAsStringAsync());

                        if (loginparams["oauth_callback_confirmed"] == "true")
                        {
                            //"Authorizing this app on your account...";
                            Vis.Phase = LoginPhase.AuthorizingApp;
                            WebView webView1 = Vis.GetWebView();
                            Windows.UI.Xaml.Navigation.LoadCompletedEventHandler handler = null;
                            handler = new Windows.UI.Xaml.Navigation.LoadCompletedEventHandler(
                                async delegate(Object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
                                {
                                    if (tcs.Task.Status == System.Threading.Tasks.TaskStatus.WaitingForActivation)
                                    {
                                        if (String.Format("{0}://{1}{2}", e.Uri.Scheme, e.Uri.Host, e.Uri.AbsolutePath) == CallbackUri)
                                        {
                                            //"Verifying your temporary twitter token...";
                                            Vis.Phase = LoginPhase.VerifyingTempToken;
                                            Vis.RemoveWebView();
                                            String webparam = e.Uri.Query;
                                            {
                                                Dictionary<String, String> dict = TwitterClient.HTTPQuery(webparam.Substring(1));
                                                if (!dict.ContainsKey("denied"))
                                                {
                                                    await webView1_LoadCompleted(Token, dict);
                                                    OnLoginCompleted(
                                                        new LoginCompletedEventArgs()
                                                        {
                                                            AuthedAccountToken = Token,
                                                            Succeed = true
                                                        });
                                                }
                                                else
                                                {
                                                    OnLoginCompleted(new LoginCompletedEventArgs() { Succeed = false });
                                                }
                                            }
                                        }
                                        else if(!Vis.IsWebViewSet)
                                        {
                                            Vis.SetWebView();
                                        }
                                    }
                                    else
                                    {
                                        webView1.LoadCompleted -= handler;
                                    }
                                });
                            webView1.LoadCompleted += handler;
                            webView1.Navigate(new Uri("https://api.twitter.com/oauth/authorize?oauth_token=" + loginparams["oauth_token"]));
                            Token.oauth_token_secret = loginparams["oauth_token_secret"];
                        }
                        else
                        {
                            //Vis.CurrentMessage = "Login Failed, oauth_callback confirming failed.";
                        }
                    }
                    else
                    {
                        //Vis.CurrentMessage = await ConvertStreamAsync(response.Content);
                    }
                }
            }

            public async Task webView1_LoadCompleted(AccountToken Token, Dictionary<String, String> webparams)
            {
                //"Accessing your twitter token...";
                Vis.Phase = LoginPhase.AccessingToken;
                try
                {
                    Token.oauth_token = webparams["oauth_token"];
                    using (HttpResponseMessage response = await Client.OAuthRequestAsync(Token, HttpMethod.Post, "https://api.twitter.com/oauth/access_token",
                        new TwitterParameter(new TwitterParameter.QueryKeyValue("oauth_verifier", webparams["oauth_verifier"], TwitterParameter.RequestType.Post)), null, HttpCompletionOption.ResponseContentRead))
                    {

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            //"Loading your account information...";
                            Vis.Phase = LoginPhase.LoadingAccountInformation;
                            Dictionary<String, String> loginparams = TwitterClient.HTTPQuery(await response.Content.ReadAsStringAsync());

                            Token.oauth_token = loginparams["oauth_token"];
                            Token.oauth_token_secret = loginparams["oauth_token_secret"];
                            Token.AccountId = Convert.ToUInt64(loginparams["user_id"]);
                            Token.AccountName = loginparams["screen_name"];

                            //"Accessing your account image...";
                            Vis.Phase = LoginPhase.GettingAccountImageURI;
                            Token.AccountInformation = await Client.UsersShowAsync(Token, new UsersShowParameter() { user_id = Token.AccountId }, new GetStatusParameter());
                            //using (HttpResponseMessage userresponse = await Client.GetUserProfileImage(Token, Token.AccountName))
                            //{
                            //    if (userresponse.StatusCode == System.Net.HttpStatusCode.Redirect)
                            //    {
                            //        //"Loading your account image...";
                            //        Token.AccountImageUri = userresponse.Headers.Location;
                            //        //Vis.Progress = 5;
                            //    }
                            //    else
                            //    {
                            //        //Vis.CurrentMessage = userresponse.ReasonPhrase;
                            //    }
                            //}
                        }
                        else
                        {
                            //Vis.CurrentMessage = response.ReasonPhrase;
                        }
                    };
                }
                catch (HttpRequestException hre)
                {
                    System.Diagnostics.Debug.WriteLine(hre.ToString());
                }
                catch (Exception ex)
                {
                    // For debugging
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        //public event LoginSucceedEventHandler LoginSucceed;
        //public delegate void LoginSucceedEventHandler(object sender, LoginSucceedEventArgs e);
        //public class LoginSucceedEventArgs : EventArgs
        //{
        //    public TwitterDatas.TwitterUser UserData;
        //}
        //protected virtual void OnLoginSucceed(LoginSucceedEventArgs e)
        //{
        //    if (LoginSucceed != null)
        //    {
        //        LoginSucceed(this, e);
        //    }
        //}

        //public void LoadLoggedOn(String _oauth_token, String _oauth_token_secret, UInt64 accountUserId)
        //{
        //    oauth_token = _oauth_token;
        //    oauth_token_secret = _oauth_token_secret;
        //    AccountUserId = accountUserId;
        //}
    }
}
