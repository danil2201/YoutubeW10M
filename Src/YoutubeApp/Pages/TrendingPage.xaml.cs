﻿using Google.Apis.YouTube.v3;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using YTApp.Classes;
using YTApp.Classes.DataTypes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YTApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrendingPage : Page
    {
        System.Collections.ObjectModel.ObservableCollection<YoutubeItemDataType> videosList = new System.Collections.ObjectModel.ObservableCollection<YoutubeItemDataType>();

        public TrendingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateVideos();
        }

        public async void UpdateVideos()
        {
            YoutubeMethods methods = new YoutubeMethods();

            var service = await YoutubeMethodsStatic.GetServiceAsync();

            var recommendations = service.Videos.List("snippet, contentDetails");
            recommendations.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
            
            recommendations.MaxResults = 1;//25;

            Google.Apis.YouTube.v3.Data.VideoListResponse result = default;

            try
            {
                result = await recommendations.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] recommendations.ExecuteAsync ex.: " + ex.Message);
            }


            if (result != null)
            {
                foreach (var video in result.Items)
                {
                    videosList.Add(methods.VideoToYoutubeItem(video));
                }
            }

            methods.FillInViews(videosList, service);
        }

        private void YoutubeItemsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (YoutubeItemDataType)e.ClickedItem;
            var listView = (ListView)sender;
            listView.PrepareConnectedAnimation("videoThumb", item, "ImageControl");
            Constants.MainPageRef.StartVideo(item.Id);
        }
    }
}
