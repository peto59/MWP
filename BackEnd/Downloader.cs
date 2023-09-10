using Android.App;
using Android.Views;
using Com.Arthenica.Ffmpegkit;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ass_Pain.BackEnd;
using Com.Geecko.Fpcalc;
using Newtonsoft.Json;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Signal = Com.Arthenica.Ffmpegkit.Signal;
#if DEBUG
using System.Diagnostics;
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    // TODO: add new song to state handler
    internal static class Downloader
    {
        public static async void Download(object sender, EventArgs e, string url, DownloadActions action)
        {
            List<string> authors = new List<string>();
            View view = (View)sender;
            //APIThrottler throttler = new APIThrottler();
            if (url.Contains("playlist"))
            {
                
                Snackbar.Make(view, $"Download started", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();

                YoutubeClient youtube = new YoutubeClient();
                Playlist playlist = await youtube.Playlists.GetAsync(url);
                IReadOnlyList<PlaylistVideo> videos = await youtube.Playlists.GetVideosAsync(playlist.Id);
                int a = FileManager.GetAvailableFile("playlistThumb");
                string ext = GetImage(playlist.Thumbnails.AsEnumerable(), $"{FileManager.PrivatePath}/tmp", $"playlistThumb{a}");
                DownloadNotification notification = new DownloadNotification(videos.Count);
                StatisticsCallback callback = new StatisticsCallback(notification);
                FFmpegKitConfig.EnableStatisticsCallback(callback);
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
                    int index1 = index;
                    new Thread(() =>
                    {
                        DownloadVideo(sender, video.Url, i, video.Id, video.Title, video.Author.ChannelTitle,
                            getAuthorImage, video.Author.ChannelId, notification, playlist.Title, a,
                            video == videos.Last(), ext, index1);
                    }).Start();
                }
            }
            else if (url.Contains("watch"))
            {
                Snackbar.Make(view, $"Download started", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                YoutubeClient youtube = new YoutubeClient();
                Video video = await youtube.Videos.GetAsync(url);
                int i = FileManager.GetAvailableFile();
                DownloadNotification notification = new DownloadNotification();
                StatisticsCallback callback = new StatisticsCallback(notification);
                FFmpegKitConfig.EnableStatisticsCallback(callback);
                new Thread(() => { DownloadVideo(sender, url, i, video.Id, video.Title, video.Author.ChannelTitle, true, video.Author.ChannelId, notification); }).Start();
            }
            else
            {
                Snackbar.Make(view, $"This is neither video nor playlist", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                //throw new ArgumentException($"{url} is not video or playlist");
            }
        }


        private static async void DownloadVideo(object sender, string url, int i, VideoId videoId, string videoTitle, string channelName, bool getAuthorImage, ChannelId channelId, DownloadNotification notification, string album = null, int a = 0, bool last = false, string playlistCoverExtension = null, int? poradieVPlayliste = null)
        {
            try
            {
                Progress<double> downloadProgress = new Progress<double>();
                notification.Stage1(downloadProgress, videoTitle, poradieVPlayliste);
                
                Task<(string title, string recordingId, string trackId, List<(string title, string id)> artists,
                    List<(string title, string id)> releaseGroup, byte[] thumbnail)> mbSearchTask = null;
                Task imageTask = GetImage(i, videoId);
                
                YoutubeClient youtube = new YoutubeClient();
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
                IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{FileManager.PrivatePath}/tmp/unprocessed{i}.mp3", downloadProgress);
                
                if (SettingsManager.ShouldUseChromaprintAtDownload)
                {
                    async Task<(string title, string recordingId, string trackId, List<(string title, string id)> artists, List<(string title, string id)> releaseGroup, byte[] thumbnail)> TaskFactory() => await GetMusicBrainzIdFromFingerprint($"{FileManager.PrivatePath}/tmp/unprocessed{i}.mp3", channelName, videoTitle);
                    mbSearchTask = MainActivity.throttler.Throttle(TaskFactory, "GetMusicBrainzIdFromFingerprint");
                }
                
                FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
                
                await imageTask;
                
                int duration;
                string s = FFprobeKit.Execute($"-i {FileManager.PrivatePath}/tmp/unprocessed{i}.mp3 -show_entries format=duration -v quiet -of csv=\"p=0\"")?.Output;
                try
                {
                    duration = s != null ? (int)float.Parse(s) : 170;
                }
                catch
                {
                    duration = 170; //random song duration of 2:50
                }
                
                // ReSharper disable once InconsistentNaming
                Task<FFmpegSession> FFmpegTask = Task.Run(() =>
                {
                    FFmpegSession session = new FFmpegSession(new []{"-i", $"{FileManager.PrivatePath}/tmp/unprocessed{i}.mp3", "-i", $"{FileManager.PrivatePath}/tmp/file{i}.jpg", "-filter_complex", "[1:v]crop=iw:iw/2[img]", "-map", "0:0", "-map", "[img]", "-c:a", "libmp3lame", "-id3v2_version", "4", "-loglevel", "quiet", "-y", $"{FileManager.PrivatePath}/tmp/video{i}.mp3"}); //aspect ratio 2:1
                    //FFmpegSession session = new FFmpegSession(new []{"-i", $"{Path}/tmp/unprocessed{i}.mp3", "-i", $"{Path}/tmp/file{i}.jpg", "-map", "0:0", "-map", "1:0", "-c:a", "libmp3lame", "-id3v2_version", "4", "-loglevel", "quiet", "-y", $"{Path}/tmp/video{i}.mp3"}); //original aspect ration 
                    MainActivity.stateHandler.SessionIdToPlaylistOrderMapping.Add(session.SessionId, (poradieVPlayliste, duration));
                    FFmpegKitConfig.FfmpegExecute(session);
                    notification.Stage3(poradieVPlayliste);
#if DEBUG
                    MyConsole.WriteLine($"ffmpeg finished {poradieVPlayliste}");
#endif
                    return session;
                });
                _ = FFmpegTask.ContinueWith(_ =>
                {
#if DEBUG
                    MyConsole.WriteLine($"ffmpeg finished2 {poradieVPlayliste}");
#endif
                    notification.Stage3(poradieVPlayliste);
                });
                
                string title = videoTitle;
                List<(string title, string id)> artists = new List<(string title, string id)>()
                    { (channelName, string.Empty) };
                List<(string title, string id)> releaseGroups = new List<(string title, string id)>()
                    { (album ?? string.Empty, string.Empty)};
                string recordingId, trackId;
                trackId = recordingId = string.Empty;
                byte[] thumbnail = null;
                
                
#if DEBUG
                MyConsole.WriteLine($"ShouldUseChromaprintAtDownload: {SettingsManager.ShouldUseChromaprintAtDownload}");
#endif
                if (SettingsManager.ShouldUseChromaprintAtDownload)
                {
                    if (mbSearchTask != null)
                        (title, recordingId, trackId, artists, releaseGroups, thumbnail) = await mbSearchTask;
                }
                string fileName = FileManager.Sanitize(title);
                string artistPath = FileManager.Sanitize(FileManager.GetAlias(artists.First().title));
                Directory.CreateDirectory($"{FileManager.MusicFolder}/{artistPath}");
                //string output = $"{FileManager.MusicFolder}/{artistPath}/{fileName}.mp3";
                

                if (releaseGroups.First().title != string.Empty)
                {
                    album = releaseGroups.First().title;
                    string albumPath = FileManager.Sanitize(album);
                    Directory.CreateDirectory($"{FileManager.MusicFolder}/{artistPath}/{albumPath}");
                    //output = $"{FileManager.MusicFolder}/{artistPath}/{albumPath}/{fileName}.mp3";
                    if (!File.Exists($"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover.jpg") && !File.Exists($"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover.png"))
                    {
                        if (thumbnail != null)
                        {
                            string imgExtenstion = FileManager.GetImageFormat(thumbnail);
                            File.Create($"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover{imgExtenstion}").Close();
                            _ = File.WriteAllBytesAsync($"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover{imgExtenstion}", thumbnail);
                        }
                        else
                        {
                            Task t = Task.Run(() => {
                                File.Copy($"{FileManager.PrivatePath}/tmp/playlistThumb{a}{playlistCoverExtension}", $"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover{playlistCoverExtension}", true);
                            });
                            if (last)
                            {
                                _ = t.ContinueWith(_ =>
                                    File.Delete($"{FileManager.PrivatePath}/tmp/playlistThumb{a}{playlistCoverExtension}"));

                            }
                        }
                    }
                }

                if (getAuthorImage)
                {
                    if (!File.Exists($"{FileManager.MusicFolder}/{artistPath}/cover.jpg") && !File.Exists($"{FileManager.MusicFolder}/{artistPath}/cover.png"))
                    {
                        _ = Task.Run(async () => {
                            Channel authorThumbnails = await youtube.Channels.GetAsync(channelId);
                            GetImage(authorThumbnails.Thumbnails.AsEnumerable(), $"{FileManager.MusicFolder}/{artistPath}");
                        });
                    }
                }

                FFmpegSession session = await FFmpegTask;
                _ = Task.Run(() => {
                    File.Delete($"{FileManager.PrivatePath}/tmp/file{i}.jpg");
                    File.Delete($"{FileManager.PrivatePath}/tmp/unprocessed{i}.mp3");
                });
                
                if (session.ReturnCode is { IsSuccess: true })
                {

                    if (album != null)
                    {
                        FileManager.AddSong((View)sender, $"{FileManager.PrivatePath}/tmp/video{i}.mp3", title, artists.Select(t => t.title).Distinct().ToArray(), artists.First().id, recordingId, trackId, album, releaseGroups.First().id);
                    }
                    else
                    {
                        FileManager.AddSong((View)sender, $"{FileManager.PrivatePath}/tmp/video{i}.mp3", title, artists.Select(t => t.title).Distinct().ToArray(), artists.First().id, recordingId, trackId);
                    }
                    notification.Stage4(true, string.Empty, poradieVPlayliste);
                    
                }
                else 
                {
                    notification.Stage4(false, $"{session.ReturnCode?.Value}", poradieVPlayliste);
                    File.Delete($"{FileManager.PrivatePath}/tmp/video{i}.mp3");
                    if (session.ReturnCode == null) return;
#if DEBUG
                    MyConsole.WriteLine($"FFmpeg failed with status code {session.ReturnCode} {session.Output} {session}");
#endif
                    View view = (View)sender;
                    Snackbar.Make(view, $"{session.ReturnCode} Failed: {title}", Snackbar.LengthLong)
                        .SetAction("Action", (View.IOnClickListener)null).Show();
                }
            }
            catch (Exception ex)
            {
                notification.Stage4(false, $"{ex.Message}", poradieVPlayliste);
#if DEBUG
                StackTrace st = new StackTrace(ex, true);
                // Get the top stack frame
                StackFrame frame = st.GetFrame(st.FrameCount -1 );
                // Get the line number from the stack frame
                int line = frame.GetFileLineNumber();
                MyConsole.WriteLine($"[tryCatch line: {line}]BIG EROOOOOOOOR: {ex}");
#endif
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
            catch(Exception ex)
            {
#if DEBUG
                MyConsole.WriteLine(ex);       
#endif
                return false;
            }
        }

        public static async Task GetImage(int i, VideoId id)
        {
            using WebClient cli = new WebClient();
            if (await GetHttpStatus($"https://img.youtube.com/vi/{id}/maxresdefault.jpg"))
            {
                await File.WriteAllBytesAsync($"{FileManager.PrivatePath}/tmp/file{i}.jpg", cli.DownloadData($"https://img.youtube.com/vi/{id}/maxresdefault.jpg"));
            }
            else
            {
                await File.WriteAllBytesAsync($"{FileManager.PrivatePath}/tmp/file{i}.jpg", cli.DownloadData($"https://img.youtube.com/vi/{id}/0.jpg"));
            }
        }
        
        public static async Task<byte[]> GetImage(string url)
        {
            //string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            using WebClient cli = new WebClient();
            if (!await GetHttpStatus(url)) return null;
#if DEBUG
            MyConsole.WriteLine($"Downloading {url}");       
#endif
            return cli.DownloadData(url);
        }

        public static string GetImage(IEnumerable<Thumbnail> authorThumbnails, string thumbnailPath, string fileName = "cover")
        {
            Directory.CreateDirectory(thumbnailPath);
            File.Create($"{thumbnailPath}/{fileName}.jpg").Close();
            int maxArea = 0;
            string thumbnailUrl = string.Empty;
            foreach (Thumbnail thumbnail in authorThumbnails)
            {
                if (thumbnail.Resolution.Area <= maxArea) continue;
                maxArea = thumbnail.Resolution.Area;
                thumbnailUrl = thumbnail.Url;
            }

            WebClient cli = new WebClient();
            byte[] bytes = cli.DownloadData(thumbnailUrl);
            cli.Dispose();
            string extension = FileManager.GetImageFormat(bytes);
            if (extension == ".jpg")
            {
                File.WriteAllBytes($"{thumbnailPath}/{fileName}.jpg", bytes);
                return ".jpg";
            }
            
            if (extension == ".idk") return ".idk";
            File.WriteAllBytes($"{thumbnailPath}/{fileName}.png", bytes);
            File.Delete($"{thumbnailPath}/{fileName}.jpg");
            return ".png";
        }

        private static async Task<(string title, string recordingId, string trackId, List<(string title, string id)> artist, List<(string title, string id)> releaseGroup, byte[] thumbnail)> GetMusicBrainzIdFromFingerprint(string filePath, string originalAuthor, string originalTitle)
        {
            ChromaprintResult chromaprintResult = JsonConvert.DeserializeObject<ChromaprintResult>(FpCalc.InvokeFpCalc(new[] { "-json", $"{filePath}" }));
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync($"https://api.acoustid.org/v2/lookup?format=xml&client=b\'5LIvrD3L&duration={(int)chromaprintResult.duration}&fingerprint={chromaprintResult.fingerprint}&meta=recordings+releasegroups+compress");
            if (!response.IsSuccessStatusCode) return (string.Empty, string.Empty, string.Empty, new List<(string, string)>{(string.Empty, string.Empty)}, new List<(string, string)>{(string.Empty, string.Empty)}, null);
            await using Stream stream = await response.Content.ReadAsStreamAsync();

            XDocument xdoc = XDocument.Load(stream);
            IEnumerable<(string title, string recordingId, string trackId, IEnumerable<(string title, string id)> artists, IEnumerable<(string title, string id )> releaseGroups)> results = 
                from result in xdoc.Descendants("result")
                from recording in result.Descendants("recording")
                select (
                    recording.Elements("title").First().Value,  //    title 
                    recording.Elements("id").First().Value,     //    recordingId
                    result.Elements("id").First().Value,        //    trackId
                    from artist in recording.Descendants("artist") select ( 
                        artist.Elements("name").First().Value,
                        artist.Elements("id").First().Value
                    ),
                    from releaseGroup in recording.Descendants("releasegroup") select ( 
                        releaseGroup.Elements("title").First().Value,
                        releaseGroup.Elements("id").First().Value
                    )
                );
            
            
            MainActivity.stateHandler.FileEvent.WaitOne();
            (string originalTitle, string, string, List<(string, string)>, List<(string, string)>, byte[]) output = (originalTitle, string.Empty, string.Empty, new List<(string, string)>{(originalAuthor, string.Empty)}, new List<(string, string)>{(string.Empty, string.Empty)}, null);
            IEnumerator<(string title, string recordingId, string trackId, IEnumerable<(string title, string id)> artists, IEnumerable<(string title, string id)> releaseGroups)> rEnumerator;
            try
            {
                rEnumerator = results.GetEnumerator();
                rEnumerator.MoveNext();
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return output;
            }
            
            LastSongSelectionNavigation lastNavigation = LastSongSelectionNavigation.None;
            int cnt = 0;
            List<(string, string, string, IEnumerable<(string, string)>, IEnumerable<(string, string)>)> buffer = new List<(string, string, string, IEnumerable<(string, string)>, IEnumerable<(string, string)>)>();
            List<CoverArt> coverBuffer = new List<CoverArt>();
            List<byte[]> imgBuffer = new List<byte[]>();
            bool hasNext = true;
            
            while (true)
            {
                (string title, string recordingId, string trackId, IEnumerable<(string title, string id)> artists, IEnumerable<(string title, string id)> releaseGroups) current;
                CoverArt coverArtResult;
                if (cnt >= buffer.Count && hasNext)
                {
                    current = rEnumerator.Current;
                    buffer.Add(current);
                    response.Dispose();
                    response = await client.GetAsync($"https://coverartarchive.org/release-group/{current.releaseGroups.First().id}");
                    
                    hasNext = rEnumerator.MoveNext();

                    if (!response.IsSuccessStatusCode)
                    {
                        coverBuffer.Add(null);
                        if (lastNavigation == LastSongSelectionNavigation.Previous)
                        {
                            cnt--;
                            continue;
                        }
                        cnt++;
                        continue;
                    }
                    
                    coverArtResult = JsonConvert.DeserializeObject<CoverArt>(await response.Content.ReadAsStringAsync());
                    coverBuffer.Add(coverArtResult);
                    string imgUrl = coverArtResult.images.First(image => image.approved).thumbnails.large;
                    //TODO: possible optimization?
                    while(true)
                    {
                        (int, string) TaskFactory()
                        {
#if DEBUG
                            Debug.Assert(imgUrl != null, nameof(imgUrl) + " != null");
#endif
                            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(imgUrl);
                            webRequest.AllowAutoRedirect = false; // IMPORTANT 
                            webRequest.Timeout = 3000; // timeout 3s    
                            using HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                            return ((int)webResponse.StatusCode, webResponse.Headers["Location"]);
                        }

                        (int statusCode, string uriString)  = await MainActivity.throttler.Throttle(TaskFactory, $"urlResolution_{originalTitle}", 1000, 1000);
                        
                        // Now look to see if it's a redirect
                        if (statusCode is >= 300 and <= 399)
                        {
#if DEBUG
                            MyConsole.WriteLine($"{imgUrl} redirect to { uriString ?? "NULL"}");
#endif
                            imgUrl = uriString;
                            continue;
                        }
                        break;
                    }
                    byte[] imgArr = await GetImage(imgUrl?.Replace("http://", "https://"));
                    imgBuffer.Add(imgArr);
                }
                else
                {
                    current = buffer[cnt];
                    coverArtResult = coverBuffer[cnt];
                    
                    if (coverArtResult == null)
                    {
                        if (lastNavigation == LastSongSelectionNavigation.Previous)
                        {
                            cnt--;
                            continue;
                        }
                        cnt++;
                        continue;
                    }
                }

                bool next = hasNext;
                int cnt1 = cnt;
                MainActivity.stateHandler.view.RunOnUiThread(() =>
                {
                    youtube.SongSelectionDialog(current.title, current.artists.First().title,
                        current.releaseGroups.First().title,
                        imgBuffer[cnt1], originalAuthor,
                        originalTitle, cnt1 < buffer.Count -1 || next, cnt1 > 0);
                });
                MainActivity.stateHandler.ResultEvent.WaitOne();
                
                if (MainActivity.stateHandler.songSelectionDialogAction == SongSelectionDialogActions.Next)
                {
                    lastNavigation = LastSongSelectionNavigation.Next;
                    cnt++;
                    MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                    continue;
                }
                
                if (MainActivity.stateHandler.songSelectionDialogAction == SongSelectionDialogActions.Previous)
                {
                    lastNavigation = LastSongSelectionNavigation.Previous;
                    cnt--;
                    MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                    continue;
                }
                
                if (MainActivity.stateHandler.songSelectionDialogAction == SongSelectionDialogActions.Cancel)
                {
                    MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                    break;
                }
                
                if (MainActivity.stateHandler.songSelectionDialogAction == SongSelectionDialogActions.Accept)
                {
                    output = ( current.title, current.recordingId, current.trackId, current.artists.ToList(), current.releaseGroups.ToList(), imgBuffer[cnt]);
                    //output = ( current.title, current.recordingId, current.trackId, current.artists.ToList(), current.releaseGroups.ToList(), imgBuffer[cnt]);
                    MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                    break;
                }

            }

            response.Dispose();
            rEnumerator.Dispose();
            MainActivity.stateHandler.FileEvent.Set();
            return output;
        }
    }

    /// <inheritdoc cref="Com.Arthenica.Ffmpegkit.IStatisticsCallback" />
    public class StatisticsCallback : Java.Lang.Object, IStatisticsCallback
    {

        ///<summary>
        ///Creates new Callback instance which should be created for every instance of FFmpeg
        ///</summary>
        ///<param name="notification">Notification on which progress should be displayed</param>
        public StatisticsCallback(DownloadNotification notification)
        {
            Notification = notification;
#if DEBUG
            MyConsole.WriteLine("Creating new callback");
#endif
        }
        private DownloadNotification Notification { get; }

        /// <inheritdoc />
        public void Apply(Statistics statistics)
        {
            try
            {
                if (statistics == null) return;
                (int? poradieVPlayliste, int duration) =
                    MainActivity.stateHandler.SessionIdToPlaylistOrderMapping[statistics.SessionId];
#if DEBUG
                MyConsole.WriteLine($"Percentage: {(statistics.Time / duration / 10).Constraint(0, 100)}");
                MyConsole.WriteLine(statistics.ToString());
#endif
                Notification.Stage2((statistics.Time / duration / 10).Constraint(0, 100), poradieVPlayliste);
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
        }
    }
}


