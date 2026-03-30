using ExcData;
using KartRider.Common.Utilities;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public class RandomTrack
    {
        public static Dictionary<uint, string> track = new Dictionary<uint, string>();
        public static XDocument randomTrack = new XDocument();

        public static string GameTrack = "village_R01";

        public static string GetTrackName(uint trackId)
        {
            if (track.ContainsKey(trackId))
            {
                return track[trackId];
            }
            else
            {
                return trackId.ToString();
            }
        }
        private static string ExcludeRecentTracks(Random random, GameRoom room, XElement TrackSet)
        {
            string RandomTrackGameTrack = "";
            // 去除之前玩过的赛道
            XElement selectedTrack = null;
            if (room != null)
            {
                // 获取房间中最近游玩的赛道列表
                List<uint> recentTracks = room.GetRecentTracks();
                Console.WriteLine("recentTracks: {0}", recentTracks.Count);
                // 如果最近游玩的赛道列表为空，则从所有赛道中随机选择一个赛道
                if (recentTracks.Count > 0)
                {
                    // 从所有赛道中过滤掉最近游玩过的赛道,得到没玩过的赛道
                    var availableTracks = TrackSet
                        .Descendants("track")
                        .Where(t =>
                            !recentTracks.Contains(
                                Adler32Helper.GenerateAdler32_UNICODE((string)t.Attribute("id"))
                            )
                        )
                        .ToList();
                    Console.WriteLine("availableTracks: {0}", availableTracks.Count);
                    // 从没玩过的赛道中随机选择一个赛道
                    if (availableTracks.Count > 0)
                    {
                        selectedTrack = availableTracks[random.Next(availableTracks.Count)];
                        Console.WriteLine("selectedTrack: {0}", selectedTrack);
                        RandomTrackGameTrack = (string)selectedTrack.Attribute("id");
                        // return Adler32Helper.GenerateAdler32_UNICODE(RandomTrackGameTrack, 0);
                        return RandomTrackGameTrack;
                    }
                }
            }
            return null;
        }

        public static uint GetRandomTrack(string Nickname, byte GameType, uint Track)
        {
            string RandomTrackGameType = "speed";
            string RandomTrackSetRandomTrack = "all";
            string RandomTrackGameTrack = "village_R01";

            if (GameType == 0)
            {
                RandomTrackGameType = "speed";
            }
            else if (GameType == 1)
            {
                RandomTrackGameType = "item";
            }

            if (Track == 0)
            {
                RandomTrackSetRandomTrack = "all";
            }
            else if (Track == 1)
            {
                RandomTrackSetRandomTrack = "clubSpeed";
            }
            else if (Track == 3)
            {
                RandomTrackSetRandomTrack = "hot1";
            }
            else if (Track == 4)
            {
                RandomTrackSetRandomTrack = "hot2";
            }
            else if (Track == 5)
            {
                RandomTrackSetRandomTrack = "hot3";
            }
            else if (Track == 6)
            {
                RandomTrackSetRandomTrack = "hot4";
            }
            else if (Track == 7)
            {
                RandomTrackSetRandomTrack = "hot5";
            }
            else if (Track == 8)
            {
                RandomTrackSetRandomTrack = "new";
            }
            else if (Track == 23)
            {
                RandomTrackSetRandomTrack = "crazy";
            }
            else if (Track == 30)
            {
                RandomTrackSetRandomTrack = "reverse";
            }
            else if (Track == 40)
            {
                RandomTrackSetRandomTrack = "speedAll";
            }
            else
            {
                RandomTrackSetRandomTrack = "Unknown";
            }

            if (RandomTrackSetRandomTrack == "all" || RandomTrackSetRandomTrack == "speedAll")
            {
                Random random = new Random();
                if (!FileName.FileNames.ContainsKey(Nickname))
                {
                    FileName.Load(Nickname);
                }
                var filename = FileName.FileNames[Nickname];
                var FavoriteTrackList = new Favorite_Track();
                if (File.Exists(filename.FavoriteTrack_LoadFile))
                {
                    FavoriteTrackList = JsonHelper.DeserializeNoBom<Favorite_Track>(filename.FavoriteTrack_LoadFile) ?? new Favorite_Track();
                }
                List<uint> availableTracks = FavoriteTrackList.GetAllTracks();
                if (availableTracks.Count > 0)
                {
                    return availableTracks[random.Next(availableTracks.Count)];
                }
                else
                {
                    Random AllRandom = new Random();
                    var validTracks = track.Where(t => t.Value.Contains("_I") || t.Value.Contains("_R") || t.Value.Contains("_C") || t.Value.Contains("_K") || t.Value.Contains("_DIY")).ToList();
                    if (validTracks.Count > 0)
                    {
                        int randomIndex = AllRandom.Next(validTracks.Count);
                        var selectedTrack = validTracks.ElementAt(randomIndex).Value;
                        return Adler32Helper.GenerateAdler32_UNICODE(selectedTrack, 0);
                    }
                    else
                    {
                        return Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
                    }
                }
            }
            else if (RandomTrackSetRandomTrack == "Unknown")
            {
                if (track.ContainsKey(Track))
                {
                    return Track;
                }
                else
                {
                    return Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
                }
                Console.WriteLine("RandomTrack: {0} / {1} / {2}", RandomTrackGameType, RandomTrackSetRandomTrack, RandomTrack.GameTrack);
            }
            else
            {
                XDocument doc = randomTrack;
                Random random = new Random();
                // 读取xml文件
                var TrackSet = doc.Descendants("RandomTrackSet")
                    .FirstOrDefault(rts =>
                        (string)rts.Attribute("gameType") == RandomTrackGameType
                        && (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack
                    );

                if (TrackSet != null)
                {
                    // 获取房间对象
                    int roomId = RoomManager.TryGetRoomId(Nickname);
                    GameRoom room = RoomManager.GetRoom(roomId);

                    // 过滤最近玩过的赛道
                    RandomTrackGameTrack = ExcludeRecentTracks(random, room, TrackSet);
                    if (RandomTrackGameTrack != null)
                    {
                        return Adler32Helper.GenerateAdler32_UNICODE(RandomTrackGameTrack, 0);
                    }

                    // 过滤赛道失败,从所有赛道中选择
                    var selectedTrack = TrackSet
                        .Descendants("track")
                        .ElementAt(random.Next(TrackSet.Descendants("track").Count()));
                    RandomTrackGameTrack = (string)selectedTrack.Attribute("id");
                }
                else
                {
                    var TrackList = doc.Descendants("RandomTrackList")
                        .FirstOrDefault(rts =>
                            (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack
                        );
                    if (TrackList != null)
                    {
                        var randomTrack = TrackList
                            .Descendants("track")
                            .ElementAt(random.Next(TrackList.Descendants("track").Count()));
                        RandomTrackGameTrack = (string)randomTrack.Attribute("id");
                    }
                }
                return Adler32Helper.GenerateAdler32_UNICODE(RandomTrackGameTrack, 0);
            }
        }
    }
}
