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
using Google.Android.Material.Snackbar;
using Com.Arthenica.Ffmpegkit;
using Signal = Com.Arthenica.Ffmpegkit.Signal;

namespace Ass_Pain
{
    public delegate bool ExecuteCallback(int hwnd, int lParam);

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
                    int i = FileManager.GetAvailableFile();
                    void ts()
                    {
                        DownloadVideo(sender, e, video.Url, playlist.Title, i);
                    }
                    new Thread(ts).Start();
                }
            }
            else if (url.Contains("watch"))
            {
                int i = FileManager.GetAvailableFile();
                void ts()
                {
                    DownloadVideo(sender, e, url, i);
                }
                new Thread(ts).Start();
                
            }
            else
            {
                throw new ArgumentException(String.Format("{0} is not video or playlist", url));
            }
        }
        
        public static async void DownloadVideo(object sender, EventArgs e, string url, int i)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;

            string dest = $"{path}/tmp/video{i}.mp3";

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            //var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            //await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest);

            Video video = await youtube.Videos.GetAsync(url);
            string file_name = FileManager.Sanitize(video.Title);

            string author = FileManager.Sanitize(video.Author.ChannelTitle);
            author = FileManager.GetAlias(author);

            Directory.CreateDirectory($"{path}/music/{author}");

            string output = $"{path}/music/{author}/{file_name}.mp3";

            await GetImage(i, video.Id);

            FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
            FFmpegSession session = FFmpegKit.Execute($"-i {dest} -i {path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{output}'");

            File.Delete($"{path}/tmp/file{i}.jpg");
            File.Delete(dest);

            if (session.ReturnCode.IsSuccess)
            {
                Console.WriteLine("Adding tags");
                var tfile = TagLib.File.Create($"{output}");
                tfile.Tag.Title = video.Title;
                string[] autors = { video.Author.ChannelTitle };
                tfile.Tag.Performers = autors;
                tfile.Tag.AlbumArtists = autors;
                tfile.Save();
                View view = (View)sender;
                Snackbar.Make(view, $"Success: {video.Title}", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
            else
            {
                Console.WriteLine($"FFmpeg failed with status code {55}");
                View view = (View)sender;
                Snackbar.Make(view, $"{55} Failed: {video.Title}", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
        }

        public static async void DownloadVideo(object sender, EventArgs e, string url, string album, int i)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;

            string dest = $"{path}/tmp/video{i}.mp3";

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            //var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            //await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest);

            Video video = await youtube.Videos.GetAsync(url);
            string file_name = FileManager.Sanitize(video.Title);

            string author = FileManager.Sanitize(video.Author.ChannelTitle);
            author = FileManager.GetAlias(author);

            string album_name = FileManager.Sanitize(album);

            Directory.CreateDirectory($"{path}/music/{author}/{album_name}");

            string output = $"{path}/music/{author}/{album_name}/{file_name}.mp3";

            await GetImage(i, video.Id);

            FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
            FFmpegSession session = FFmpegKit.Execute($"-i {dest} -i {path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{output}'");

            File.Delete($"{path}/tmp/file{i}.jpg");
            File.Delete(dest);

            if (session.ReturnCode.IsSuccess)
            {
                Console.WriteLine("Adding tags");
                var tfile = TagLib.File.Create($"{output}");
                tfile.Tag.Title = video.Title;
                string[] autors = { video.Author.ChannelTitle };
                tfile.Tag.Performers = autors;
                tfile.Tag.AlbumArtists = autors;
                tfile.Tag.Album = album;
                tfile.Save();
                View view = (View)sender;
                Snackbar.Make(view, $"Success: {video.Title}", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
            else
            {
                Console.WriteLine($"FFmpeg failed with status code {55}");
                View view = (View)sender;
                Snackbar.Make(view, $"{55} Failed: {video.Title}", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
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
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task GetImage(int i, VideoId id)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            WebClient cli = new WebClient();

            if (await GetHttpStatus($"http://img.youtube.com/vi/{id}/maxresdefault.jpg"))
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{id}/maxresdefault.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }
            else
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{id}/0.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }
        }
    }
}