using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountTheNumberOfLineCodes
{
    /// <summary>
    /// Refer version 3 for the latest implementation.
    /// </summary>
    public static class CountChagedLinesV1
    {
        public static void UsingFunction()
        {
            Console.Write("Enter author name or email: ");
            string author = Console.ReadLine();
            Console.Write("Enter start date (yyyy-MM-dd): ");
            DateTime startDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter end date (yyyy-MM-dd): ");
            DateTime endDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter repository path: ");
            string repoPath = Console.ReadLine();
            var (added, removed, total) = CountChangedLines(author, startDate, endDate, repoPath);
            Console.WriteLine($"\nAdded lines: {added}");
            Console.WriteLine($"Removed lines: {removed}");
            Console.WriteLine($"Total changed lines: {total}");
        }

        public static (int added, int removed, int total) CountChangedLines(string author, DateTime startDate, DateTime endDate, string repoPath = "default")
        {
            try
            {
                // Get commits by author in date range
                string dateFormat = "yyyy-MM-dd";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C \"{repoPath}\" log --author=\"{author}\" --since=\"{startDate.ToString(dateFormat)}\" --until=\"{endDate.ToString(dateFormat)}\" --pretty=%H",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string commitOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] commits = commitOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                int totalAdded = 0, totalRemoved = 0;

                foreach (string commit in commits)
                {
                    var diffProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"-C \"{repoPath}\" diff {commit}^ {commit}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    diffProcess.Start();
                    string diffOutput = diffProcess.StandardOutput.ReadToEnd();
                    diffProcess.WaitForExit();

                    var lines = diffOutput.Split('\n');
                    int added = lines.Count(line => line.StartsWith("+") && !line.StartsWith("+++"));
                    int removed = lines.Count(line => line.StartsWith("-") && !line.StartsWith("---"));

                    totalAdded += added;
                    totalRemoved += removed;
                }

                return (totalAdded, totalRemoved, totalAdded + totalRemoved);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, 0, 0);
            }
        }
    }
}
