using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebApp.Startup))]
namespace WebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            // 자동 관리자 시드 실행
            SeedRolesAndAdmin(app);
        }

        private void SeedRolesAndAdmin(IAppBuilder app)
        {
            // OWIN 컨텍스트에서 RoleManager/UserManager를 바로 얻을 수 없는 경우가 있으므로
            // 연결 문자열로 Dapper 스토어와 매니저를 직접 생성해서 시드 작업을 수행합니다.
            var connString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString)) return;

            using (var roleStore = new WebApp.Infrastructure.DapperRoleStore(connString))
            using (var userStore = new WebApp.Infrastructure.DapperUserStore(connString))
            {
                var roleManager = new WebApp.ApplicationRoleManager(roleStore);
                var userManager = new WebApp.ApplicationUserManager(userStore);

                System.Threading.Tasks.Task.Run(async () =>
                {
                    const string adminRoleName = "Admin";
                    const string adminEmail = "admin@local.kr";
                    const string adminPassword = "Admin@12345"; // 배포 환경에서는 강력한 암호로 변경하세요

                    if (!await roleManager.RoleExistsAsync(adminRoleName))
                    {
                        await roleManager.CreateAsync(new WebApp.Models.ApplicationRole(adminRoleName));
                    }

                    var adminUser = await userManager.FindByNameAsync(adminEmail);
                    if (adminUser == null)
                    {
                        adminUser = new WebApp.Models.ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true
                        };
                        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                        if (createResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser.Id, adminRoleName);
                        }
                    }
                    else
                    {
                        if (!await userManager.IsInRoleAsync(adminUser.Id, adminRoleName))
                        {
                            await userManager.AddToRoleAsync(adminUser.Id, adminRoleName);
                        }
                    }
                }).GetAwaiter().GetResult();
            }
        }
    }
}
