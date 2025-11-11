using Bogus;
using EntityFrameworkTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkTasks;

public static class Seeder
{
    public static void Seed(DbContext dbContext, bool seeded)
    {
        if (seeded)
        {
            return;
        }

        Randomizer.Seed = new(100500);

        int teamsCount = 50;
        int usersCount = 500;
        int projectsCount = 10;
        int tasksCount = 90000;
        int commentsCount = 10000;
        int taskTagsCount = 90000;

        var projects = new Faker<Project>()
            .RuleFor(p => p.Id, f => f.IndexFaker + 1)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Lorem.Sentence())
            .Generate(projectsCount);
        dbContext.AddRange(projects);
        dbContext.SaveChanges();
        var projectsMap = projects.ToDictionary(x => x.Id);

        var teams = new Faker<Team>()
            .RuleFor(t => t.Id, f => f.IndexFaker + 1)
            .RuleFor(t => t.Name, f => f.Company.CompanySuffix() + " " + f.Company.CompanyName())
            .RuleFor(t => t.ProjectId, f => f.PickRandom(projects).Id)
            .Generate(teamsCount);
        dbContext.Set<Team>().AddRange(teams);
        dbContext.SaveChanges();

        var teamsMap = teams.ToDictionary(x => x.Id);

        // migrationBuilder.InsertData("Teams", new[] { "Id", "Name" }, teams.Select(t => new object[] { t.Id, t.Name }).ToArray());

        var users = new Faker<User>()
            .RuleFor(u => u.Id, f => f.IndexFaker + 1)
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            .Generate(usersCount);
        var usersMap = users.ToDictionary(x => x.Id);

        // migrationBuilder.InsertData("Users", new[] { "Id", "Name", "Email" }, users.Select(u => new object[] { u.Id, u.Name, u.Email }).ToArray());

        var faker = new Faker();

        var teamUsers = Enumerable.Range(1, usersCount)
            .Select(i => new { UserId = users[i - 1].Id, TeamId = faker.PickRandom(teams).Id })
            .ToLookup(x => teamsMap[x.TeamId], x => usersMap[x.UserId]);

        foreach (var teamUser in teamUsers)
        {
            foreach (var user in teamUser)
            {
                user.Projects.Add(projectsMap[teamUser.Key.ProjectId]);
            }

            teamUser.Key.Members = teamUser.ToList();
        }

        dbContext.Set<User>().AddRange(users);
        dbContext.SaveChanges();

        // dbContext.AddRange(teamUsers);
        // foreach (var teamUser in teamUsers)
        // {
        //     migrationBuilder.InsertData("TeamUser", new[] { "UsersId", "TeamsId" }, new object[] { teamUser.UserId, teamUser.TeamId });
        // }

        // migrationBuilder.InsertData("Projects", new[] { "Id", "Name", "Description" }, projects.Select(p => new object[] { p.Id, p.Name, p.Description }).ToArray());

        var tasks = new Faker<Models.Task>()
            .RuleFor(t => t.Id, f => f.IndexFaker + 1)
            .RuleFor(t => t.Title, f => f.Lorem.Sentence(3))
            .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
            .RuleFor(t => t.CreatorId, f => f.PickRandom(users).Id)
            .RuleFor(t => t.AssigneeId, f => f.PickRandom(users).Id)
            .RuleFor(t => t.ProjectId, f => f.PickRandom(projects).Id)
            .Generate(tasksCount);
        var tasksMap = tasks.ToDictionary(x => x.Id);

        // migrationBuilder.InsertData("Comments", new[] { "Id", "Text", "TaskId", "AuthorId" }, comments.Select(c => new object[] { c.Id, c.Text, c.TaskId, c.AuthorId }).ToArray());

        List<Tag> tags =
        [
            new() { Id = 1, Name = "DEV" },
            new() { Id = 2, Name = "UT" },
            new() { Id = 3, Name = "E2E" },
            new() { Id = 4, Name = "UI" },
            new() { Id = 5, Name = "UX" },
            new() { Id = 6, Name = "INVESTIGATE" },
            new() { Id = 7, Name = "BUG" },
            new() { Id = 8, Name = "FEATURE" },
            new() { Id = 9, Name = "STORY" }
        ];
        dbContext.Set<Tag>().AddRange(tags);
        dbContext.SaveChanges();
        var tagsMap = tags.ToDictionary(x => x.Id);

        // migrationBuilder.InsertData("Tags", new[] { "Id", "Name" }, tags.Select(t => new object[] { t.Id, t.Name }).ToArray());

        var taskTags = Enumerable.Range(1, taskTagsCount)
            .Select(_ => new
            {
                TaskId = faker.PickRandom(tasks).Id,
                TagId = faker.PickRandom(tags).Id
            })
            .Distinct()
            .ToLookup(x => tasksMap[x.TaskId], x => tagsMap[x.TagId]);

        foreach (var taskTag in taskTags)
        {
            taskTag.Key.Tags = taskTag.ToList();
        }

        dbContext.Set<Models.Task>().AddRange(tasks);
        dbContext.SaveChanges();

        // migrationBuilder.InsertData("Tasks", new[] { "Id", "Title", "Description", "DueDate", "CreatorId", "AssigneeId", "ProjectId", "ParentTaskId" }, tasks.Select(t => new object[] { t.Id, t.Title, t.Description, t.DueDate, t.CreatorId, t.AssigneeId, t.ProjectId, t.ParentTaskId }).ToArray());

        var comments = new Faker<Comment>()
            .RuleFor(c => c.Id, f => f.IndexFaker + 1)
            .RuleFor(c => c.Text, f => f.Lorem.Sentence())
            .RuleFor(c => c.TaskId, f => f.PickRandom(tasks).Id)
            .RuleFor(c => c.AuthorId, f => f.PickRandom(users).Id)
            .Generate(commentsCount);
        dbContext.AddRange(comments);
        dbContext.SaveChanges();
        
    }
}