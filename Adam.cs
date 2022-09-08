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
using Laerdal.FFmpeg.Android;
using Laerdal.FFmpeg.Android.Util;

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

            Console.WriteLine("Download");
            Console.WriteLine(url);
            var youtube = new YoutubeClient();

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
            //var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            //foreach(var stream in streamInfo)
            //{
            //    Console.WriteLine(stream);
            //}
            //return;
            var progress = new Progress<double>(p => Console.WriteLine($"YoutubeExplode Demo [{p:P0}]"));
            string path = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/video.{streamInfo.Container}";
            string new_path = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/video.mp3";
            await youtube.Videos.Streams.DownloadAsync(streamInfo, path, progress);
            Console.WriteLine(path);
            int status = FFmpeg.Execute($"-i {path} {new_path}");
            if(status == 0)
            {
                Console.WriteLine("Success");
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