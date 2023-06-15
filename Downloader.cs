using Android.App;
using Android.Views;
using Com.Arthenica.Ffmpegkit;
using Google.Android.Material.Snackbar;
using Hqub.MusicBrainz.API.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Signal = Com.Arthenica.Ffmpegkit.Signal;

namespace Ass_Pain
{
    // TODO: add new song to state handler
    internal static class Downloader
    {
        private static readonly string Path = Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        private static readonly string MusicPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;

        public static async void Download(object sender, EventArgs e, string url)
        {
            List<string> authors = new List<string>();
            //APIThrottler throttler = new APIThrottler();
            if (url.Contains("playlist"))
            {
                YoutubeClient youtube = new YoutubeClient();
                Playlist playlist = await youtube.Playlists.GetAsync(url);
                IReadOnlyList<PlaylistVideo> videos = await youtube.Playlists.GetVideosAsync(playlist.Id);
                int a = FileManager.GetAvailableFile("playlistThumb");
                string ext = GetImage(playlist.Thumbnails.AsEnumerable(), $"{Path}/tmp", $"playlistThumb{a}");
                DownloadNotification notification = new DownloadNotification(videos.Count);
                for (int index = 0; index < videos.Count; index++)
                {
                    PlaylistVideo video = videos[index];
                    string author = FileManager.Sanitize(FileManager.GetAlias(video.Author.ChannelTitle));
                    bool getAuthorImage;
                    if (authors.Contains(author))
                    {
                        getAuthorImage = false;
                    }
                    else
                    {
                        authors.Add(author);
                        getAuthorImage = true;
                    }

                    int i = FileManager.GetAvailableFile();
                    new Thread(() =>
                    {
                        DownloadVideo(sender, e, video.Url, i, video.Id, video.Title, video.Author.ChannelTitle,
                            getAuthorImage, video.Author.ChannelId, notification, playlist.Title, a,
                            video == videos.Last(), ext, index);
                    }).Start();
                }
            }
            else if (url.Contains("watch"))
            {
                YoutubeClient youtube = new YoutubeClient();
                Video video = await youtube.Videos.GetAsync(url);
                int i = FileManager.GetAvailableFile();
                new Thread(() => { DownloadVideo(sender, e, url, i, video.Id, video.Title, video.Author.ChannelTitle, true, video.Author.ChannelId, new DownloadNotification()); }).Start();
            }
            else
            {
                throw new ArgumentException($"{url} is not video or playlist");
            }
        }


        private static async void DownloadVideo(object sender, EventArgs e, string url, int i, VideoId videoId, string videoTitle, string channelName, bool getAuthorImage, ChannelId channelId, DownloadNotification notification, string album = null, int a = 0, bool last = false, string playlistCoverExtension = null, int? poradieVPlayliste = null)
        {
            try
            {
                Progress<double> downloadProgress = new Progress<double>();
                notification.Stage1(downloadProgress, videoTitle, poradieVPlayliste);
                Task imageTask = GetImage(i, videoId);
                //var searchTask = SearchAPI(channelName, videoTitle, album);
                Task<(string, string, string, byte[])> searchTask = MainActivity.throttler.Throttle(new List<string> { channelName, videoTitle, album });

                YoutubeClient youtube = new YoutubeClient();
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
                IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{Path}/tmp/unprocessed{i}.mp3", downloadProgress);
                FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
                await imageTask;
                int length;
                
                string s = FFprobeKit
                    .Execute(
                        $"-i {Path}/tmp/unprocessed{i}.mp3 -show_entries format=duration -v quiet -of csv=\"p=0\"")
                    ?.Output;
                length = s != null ? int.Parse(s) : 100;
                
                StatisticsCallback callback = new StatisticsCallback(length, poradieVPlayliste, notification);
                FFmpegKitConfig.EnableStatisticsCallback(callback);

                // ReSharper disable once InconsistentNaming
                Task<FFmpegSession> FFmpegTask = Task.Run(() => FFmpegKit.Execute($"-i {Path}/tmp/unprocessed{i}.mp3 -i {Path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -loglevel quiet -y '{Path}/tmp/video{i}.mp3'"));
                _ = FFmpegTask.ContinueWith(notification.Stage3(poradieVPlayliste));

                (string author, string albumName, string albumCoverExtension, byte[] albumCover) = await searchTask;
                string fileName = FileManager.Sanitize(videoTitle);
                string authorPath;
                if (author == string.Empty)
                {
                    author = channelName;
                    authorPath = FileManager.Sanitize(FileManager.GetAlias(channelName));
                }
                else
                {
                    album = albumName;
                    authorPath = FileManager.Sanitize(FileManager.GetAlias(author));
                }

                string output;
                if (album != null)
                {
                    string album_name = FileManager.Sanitize(album);
                    Directory.CreateDirectory($"{MusicPath}/{authorPath}/{album_name}");
                    output = $"{MusicPath}/{authorPath}/{album_name}/{fileName}.mp3";
                    if (!File.Exists($"{MusicPath}/{authorPath}/{album_name}/cover.jpg") && !File.Exists($"{MusicPath}/{authorPath}/{album_name}/cover.png"))
                    {
                        File.Create($"{MusicPath}/{authorPath}/{album_name}/cover{albumCoverExtension ?? playlistCoverExtension ?? ".jpg"}").Close();
                        if (albumCoverExtension != string.Empty)
                        {
                            _ = Task.Run(() => { File.WriteAllBytes($"{MusicPath}/{authorPath}/{album_name}/cover{albumCoverExtension}", albumCover); });
                        }
                        else
                        {
                            Task t = Task.Run(() => {
                                File.Copy($"{Path}/tmp/playlistThumb{a}{playlistCoverExtension}", $"{MusicPath}/{authorPath}/{album_name}/cover{playlistCoverExtension}", true);
                            });
                            if (last)
                            {
                                await t;
                                File.Delete($"{Path}/tmp/playlistThumb{a}{playlistCoverExtension}");
                            }
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory($"{MusicPath}/{authorPath}");
                    output = $"{MusicPath}/{authorPath}/{fileName}.mp3";
                }

                if (getAuthorImage)
                {
                    if (!File.Exists($"{MusicPath}/{authorPath}/cover.jpg") && !File.Exists($"{MusicPath}/{authorPath}/cover.png"))
                    {
                        _ = Task.Run(async () => {
                            Channel authorThumbnails = await youtube.Channels.GetAsync(channelId);
                            GetImage(authorThumbnails.Thumbnails.AsEnumerable(), $"{MusicPath}/{authorPath}");
                        });
                    }
                }

                FFmpegSession session = await FFmpegTask;
                _ = Task.Run(() => {
                    File.Delete($"{Path}/tmp/file{i}.jpg");
                    File.Delete($"{Path}/tmp/unprocessed{i}.mp3");
                });

                if (session.ReturnCode is { IsSuccess: true })
                {
                    notification.Stage4();
                    View view = (View)sender;
                    try
                    {
                        File.Move($"{Path}/tmp/video{i}.mp3", output);
                    }
                    catch
                    {
                        File.Delete($"{Path}/tmp/video{i}.mp3");
                        Console.WriteLine("Video already exists");
                        Snackbar.Make(view, $"Exists: {videoTitle}", Snackbar.LengthLong)
                            .SetAction("Action", (View.IOnClickListener)null).Show();
                        return;
                    }
                    Console.WriteLine("Adding tags");
                    TagLib.File tfile = TagLib.File.Create(output);
                    tfile.Tag.Title = videoTitle;
                    string[] authors = { FileManager.GetAlias(author) };
                    tfile.Tag.Performers = authors;
                    tfile.Tag.AlbumArtists = authors;
                    if (album != null)
                    {
                        tfile.Tag.Album = album;
                    }
                    tfile.Save();
                    tfile.Dispose();
                    Snackbar.Make(view, $"Success: {videoTitle}", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                }
                else
                {
                    File.Delete($"{Path}/tmp/video{i}.mp3");
                    if (session.ReturnCode == null) return;
                    Console.WriteLine($"FFmpeg failed with status code {session.ReturnCode.Value}");
                    View view = (View)sender;
                    Snackbar.Make(view, $"{session.ReturnCode.Value} Failed: {videoTitle}", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BIG EROOOOOOOOR: {ex.Message}");
                View view = (View)sender;
                Snackbar.Make(view, $"Failed: {videoTitle}", Snackbar.LengthLong)
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
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task GetImage(int i, VideoId id)
        {
            //string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            using WebClient cli = new WebClient();
            if (await GetHttpStatus($"http://img.youtube.com/vi/{id}/maxresdefault.jpg"))
            {
                //return cli.DownloadData($"http://img.youtube.com/vi/{id}/maxresdefault.jpg");
                await File.WriteAllBytesAsync($"{Path}/tmp/file{i}.jpg", cli.DownloadData($"http://img.youtube.com/vi/{id}/maxresdefault.jpg"));
            }
            else
            {
                //return cli.DownloadData($"http://img.youtube.com/vi/{id}/0.jpg");
                await File.WriteAllBytesAsync($"{Path}/tmp/file{i}.jpg", cli.DownloadData($"http://img.youtube.com/vi/{id}/0.jpg"));
            }
        }

        public static string GetImage(IEnumerable<Thumbnail> authorThumbnails, string thumbnailPath, string fileName = "cover")
        {
            Directory.CreateDirectory(thumbnailPath);
            File.Create($"{thumbnailPath}/{fileName}.jpg").Close();
            int maxArea = 0;
            string thumbnailUrl = String.Empty;
            foreach (Thumbnail thumbnail in authorThumbnails)
            {
                if (thumbnail.Resolution.Area <= maxArea) continue;
                maxArea = thumbnail.Resolution.Area;
                thumbnailUrl = thumbnail.Url;
            }

            WebClient cli = new WebClient();
            byte[] bytes = cli.DownloadData(thumbnailUrl);
            cli.Dispose();
            byte[] PNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
            byte[] JPG = { 255, 216, 255 };
            if (bytes.Take(3).SequenceEqual(JPG))
            {
                File.WriteAllBytes($"{thumbnailPath}/{fileName}.jpg", bytes);
                return ".jpg";
            }

            if (!bytes.Take(16).SequenceEqual(PNG)) return ".idk";
            File.WriteAllBytes($"{thumbnailPath}/{fileName}.png", bytes);
            File.Delete($"{thumbnailPath}/{fileName}.jpg");
            return ".png";
        }

        public static async Task<(string, string, string, byte[])> SearchAPI(string name=null , string song = null, string album = null)
        {
            Console.WriteLine("STARTING API SEARCH");
            name = "Mori Calliope Ch. hololive-EN";
            song = "[Original Rap] DEAD BEATS - Calliope Mori #holoMyth #hololiveEnglish";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://musicbrainz.org/ws/2/recording/?query=arid:8a9d0b90-951e-4ab8-b2dc-9d3618af3d28,recording:[Original%20Rap]%20DEAD%20BEATS%20-%20Calliope%20Mori%20#holoMyth%20#hololiveEnglish");
            Stream stream = await response.Content.ReadAsStreamAsync();
            /*XmlTextReader reader = new XmlTextReader(stream);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        Console.Write("<" + reader.Name);

                        while (reader.MoveToNextAttribute()) // Read the attributes.
                            Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                        Console.Write(">");
                        Console.WriteLine(">");
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        Console.WriteLine(reader.Value);
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        Console.Write("</" + reader.Name);
                        Console.WriteLine(">");
                        break;
                }
            }*/
            //stream.Seek(0, SeekOrigin.Begin);
            //var fs = File.Create(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath + "/pain.xml");
            //stream.CopyTo(fs);
            //fs.Close();
            //var xdoc = new XDocument();
            XDocument xdoc = XDocument.Load(stream);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();
            //Console.WriteLine($"COUNT: {xdoc.Descendants(ns + "recording")}");

            //Console.WriteLine(xdoc.FirstNode);
            /*foreach (XElement element in xdoc.Descendants("recording"))
            {
                Console.WriteLine(element);
            }*/
            IEnumerable<XElement> recordings = xdoc.Descendants(ns + "recording");
             IEnumerable<XElement> x = from recording in recordings
                     from artists in recording.Descendants(ns + "artist-credit").Descendants(ns + "name-credit")
                     from credits in artists.Descendants(ns + "artist").Where(des => des.Attribute("id").Value == "8a9d0b90-951e-4ab8-b2dc-9d3618af3d28")
                     select credits;
             foreach(XElement m in x)
             {
                 Console.WriteLine(m.Attribute("id").Value);
             }
            /*xmlDocument.Validate(new System.Xml.Schema.ValidationEventHandler((object sender, System.Xml.Schema.ValidationEventArgs args) =>
            {
                Console.WriteLine("VALIDATOR");
                Console.WriteLine(args.Message);
            }));*/
            //xmlDocument.Save(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath+"/pain.xml");
            /*foreach(var x in xmlDocument.FirstChild.Attributes)
            {
                Console.WriteLine(x);
            }
            foreach (XmlNode node in xmlDocument.SelectNodes("recording-list"))
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    foreach (var attr in childNode.Attributes)
                        Console.WriteLine(attr);
                }
            }*/
            Console.WriteLine("ENDING API SEARCH");
            return (String.Empty, String.Empty, String.Empty, null);
            //https://musicbrainz.org/doc/Cover_Art_Archive/API
            //https://stackoverflow.com/questions/13453678/how-to-get-album-image-using-musicbrainz
            //https://github.com/avatar29A/MusicBrainz/wiki/Example-4-Recordings
        }
        public static string Quote(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            if (s.IndexOf(' ') < 0)
            {
                return s;
            }

            return "\"" + s + "\"";
        }

        static bool IsOfficial(Release r)
        {
            return !string.IsNullOrEmpty(r.Date) && !string.IsNullOrEmpty(r.Status)
                 && r.Status.Equals("Official", StringComparison.OrdinalIgnoreCase);
        }
        public static string ToShortDate(this string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 4)
            {
                return "----";
            }

            return s[..4];
        }
    }

    public class StatisticsCallback : IStatisticsCallback
    {
        public StatisticsCallback(int duration, int? poradieVPlayliste, DownloadNotification notification)
        {
            Duration = duration;
            PoradieVPlayliste = poradieVPlayliste;
            Notification = notification;
        }
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public IntPtr Handle { get; }
        private int Duration { get; }
        private int? PoradieVPlayliste { get; }
        private DownloadNotification Notification { get; }
        public void Apply(Statistics statistics)
        {
            // statistics.Time;
            Notification.Stage2(statistics.Time / Duration * 100);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Image
    {
        public List<string> types { get; set; }
        public bool front { get; set; }
        public bool back { get; set; }
        public int edit { get; set; }
        public string image { get; set; }
        public string comment { get; set; }
        public bool approved { get; set; }
        public string id { get; set; }
        public Thumbnails thumbnails { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MusicBrainzThumbnail
    {
        public List<Image> images { get; set; }
        public string release { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Thumbnails
    {
        public string large { get; set; }
        public string small { get; set; }
    }
}
