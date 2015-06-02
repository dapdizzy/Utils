using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            GreetDearUser();
            string logFileName = PromptForLogFileName();
            ushort tailSizeinKb = PromptForTailSize();
            string keyWord = PromptForKeyword();
            int relevantLines = PromptForNumberOfRelevantLines();
            string outputFileName = ProcessLogFile(logFileName, tailSizeinKb, keyWord, relevantLines);

            Console.WriteLine("See generated output {0}", outputFileName);
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                Process.Start(outputFileName);
            });
            Console.Write("Press Enter to exit");
            var cts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }
                    Console.Write(".");
                    await Task.Delay(500);
                }
            }, cts.Token);
            Console.ReadLine();
            cts.Cancel();
            Thread.Sleep(3000);
        }

        private static string ProcessLogFile(string logFileName, ushort tailSizeinKb, string keyWord, int relevantLines)
        {
            const int bytesInKb = 1024;
            long offset = tailSizeinKb * bytesInKb;
            int linesRead = 0;
            string outputFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Output.txt");
            using (var writer = new StreamWriter(File.Create(outputFileName)))
            using (var reader = new StreamReader(logFileName))
            {
                if (reader.BaseStream.Length > offset)
                {
                    reader.BaseStream.Seek(-offset, SeekOrigin.End);
                }
                string line;
                int lineNum = 0;
                bool readLine = false;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    if (!readLine)
                    {
                        readLine = line.ToUpperInvariant().Contains(keyWord.ToUpperInvariant());
                        if (readLine)
                        {
                            lineNum = 0;
                        }
                    }
                    if (readLine && lineNum++ < relevantLines)
                    {
                        writer.WriteLine(line);
                    }
                    if (readLine && lineNum >= relevantLines)
                    {
                        writer.WriteLine();
                        readLine = false;
                    }
                    ++linesRead;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Total lines read: {0}", linesRead);
            Console.WriteLine();
            return outputFileName;
        }

        private static int PromptForNumberOfRelevantLines()
        {
            int relevantLines;
            Console.WriteLine("Please enter the number of relevant lines to read after keyword match:");
            while (!int.TryParse(ReadLine(), out relevantLines))
            {
                Console.WriteLine("That was a bad input, please have another try:");
            }
            return relevantLines;
        }

        private static string PromptForKeyword()
        {
            Console.WriteLine("Please enter the keyword to search for:");
            string keyWord = ReadLine();
            return keyWord;
        }

        private static ushort PromptForTailSize()
        {
            string input;
            bool inputFailed = false;
            ushort tailSizeinKb;
            do
            {
                if (inputFailed)
                {
                    Console.WriteLine("You should a avalid positive integer number.");
                }
                Console.WriteLine("Please enter the size of log tail to read in Kb:");
                input = Console.ReadLine();
            } while (inputFailed = !ushort.TryParse(input, out tailSizeinKb));
            return tailSizeinKb;
        }

        private static string PromptForLogFileName()
        {
            string logFileName;
            bool invalidLogFileName = false;
            do
            {
                if (invalidLogFileName)
                {
                    Console.WriteLine("Sorry, the path entered in incorrect.");
                }
                Console.WriteLine("Please enter the path to the log file:");
                logFileName = ReadLine();
            } while (invalidLogFileName = (string.IsNullOrEmpty(logFileName) || !File.Exists(logFileName)));
            return logFileName;
        }

        private static void GreetDearUser()
        {

            string userName = System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName;
            string regex = "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))";
            userName = Regex.Replace(userName, regex, "").Trim();
            Console.WriteLine("Hello dear {0}", userName);
            //Console.WriteLine("Hello dear Smart Log User!");
        }

        private static string ReadLine()
        {
            string line = Console.ReadLine();
            if (line.ToUpperInvariant().Contains("Fuck you".ToUpperInvariant()))
            {
                Console.WriteLine("Okay, I'm gonna gracefully exit then...");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }
            return line;
        }
    }
}
