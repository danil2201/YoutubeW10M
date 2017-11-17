﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YTApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstStartupPage : Page
    {
        MainPage MainPageReference;

        public FirstStartupPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Classes.DataTypes.NavigateParams result = (Classes.DataTypes.NavigateParams)e.Parameter;
            base.OnNavigatedTo(e);
            MainPageReference = result.mainPageRef;
        }

        private async void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Google.Apis.Auth.OAuth2.UserCredential credential = await Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(new Google.Apis.Auth.OAuth2.ClientSecrets
            {
                ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
                ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm"
            }, new[] { Google.Apis.YouTube.v3.YouTubeService.Scope.Youtube }, "user", System.Threading.CancellationToken.None);
            MainPageReference.LoadSubscriptions();
            MainPageReference.contentFrame.Navigate(typeof(HomePage), new Classes.DataTypes.NavigateParams() { mainPageRef = MainPageReference, Refresh = true });
        }
    }
}
