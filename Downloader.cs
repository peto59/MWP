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
using System.Security.Cryptography;
using Org.Apache.Http.Authentication;
using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities.Collections;
using Hqub.MusicBrainz.API.Entities;
using YoutubeExplode.Channels;

namespace Ass_Pain
{
    internal static class Downloader
    {
        public static string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
        public static string musicPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;

        public static async void Download(object sender, EventArgs e, string url)
        {
            List<string> authors = new List<string>();
            //APIThrottler throttler = new APIThrottler();
            if (url.Contains("playlist"))
            {
                YoutubeClient youtube = new YoutubeClient();
                Playlist playlist = await youtube.Playlists.GetAsync(url);
                var videos = await youtube.Playlists.GetVideosAsync(playlist.Id);
                int a = FileManager.GetAvailableFile("playlistThumb");
                string ext = GetImage(playlist.Thumbnails.AsEnumerable(), $"{path}/tmp", $"playlistThumb{a}");
                foreach (PlaylistVideo video in videos)
                {
                    string author = FileManager.Sanitize(FileManager.GetAlias(video.Author.ChannelTitle));
                    bool getAuthourImage;
                    if (authors.Contains(author))
                    {
                        getAuthourImage = false;
                    }
                    else
                    {
                        authors.Add(author);
                        getAuthourImage = true;
                    }
                    int i = FileManager.GetAvailableFile();
                    new Thread(() => { DownloadVideo(sender, e, video.Url, i, video.Id, video.Title, video.Author.ChannelTitle, getAuthourImage, video.Author.ChannelId, playlist.Title, a, video == videos.Last(), ext); }).Start();
                }
            }
            else if (url.Contains("watch"))
            {
                YoutubeClient youtube = new YoutubeClient();
                Video video = await youtube.Videos.GetAsync(url);
                int i = FileManager.GetAvailableFile();
                new Thread(() => { DownloadVideo(sender, e, url, i, video.Id, video.Title, video.Author.ChannelTitle, true, video.Author.ChannelId); }).Start();
            }
            else
            {
                throw new ArgumentException(String.Format("{0} is not video or playlist", url));
            }
        }


        public static async void DownloadVideo(object sender, EventArgs e, string url, int i, VideoId videoId, string videoTitle, string channelName, bool getAuthourImage, ChannelId channelId, string album = null, int a = 0, bool last = false, string playlistCoverExtension = null)
        {
            try
            {
                var imageTask = GetImage(i, videoId);
                //var searchTask = SearchAPI(channelName, videoTitle, album);
                var searchTask = MainActivity.throttler.Throttle(new List<string> { channelName, videoTitle, album });

                YoutubeClient youtube = new YoutubeClient();
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{path}/tmp/unproccessed{i}.mp3");

                FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
                await imageTask;
                var FFmpegTask = Task.Run(() => { return FFmpegKit.Execute($"-i {path}/tmp/unproccessed{i}.mp3 -i {path}/tmp/file{i}.jpg -map 0:0 -map 1:0 -c:a libmp3lame -id3v2_version 4 -write_xing 0 -loglevel quiet -y '{path}/tmp/video{i}.mp3'"); });

                (string author, string albumName, string albumCoverExtension, byte[] albumCover) = await searchTask;
                string file_name = FileManager.Sanitize(videoTitle);
                string authorPath;
                if (author == String.Empty)
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
                    Directory.CreateDirectory($"{musicPath}/{authorPath}/{album_name}");
                    output = $"{musicPath}/{authorPath}/{album_name}/{file_name}.mp3";
                    if (!File.Exists($"{musicPath}/{authorPath}/{album_name}/cover.jpg") && !File.Exists($"{musicPath}/{authorPath}/{album_name}/cover.png"))
                    {
                        File.Create($"{musicPath}/{authorPath}/{album_name}/cover{albumCoverExtension ?? playlistCoverExtension ?? ".jpg"}").Close();
                        if (albumCoverExtension != String.Empty)
                        {
                            _ = Task.Run(() => { File.WriteAllBytes($"{musicPath}/{authorPath}/{album_name}/cover{albumCoverExtension}", albumCover); });
                        }
                        else
                        {
                            var t = Task.Run(() => {
                                File.Copy($"{path}/tmp/playlistThumb{a}{playlistCoverExtension}", $"{musicPath}/{authorPath}/{album_name}/cover{playlistCoverExtension}", true);
                            });
                            if (last)
                            {
                                await t;
                                File.Delete($"{path}/tmp/playlistThumb{a}{playlistCoverExtension}");
                            }
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory($"{musicPath}/{authorPath}");
                    output = $"{musicPath}/{authorPath}/{file_name}.mp3";
                }

                if (getAuthourImage)
                {
                    if (!File.Exists($"{musicPath}/{authorPath}/cover.jpg") && !File.Exists($"{musicPath}/{authorPath}/cover.png"))
                    {
                        _ = Task.Run(async () => {
                            var authorThumbnails = await youtube.Channels.GetAsync(channelId);
                            GetImage(authorThumbnails.Thumbnails.AsEnumerable(), $"{musicPath}/{authorPath}");
                        });
                    }
                }

                FFmpegSession session = await FFmpegTask;
                _ = Task.Run(() => {
                    File.Delete($"{path}/tmp/file{i}.jpg");
                    File.Delete($"{path}/tmp/unproccessed{i}.mp3");
                });

                if (session.ReturnCode.IsSuccess)
                {
                    View view = (View)sender;
                    try
                    {
                        File.Move($"{path}/tmp/video{i}.mp3", output);
                    }
                    catch
                    {
                        File.Delete($"{path}/tmp/video{i}.mp3");
                        Console.WriteLine("Video already exists");
                        Snackbar.Make(view, $"Exists: {videoTitle}", Snackbar.LengthLong)
                            .SetAction("Action", (View.IOnClickListener)null).Show();
                        return;
                    }
                    Console.WriteLine("Adding tags");
                    var tfile = TagLib.File.Create(output);
                    tfile.Tag.Title = videoTitle;
                    string[] autors = { FileManager.GetAlias(author) };
                    tfile.Tag.Performers = autors;
                    tfile.Tag.AlbumArtists = autors;
                    if (album != null)
                    {
                        tfile.Tag.Album = album;
                    }
                    tfile.Save();
                    Snackbar.Make(view, $"Success: {videoTitle}", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                }
                else
                {
                    File.Delete($"{path}/tmp/video{i}.mp3");
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
            //string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            using (WebClient cli = new WebClient())
            {
                if (await GetHttpStatus($"http://img.youtube.com/vi/{id}/maxresdefault.jpg"))
                {
                    //return cli.DownloadData($"http://img.youtube.com/vi/{id}/maxresdefault.jpg");
                    File.WriteAllBytes($"{path}/tmp/file{i}.jpg", cli.DownloadData($"http://img.youtube.com/vi/{id}/maxresdefault.jpg"));
                }
                else
                {
                    //return cli.DownloadData($"http://img.youtube.com/vi/{id}/0.jpg");
                    File.WriteAllBytes($"{path}/tmp/file{i}.jpg", cli.DownloadData($"http://img.youtube.com/vi/{id}/0.jpg"));
                }
            }
        }

        public static string GetImage(IEnumerable<Thumbnail> authorThumbnails, string thumbnailPath, string fileName = "cover")
        {
            Directory.CreateDirectory(thumbnailPath);
            File.Create($"{thumbnailPath}/{fileName}.jpg").Close();
            int maxArea = 0;
            string thumbnailUrl = String.Empty;
            foreach (var thumbnail in authorThumbnails)
            {
                if (thumbnail.Resolution.Area > maxArea)
                {
                    maxArea = thumbnail.Resolution.Area;
                    thumbnailUrl = thumbnail.Url;
                }
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
            }else if (bytes.Take(16).SequenceEqual(PNG))
            {
                File.WriteAllBytes($"{thumbnailPath}/{fileName}.png", bytes);
                File.Delete($"{thumbnailPath}/{fileName}.jpg");
                return ".png";
            }
            return ".idk";
        }

        public static async Task<(string, string, string, byte[])> SearchAPI(string name=null , string song = null, string album = null)
        {
            Console.WriteLine("STARTING API SEARCH");
            name = "Mori Calliope Ch. hololive-EN";
            song = "[Original Rap] DEAD BEATS - Calliope Mori #holoMyth #hololiveEnglish";
            try
            {
                using (MusicBrainzClient client = new MusicBrainzClient())
                {
                    ArtistList artists = await client.Artists.SearchAsync(name, 20);
                    //Console.WriteLine("Total matches for '{0}': {1}", name, artists.Count);

                    int count = artists.Items.Count(a => a.Score == 100);

                    //Console.WriteLine("Exact matches for '{0}': {1}", name, count);

                    Artist artist;
                    if (count > 1)
                    {
                        count = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, name)).Count(a => a.Score == 1.0f);
                        Console.WriteLine("Levenshtein matches for '{0}': {1}", name, count);
                        if (count > 1)
                        {
                            count = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Disambiguation, name)).Count(a => a.Score == 1.0f);
                            Console.WriteLine("Levenshtein Disambiguation matches for '{0}': {1}", name, count);
                            artist = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Disambiguation, name)).First();
                        }
                        else
                        {
                            artist = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, name)).First();
                        }
                    }
                    else
                    {
                        artist = artists.Items.First();
                    }
                    Console.WriteLine($"{artist.Name} {artist.Id} {artist.Disambiguation}");
                    Console.WriteLine();
                    await Task.Delay(1000);
                    var query = new QueryParameters<Recording>()
                    {
                        { "arid", artist.Id },
                        { "recording", song }
                    };

                    var recordings = await client.Recordings.SearchAsync(query, 100);
                    foreach(var recording in recordings)
                    {
                        Console.WriteLine(recording.Title);
                    }
                    if (recordings.Count == 0)
                    {
                        Console.WriteLine("No matches for recording");
                        return (string.Empty, string.Empty, string.Empty, null);
                    }
                    Console.WriteLine("Total matches for recording '{0} by {1}': {2}", song, artist.Name, recordings.Count);
                    var matches = recordings.Items.Where(r => r.Credits.Where(a => a.Artist.Id == artist.Id) != null ).Where(r => r.Title == song);
                    Console.WriteLine("Total exact matches recording for '{0} by {1}': {2}", song, artist.Name, matches.Count());

                    if (matches.Count() == 0)
                    {
                        //matches = recordings.Items.Where(r => r.Credits.Where(a => a.Artist.Id == artist.Id) != null).OrderByDescending(a => Levenshtein.Similarity(a.Title, song));
                        /*var answer = from recording in recordings.Items
                                     from credit in recording.Credits
                                     where credit.Artist.Id == artist.Id
                                     select recording;*/
                        List<Recording> x = new List<Recording>();
                        foreach(Recording recor in recordings.Items)
                        {
                            Console.WriteLine(recor.Title);
                            foreach (var cred in recor.Credits)
                            {
                                if(cred.Artist.Id == artist.Id && recor.Releases != null)
                                {
                                    if (recor.Releases.Count > 0)
                                    {
                                        if (song.IndexOf("remix", StringComparison.OrdinalIgnoreCase) < 0)
                                        {
                                            if (recor.Title.IndexOf("remix", StringComparison.OrdinalIgnoreCase) < 0)
                                            {
                                                x.Add(recor);
                                                Console.WriteLine(recor.Title);
                                            }
                                        }
                                        else
                                        {
                                            x.Add(recor);
                                            Console.WriteLine(recor.Title);
                                        }
                                    }
                                }
                            }
                        }
                        x = x.OrderByDescending(s => Levenshtein.Similarity(s.Title, song)).ToList();
                        //Console.WriteLine($"LINQ matches: {answer.Count()}");
                        foreach (var match in x)
                        {
                            try
                            {
                                Console.WriteLine($"TITLE: {match.Title}");
                                Console.WriteLine($"RELEASES: {match.Releases.Count}");
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        matches = x;
                        return (string.Empty, string.Empty, string.Empty, null);
                    }
                    using (HttpClient httpClient = new HttpClient())
                    {
                        foreach (Recording match in matches)
                        {
                            IEnumerable<Release> releases;
                            if (album != null)
                            {
                                releases = match.Releases.OrderBy(r => Levenshtein.Similarity(r.Title, album));
                                Console.WriteLine($"asdASDASD: {releases.First().ReleaseGroup.Title}");
                            }
                            else
                            {
                                releases = match.Releases.OrderBy(r => r.Date);
                            }
                            foreach (var release in releases)
                            {
                                //Console.WriteLine(release.);
                                /*await Task.Delay(1000);
                                var query2 = new QueryParameters<ReleaseGroup>()
                                {
                                    { "arid", artist.Id },
                                    { "reid", release.Id }
                                };

                                var groups = await client.ReleaseGroups.SearchAsync(query2);

                                Console.WriteLine($"release group matches {groups.Count}");

                                var response = await httpClient.GetAsync($"https://coverartarchive.org/release-group/{groups.First().Id}");
                                if (response.IsSuccessStatusCode)
                                {
                                    //show prompt for user to confirm image
                                    //confimation granted
                                    if (1 == 1)
                                    {
                                        MusicBrainzThumbnail thumbnail = Newtonsoft.Json.JsonConvert.DeserializeObject<MusicBrainzThumbnail>(await response.Content.ReadAsStringAsync());
                                        string url = thumbnail.images.FirstOrDefault().image;
                                        using (WebClient cli = new WebClient())
                                        {
                                            byte[] bytes = cli.DownloadData(url);
                                            Console.WriteLine($"Returning: {artist.Name} {groups.First().Title} {System.IO.Path.GetExtension(url)} + bytes[]");
                                            return (artist.Name, groups.First().Title, System.IO.Path.GetExtension(url), bytes);
                                        }
                                    }
                                }*/

                                var response = await httpClient.GetAsync($"https://coverartarchive.org/release/{release.Id}");
                                if (response.IsSuccessStatusCode)
                                {
                                    //show prompt for user to confirm image
                                    //confimation granted
                                    if (1 == 1)
                                    {
                                        MusicBrainzThumbnail thumbnail = Newtonsoft.Json.JsonConvert.DeserializeObject<MusicBrainzThumbnail>(await response.Content.ReadAsStringAsync());
                                        string url = thumbnail.images.FirstOrDefault().image;
                                        string extension = System.IO.Path.GetExtension(url);
                                        using (WebClient cli = new WebClient())
                                        {
                                            byte[] bytes = cli.DownloadData(url);
                                            Console.WriteLine($"Returning: {artist.Name} {release.Title} {System.IO.Path.GetExtension(url)} + bytes[]");
                                            return (artist.Name, release.Title, System.IO.Path.GetExtension(url), bytes);
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine("Returning null");
                        return (string.Empty, string.Empty, string.Empty, null);
                    }
                    //https://musicbrainz.org/doc/Cover_Art_Archive/API
                    //https://stackoverflow.com/questions/13453678/how-to-get-album-image-using-musicbrainz
                    //https://github.com/avatar29A/MusicBrainz/wiki/Example-4-Recordings
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (string.Empty, string.Empty, string.Empty, null);
            }
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

            return s.Substring(0, 4);
        }
    }

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

    public class MusicBrainzThumbnail
    {
        public List<Image> images { get; set; }
        public string release { get; set; }
    }

    public class Thumbnails
    {
        public string large { get; set; }
        public string small { get; set; }
    }
}
