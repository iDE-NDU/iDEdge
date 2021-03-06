﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace iDEdge
{
    public class Core
    {
        public const string ver = "1.2.1";

        static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    Console.WriteLine("参数错误\n" +
                                      "iDEdge.Local [mp3 路径] [lrc 路径(可选)]");
                    Environment.Exit(2);
                    break;
                case 1:
                    if (File.Exists(args[0]))
                        Environment.Exit(Local(args[0]));
                    else
                    {
                        Console.WriteLine("文件不存在");
                        Environment.Exit(1);
                    }
                    break;
                case 2:
                default:
                    Environment.Exit(Local(args[0], args[1]));
                    break;
            }
        }

        public static int Local(string mp3)
        {
            Process merge = new Process();
            merge.StartInfo.CreateNoWindow = true;
            merge.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\iDEdge.Merge.exe";
            merge.StartInfo.UseShellExecute = false;
            merge.StartInfo.Arguments = "--ui-language zh_CN " +
                $"--output \"{mp3}.mkv\" " +
                $"--language 0:eng ( \"{AppDomain.CurrentDomain.BaseDirectory}\\iDEdge.Resources\" ) " +
                $"--language 0:und ( \"{mp3}\" ) --track-order 0:0,1:0";
            merge.StartInfo.RedirectStandardOutput = true;
            merge.Start();
            merge.WaitForExit();
            string output = merge.StandardOutput.ReadToEnd();
            merge.Close();
            if (File.Exists($"{mp3}.mkv"))
                return 0;
            else
                return 4;
        }
        public static int Local(string mp3, string lrc)
        {
            string dir = Environment.GetEnvironmentVariable("temp") + "\\" + DateTime.Now.ToBinary().ToString() + "\\";
            Directory.CreateDirectory(dir);
            File.Copy(mp3, dir + "mp3");
            lrc = Lrc2Ass(File.ReadAllText(lrc, EncodingType.GetType(lrc)), $"iDEdge {Core.ver} 生成的室内操");
            File.WriteAllText(dir + "lrc", lrc, Encoding.UTF8);
            string output = Merge(dir, mp3 + ".mkv");
            if (File.Exists($"{mp3}.mkv"))
                Console.WriteLine("成功");
            else
            {
                Console.WriteLine("失败");
                File.WriteAllText(dir + "log", output);
                Console.WriteLine($"日志已保存到 {dir}");
                return 4;
            }
            return 0;
        }

        public static string Merge(string dir, string name)
        {
            Process merge = new Process();
            merge.StartInfo.CreateNoWindow = true;
            merge.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\iDEdge.Merge.exe";
            merge.StartInfo.UseShellExecute = false;
            merge.StartInfo.Arguments = "--ui-language zh_CN " +
                $"--output \"{name}\" " +
                $"--language 0:eng ( \"{AppDomain.CurrentDomain.BaseDirectory}\\iDEdge.Resources\" ) " +
                $"--language 0:und ( \"{dir}mp3\" ) " +
                $"--language 0:und ( \"{dir}lrc\" ) --track-order 0:0,1:0,2:0";
            merge.StartInfo.RedirectStandardOutput = true;
            merge.Start();
            merge.WaitForExit();
            string output = merge.StandardOutput.ReadToEnd();
            merge.Close();
            return output;
        }

        public static string Lrc2Ass(string lrc, string title)
        {
            Regex timeReg = new Regex(@"(?<=^\[)(\d|\:|\.)+(?=])");
            Regex strReg = new Regex(@"(?<=]).+", RegexOptions.RightToLeft);
            string[] lrcLines = lrc.Split('\n');
            lrc = "[Script Info]\nTitle: Convented Sub\nScriptType: v4.00+\nWrapStyle: 0\nScaledBorderAndShadow: yes\nYCbCr Matrix: None\n\n[V4 + Styles]\nFormat: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\nStyle: Default,微软雅黑,30,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,2,8,10,10,10,1\n\n[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n";
            TimeSpan last = TimeSpan.Zero;
            string lastlrc = title;
            foreach (string line in lrcLines)
            {
                if (line.Trim() == "")
                    continue;
                Match timeMatch = timeReg.Match(line);
                if (!timeMatch.Success)
                    continue;
                Match strMatch = strReg.Match(line);
                TimeSpan time = TimeSpan.Parse("00:" + timeMatch.Value);
                if (lastlrc.Trim() != "")
                    lrc += "Dialogue: 0," + string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d2}", last.Hours, last.Minutes, last.Seconds, last.Milliseconds / 10) +
                        "," + string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d2}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds / 10) +
                        ",Default,,0,0,0,," + lastlrc.Trim() + "\n";
                last = time;
                lastlrc = strMatch.Success ? strMatch.Value : "";
            }
            TimeSpan timelast = last.Add(TimeSpan.FromSeconds(10));
            if (lastlrc.Trim() != "")
                lrc += "Dialogue: 0," + string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d2}", last.Hours, last.Minutes, last.Seconds, last.Milliseconds / 10) +
                    "," + string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d2}", timelast.Hours, timelast.Minutes, timelast.Seconds, timelast.Milliseconds / 10) +
                    ",Default,,0,0,0,," + lastlrc.Trim() + "\n";
            return lrc;
        }


        public static string GetWebText(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream datastream = response.GetResponseStream();
            StreamReader reader = new StreamReader(datastream, Encoding.UTF8);
            string result = reader.ReadToEnd();
            reader.Close();
            datastream.Close();
            response.Close();
            return result;
        }
    }
}
