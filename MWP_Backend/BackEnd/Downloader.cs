using System.Diagnostics;
using System.Net;
using MWP_Backend.BackEnd.Helpers;
using MWP.BackEnd;
using MWP.DatatypesAndExtensions;
using YoutubeReExplode;
using YoutubeReExplode.Channels;
using YoutubeReExplode.Common;
using YoutubeReExplode.Playlists;
using YoutubeReExplode.Videos;
using YoutubeReExplode.Videos.Streams;
using MWP.Chromaprint;
using MWP.FFmpeg;
using MWP.FFmpeg.DataTypes;
using MWP.UIBinding;

namespace MWP_Backend.BackEnd
{
    // TODO: add new song to state handler
    public static class Downloader
    {
        public delegate void SnackbarMessageDelegate(string message);
        private static Chromaprint _chromaprint = new Chromaprint();
        public static async void Download(string url, DownloadActions action)
        {
            List<string> authors = new List<string>();
            //APIThrottler throttler = new APIThrottler();
            if (url.Contains("playlist"))
            {
                //snackbarMessageDelegate("Download started");

                YoutubeClient youtube = new YoutubeClient();
                Playlist playlist = await youtube.Playlists.GetAsync(url);
                IReadOnlyList<PlaylistVideo> videos = await youtube.Playlists.GetVideosAsync(playlist.Id);
                string playlistThumbnailPath = FileManager.GetAvailableTempFile("playlistThumb", "img");
                string realPlaylistThumbnailPath = GetImage(playlist.Thumbnails.AsEnumerable(), playlistThumbnailPath);
                DownloadNotification notification = new DownloadNotification(videos.Count);
                //StatisticsCallback callback = new StatisticsCallback(notification);
                //FFmpegKitConfig.EnableStatisticsCallback(callback);
                //FFmpegConfig.FFmpegStatisticsDelegate = callback;
                for (int index = 0; index < videos.Count; index++)
                {
                    (string songTempPath, string unprocessedTempPath, string thumbnailTempPath) = FileManager.GetAvailableDownloaderFiles();
                    PlaylistVideo video = videos[index];
                    string author = FileManager.Sanitize(FileManager.GetAlias(video.Author.ChannelTitle));
                    bool shouldGetAuthorImage;
                    if (authors.Contains(author))
                    {
                        shouldGetAuthorImage = false;
                    }
                    else
                    {
                        authors.Add(author);
                        shouldGetAuthorImage = true;
                    }

                    int poradieVPlayliste = index;
                    new Thread(() =>
                    {
                        DownloadVideo(video.Url, songTempPath, unprocessedTempPath, thumbnailTempPath, video.Id, video.Title, video.Author.ChannelTitle,
                            shouldGetAuthorImage, video.Author.ChannelId, notification, playlist.Title, realPlaylistThumbnailPath,
                            video == videos.Last(), poradieVPlayliste);
                    }).Start();
                }
            }
            else if (url.Contains("watch"))
            {
                //snackbarMessageDelegate("Download started");
                try
                {
                    YoutubeClient youtube = new YoutubeClient();
                    Video video = await youtube.Videos.GetAsync(url);
                    (string songTempPath, string unprocessedTempPath, string thumbnailTempPath) = FileManager.GetAvailableDownloaderFiles();
                    DownloadNotification notification = new DownloadNotification();
                    //StatisticsCallback callback = new StatisticsCallback(notification);
                    //FFmpegKitConfig.EnableStatisticsCallback(callback);
                    //FFmpegConfig.FFmpegStatisticsDelegate = callback;
                    new Thread(() => { DownloadVideo(url, songTempPath, unprocessedTempPath, thumbnailTempPath, video.Id, video.Title, video.Author.ChannelTitle, true, video.Author.ChannelId, notification); }).Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
            }
            else
            {
                //snackbarMessageDelegate("This is neither video nor playlist");
                //throw new ArgumentException($"{url} is not video or playlist");
            }
        }


        private static async void DownloadVideo(string url, string songTempPath, string unprocessedPath, string thumbnailTempPath, VideoId videoId, string videoTitle, string channelName, bool getAuthorImage, ChannelId channelId, DownloadNotification notification, string? album = null, string? playlistThumbnailPath = null, bool last = false, int? poradieVPlayliste = null)
        {
            try
            {
                FFmpeg ffmpeg = new FFmpeg();
                Progress<double> downloadProgress = new Progress<double>();
                notification.Stage1(downloadProgress, videoTitle, poradieVPlayliste);
                
                Task<(string title, string recordingId, string trackId, List<(string title, string id)> artists,
                    List<(string title, string id)> releaseGroup, byte[]? thumbnail)>? mbSearchTask = null;
                Task imageTask = GetImage(thumbnailTempPath, videoId);
                
                YoutubeClient youtube = new YoutubeClient();
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
                IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                await youtube.Videos.Streams.DownloadAsync(streamInfo, unprocessedPath, downloadProgress);
                
                if (SettingsManager.ShouldUseChromaprintAtDownload)
                {
                    string? album1 = album;
                    async Task<(string title, string recordingId, string trackId, List<(string title, string id)> artists, List<(string title, string id)> releaseGroup, byte[]? thumbnail)> TaskFactory() => await _chromaprint.Search(unprocessedPath, channelName, videoTitle, album1);
                    mbSearchTask = StateHandler.Throttler.Throttle(TaskFactory, "GetMusicBrainzIdFromFingerprint");
                }
                
                //FFmpegKitConfig.IgnoreSignal(Signal.Sigxcpu);
                //FFmpegConfig.IgnoreSignal(24);
                
                await imageTask;
                
                int duration;
                FFmpegSession sessionLength = new FFmpegSession();
                await ffmpeg.Run($"-i {unprocessedPath} -show_entries format=duration -v quiet -of csv=\"p=0\"", sessionLength);
                /*try
                {
                    duration = s;
                }
                catch
                {
                    duration = 170; //random song duration of 2:50
                }*/
                
                // ReSharper disable once InconsistentNaming
                FFmpegSession sessionConvert = new FFmpegSession();
                Task FFmpegTask = Task.Run(() =>
                {
                    ffmpeg.Run($"-i {unprocessedPath} -i {thumbnailTempPath} -filter_complex [1:v]crop=iw:iw/2[img] -map 0:0 -map [img] -c:a libmp3lame -id3v2_version 4 -loglevel quiet -y {songTempPath}", sessionConvert); //aspect ratio 2:1
                    //session = new FFmpegSession(new []{"-i", $"{Path}/tmp/unprocessed{i}.mp3", "-i", $"{Path}/tmp/file{i}.jpg", "-map", "0:0", "-map", "1:0", "-c:a", "libmp3lame", "-id3v2_version", "4", "-loglevel", "quiet", "-y", $"{Path}/tmp/video{i}.mp3"}); //original aspect ration 
                    //StateHandler.SessionIdToPlaylistOrderMapping.Add(executionId, (poradieVPlayliste, duration));
                    notification.Stage3(poradieVPlayliste);
#if DEBUG
                    MyConsole.WriteLine($"ffmpeg finished {poradieVPlayliste}");
#endif
                });
                _ = FFmpegTask.ContinueWith(_ =>
                {
#if DEBUG
                    MyConsole.WriteLine($"ffmpeg finished2 {poradieVPlayliste}");
#endif
                    notification.Stage3(poradieVPlayliste);
                });
                
                string title = videoTitle;
                List<(string title, string id)> artists = new List<(string title, string id)>
                    { (channelName, string.Empty) };
                List<(string title, string id)> releaseGroups = new List<(string title, string id)>
                    { (album ?? string.Empty, string.Empty)};
                string recordingId;
                string trackId = recordingId = string.Empty;
                byte[]? thumbnail = null;
                
                
#if DEBUG
                MyConsole.WriteLine($"ShouldUseChromaprintAtDownload: {SettingsManager.ShouldUseChromaprintAtDownload}");
#endif
                if (SettingsManager.ShouldUseChromaprintAtDownload)
                {
                    if (mbSearchTask != null)
                        (title, recordingId, trackId, artists, releaseGroups, thumbnail) = await mbSearchTask;
                }
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
                        else if(playlistThumbnailPath != null)
                        {
                            Task t = Task.Run(() =>
                            {
                                    File.Copy(playlistThumbnailPath, $"{FileManager.MusicFolder}/{artistPath}/{albumPath}/cover{Path.GetExtension(playlistThumbnailPath)}", true);
                            });
                            if (last)
                            {
                                _ = t.ContinueWith(_ =>
                                { 
                                    File.Delete(playlistThumbnailPath);
                                });

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

                await FFmpegTask;
                _ = Task.Run(() => {
                    File.Delete(thumbnailTempPath);
                    File.Delete(unprocessedPath);
                });
                
                if (sessionConvert.ReturnCode is FFmpegStatusCode.Success)
                {

                    if (album != null)
                    {
                        FileManager.AddSong(songTempPath, title, artists.Select(t => t.title).Distinct().ToArray(), artists.First().id, recordingId, trackId, album, releaseGroups.First().id);
                    }
                    else
                    {
                        FileManager.AddSong(songTempPath, title, artists.Select(t => t.title).Distinct().ToArray(), artists.First().id, recordingId, trackId);
                    }
                    notification.Stage4(true, string.Empty, poradieVPlayliste);
                    
                }
                else 
                {
                    notification.Stage4(false, $"{sessionConvert.ReturnCode}", poradieVPlayliste);
                    File.Delete(songTempPath);
                    //if (sessionConvert.ReturnCode == null) return;
#if DEBUG
                    MyConsole.WriteLine($"FFmpeg failed with status code {sessionConvert.ReturnCode} {sessionConvert.RawOutput}");
#endif
                    //View view = (View)sender;
                    //Snackbar.Make(view, $"{session.ReturnCode} Failed: {title}", BaseTransientBottomBar.LengthLong).Show();
                    Snackbar.Make($"{sessionConvert.ReturnCode} Failed: {title}");
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
                ///View view = (View)sender;
                //Snackbar.Make(view, $"Failed: {videoTitle}", BaseTransientBottomBar.LengthLong).Show();
                Snackbar.Make($"Failed: {videoTitle}");
            }
        }

        private static async Task<bool> GetHttpStatus(string url)
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

        public static async Task GetImage(string path, VideoId id)
        {
            using WebClient cli = new WebClient();
            if (await GetHttpStatus($"https://img.youtube.com/vi/{id}/maxresdefault.jpg"))
            {
                await File.WriteAllBytesAsync(path, cli.DownloadData($"https://img.youtube.com/vi/{id}/maxresdefault.jpg"));
            }
            else
            {
                await File.WriteAllBytesAsync(path, cli.DownloadData($"https://img.youtube.com/vi/{id}/0.jpg"));
            }
        }

        private static async Task<byte[]?> GetImage(string? url)
        {
            if (url == null) return null;
            if (!await GetHttpStatus(url)) return null;
            using WebClient cli = new WebClient();
#if DEBUG
            MyConsole.WriteLine($"Downloading {url}");       
#endif
            return cli.DownloadData(url);
        }

        private static string GetImage(IEnumerable<Thumbnail> authorThumbnails, string thumbnailPath)
        {
            int maxArea = 0;
            string thumbnailUrl = string.Empty;
            foreach (Thumbnail thumbnail in authorThumbnails)
            {
                if (thumbnail.Resolution.Area <= maxArea) continue;
                maxArea = thumbnail.Resolution.Area;
                thumbnailUrl = thumbnail.Url;
            }

            WebClient cli = new WebClient();
            File.WriteAllBytes(thumbnailPath, cli.DownloadData(thumbnailUrl));
            cli.Dispose();
            string extension = FileManager.GetImageFormat(thumbnailPath);
            string originalExtension = Path.GetExtension(thumbnailPath);
            File.Move(thumbnailPath, thumbnailPath.Replace(originalExtension, extension));
            return thumbnailPath.Replace(originalExtension, extension);
        }
    }

    /*/// <inheritdoc cref="Com.Arthenica.Ffmpegkit.IStatisticsCallback" />
    public class StatisticsCallback : FFmpegStatisticsDelegate
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
        public override void OnStatisticsReceived(FFmpegStatistics statistics)
        {
            try
            {
                (int? poradieVPlayliste, int duration) =
                    StateHandler.SessionIdToPlaylistOrderMapping[statistics.ExecutionId];
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
    }*/
}


