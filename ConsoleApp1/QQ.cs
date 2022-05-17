﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Formatting = Newtonsoft.Json.Formatting;
using System.Collections;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ConsoleApp1
{
    public class Test
    {
        public static void Main()
        {
            /*
              self.params = {
            "callback": "MusicJsonCallback_lrc",
            "pcachetime": str(round(time.time(), 3)).replace(".", ""),  # pcachetime随便写一个固定值也可以
            "songmid": self.song_id,
            "g_tk": "5381",
            "jsonpCallback": "MusicJsonCallback_lrc",
            "loginUin": "0",
            "hostUin": "0",
            "format": "jsonp",
            "inCharset": "utf8",
            "outCharset": "utf8",
            "notice": "0",
            "platform": "yqq",
            "needNewCode": "0",
            }
            "referer": "https://y.qq.com/portal/player.html",
            self.lyric_url = "https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg"
            self.info_url = "https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg"
             */
            QQ netEase = new QQ("https://music.163.com/#/song?id=1837557715");
            Srt.WriteSrt(Lyric.ToRangeList(netEase, Lyric.RangeMode.NO), "", netEase.SongName, netEase.Singer);
        }
    }
    public class QQ
    {

        readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.60 Safari/537.36";
        readonly int _timeOut = 2000;

        string Id { get; set; }
        string LyricUrl { get; set; }
        string InfoUrl { get; set; }
        string LyricData { get; set; }
        string InfoData { get; set; }

        JObject LyricJson => JObject.Parse(LyricData);
        JObject InfoJson => JObject.Parse(InfoData);

        public string Lrc => LyricJson["lrc"]["lyric"].ToString();
        public string TLrc => LyricJson["tlyric"]["lyric"].ToString();
        public string FullLyric => TLrc + Lrc;

        public string SongName => InfoJson["songs"][0]["name"].ToString();
        public string Singer => InfoJson["songs"][0]["artists"][0]["name"].ToString();

        public QQ(string id)
        {
            if (int.TryParse(id, out _))
            {
                Id = id.Trim(' ');
            }
            else
            {
                try
                {
                    Id = id.Trim(' ').Split('=')[1];
                }
                catch
                {
                    //输入链接错误
                }
            }

            LyricUrl = "http://music.163.com/api/song/lyric?os=pc&id=" + Id + "&lv=-1&tv=-1";
            InfoUrl = "http://music.163.com/api/song/detail/?id=" + Id + "&ids=[" + Id + "]";
            LyricData = Request(LyricUrl);
            InfoData = Request(InfoUrl);
        }

        string Request(string url)
        {
            try
            {
                HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(url);
                Req.UserAgent = _userAgent;
                Req.Timeout = _timeOut;
                Req.Proxy = null;

                HttpWebResponse Resp = (HttpWebResponse)Req.GetResponse();

                using (StreamReader sr = new StreamReader(Resp.GetResponseStream()))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
                return null;//网络错误
            }
        }
    }

    public class Lyric
    {
        public enum RangeMode
        {
            //T:translated lyric    L:lyric
            NO,//不排
            LT,//交错优先原文
            TL,//交错优先译文
        }

        public static List<string> ToList(string lrc)
        {
            List<string> list = new List<string>();
            string s = "";
            int count = 0;

            foreach (char item in lrc)
            {
                s += item;
                count++;
                if (item.Equals('\n'))
                {
                    list.Add(s);
                    s = "";
                }
                else if (lrc.Length == count)
                {
                    s += "\n";
                    list.Add(s);
                }
            }

            return list;
        }

        public static void WriteLrc(string path, string songName, string singer, List<string> lyricList)
        {
            songName = string.Join("-", songName.Split(Path.GetInvalidFileNameChars()));
            singer = string.Join("-", singer.Split(Path.GetInvalidFileNameChars()));
            using (StreamWriter lrc = File.CreateText(path + songName + "-" + singer + ".lrc"))
            {
                foreach (string lyric in lyricList)
                {
                    lrc.Write(lyric);
                }
            }
        }

        public static string GetTime(string lrc)
        {
            return lrc.Split('[')[1].Split(']')[0];
        }

        public static List<string> ToRangeList(QQ netEase, RangeMode rangeMode)
        {
            List<string> lyric = ToList(netEase.Lrc);
            List<string> tlyric = ToList(netEase.TLrc);
            List<string> rangeList = new List<string>();

            if (rangeMode == RangeMode.NO)
            {
                rangeList.AddRange(tlyric);
                rangeList.AddRange(lyric);
            }
            else
            {
                foreach (string lrc in lyric)
                {
                    int index = tlyric.FindIndex(tlrc => GetTime(tlrc) == GetTime(lrc));
                    if (index != -1)
                    {
                        if (rangeMode == RangeMode.LT)
                        {
                            rangeList.Add(lrc);
                            rangeList.Add(tlyric[index]);
                        }
                        else
                        {
                            rangeList.Add(tlyric[index]);
                            rangeList.Add(lrc);
                        }
                    }
                    else
                    {
                        rangeList.Add(lrc);
                    }
                }
            }

            return rangeList;
        }
    }

    public static class Srt
    {
        public static string GetTime(string lrc)
        {
            return lrc.Split('[')[1].Split(']')[0];
        }

        public static string GetLyric(string lyric)
        {
            return lyric.Split(']')[1];
        }

        public static double TransTime(string lyric)
        {
            string[] timeSplit = lyric.Split(':', '.');

            return double.Parse(timeSplit[0]) * 60 + double.Parse(timeSplit[1]) + double.Parse(timeSplit[2]) * Math.Pow(10, -timeSplit[2].Length);
        }

        public static string ExtendTime(string time)
        {
            string milliSecond = time.Split('.')[1];
            double extendTime = TransTime(time) + 8;
            string minute = ((int)extendTime / 60).ToString();
            string second = ((int)extendTime % 60).ToString();

            while (minute.Length <= 1)
                minute = "0" + minute;
            while (second.Length <= 1)
                second = "0" + second;

            return minute + ":" + second + "," + milliSecond;
        }

        public static bool IsLyric(string lyric)
        {
            return int.TryParse(lyric.Split(':')[0].Split('[')[1], out _);
        }

        public static string HandleLyric(string lyric)
        {
            if (lyric == "\n" || lyric == "\\\\")
            {
                lyric = "\\n\n";
            }

            return lyric;
        }

        public static void CheckTime(ref string preTime, ref string time, ref bool flag)
        {
            if (TransTime(preTime) - TransTime(time) > 60)
            {
                flag = true;
            }
        }

        public static void WriteSrt(List<string> list, string path, string songName, string singer)
        {
            int count = 0;
            string preTime = "";
            string time = "";
            string prelyric = "";
            bool flag = false;

            using (StreamWriter srt = File.CreateText(path + songName + "-" + singer + ".srt"))
            {
                foreach (string line in list)
                {
                    if (!IsLyric(line))
                        continue;
                    if (count == 0)
                    {
                        preTime = GetTime(line);
                        prelyric = HandleLyric(GetLyric(line));
                        count++;
                        continue;
                    }

                    time = GetTime(line);
                    CheckTime(ref preTime, ref time, ref flag);

                    if (flag)
                    {
                        time = ExtendTime(preTime);
                    }

                    srt.WriteLine(count);
                    srt.WriteLine("00:" + preTime.Replace('.', ',') + " --> " + "00:" + time.Replace('.', ','));
                    srt.WriteLine(prelyric);

                    if (flag)
                    {
                        flag = false;
                        time = GetTime(line);
                    }

                    preTime = time;
                    prelyric = HandleLyric(GetLyric(line));
                    count++;
                }

                srt.WriteLine(count);
                srt.WriteLine("00:" + preTime.Replace('.', ',') + " --> " + "00:" + ExtendTime(preTime).Replace('.', ','));
                srt.WriteLine(prelyric);
            };
        }
    }
}


