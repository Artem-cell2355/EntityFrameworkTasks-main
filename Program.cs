using EntityFrameworkTasks;
using Microsoft.EntityFrameworkCore;
using EntityFrameworkTasks.Models;
using Task = EntityFrameworkTasks.Models.Task;

await using (AppDbContext dbContext = new())
{
    Console.WriteLine("-Start-");

    //1
    var projectInfo = await dbContext.Projects
        .Select(p => new
        {
            p.Name,
            p.Description,
            TasksCount = p.Tasks.Count,
            MembersCount = p.Members.Count
        })
        .ToListAsync();
    Console.WriteLine("\n--- 1) Project Info (Name, Tasks Count, Members Count) ---");
    foreach (var p in projectInfo)
    {
        Console.WriteLine($"Project: {p.Name} | Tasks: {p.TasksCount} | Members: {p.MembersCount}");
    }

    //2
    var tasksWithManyComments = await dbContext.Tasks
        .Where(t => t.Comments.Count > 2)
        .Select(t => new { t.Title, CommentsCount = t.Comments.Count })
        .ToListAsync();
    Console.WriteLine("\n--- 2) Tasks with > 2 comments ---");
    foreach (var t in tasksWithManyComments.Take(5))
    {
        Console.WriteLine($"Task: {t.Title} | cpmments: {t.CommentsCount}");
    }

    //3
    var topBugCreator = await dbContext.Users
        .Select(u => new
        {
            User = u,
            BugTaskCount = u.CreatedTasks
                .Count(t => t.Tags.Any(tag => tag.Name == "BUG"))
        })
        .OrderByDescending(x => x.BugTaskCount)
        .FirstOrDefaultAsync();
    Console.WriteLine("\n--- 3) Top BUG task creator ---");
    Console.WriteLine(topBugCreator != null
        ? $"User: {topBugCreator.User.Name} | BUG Tasks: {topBugCreator.BugTaskCount}"
        : "No BUG tasks found.");

    //4
    var tagCounts = await dbContext.Tags
        .Select(t => new
        {
            TagName = t.Name,
            TaskCount = t.Tasks.Count
        })
        .OrderByDescending(x => x.TaskCount)
        .ToListAsync();
    Console.WriteLine("\n--- 4) Task count for each tag ---");
    foreach (var t in tagCounts)
    {
        Console.WriteLine($"Tag: {t.TagName} | Tasks: {t.TaskCount}");
    }

    //5
    var selfAssignedTasks = await dbContext.Tasks
        .Where(t => t.CreatorId == t.AssigneeId)
        .Select(t => new
        {
            t.Title,
            CreatorName = t.Creator.Name
        })
        .ToListAsync();
    Console.WriteLine("\n--- 5) Self-assigned tasks ---");
    foreach (var t in selfAssignedTasks.Take(5))
    {
        Console.WriteLine($"Task: {t.Title} | Creator/Assignee: {t.CreatorName}");
    }

    //6
    var tasksWithLatestComment = await dbContext.Tasks
        .Where(t => t.Comments.Any())
        .OrderBy(t => t.Id)
        .Take(15)
        .Select(t => new
        {
            TaskTitle = t.Title,
            LatestComment = t.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Text, c.CreatedAt, Author = c.Author.Name })
                .FirstOrDefault()
        })
        .ToListAsync();
    Console.WriteLine("\n--- 6. Latest comment for first 15 tasks with comments ---");
    foreach (var t in tasksWithLatestComment)
    {
        Console.WriteLine($"Task: {t.TaskTitle} | Latest Comment: '{t.LatestComment?.Text}' by {t.LatestComment?.Author}");
    }

    //7 
    var multiTaggedTasks = await dbContext.Tasks
        .Where(t => t.Tags.Count > 1)
        .Select(t => new
        {
            t.Title,
            Tags = t.Tags.Select(tag => tag.Name).ToList()
        })
        .ToListAsync();
    Console.WriteLine("\n--- 7) Tasks with more than one tag ---");
    foreach (var t in multiTaggedTasks.Take(5))
    {
        Console.WriteLine($"Tфsk: {t.Title} | Tфgs: {string.Join(", ", t.Tags)}");
    }

    //8
    var commentsPerUser = await dbContext.Comments
        .GroupBy(c => c.Author)
        .Select(g => new
        {
            UserName = g.Key.Name,
            TotalComments = g.Count()
        })
        .OrderByDescending(x => x.TotalComments)
        .ToListAsync();
    Console.WriteLine("\n--- 8) Total comments per user (Top 5) ---");
    foreach (var u in commentsPerUser.Take(5))
    {
        Console.WriteLine($"User: {u.UserName} | Total Comments: {u.TotalComments}");
    }

    //9
    var teamsRankedByTaskLoad = await dbContext.Teams
        .Select(t => new
        {
            t.Name,
            CreatedTasksIds = t.Members.SelectMany(m => m.CreatedTasks.Select(task => task.Id)).Distinct(),
            AssignedTasksIds = t.Members.SelectMany(m => m.AssignedTasks.Select(task => task.Id)).Distinct(),
        })
        .Select(t => new
        {
            TeamName = t.Name,
            TotalTasksLoad = t.CreatedTasksIds.Concat(t.AssignedTasksIds).Distinct().Count()
        })
        .OrderByDescending(x => x.TotalTasksLoad)
        .ToListAsync();
    Console.WriteLine("\n--- 9. Teams ranked by task load (Top 5) ---");
    foreach (var t in teamsRankedByTaskLoad.Take(5))
    {
        Console.WriteLine($"Team: {t.TeamName} | Total Unique Tasks: {t.TotalTasksLoad}");
    }

    //10g Get information about users who left comments under a task with the tag "STORY"
    var storyCommentersInfo = await dbContext.Comments
        .Where(c => c.Task.Tags.Any(tag => tag.Name == "STORY"))
        .Select(c => new
        {
            UserName = c.Author.Name,
            c.Author.Email,
            TaskTitle = c.Task.Title,
            ProjectName = c.Task.Project.Name,
            TeamNames = dbContext.Teams
                .Where(team => team.ProjectId == c.Task.ProjectId)
                .Select(team => team.Name)
                .ToList()
        })
        .Distinct()
        .ToListAsync();
    Console.WriteLine("\n--- 10) Users who commented on 'STORY' tasks (Top 5) ---");
    foreach (var c in storyCommentersInfo.Take(5))
    {
        Console.WriteLine($"User: {c.UserName} ({c.Email}) | Task: {c.TaskTitle} | Project: {c.ProjectName}");
    }

    //11. For each user, find the tag they use most frequently on tasks they hhave created
    var mostFrequentTagsPerUser = await dbContext.Users
        .Select(u => new
        {
            User = u.Name,
            TagUsages = u.CreatedTasks
                .SelectMany(t => t.Tags)
                .GroupBy(tag => tag.Name)
                .Select(g => new
                {
                    TagName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(t => t.Count)
                .FirstOrDefault()
        })
        .Where(x => x.TagUsages != null)
        .Select(x => new
        {
            x.User,
            MostFrequentTag = x.TagUsages!.TagName,
            UsageCount = x.TagUsages!.Count
        })
        .ToListAsync();
    Console.WriteLine("\n--- 11) Most frequent tag per user (Top 5) ---");
    foreach (var u in mostFrequentTagsPerUser.Take(5))
    {
        Console.WriteLine($"User: {u.User} | Most Frequent Tag: {u.MostFrequentTag} (Count: {u.UsageCount})");
    }

    //12. List projects ordered by the average number of comments per task
    var projectsByAvgComments = await dbContext.Projects
        .Select(p => new
        {
            p.Name,
            AverageComments = p.Tasks.Any()
                ? p.Tasks.Average(t => t.Comments.Count)
                : 0
        })
        .OrderByDescending(x => x.AverageComments)
        .ToListAsync();
    Console.WriteLine("\n--- 12) Projects by Avg Comments per task ---");
    foreach (var p in projectsByAvgComments)
    {
        Console.WriteLine($"Project: {p.Name} | Avg Comments: {p.AverageComments:F2}");
    }

    //13. Find users who have commented on tasks they did not create
    var usersCommentingOnOthersTasks = await dbContext.Comments
        .Where(c => c.AuthorId != c.Task.CreatorId)
        .Select(c => new
        {
            UserName = c.Author.Name,
            c.Author.Email
        })
        .Distinct()
        .ToListAsync();
    Console.WriteLine("\n--- 13. Users commenting on tasks they did not create (Top 5) ---");
    foreach (var u in usersCommentingOnOthersTasks.Take(5))
    {
        Console.WriteLine($"User: {u.UserName} ({u.Email})");
    }
}