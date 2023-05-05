using CrucibleContactPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrucibleContactPro.Data
{
    public static class DataUtility
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString! : BuildConnectionString(databaseUrl);
            //if (string.IsNullOrEmpty(databaseUrl))
            //{
            //    return connectionString;
            //}
            //else
            //{
            //    return BuildConnectionString(databaseUrl);
            //}
        }
        private static string BuildConnectionString(string databaseUrl)
        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            return builder.ToString();
        }

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            // Obtaining the necessary services based on the IServiceProvider parameter
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();

            // Align the database by checking the Migrations
            await dbContextSvc.Database.MigrateAsync();

            // Seed Demo User(s)
            await SeedDemoUsersAsync(userManagerSvc);
        }


        // Demo Users Seed Method
        private static async Task SeedDemoUsersAsync(UserManager<AppUser> userManagerSvc)
        {
            AppUser demoUser = new AppUser()
            {
                UserName = "demologin@example.com",
                Email = "demologin@example.com",
                FirstName = "Demo",
                LastName = "User",
                EmailConfirmed = true,
            };

            try
            {
                AppUser? user = await userManagerSvc.FindByEmailAsync(demoUser.Email);

                if (user == null)
                {
                    // TODO: Use Environment Variable
                    await userManagerSvc.CreateAsync(demoUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*************  ERROR  *************");
                Console.WriteLine("Error Seeding Demo Login User.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("***********************************");
                throw;
            }
        }
    }
}
