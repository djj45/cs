using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Formatting = Newtonsoft.Json.Formatting;
using System.Collections;
using System.Net;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main()
        {
            using (StreamWriter srt = File.CreateText("1.txt"))
            {
                srt.WriteLine("111");
                srt.WriteLine("111");
            };

        }
    }
}


