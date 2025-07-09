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
            Console.WriteLine("----------GitLab AuthorDiff---------\n");
            while (true)
            {
                Console.Write("Enter author name or email: ");
                string author = Console.ReadLine();
                Console.Write("Enter start date (yyyy-MM-dd): ");
                DateTime startDate = DateTime.Parse(Console.ReadLine());
                Console.Write("Enter end date (yyyy-MM-dd): ");
                DateTime endDate = DateTime.Parse(Console.ReadLine());
                Console.Write("Enter repository path: ");
                string repoPath = Console.ReadLine();
                Console.Write("Enter branch name: ");
                string branch = Console.ReadLine();
                Console.WriteLine("Process...");
                var (added, removed, total, commitCount, commits) = CountChangedLines(author, startDate, endDate, branch, repoPath);
                Console.WriteLine($"\nAdded lines: {added}");
                Console.WriteLine($"Removed lines: {removed}");
                Console.WriteLine($"Total changed lines: {total}");
                Console.WriteLine($"Total commits: {commitCount}");
                Console.Write("\n\nDo you want to show the commits? ('Y' or Enter (to display commits)/ 'N' or anyKey (to continue)): ");
                string isShowCommit = Console.ReadLine();
                if ((isShowCommit?.ToLower() is "" or "y" or "yes"))
                {
                    Console.WriteLine("\nCommit List:");
                    foreach (var commit in commits)
                    {
                        Console.WriteLine($"\n+{commit.added}\t | - {commit.removed}\t | Message:{commit.message} | Date: ({commit.date})");
                    }
                }
                Console.Write("\nContinue? ('Y' or Enter (to continue) / 'N' or anyKey (to stop)): ");
                string isContinue = Console.ReadLine();
                if (!(isContinue?.ToLower() is "" or "y" or "yes"))
                {
                    Console.WriteLine("\n-----------Finished-----------");
                    break;
                }
                Console.WriteLine("\n----------------------------\n");
            }
        }

        public static (int added, int removed, int total, int commitCount, List<(string hash, string message, string date, int added, int removed)> commits) CountChangedLines(
                string author,
                DateTime startDate,
                DateTime endDate,
                string branch,
                string repoPath)
        {
            try
            {
                string dateFormat = "yyyy-MM-dd";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C \"{repoPath}\" log {branch} --author=\"{author}\" --since=\"{startDate:yyyy-MM-dd}\" --until=\"{endDate:yyyy-MM-dd}\" --pretty=%H|%s|%ad",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string commitOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var commitLines = commitOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var commits = new List<(string hash, string message, string date, int added, int removed)>();

                int totalAdded = 0, totalRemoved = 0;

                foreach (var line in commitLines)
                {
                    var parts = line.Split('|');
                    if (parts.Length < 3) continue;
                    string hash = parts[0];
                    string message = parts[1];
                    string date = parts[2];

                    int added = 0, removed = 0;

                    // Handle first commit (no parent)
                    var diffProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"-C \"{repoPath}\" diff {(IsFirstCommit(hash, repoPath) ? hash : hash + "^")} {hash}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    diffProcess.Start();
                    string diffOutput = diffProcess.StandardOutput.ReadToEnd();
                    diffProcess.WaitForExit();

                    var lines = diffOutput.Split('\n');
                    added = lines.Count(l => l.StartsWith("+") && !l.StartsWith("+++"));
                    removed = lines.Count(l => l.StartsWith("-") && !l.StartsWith("---"));

                    totalAdded += added;
                    totalRemoved += removed;

                    commits.Add((hash, message, date, added, removed));
                }

                return (totalAdded, totalRemoved, totalAdded + totalRemoved, commits.Count, commits);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, 0, 0, 0, new List<(string, string, string, int, int)>());
            }
        }

        // Helper to check if a commit is the first in the repo
        private static bool IsFirstCommit(string hash, string repoPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"-C \"{repoPath}\" rev-list --parents -n 1 {hash}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            // If only one hash is present, it's the first commit
            return output.Trim().Split(' ').Length == 1;
        }
    }
}
