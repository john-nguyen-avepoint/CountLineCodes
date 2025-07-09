using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CountTheNumberOfLineCodes
{
    /// <summary>
    /// unfinished
    /// </summary>
    public class CountGitLabDiffLinesByAuthor_OnlineV1
    {
        public static async Task UsingFunction()
        {
            Console.Write("Enter GitLab instance URL (e.g., https://gitlab.com): ");
            string gitlabUrl = Console.ReadLine();
            Console.Write("Enter project ID (e.g., namespace/project): ");
            string projectId = Console.ReadLine();
            Console.Write("Enter author name or email: ");
            string author = Console.ReadLine();
            Console.Write("Enter start date (yyyy-MM-dd): ");
            DateTime startDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter end date (yyyy-MM-dd): ");
            DateTime endDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter branch name (e.g., main): ");
            string branch = Console.ReadLine();
            Console.Write("Enter access token (leave empty for public repos): ");
            string accessToken = Console.ReadLine();

            var (added, removed, total, commitCount, commits) = await CountChangedLines(gitlabUrl, projectId, author, startDate, endDate, branch, accessToken);
            Console.WriteLine($"Added lines: {added}");
            Console.WriteLine($"Removed lines: {removed}");
            Console.WriteLine($"Total changed lines: {total}");
            Console.WriteLine($"Total commits: {commitCount}");
            Console.WriteLine("\nCommit List:");
            foreach (var commit in commits)
            {
                Console.WriteLine($"{commit.message} ({commit.date})");
            }
        }


        public static async Task<(int added,
            int removed,
            int total,
            int commitCount,
            List<(string hash, string message, string date)> commits)>
            CountChangedLines(
                string gitlabUrl,
                string projectId,
                string author,
                DateTime startDate,
                DateTime endDate,
                string branch,
                string accessToken = "")
        {
            try
            {
                using var client = new HttpClient();
                if (!string.IsNullOrEmpty(accessToken))
                    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", accessToken);

                // Get commits from GitLab API
                string dateFormat = "yyyy-MM-ddTHH:mm:ssZ";
                string apiUrl = $"{gitlabUrl}/api/v4/projects/{Uri.EscapeDataString(projectId)}/repository/commits?author={Uri.EscapeDataString(author)}&since={startDate.ToString(dateFormat)}&until={endDate.ToString(dateFormat)}&ref_name={Uri.EscapeDataString(branch)}";
                var response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var commits = JsonSerializer.Deserialize<List<Commit>>(json);
                if (commits == null) return (0, 0, 0, 0, new List<(string, string, string)>());

                int totalAdded = 0, totalRemoved = 0;
                var commitDetails = new List<(string hash, string message, string date)>();

                foreach (var commit in commits)
                {
                    // Get diff for each commit
                    string diffUrl = $"{gitlabUrl}/api/v4/projects/{Uri.EscapeDataString(projectId)}/repository/commits/{commit.id}/diff";
                    var diffResponse = await client.GetAsync(diffUrl);
                    diffResponse.EnsureSuccessStatusCode();

                    var diffJson = await diffResponse.Content.ReadAsStringAsync();
                    var diffs = JsonSerializer.Deserialize<List<Diff>>(diffJson);
                    if (diffs == null) continue;

                    foreach (var diff in diffs)
                    {
                        var lines = diff.diff.Split('\n');
                        int added = lines.Count(line => line.StartsWith("+") && !line.StartsWith("+++"));
                        int removed = lines.Count(line => line.StartsWith("-") && !line.StartsWith("---"));
                        totalAdded += added;
                        totalRemoved += removed;
                    }

                    commitDetails.Add((commit.id, commit.message, commit.authored_date));
                }

                return (totalAdded, totalRemoved, totalAdded + totalRemoved, commits.Count, commitDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, 0, 0, 0, new List<(string, string, string)>());
            }
        }

        public class Commit
        {
            public string id { get; set; }
            public string message { get; set; }
            public string authored_date { get; set; }
        }

        public class Diff
        {
            public string diff { get; set; }
        }
    }
}
