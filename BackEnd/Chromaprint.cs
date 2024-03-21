using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Com.Geecko.Fpcalc;
using MWP.DatatypesAndExtensions;
using Newtonsoft.Json;

#if DEBUG
using System.Diagnostics;
using MWP.Helpers;
#endif

namespace MWP.BackEnd
{
    /// <summary>
    /// Functions for generating fingerprint from mp3 file and fetching metadata
    /// </summary>
    public static class Chromaprint
    {
        /// <summary>
        /// Searches for ID3v2 tags on the internet and returns them
        /// </summary>
        /// <param name="filePath">path to file for which to handle search</param>
        /// <param name="originalAuthor">Original author of file, only used when user rejects all options</param>
        /// <param name="originalTitle">Original title of file, only used when user rejects all options</param>
        /// <param name="originalAlbum">Original album of file, only used when user rejects all options</param>
        /// <param name="manual">Whether to display popup to user to choose which tags are preffered or handle best set of tags automatically</param>
        /// <returns>New ID3v2 tags or original values of file</returns>
        public static async Task<(string title, string recordingId, string trackId, List<(string title, string id)> artist, List<(string title, string id)> releaseGroup, byte[]? thumbnail)> Search(string filePath, string originalAuthor, string originalTitle, string? originalAlbum, bool manual = true)
        {
            (string, string, string, List<(string, string)>, List<(string, string)>, byte[]?) output = (originalTitle, string.Empty, string.Empty, new List<(string, string)>{(originalAuthor, string.Empty)}, new List<(string, string)>{(originalAlbum ?? string.Empty, string.Empty)}, null);
            try
            {
                ChromaprintResult? chromaprintResult =
                    JsonConvert.DeserializeObject<ChromaprintResult>(FpCalc.InvokeFpCalc(new[]
                        { "-json", $"{filePath}" }));
                
                if (chromaprintResult == null) return output;
                
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(
                    $"https://api.acoustid.org/v2/lookup?format=xml&client=b\'5LIvrD3L&duration={(int)chromaprintResult.duration}&fingerprint={chromaprintResult.fingerprint}&meta=recordings+releasegroups+compress");
                if (!response.IsSuccessStatusCode) return output;
                await using Stream stream = await response.Content.ReadAsStreamAsync();

                XDocument xdoc = XDocument.Load(stream);
                IEnumerable<(string title, string recordingId, string trackId, IEnumerable<(string title, string id)>
                    artists, IEnumerable<(string title, string id )> releaseGroups)> results =
                    from result in xdoc.Descendants("result")
                    from recording in result.Descendants("recording")
                    select (
                        recording.Elements("title").First().Value, //    title 
                        recording.Elements("id").First().Value, //    recordingId
                        result.Elements("id").First().Value, //    trackId
                        from artist in recording.Descendants("artist")
                        select (
                            artist.Elements("name").First().Value,
                            artist.Elements("id").First().Value
                        ),
                        from releaseGroup in recording.Descendants("releasegroup")
                        select (
                            releaseGroup.Elements("title").First().Value,
                            releaseGroup.Elements("id").First().Value
                        )
                    );


                if (manual)
                {
                    MainActivity.StateHandler.FileEvent.WaitOne();
                }
                IEnumerator<(string title, string recordingId, string trackId, IEnumerable<(string title, string id)>
                    artists, IEnumerable<(string title, string id)> releaseGroups)> rEnumerator;
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
                    if (manual)
                    {
                        MainActivity.StateHandler.FileEvent.Set();
                    }
                    return output;
                }

                LastSongSelectionNavigation lastNavigation = LastSongSelectionNavigation.None;
                int cnt = 0;
                List<(string, string, string, IEnumerable<(string, string)>, IEnumerable<(string, string)>)> buffer =
                    new List<(string, string, string, IEnumerable<(string, string)>, IEnumerable<(string, string)>)>();
                List<CoverArt?> coverBuffer = new List<CoverArt?>();
                List<byte[]> imgBuffer = new List<byte[]>();
                bool hasNext = true;

                while (true)
                {
                    (string title, string recordingId, string trackId, IEnumerable<(string title, string id)> artists,
                        IEnumerable<(string title, string id)> releaseGroups) current;
                    CoverArt? coverArtResult;
                    if (cnt >= buffer.Count && hasNext)
                    {
                        current = rEnumerator.Current;
                        List<(string title, string id)> releaseGroupsList = current.releaseGroups.ToList();
                        for (int i = 0; i < current.releaseGroups.Count(); i++)
                        {
                            (string? title, string? id) = releaseGroupsList[i];
                            response.Dispose();
                            response = await client.GetAsync(
                                $"https://coverartarchive.org/release-group/{id}");

                            hasNext = rEnumerator.MoveNext();
                            if (!response.IsSuccessStatusCode) continue;
                            current.releaseGroups = new List<(string title, string id)> { (title, id) };
                            break;
                        }
                        buffer.Add(current);
                        if (!response.IsSuccessStatusCode)
                        {
#if DEBUG
                            MyConsole.WriteLine("Response isn't success");
#endif
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
                        string? imgUrl = coverArtResult?.images.First(image => image.approved).thumbnails.large;
                        //TODO: possible optimization?
                        while (true)
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

                            (int statusCode, string uriString) = await MainActivity.Throttler.Throttle(TaskFactory,
                                $"urlResolution_{originalTitle}", 1000, 1000);

                            // Now look to see if it's a redirect
                            if (statusCode is >= 300 and <= 399)
                            {
#if DEBUG
                                MyConsole.WriteLine($"{imgUrl} redirect to {uriString}");
#endif
                                imgUrl = uriString;
                                continue;
                            }

                            break;
                        }

                        byte[]? imgArr = await GetImage(imgUrl?.Replace("http://", "https://"));
                        if (imgArr != null) imgBuffer.Add(imgArr);
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
                    if (manual)
                    {
                        if (MainActivity.StateHandler.view != null)
                        {
                            MainActivity.StateHandler.view.RunOnUiThread(() =>
                            {
                                BottomDialogFunctions.UpdateSsDialog(current.title, current.artists.First().title,
                                    current.releaseGroups.First().title,
                                    imgBuffer[cnt1], originalAuthor,
                                    originalTitle, cnt1 < buffer.Count - 1 || next, cnt1 > 0);
                            });
                        }
#if DEBUG
                        else
                        {
                            MyConsole.WriteLine("Empty view");
                        }
#endif
                        MainActivity.StateHandler.ResultEvent.WaitOne();
                    }

                    if (MainActivity.StateHandler.songSelectionDialogAction == SongSelectionDialogActions.Next)
                    {
                        lastNavigation = LastSongSelectionNavigation.Next;
                        cnt++;
                        MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                        continue;
                    }

                    if (MainActivity.StateHandler.songSelectionDialogAction == SongSelectionDialogActions.Previous)
                    {
                        lastNavigation = LastSongSelectionNavigation.Previous;
                        cnt--;
                        MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                        continue;
                    }

                    if (MainActivity.StateHandler.songSelectionDialogAction == SongSelectionDialogActions.Cancel)
                    {
                        MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                        break;
                    }

                    if (MainActivity.StateHandler.songSelectionDialogAction == SongSelectionDialogActions.Accept || !manual)
                    {
                        output = (current.title, current.recordingId, current.trackId, current.artists.ToList(),
                            current.releaseGroups.ToList(), imgBuffer[cnt]);
                        //output = ( current.title, current.recordingId, current.trackId, current.artists.ToList(), current.releaseGroups.ToList(), imgBuffer[cnt]);
                        if (manual)
                        {
                            MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.None;
                        }
                        break;
                    }
                }

                response.Dispose();
                rEnumerator.Dispose();
                if (manual)
                {
                    MainActivity.StateHandler.FileEvent.Set();
                }
                return output;
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e.ToString());       
#endif
            }

            if (manual)
            {
                MainActivity.StateHandler.FileEvent.Set();
            }
            return output;
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
    }
}