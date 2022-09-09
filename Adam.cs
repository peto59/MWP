using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using Android.Webkit;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
//using YoutubeExplode.Converter;
using Laerdal.FFmpeg.Android;
using System.Net;

namespace Ass_Pain
{
    internal class Adam
    {
        protected MediaPlayer player;

        public void logout_Click(object sender, EventArgs e)
        {
            //var folder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            Console.WriteLine("Clicked");
            var folder = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            Console.WriteLine(folder);
            var filesList = Directory.GetFiles(folder);
            foreach (var file in filesList)
            {
                Console.WriteLine(file);
                if (player == null)
                {
                    player = new MediaPlayer();
                }
                else
                {
                    player.Reset();
                }
                player.SetDataSource(file);
                player.Prepare();
                player.Start();
            }
            //RootDirectory();
        }

        public void stop(object sender, EventArgs e)
        {
            Console.WriteLine("Stopped");
            player.Pause();
        }

        public async void Download(object sender, EventArgs e, string url)
        {
            string path = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}";
            string dest = $"{path}/video.mp3";

            YoutubeClient youtube = new YoutubeClient();

            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            await youtube.Videos.Streams.DownloadAsync(streamInfo, dest, progress);

            YoutubeExplode.Videos.Video video = await youtube.Videos.GetAsync(url);
            string title = video.Title.Replace("/", "");
            title = title.Replace("#", "");
            //title = title.Replace("?", "");

            WebClient cli = new WebClient();
            var imgBytes = cli.DownloadData($"http://img.youtube.com/vi/{video.Id}/maxresdefault.jpg");
            File.WriteAllBytes($"{path}/file.jpg", imgBytes);

            Config.IgnoreSignal(Laerdal.FFmpeg.Android.Signal.Sigxcpu);
            int status = FFmpeg.Execute($"-i {dest} -i {path}/file.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -y '{path}/{video.Title}.mp3'");
            if (status == 0)
            {
                Console.WriteLine("Adding tags");
                var tfile = TagLib.File.Create($"{path}/{video.Title}.mp3");
                tfile.Tag.Title = video.Title;
                string[] autors = { video.Author.ChannelTitle };
                tfile.Tag.Performers = autors;
                tfile.Save();
            }
            else
            {
                Console.WriteLine($"FFmpeg failed with status code {status}");
            }
        }

        public IList<string> RootDirectory()
        {
            var Pathlist = new List<string>();

            var temp = new List<string>();
            Pathlist.Add(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath);
            Pathlist.Add(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath);
            Pathlist.Add(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath);
            foreach (string path in Pathlist)
            {
                if (Directory.Exists(path))
                {
                    Directory.GetFileSystemEntries(path);
                    //temp.AddRange(Directory.EnumerateDirectories(path).ToList());
                }
            }

            return Pathlist;
            //Pathlist.AddRange(temp);

        }

    }
}