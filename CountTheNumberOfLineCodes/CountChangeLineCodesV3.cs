using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountTheNumberOfLineCodes
{
    /// <summary>
    /// This version to check the number of changed lines in a git repository by author, date range, and branch.
    /// </summary>
    public static class CountChangeLineCodesV3
    {
        public static void UsingFunction()
        {
            Console.Write("Enter author name or email: ");
            string author = Console.ReadLine();
            Console.Write("Enter start date (yyyy-MM-dd): ");
            DateTime startDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter end date (yyyy-MM-dd): ");
            DateTime endDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter branch name: ");
            string branch = Console.ReadLine();
            Console.Write("Enter repository path: ");
            string repoPath = Console.ReadLine();
            var (added, removed, total, commitCount, commits) = CountChangedLines(author, startDate, endDate, branch, repoPath);
            Console.WriteLine($"\nAdded lines: {added}");
            Console.WriteLine($"Removed lines: {removed}");
            Console.WriteLine($"Total changed lines: {total}");
            Console.WriteLine($"Total commits: {commitCount}");
            Console.WriteLine("\nCommit List:");
            foreach (var commit in commits)
            {
                Console.WriteLine($"{commit.message} ({commit.date})");
            }
        }

        public static (int added, int removed, int total, int commitCount, List<(string hash, string message, string date)> commits) CountChangedLines(string author, DateTime startDate, DateTime endDate, string branch, string repoPath = "default")
        {
            try
            {
                // Get commits by author in date range on specific branch
                string dateFormat = "yyyy-MM-dd";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C \"{repoPath}\" log {branch} --author=\"{author}\" --since=\"{startDate.ToString(dateFormat)}\" --until=\"{endDate.ToString(dateFormat)}\" --pretty=%H|%s|%ad",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string commitOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var commits = commitOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split('|'))
                    .Select(parts => (hash: parts[0], message: parts[1], date: parts[2]))
                    .ToList();

                int totalAdded = 0, totalRemoved = 0;

                foreach (var commit in commits)
                {
                    var diffProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"-C \"{repoPath}\" diff {commit.hash}^ {commit.hash}",
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

                return (totalAdded, totalRemoved, totalAdded + totalRemoved, commits.Count, commits);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, 0, 0, 0, new List<(string, string, string)>());
            }
        }
    }
}
