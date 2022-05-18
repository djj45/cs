using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace _1
{
    public class Wyyyy
    {
        public static void Main()
        {
            QQ qq = new QQ("https://y.qq.com/n/ryqq/songDetail/001iZpgK2FNgjr");
            //List<string> list = qq.LyricList;
            //foreach (string str in list)
            //{
            //    Console.Write(str);
            //}
            Console.WriteLine(qq.Singer);
            //using (StreamWriter tr = new StreamWriter("12.txt"))
            //{
            //    foreach (var item in list)
            //    {
            //        tr.Write(item);
            //    }
            //};
            //Console.Write(netEase.LyricJson);


        }
    }
    public class NetEase
    {
        readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.60 Safari/537.36";
        readonly int _timeOut = 2000;

        string Id { get; set; }
        string LyricUrl { get; set; }
        string InfoUrl { get; set; }
        string LyricData { get; set; }
        string InfoData { get; set; }

        public JObject LyricJson => JObject.Parse(LyricData);
        JObject InfoJson => JObject.Parse(InfoData);

        string Lrc
        {
            get
            {
                string lrc = LyricJson["lrc"]["lyric"].ToString();
                if (lrc[lrc.Length - 1] != '\n')
                {
                    return lrc + "\n";
                }
                else
                {
                    return lrc;
                }
            }
        }
        string TLrc
        {
            get
            {
                string tlrc = LyricJson["tlyric"]["lyric"].ToString();
                if (tlrc[tlrc.Length - 1] != '\n')
                {
                    return tlrc + "\n";
                }
                else
                {
                    return tlrc;
                }
            }
        }
        string LTLyric => Lrc + TLrc;
        string TLLyric => TLrc + Lrc;

        public List<string> LyricList => Lyric.ToList(Lrc, true);
        public List<string> TLyricList => Lyric.ToList(TLrc, true);
        public List<string> LTLyricList => Lyric.ToList(LTLyric, false);
        public List<string> TLLyricList => Lyric.ToList(TLLyric, true);
        public List<string> LTRangeLyricList => Lyric.ToRangeList(LyricList, TLyricList, Lyric.RangeMode.LT);
        public List<string> TLRangeLyricList => Lyric.ToRangeList(LyricList, TLyricList, Lyric.RangeMode.TL);

        public string SongName => InfoJson["songs"][0]["name"].ToString();
        public string Singer => InfoJson["songs"][0]["artists"][0]["name"].ToString();

        public NetEase(string id)
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
                catch (Exception)
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
            catch (Exception)
            {
                return null;//网络错误
            }
        }
    }

    public static class Lyric
    {
        public enum RangeMode
        {
            //T:translated lyric    L:lyric
            LT,//交错优先原文
            TL,//交错优先译文
        }

        public static bool IsLyric(string lyric)
        {
            return int.TryParse(lyric.Split(':')[0].Split('[')[1], out _) && lyric.Split(']')[1] != "//\n" && lyric.Split(']')[1] != "\n";
        }

        public static List<string> ToList(string lyric, bool remove)
        {
            List<string> list = new List<string>();
            List<string> removeInfoList = new List<string>();
            string s = "";

            foreach (char item in lyric)
            {
                s += item;

                if (item == '\n')
                {
                    list.Add(s);
                    s = "";
                }
            }

            if (remove)
            {
                foreach (string item in list)
                {
                    if (IsLyric(item))
                    {
                        removeInfoList.Add(item);
                    }
                }

                return removeInfoList;
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

        public static string GetTime(string lyric)
        {
            return lyric.Split('[')[1].Split(']')[0];
        }

        public static List<string> ToRangeList(List<string> lyric, List<string> tlyric, RangeMode rangeMode)
        {
            List<string> rangeList = new List<string>();

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
            return lyric.TrimEnd('\n', '\\') + "\\n\n";
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

    public class QQ
    {

        readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.60 Safari/537.36";
        readonly string _referer = "https://y.qq.com";
        readonly string _params = "callback=MusicJsonCallback_lrc&g_tk=5381&format=jsonp&songmid=";
        readonly int _timeOut = 2000;

        string Id { get; set; }
        string LyricUrl { get; set; }
        string InfoUrl { get; set; }
        public string LyricData { get; set; }
        public string InfoData { get; set; }

        public JObject LyricJson => JObject.Parse(LyricData.Replace("MusicJsonCallback_lrc(", "").TrimEnd(')'));
        public JObject InfoJson => JObject.Parse(InfoData.Replace("MusicJsonCallback_lrc(", "").TrimEnd(')'));

        public string Base64Lrc => LyricJson["lyric"].ToString();
        public string Base64TLrc => LyricJson["trans"].ToString();

        string _lrc;
        string _tlrc;
        public string Lrc
        {
            get
            {
                if (_lrc[_lrc.Length-1] != '\n')
                {
                    return _lrc + "\n";
                }
                else
                {
                    return _lrc;
                }
            }
            set
            {
                _lrc = value; 
            }
        }

        public string TLrc
        {
            get
            {
                if (_tlrc[_tlrc.Length - 1] != '\n')
                {
                    return _tlrc + "\n";
                }
                else
                {
                    return _tlrc;
                }
            }
            set
            {
                _tlrc = value;
            }
        }

        string LTLyric => Lrc + TLrc;
        string TLLyric => TLrc + Lrc;

        public List<string> LyricList => Lyric.ToList(Lrc, true);
        public List<string> TLyricList => Lyric.ToList(TLrc, true);
        public List<string> LTLyricList => Lyric.ToList(LTLyric, false);
        public List<string> TLLyricList => Lyric.ToList(TLLyric, true);
        public List<string> LTRangeLyricList => Lyric.ToRangeList(LyricList, TLyricList, Lyric.RangeMode.LT);
        public List<string> TLRangeLyricList => Lyric.ToRangeList(LyricList, TLyricList, Lyric.RangeMode.TL);

        public string SongName => InfoJson["data"][0]["name"].ToString();
        public string Singer => InfoJson["data"][0]["singer"][0]["name"].ToString();
        byte[] ByteLrc => Convert.FromBase64String(Base64Lrc);
        byte[] ByteTLrc => Convert.FromBase64String(Base64TLrc);

        public QQ(string id)
        {
            try
            {
                id = id.Split('/')[6];
            }
            catch (Exception)
            {

            }

            Id = id.Trim(' ');
            LyricUrl = "https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?" + _params + Id;
            InfoUrl = "https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg?" + _params + Id;
            LyricData = Request(LyricUrl);
            InfoData = Request(InfoUrl);
            Lrc = Encoding.UTF8.GetString(ByteLrc);
            TLrc = Encoding.UTF8.GetString(ByteTLrc);
        }

        public string Request(string url)
        {
            try
            {
                HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(url);
                Req.UserAgent = _userAgent;
                Req.Timeout = _timeOut;
                Req.Referer = _referer;
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
}