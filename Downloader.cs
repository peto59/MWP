using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using Android.Webkit;
using Android.Graphics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;
using Laerdal.FFmpeg.Android;
using Google.Android.Material.Snackbar;

namespace Ass_Pain
{
    internal static class Downloader
    {
        public static async void Download(object sender, EventArgs e, string url)
        {
            if (url.Contains("playlist"))
            {
                YoutubeClient youtube = new YoutubeClient();
                Playlist playlist = await youtube.Playlists.GetAsync(url);
                var videos = await youtube.Playlists.GetVideosAsync(playlist.Id);
                foreach (PlaylistVideo video in videos)
                {
                    int x = await DownloadVideo(sender, e, video.Url, playlist.Title);
                    Console.WriteLine($"{x} RETURN NUMBER OF {video.Url}");
                }
            }
            else if (url.Contains("watch"))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                DownloadVideo(sender, e, url);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                throw new ArgumentException(String.Format("{0} is not video or playlist", url));
            }
        }

        public static async Task<int> DownloadVideo(object sender, EventArgs e, string url, string album = "null")
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string dest = $"{path}/tmp/video.mp3";

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);

            Video video = await youtube.Videos.GetAsync(url);
            string title = video.Title.Replace("/", "");
            title = title.Replace("#", "");
            //title = title.Replace("?", "");

            if (!Directory.Exists($"{path}/music/{video.Author.ChannelTitle}"))
            {
                Console.WriteLine("Creating " + $"{path}/music/{video.Author.ChannelTitle}");
                Directory.CreateDirectory($"{path}/music/{video.Author.ChannelTitle}");
            }

            string output;
            if (album != "null")
            {
                if (!Directory.Exists($"{path}/music/{video.Author.ChannelTitle}/{album}"))
                {
                    Console.WriteLine("Creating " + $"{path}/music/{video.Author.ChannelTitle}/{album}");
                    Directory.CreateDirectory($"{path}/music/{video.Author.ChannelTitle}/{album}");
                }

                output = $"{path}/music/{video.Author.ChannelTitle}/{album}/{title}.mp3";
            }
            else
            {
                output = $"{path}/music/{video.Author.ChannelTitle}/{title}.mp3";
            }
            WebClient cli = new WebClient();

            if (await GetHttpStatus($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg"))
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg");
                File.WriteAllBytes($"{path}/tmp/file.jpg", imgBytes);
            }
            else
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/0.jpg");
                File.WriteAllBytes($"{path}/tmp/file.jpg", imgBytes);
            }

            Config.IgnoreSignal(Laerdal.FFmpeg.Android.Signal.Sigxcpu);
            int status = FFmpeg.Execute($"-i {dest} -i {path}/tmp/file.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{output}'");
            if (status == 0)
            {
                Console.WriteLine("Adding tags");
                var tfile = TagLib.File.Create($"{output}");
                tfile.Tag.Title = video.Title;
                string[] autors = { video.Author.ChannelTitle };
                tfile.Tag.Performers = autors;
                tfile.Tag.AlbumArtists = autors;
                if (album != "null")
                {
                    tfile.Tag.Album = album;
                }
                tfile.Save();
                View view = (View)sender;
                Snackbar.Make(view, $"{video.Title} downloaded successfully", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
                return 0;
            }
            else
            {
                Console.WriteLine($"FFmpeg failed with status code {status}");
                View view = (View)sender;
                Snackbar.Make(view, $"{video.Title} failed with {status} code", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
                return 1;
            }
        }

        public static async Task<bool> GetHttpStatus(string url)
        {
            Uri uri = new Uri(url);
            if (!uri.IsWellFormedOriginalString())
                return false;
            try
            {
                var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}