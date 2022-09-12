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
                    _ = DownloadVideo(sender, e, video.Url, playlist.Title);
                    //int x = await DownloadVideo(sender, e, video.Url, playlist.Title);
                    //Console.WriteLine($"{x} RETURN NUMBER OF {video.Url}");
                }
            }
            else if (url.Contains("watch"))
            {
                _ = DownloadVideo(sender, e, url);
            }
            else
            {
                throw new ArgumentException(String.Format("{0} is not video or playlist", url));
            }
        }
        
        public static async Task DownloadVideo(object sender, EventArgs e, string url)
        {
            int i = 0;
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            while (File.Exists($"{path}/tmp/video{i}.mp3"))
            {
                i++;
            }
            string dest = $"{path}/tmp/video{i}.mp3";
            File.Create(dest).Close();

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);

            Video video = await youtube.Videos.GetAsync(url);
            string file_name = video.Title.Replace("/", "");
            file_name = file_name.Replace("#", "");
            file_name = file_name.Replace("?", "");

            string author = video.Author.ChannelTitle.Replace("/", "");
            author = author.Replace("#", "");
            author = author.Replace("?", "");
            author = FileManager.GetAlias(author);

            if (!Directory.Exists($"{path}/music/{author}"))
            {
                Console.WriteLine("Creating " + $"{path}/music/{author}");
                Directory.CreateDirectory($"{path}/music/{author}");
            }

            string output = $"{path}/music/{author}/{file_name}.mp3";
            WebClient cli = new WebClient();

            if (await GetHttpStatus($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg"))
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }
            else
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/0.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }

            Config.IgnoreSignal(Laerdal.FFmpeg.Android.Signal.Sigxcpu);
            int status = FFmpeg.Execute($"-i {dest} -i {path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{output}'");

            File.Delete($"{path}/tmp/file{i}.jpg");
            File.Delete(dest);

            if (status == 0)
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
                Console.WriteLine($"FFmpeg failed with status code {status}");
                View view = (View)sender;
                Snackbar.Make(view, $"{status} Failed: {video.Title}", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
        }

        public static async Task DownloadVideo(object sender, EventArgs e, string url, string album)
        {
            int i = 0;
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            while (File.Exists($"{path}/tmp/video{i}.mp3"))
            {
                i++;
            }
            string dest = $"{path}/tmp/video{i}.mp3";
            File.Create(dest).Close();

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);

            Video video = await youtube.Videos.GetAsync(url);
            string file_name = video.Title.Replace("/", "");
            file_name = file_name.Replace("#", "");
            file_name = file_name.Replace("?", "");

            string author = video.Author.ChannelTitle.Replace("/", "");
            author = author.Replace("#", "");
            author = author.Replace("?", "");
            author = FileManager.GetAlias(author);

            string album_name = album.Replace("/", "");
            album_name = album_name.Replace("#", "");
            album_name = album_name.Replace("?", "");

            if (!Directory.Exists($"{path}/music/{author}"))
            {
                Console.WriteLine("Creating " + $"{path}/music/{author}");
                Directory.CreateDirectory($"{path}/music/{author}");
            }

            if (!Directory.Exists($"{path}/music/{author}/{album_name}"))
            {
                Console.WriteLine("Creating " + $"{path}/music/{author}/{album_name}");
                Directory.CreateDirectory($"{path}/music/{author}/{album_name}");
            }

            string output = $"{path}/music/{author}/{album_name}/{file_name}.mp3";

            WebClient cli = new WebClient();

            if (await GetHttpStatus($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg"))
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }
            else
            {
                var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/0.jpg");
                File.WriteAllBytes($"{path}/tmp/file{i}.jpg", imgBytes);
            }

            Config.IgnoreSignal(Laerdal.FFmpeg.Android.Signal.Sigxcpu);
            int status = FFmpeg.Execute($"-i {dest} -i {path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{output}'");

            File.Delete($"{path}/tmp/file{i}.jpg");
            File.Delete(dest);

            if (status == 0)
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
                Console.WriteLine($"FFmpeg failed with status code {status}");
                View view = (View)sender;
                Snackbar.Make(view, $"{status} Failed: {video.Title}", Snackbar.LengthLong)
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