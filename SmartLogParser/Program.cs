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

        private static string ProcessLogFile(string logFileName, ushort tailSizeInKb, string keyWord, int relevantLines)
        {
            const int bytesInKb = 1024;
            long offset = tailSizeInKb * bytesInKb;
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
                int relevantLineNumber = 0;
                bool keywordMatch = false;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    if (!keywordMatch)
                    {
                        keywordMatch = line.ToUpperInvariant().Contains(keyWord.ToUpperInvariant());
                        if (keywordMatch)
                        {
                            relevantLineNumber = 0;
                        }
                    }
                    if (keywordMatch)
                    {
                        if (relevantLineNumber++ < relevantLines)
                        {
                            writer.WriteLine(line);
                        }
                        else
                        {
                            writer.WriteLine();
                            keywordMatch = false;
                        }
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
            bool retry;
            do
            {
                retry = false;
                Console.WriteLine("Please enter the number of relevant lines to read after keyword match:");
                while (!int.TryParse(ReadLine(), out relevantLines) || relevantLines == 0)
                {
                    Console.WriteLine("That was a bad input, please have another try:");
                }
                if (relevantLines < 10)
                {
                    var retryTask = Task.Factory.StartNew<bool>(() =>
                        {
                            Console.WriteLine("Come on, do you really think {0} lines of data is of any relevance?", relevantLines);
                            Console.Write("[Y/N]:");
                            char c = Console.ReadKey().KeyChar;
                            return c == 'y' || c == 'Y';
                        });
                    var task = Task.WhenAny(retryTask, Task.Delay(3000)).Result;
                    if (task == retryTask)
                    {
                        retry = retryTask.Result;
                        if (retry)
                        {
                            Console.WriteLine();
                        }
                    }
                }
            } while (retry);
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
