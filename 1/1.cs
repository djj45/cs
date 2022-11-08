using System;
using System.Collections.Generic;
using System.Text;
using Kawazu;
using System.Threading.Tasks;
using NMeCab.Specialized;
using System.IO;

namespace LyricTool
{
    class Program
    {
        static int fontConstant = 2800;//字号乘以字
        static int width = 1920;
        static int height = 1080;
        static int fontSize = 60;
        static int top = 30;
        static float heightPos = height - (fontSize + top) * width / fontConstant;
        static List<List<float>> widthPoss = new List<List<float>>();
        static List<List<string>> kanas = new List<List<string>>();
        static List<string> lines = new List<string>();
        static List<float> widthPos = new List<float>();

        static async Task<(List<float>, List<string>)> GetKana(string line)
        {
            //歌词有空格或者括号位置有误差
            List<int> index = new List<int>();
            List<string> kana = new List<string>();
            widthPos.Clear();
            int count = 0;
            float start = width / 2 - (float)line.Length / 2 * fontSize * width / fontConstant;

            using (var tagger = MeCabIpaDicTagger.Create())
            {
                var nodes = tagger.Parse(line);
                var converter = new KawazuConverter();

                foreach (var node in nodes)
                {
                    bool flag = true;
                    List<string> ss = new List<string>();

                    if (node.Surface.Length >= 3)
                    {
                        //断ち切る
                        string code1 = GetCode(node.Surface[0].ToString());
                        string code2 = GetCode(node.Surface[1].ToString());
                        string code3 = GetCode(node.Surface[2].ToString());

                        bool isKanji1 = code1.CompareTo("30ff") > 0 || code1.CompareTo("3041") < 0;
                        bool isKanji2 = code2.CompareTo("30ff") > 0 || code2.CompareTo("3041") < 0;
                        bool isKanji3 = code3.CompareTo("30ff") > 0 || code3.CompareTo("3041") < 0;

                        if (isKanji1 && !isKanji2 && isKanji3)
                        {
                            List<float> temp = new List<float>();
                            string s = await converter.Convert(node.Reading, To.Hiragana, Mode.Spaced, RomajiSystem.Nippon, "(", ")");
                            ss.Add(s.Substring(0, 2));
                            ss.Add(s.Substring(2, s.Length - 2));

                            index.Add(0);
                            temp = GetWidthPos(index, start, count);
                            widthPos.Add(temp[0]);
                            count += 2;
                            index.Clear();

                            index.Add(0);
                            temp = GetWidthPos(index, start, count);
                            widthPos.Add(temp[0]);
                            count += node.Surface.Length - 2;
                            index.Clear();

                            flag = false;
                        }
                    }
                    if (flag)
                    {
                        for (int i = 0; i < node.Surface.Length; i++)
                        {
                            string _code = GetCode(node.Surface[i].ToString());
                            if (_code.CompareTo("30ff") > 0 || _code.CompareTo("3041") < 0)
                            {
                                index.Add(i);
                            }
                        }

                        if (index.Count != 0)
                        {
                            foreach (var item in GetWidthPos(index, start, count))
                            {
                                widthPos.Add(item);
                            }
                        }

                        count += node.Surface.Length;
                        index.Clear();
                    }

                    bool hasKanji = false;
                    foreach (var item in node.Surface)
                    {
                        string code = GetCode(item.ToString());
                        if (code.CompareTo("30ff") > 0 || code.CompareTo("3041") < 0)
                        {
                            hasKanji = true;
                            break;
                        }
                    }

                    if (hasKanji)
                    {
                        string s = await converter.Convert(node.Reading, To.Hiragana, Mode.Spaced, RomajiSystem.Nippon, "(", ")");
                        s = s.TrimEnd(' ');

                        if (s.Contains(" ") && kana.Count + 1 != widthPos.Count && ss.Count == 0)
                        {
                            foreach (var item in s.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                            {
                                ss.Add(item);
                            }
                        }
                        else if(s.Contains(" ") && kana.Count + 1 == widthPos.Count)
                        {
                            s = s.Replace(" ", "");
                        }

                        if (ss.Count != 0)
                        {
                            for (int i = 0; i < ss.Count; i++)
                            {
                                s = ss[i];
                                for (int j = 0; j < s.Length; j++)
                                {
                                    if (node.Surface.Contains(s[j].ToString()))
                                    {
                                        //汉字如果和假名同音不行
                                        s = s.Remove(j);
                                        j--;
                                    }
                                }
                                if (s != "")
                                {
                                    kana.Add(s);
                                }
                            }

                        }
                        else
                        {
                            for (int i = 0; i < s.Length; i++)
                            {
                                if (node.Surface.Contains(s[i].ToString()))
                                {
                                    //汉字如果和假名同音不行
                                    s = s.Replace(s[i].ToString(), "");
                                    i--;
                                }
                            }
                            
                            if (s != "")
                            {
                                kana.Add(s);
                            }
                        }
                    }
                }
            }

            return (widthPos, kana);
        }

        static string GetCode(string s)
        {
            byte[] b = Encoding.Unicode.GetBytes(s);
            return Convert.ToString(b[1], 16) + Convert.ToString(b[0], 16);
        }

        static List<float> GetWidthPos(List<int> index, float start, int count)
        {
            List<float> list = new List<float>();
            List<float> widthPos = new List<float>();

            if (index.Count == 1)
            {
                list.Add(index[0]);
            }
            else
            {
                float sum = index[0];
                int j = 1;
                int temp = index[0];

                for (int i = 1; i < index.Count; i++)
                {
                    if (index[i] == temp + 1)
                    {
                        j++;
                        sum += index[i];
                    }
                    else
                    {
                        list.Add(sum / j);
                        sum = 0;
                        j = 1;
                        sum += index[i];

                    }
                    temp = index[i];
                }

                list.Add(sum / j);
            }

            foreach (var item in list)
            {
                widthPos.Add(start + (fontSize / 2 + item * fontSize + count * fontSize) * width / fontConstant);
            }

            return widthPos;
        }

        static string GetLyric(string line)
        {
            return line.Split(']')[1];
        }

        static string GetTime(string line)
        {
            return line.Split(']')[0].Split('[')[1];
        }

        static void GetResult(ref List<List<float>> widthPoss, ref List<List<string>> kanas, ref List<string> lines)
        {
            using (StreamReader sr = new StreamReader("祝福 - YOASOBI.lrc"))
            {
                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string lyric = GetLyric(line);
                    var result = GetKana(lyric).GetAwaiter().GetResult();
                    List<float> widthPos = new List<float>();
                    List<string> kana = new List<string>();

                    foreach (var item in result.Item1)
                    {
                        widthPos.Add(item);
                    }

                    foreach (var item in result.Item2)
                    {
                        kana.Add(item);
                    }

                    widthPoss.Add(widthPos);
                    kanas.Add(kana);
                    lines.Add(line);
                }
            }
        }

        static void Main()
        {
            string template;
            using (StreamReader sr = new StreamReader("template.ass"))
            {
                template = sr.ReadToEnd();
            }

            GetResult(ref widthPoss, ref kanas, ref lines);

            using (StreamWriter sw = new StreamWriter("1.ass"))
            {
                string time = "";
                string pretime = "";
                string lyric = "";
                string prelyric = "";

                sw.Write(template);
                sw.Write("\n");

                for (int i = 0; i < lines.Count; i++)
                {
                    if (i == 0)
                    {
                        pretime = GetTime(lines[i]);
                        prelyric = GetLyric(lines[i]);
                        continue;
                    }

                    lyric = GetLyric(lines[i]);
                    time = GetTime(lines[i]);

                    sw.WriteLine("Dialogue: 0,0:" + pretime + ",0:" + time + ",lyric,,0,0,0,," + prelyric);

                    Console.WriteLine(prelyric);
                    for (int j = 0; j < kanas[i - 1].Count; j++)
                    {
                        sw.WriteLine("Dialogue: 0,0:" + pretime + ",0:" + time + ",kana,,0,0,0,,{\\pos(" + widthPoss[i - 1][j] + "," + heightPos + ")}" + kanas[i - 1][j]);
                    }


                    pretime = time;
                    prelyric = lyric;

                    //最后一句，8秒提醒
                }
            }

        }
    }
}
