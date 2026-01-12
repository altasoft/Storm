using AltaSoft.Storm.TestModels;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm.TestApp;

internal sealed class Program
{
    private static async Task Main()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Starting the test application...");

        // Rest of the code remains unchanged
        var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        // Set the logger in StormManager
        StormManager.SetLogger(logger);

        var users = DatabaseHelper.CreateUserList();
        var user1ToUpdate = users.Last();
        user1ToUpdate.FullName = "UpdatedFirstName";

        var user2ToUpdate = users.First();
        user2ToUpdate.FullName = "UpdatedFirstName";

        int updateResult1;
        int updateResult2;

        var uow1 = new StormTransactionScope();
        {
            //await using var tx1 = await uow1.BeginAsync(fixture.ConnectionString, CancellationToken.None).ConfigureAwait(false);

            if (uow1.Ambient is null)
                throw new Exception("Master unit of work should not be null.");

            using (var uow2 = new StormTransactionScope())
            {
                if (uow2.Ambient is null)
                    throw new Exception("Master unit of work should not be null.");

                if (uow1.Ambient != uow2.Ambient)
                    throw new Exception("Ambient Unit of work should be the same.");

                var context = new TestStormContext(fixture.ConnectionString);

                updateResult1 = await context.UpdateUsersTable().WithoutConcurrencyCheck().Set(user1ToUpdate).GoAsync();
                updateResult2 = await context.UpdateUsersTable().WithoutConcurrencyCheck().Set(user2ToUpdate).GoAsync();

                await uow2.CompleteAsync(CancellationToken.None);
            }

            await uow1.CompleteAsync(CancellationToken.None);
        }

        uow1.Dispose();

        Console.WriteLine($"Update result 1: {updateResult1}");
        Console.WriteLine($"Update result 2: {updateResult2}");

        var context2 = new TestStormContext(fixture.ConnectionString);
        await AssertUserUpdated(context2, user1ToUpdate.UserId, user1ToUpdate);
        await AssertUserUpdated(context2, user2ToUpdate.UserId, user2ToUpdate);

        Console.WriteLine("Test completed.");

        await fixture.DisposeAsync();
        //X();
    }

    //private static async Task X()
    //{
    //    {
    //        using var uow = UnitOfWork.Create();
    //        var zxy = await uow.BeginAsync(null, null, default).ConfigureAwait(false);

    //        //await uow.BeginAsync(null, null, default).ConfigureAwait(false);

    //        //var _ = await uow.BeginAsync(null, null, default).ConfigureAwait(false);

    //        //await using var tx = await uow.BeginAsync(null, null, default).ConfigureAwait(false);
    //    }
    //    return;
    //}

    private static async Task AssertUserUpdated(TestStormContext context, int userId, User expected)
    {
        var actual = await context.SelectFromUsersTable(userId, 7).GetAsync();
        if (actual == null)
        {
            Console.WriteLine($"ERROR: User {userId} not found.");
            return;
        }
        CheckUser(actual, expected);
    }

    private static void CheckUser(User? actual, User expected)
    {
        if (actual == null)
        {
            Console.WriteLine("ERROR: User is null.");
            return;
        }

        if (actual.UserId != expected.UserId ||
            actual.FullName != expected.FullName)
        {
            Console.WriteLine($"ERROR: User {actual.UserId} does not match expected values.");
        }
        else
        {
            Console.WriteLine($"User {actual.UserId} updated successfully.");
        }
    }
}
