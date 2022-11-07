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

        static async Task<(List<float>, List<string>)> GetKana(string line)
        {
            //@用语素处理之后确定index和widthPos
            List<int> index = new List<int>();
            List<string> kana = new List<string>();
            List<float> widthPos;

            for (int i = 0; i < line.Length; i++)
            {
                string code = GetCode(line[i].ToString());
                if (code.CompareTo("30ff") > 0 || code.CompareTo("3041") < 0)
                {
                    index.Add(i);
                }
            }

            widthPos = GetWidthPos(index, line);

            using (var tagger = MeCabIpaDicTagger.Create())
            {
                var nodes = tagger.Parse(line);
                var converter = new KawazuConverter();

                foreach (var node in nodes)
                {
                    string code = GetCode(node.Surface[0].ToString());
                    if (code.CompareTo("30ff") > 0 || code.CompareTo("3041") < 0)
                    {
                        string s = await converter.Convert(node.Reading, To.Hiragana, Mode.Spaced, RomajiSystem.Nippon, "(", ")");
                        string[] ss = null;
                        s = s.TrimEnd(' ');

                        if (s.Contains(" "))
                        {
                            ss = s.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        }

                        if (ss != null)
                        {
                            for (int i = 0; i < ss.Length; i++)
                            {
                                s = ss[i];
                                for (int j = 0; j < s.Length; j++)
                                {
                                    if (node.Surface.Contains(s[j].ToString()))
                                    {
                                        s = s.Remove(j);
                                        j--;
                                    }
                                }

                                kana.Add(s);
                            }

                        }
                        else
                        {
                            foreach (var item in node.Surface)
                            {
                                for (int i = 0; i < s.Length; i++)
                                {
                                    if (node.Surface.Contains(s[i].ToString()))
                                    {
                                        s = s.Remove(i);
                                        i--;
                                    }
                                }
                            }
                            kana.Add(s);
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

        static List<float> GetWidthPos(List<int> index, string line)
        {
            List<float> list = new List<float>();
            List<float> widthPos = new List<float>();
            float start = width / 2 - (float)line.Length / 2 * fontSize * width / fontConstant;

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
                widthPos.Add(start + (fontSize / 2 + item * fontSize) * width / fontConstant);
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

                    for (int j = 0; j < kanas[i - 1].Count; j++)
                    {
                        sw.WriteLine("Dialogue: 0,0:" + pretime + ",0:" + time + ",kana,,0,0,0,,{\\pos(" + widthPoss[i - 1][j] + "," + heightPos + ")}" + kanas[i - 1][j]);
                    }

                    Console.WriteLine(prelyric);

                    pretime = time;
                    prelyric = lyric;
                }
            }

        }
    }
}
