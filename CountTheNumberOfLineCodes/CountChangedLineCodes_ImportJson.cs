using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CountTheNumberOfLineCodes
{
    /// <summary>
    /// Using file Request.Json to import parameters
    /// The Result has been written to the export file Result_xxx.json
    /// </summary>
    public static class CountChangedLineCodes_ImportJson
    {
        public static void UsingFunction()
        {
            try
            {
                // Read JSON config
                string jsonContent = File.ReadAllText("..\\..\\..\\Request.json");
                Console.Write("Do you want to record the commits message ? (Y/N):");
                bool isRecordCommitsMessage = Console.ReadLine()?.ToLower() == "Y";
                var config = JsonSerializer.Deserialize<RequestConfig>(jsonContent);

                var resultOutput = new ResultOutput { Results = new List<AuthorResult>() };

                foreach (var request in config.Request)
                {
                    var authorResult = new AuthorResult
                    {
                        Author = request.Author,
                        Repositories = new List<RepositoryResult>()
                    };

                    foreach (var repo in request.Repository)
                    {
                        DateTime startDate = DateTime.Parse(repo.StartTime);
                        DateTime endDate = DateTime.Parse(repo.EndTime);
                        var (added, removed, total, commitCount, commits) = CountChangedLines(
                            request.Author, startDate, endDate, repo.Branch, repo.Path);

                        authorResult.Repositories.Add(new RepositoryResult
                        {
                            Path = repo.Path,
                            AddedLines = added,
                            RemovedLines = removed,
                            TotalChangedLines = total,
                            CommitCount = commitCount,
                            Commits = isRecordCommitsMessage ? commits : [],
                            StartTime = repo.StartTime,
                            EndTime = repo.EndTime,
                        });
                    }

                    resultOutput.Results.Add(authorResult);
                }

                // Write to Result.json
                var options = new JsonSerializerOptions { WriteIndented = true };
                string fileName = $"Result_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
                File.WriteAllText("..\\..\\..\\" + fileName, JsonSerializer.Serialize(resultOutput, options));
                Console.WriteLine("Results written to " + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static (int added, int removed, int total, int commitCount, List<CommitDetail> commits) CountChangedLines(
    string author, DateTime startDate, DateTime endDate, string branch, string repoPath)
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
                var commits = new List<CommitDetail>();
                int totalAdded = 0, totalRemoved = 0;

                foreach (var line in commitLines)
                {
                    var parts = line.Split('|');
                    if (parts.Length < 3) continue;
                    string hash = parts[0];
                    string message = parts[1];
                    string date = parts[2];

                    int added = 0, removed = 0;

                    // Check if this is the first commit (no parent)
                    bool isFirstCommit = false;
                    using (var parentCheck = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"-C \"{repoPath}\" rev-list --parents -n 1 {hash}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    })
                    {
                        parentCheck.Start();
                        string parentOutput = parentCheck.StandardOutput.ReadToEnd();
                        parentCheck.WaitForExit();
                        isFirstCommit = parentOutput.Trim().Split(' ').Length == 1;
                    }

                    string diffArgs = isFirstCommit
                        ? $"-C \"{repoPath}\" show {hash}"
                        : $"-C \"{repoPath}\" diff {hash}^ {hash}";

                    using (var diffProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = diffArgs,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    })
                    {
                        diffProcess.Start();
                        string diffOutput = diffProcess.StandardOutput.ReadToEnd();
                        diffProcess.WaitForExit();

                        var lines = diffOutput.Split('\n');
                        added = lines.Count(l => l.StartsWith("+") && !l.StartsWith("+++"));
                        removed = lines.Count(l => l.StartsWith("-") && !l.StartsWith("---"));
                    }

                    totalAdded += added;
                    totalRemoved += removed;

                    commits.Add(new CommitDetail
                    {
                        Hash = hash,
                        Message = message,
                        Date = date,
                        // Optionally add these properties to CommitDetail if you want per-commit stats:
                        Added = added + "",
                        Removed = removed + ""
                    });
                }

                return (totalAdded, totalRemoved, totalAdded + totalRemoved, commits.Count, commits);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {repoPath}: {ex.Message}");
                return (0, 0, 0, 0, new List<CommitDetail>());
            }
        }
    }


    public class Repository
    {
        public string Path { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Branch { get; set; }
    }

    public class AuthorRequest
    {
        public string Author { get; set; }
        public List<Repository> Repository { get; set; }
    }

    public class RequestConfig
    {
        public List<AuthorRequest> Request { get; set; }
    }

    public class CommitDetail
    {
        public string Hash { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
        public string Added { get; set; }
        public string Removed { get; set; }
    }

    public class RepositoryResult
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Path { get; set; }
        public int AddedLines { get; set; }
        public int RemovedLines { get; set; }
        public int TotalChangedLines { get; set; }
        public int CommitCount { get; set; }
        public List<CommitDetail> Commits { get; set; }
    }
    public class AuthorResult
    {
        public string Author { get; set; }
        public List<RepositoryResult> Repositories { get; set; }
    }

    public class ResultOutput
    {
        public List<AuthorResult> Results { get; set; }
    }
}
