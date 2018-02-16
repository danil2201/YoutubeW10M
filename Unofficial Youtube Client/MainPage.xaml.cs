﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Oauth2.v2;
using VideoLibrary;
using YTApp.Classes;
using YTApp.Pages;
using YTApp.Classes.DataTypes;
using System.Collections.ObjectModel;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Util.Store;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;
using System.Text;

namespace YTApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string VideoID = "";

        public ObservableCollection<SubscriptionDataType> subscriptionsList = new ObservableCollection<SubscriptionDataType>();
        List<SearchListResponse> youtubeVideos = new List<SearchListResponse>();

        public string OAuthToken;

        BackgroundWorker bg = new BackgroundWorker();

        public event EventHandler SwitchToFullSize;

        public MainPage()
        {
            this.InitializeComponent();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            Startup();
        }

        #region Main Events

        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (contentFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                contentFrame.GoBack();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus(FocusState.Keyboard);
        }

        #endregion

        #region Startup
        private async void Startup()
        {
            //Check if user is authenticated and show startup page if not
            if (await IsUserAuthenticated())
                contentFrame.Navigate(typeof(WelcomePage), new NavigateParams() { mainPageRef = this, Refresh = true });
            else
            {
                await LoadSubscriptions();
                contentFrame.Navigate(typeof(HomePage), new NavigateParams() { mainPageRef = this, Refresh = true });
                UpdateLoginDetails();
            }

            //Plays Youtube link in clipboard
            PlayClipboardYLink();
        }

        public class MyWebClient
        {
            //The cookies will be here.
            private CookieContainer _cookies = GetUriCookieContainer(new Uri("https://accounts.google.com"));

            //In case you need to clear the cookies
            public void ClearCookies()
            {
                _cookies = new CookieContainer();
            }

            public async Task<HtmlDocument> GetPage(string url)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                //Set more parameters here...
                //...

                //This is the important part.
                request.CookieContainer = _cookies;

                WebResponse response = await request.GetResponseAsync();
                var stream = response.GetResponseStream();

                //When you get the response from the website, the cookies will be stored
                //automatically in "_cookies".

                using (var reader = new System.IO.StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    return doc;
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookieEx(
        string url,
        string cookieName,
        StringBuilder cookieData,
        ref int size,
        Int32 dwFlags,
        IntPtr lpReserved);

        private const Int32 InternetCookieHttponly = 0x2000;

        /// <summary>
        /// Gets the URI cookie container.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;
            // Determine the size of the cookie
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;
                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(
                    uri.ToString(),
                    null, cookieData,
                    ref datasize,
                    InternetCookieHttponly,
                    IntPtr.Zero))
                    return null;
            }
            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }

        private async void PlayClipboardYLink()
        {
            try
            {
                var dataPackageView = await Clipboard.GetContent().GetTextAsync();

                Uri uriResult;
                if (Uri.TryCreate(dataPackageView, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == "http" || uriResult.Scheme == "https"))
                {
                    if (Uri.IsWellFormedUriString("https://www.youtube.com/watch?v=", UriKind.Absolute))
                    {
                        StartVideo(dataPackageView.Remove(0, 32));
                    }
                    else if (Uri.IsWellFormedUriString("https://youtu.be/", UriKind.Absolute))
                    {
                        StartVideo(dataPackageView.Remove(0, 17));
                    }
                }
            }
            catch { }
        }

        private async Task<bool> IsUserAuthenticated()
        {
            GoogleAuthorizationCodeFlow.Initializer initializer = new GoogleAuthorizationCodeFlow.Initializer();
            var secrets = new ClientSecrets
            {
                ClientSecret = Constants.ClientSecret,
                ClientId = Constants.ClientID
            };
            initializer.ClientSecrets = secrets;
            initializer.DataStore = new PasswordVaultDataStore();
            var test = new AuthorizationCodeFlow(initializer);
            var token = await test.LoadTokenAsync("user", CancellationToken.None);
            if (token == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Menu

        public async Task<bool> LoadSubscriptions()
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
                ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm",
            }, new[] { YouTubeService.Scope.Youtube, Oauth2Service.Scope.UserinfoProfile }, "user", CancellationToken.None);

            // Create the service.
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Youtube Viewer",
            });

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyA4VfPxB0gsMtFRmOww8hPuOoRepPEv6W0",
                ApplicationName = this.GetType().ToString()
            });

            string nextPageToken;
            var tempSubscriptions = GetSubscriptions(null, service);

            List<SubscriptionDataType> subscriptionsListTemp = new List<SubscriptionDataType>();

            foreach (Subscription sub in tempSubscriptions.Items)
            {
                var subscription = new SubscriptionDataType
                {
                    Id = sub.Snippet.ResourceId.ChannelId,
                    Thumbnail = new BitmapImage(new Uri(sub.Snippet.Thumbnails.Medium.Url)),
                    Title = sub.Snippet.Title,
                    NewVideosCount = Convert.ToString(sub.ContentDetails.NewItemCount),
                    SubscriptionID = sub.Id
                };
                subscriptionsListTemp.Add(subscription);
            }
            if (tempSubscriptions.NextPageToken != null)
            {
                nextPageToken = tempSubscriptions.NextPageToken;
                while (nextPageToken != null)
                {
                    var tempSubs = GetSubscriptions(nextPageToken, service);
                    foreach (Subscription sub in tempSubs.Items)
                    {
                        var subscription = new SubscriptionDataType
                        {
                            Id = sub.Snippet.ResourceId.ChannelId,
                            Thumbnail = new BitmapImage(new Uri(sub.Snippet.Thumbnails.Medium.Url)),
                            Title = sub.Snippet.Title,
                            NewVideosCount = Convert.ToString(sub.ContentDetails.NewItemCount)
                        };
                        subscriptionsListTemp.Add(subscription);
                    }
                    nextPageToken = tempSubs.NextPageToken;
                }
            }
            subscriptionsListTemp.Sort((x, y) => string.Compare(x.Title, y.Title));
            subscriptionsList = new ObservableCollection<SubscriptionDataType>(subscriptionsListTemp);
            SubscriptionsList.ItemsSource = subscriptionsList;

            return true;
        }

        private void SubscriptionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var temp = (SubscriptionDataType)e.ClickedItem;
            contentFrame.Navigate(typeof(ChannelPage), new NavigateParams()
            {
                mainPageRef = this,
                ID = temp.Id
            });
        }

        public async void UpdateLoginDetails()
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
                ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm"
            }, new[] { Oauth2Service.Scope.UserinfoProfile }, "user", CancellationToken.None);


            // Create the service.
            var service = new Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Youtube Viewer",
            });

            var GetLoginInfo = service.Userinfo.Get();

            try
            {
                var LoginInfo = await GetLoginInfo.ExecuteAsync();

                txtLoginName.Text = LoginInfo.Name;

                var profileImg = new Windows.UI.Xaml.Media.ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(LoginInfo.Picture))
                };
                imgProfileIcon.Fill = profileImg;
            }
            catch (Exception)
            {
                txtLoginName.Text = "";
                imgProfileIcon.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            }
        }

        private void PageMenuControls_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (SplitViewItemDataType)e.ClickedItem;
            if (item.Text == "Home")
                contentFrame.Navigate(typeof(HomePage), new NavigateParams() { mainPageRef = this });
        }

        private void PlaylistOptions_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (SplitViewItemDataType)e.ClickedItem;
            if (item.Text == "Trending")
                contentFrame.Navigate(typeof(TrendingPage), new NavigateParams() { mainPageRef = this });
            else if (item.Text == "History")
                contentFrame.Navigate(typeof(HistoryPage), new NavigateParams() { mainPageRef = this, Refresh = true });
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBarSplitView.IsPaneOpen = !SideBarSplitView.IsPaneOpen;
        }

        #endregion

        #region Search

        #region Events

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(SearchPage), new NavigateParams() { mainPageRef = this });
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (Uri.IsWellFormedUriString(SearchBox.Text, UriKind.Absolute))
                {
                    PlayClipboardYLink();
                }
                contentFrame.Navigate(typeof(SearchPage), new NavigateParams() { mainPageRef = this });
            }
        }

        #endregion

        #region API

        private SubscriptionListResponse GetSubscriptions(string NextPageToken, YouTubeService service)
        {
            var subscriptions = service.Subscriptions.List("snippet, contentDetails");
            subscriptions.PageToken = NextPageToken;
            subscriptions.Mine = true;
            subscriptions.MaxResults = 50;

            return subscriptions.Execute();
        }
        #endregion

        #endregion

        #region Media Viewer

        #region Methods

        public void StartVideo(string Id)
        {
            videoFrame.Visibility = Visibility.Visible;
            videoFrame.Navigate(typeof(VideoPage), new NavigateParams() { mainPageRef = this, ID = Id });
        }

        #endregion

        #region Events

        public void Viewer_SwitchToFullSize(object sender, EventArgs e)
        {
            SwitchToFullSize.Invoke(this, new EventArgs());
        }

        #endregion

        #endregion

        #region Flyout

        private void Flyout_CopyLink(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("https://youtu.be/" + VideoID);
            Clipboard.SetContent(dataPackage);
        }

        private void Flyout_CopyLinkAtTime(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("https://youtu.be/" + VideoID + "?t=" + Convert.ToInt32(viewer.Position.TotalSeconds) + "s");
            Clipboard.SetContent(dataPackage);
        }


        #endregion

        #region Login Region
        private async void btnSignOut_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
                ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm"
            }, new[] { YouTubeService.Scope.Youtube, Oauth2Service.Scope.UserinfoProfile }, "user", CancellationToken.None);

            await credential.RevokeTokenAsync(CancellationToken.None);

            //Clear Login details
            txtLoginName.Text = "";
            imgProfileIcon.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

            //Clear Subscriptions
            SubscriptionsList.ItemsSource = null;

            contentFrame.Navigate(typeof(WelcomePage), new NavigateParams() { mainPageRef = this, Refresh = true });
        }

        private void btnLoginFlyout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void btnMyChannel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = Constants.ClientID,
                ClientSecret = Constants.ClientSecret
            }, new[] { Oauth2Service.Scope.UserinfoProfile }, "user", CancellationToken.None);


            // Create the service.
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Youtube Viewer",
            });

            var getMyChannel = service.Channels.List("snippet");
            getMyChannel.Mine = true;
            var result = await getMyChannel.ExecuteAsync();

            contentFrame.Navigate(typeof(ChannelPage), new NavigateParams() { mainPageRef = this, ID = result.Items[0].Id });
        }
        #endregion
    }
}