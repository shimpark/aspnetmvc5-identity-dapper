using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleAdminController : Controller
    {
        private ApplicationRoleManager _roleManager;

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set { _roleManager = value; }
        }

        // GET: RoleAdmin
        public ActionResult Index(string search, int page = 1, int pageSize = 10)
        {
            // RoleStore doesn't implement IQueryableRoleStore, so RoleManager.Roles will throw.
            // Query roles directly using Dapper instead, with search and paging.
            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString))
            {
                return View(Enumerable.Empty<ApplicationRole>());
            }

            using (var conn = new SqlConnection(connString))
            {
                var where = string.IsNullOrEmpty(search) ? "" : "WHERE Name LIKE @Search";
                var offset = (page - 1) * pageSize;
                var roles = conn.Query<ApplicationRole>($"SELECT Id, Name FROM AspNetRoles {where} ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", new { Search = "%" + search + "%", Offset = offset, PageSize = pageSize }).ToList();
                var total = conn.ExecuteScalar<int>(string.IsNullOrEmpty(search)
                    ? "SELECT COUNT(1) FROM AspNetRoles"
                    : "SELECT COUNT(1) FROM AspNetRoles WHERE Name LIKE @Search", new { Search = "%" + search + "%" });

                ViewBag.Search = search;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Total = total;

                return View(roles);
            }
        }

        // GET: RoleAdmin/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: RoleAdmin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();

            // Get role
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            // Get users in role via Dapper
            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString))
            {
                ViewBag.Role = role;
                return View(Enumerable.Empty<WebApp.Models.ApplicationUser>());
            }

            using (var conn = new SqlConnection(connString))
            {
                var users = conn.Query<WebApp.Models.ApplicationUser>(
                    "SELECT u.* FROM AspNetUsers u INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId WHERE ur.RoleId = @RoleId",
                    new { RoleId = role.Id }).ToList();

                ViewBag.Role = role;
                return View(users);
            }
        }

        // GET: RoleAdmin/AddUser/5
        public async Task<ActionResult> AddUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString))
            {
                ViewBag.Role = role;
                return View(Enumerable.Empty<WebApp.Models.ApplicationUser>());
            }

            using (var conn = new SqlConnection(connString))
            {
                var usersNotInRole = conn.Query<WebApp.Models.ApplicationUser>(
                    "SELECT u.* FROM AspNetUsers u WHERE NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id AND ur.RoleId = @RoleId)",
                    new { RoleId = role.Id }).ToList();

                ViewBag.Role = role;
                return View(usersNotInRole);
            }
        }

        // POST: RoleAdmin/AddUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddUser(string id, string userId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(userId)) return RedirectToAction("Details", new { id });

            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var userManager = HttpContext.GetOwinContext().GetUserManager<WebApp.ApplicationUserManager>();
            if (userManager != null)
            {
                await userManager.AddToRoleAsync(userId, role.Name);
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: RoleAdmin/AddUsers (bulk)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddUsers(string id, string[] userIds)
        {
            if (string.IsNullOrEmpty(id) || userIds == null || userIds.Length == 0) return RedirectToAction("Details", new { id });

            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var userManager = HttpContext.GetOwinContext().GetUserManager<WebApp.ApplicationUserManager>();
            if (userManager != null)
            {
                foreach (var uid in userIds)
                {
                    await userManager.AddToRoleAsync(uid, role.Name);
                }
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: RoleAdmin/RemoveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveUser(string id, string userId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var userManager = HttpContext.GetOwinContext().GetUserManager<WebApp.ApplicationUserManager>();
            if (userManager != null)
            {
                await userManager.RemoveFromRoleAsync(userId, role.Name);
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: RoleAdmin/RemoveUsers (bulk)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveUsers(string id, string[] userIds)
        {
            if (string.IsNullOrEmpty(id) || userIds == null || userIds.Length == 0) return RedirectToAction("Details", new { id });
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var userManager = HttpContext.GetOwinContext().GetUserManager<WebApp.ApplicationUserManager>();
            if (userManager != null)
            {
                foreach (var uid in userIds)
                {
                    await userManager.RemoveFromRoleAsync(uid, role.Name);
                }
            }

            return RedirectToAction("Details", new { id });
        }

        // GET: RoleAdmin/ManagePermissions/5
        public async Task<ActionResult> ManagePermissions(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString))
            {
                ViewBag.Role = role;
                return View(Enumerable.Empty<WebApp.Models.RolePermission>());
            }

            using (var conn = new SqlConnection(connString))
            {
                // Ensure RolePermissions table exists (in case SQL script wasn't run)
                var ensureSql = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions] (
        [RoleId] NVARCHAR(128) NOT NULL,
        [Permission] NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY (RoleId, Permission),
        CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY (RoleId) REFERENCES [dbo].[AspNetRoles](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_RolePermissions_RoleId ON [dbo].[RolePermissions] ([RoleId]);
END";
                conn.Execute(ensureSql);

                var perms = conn.Query<WebApp.Models.RolePermission>("SELECT RoleId, Permission FROM RolePermissions WHERE RoleId = @RoleId", new { RoleId = id }).ToList();
                ViewBag.Role = role;
                return View(perms);
            }
        }

        // POST: RoleAdmin/AddPermission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPermission(string id, string permission)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(permission)) return RedirectToAction("ManagePermissions", new { id });
            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (!string.IsNullOrEmpty(connString))
            {
                using (var conn = new SqlConnection(connString))
                {
                    // Insert only if not exists to avoid duplicate key errors
                    var insertSql = @"IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @RoleId AND Permission = @Permission)
BEGIN
    INSERT INTO RolePermissions (RoleId, Permission) VALUES (@RoleId, @Permission)
END";
                    conn.Execute(insertSql, new { RoleId = id, Permission = permission });
                }
            }
            return RedirectToAction("ManagePermissions", new { id });
        }

        // POST: RoleAdmin/RemovePermissions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemovePermissions(string id, string[] permissions)
        {
            if (string.IsNullOrEmpty(id) || permissions == null || permissions.Length == 0) return RedirectToAction("ManagePermissions", new { id });
            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (!string.IsNullOrEmpty(connString))
            {
                using (var conn = new SqlConnection(connString))
                {
                    foreach (var p in permissions)
                    {
                        conn.Execute("DELETE FROM RolePermissions WHERE RoleId = @RoleId AND Permission = @Permission", new { RoleId = id, Permission = p });
                    }
                }
            }
            return RedirectToAction("ManagePermissions", new { id });
        }

        // POST: RoleAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ApplicationRole model)
        {
            if (ModelState.IsValid)
            {
                var role = new ApplicationRole(model.Name);
                var result = await RoleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                AddErrors(result);
            }
            return View(model);
        }

        // GET: RoleAdmin/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();
            return View(role);
        }

        // POST: RoleAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ApplicationRole model)
        {
            if (!ModelState.IsValid) return View(model);
            var role = await RoleManager.FindByIdAsync(model.Id);
            if (role == null) return HttpNotFound();
            role.Name = model.Name;
            var result = await RoleManager.UpdateAsync(role);
            if (result.Succeeded) return RedirectToAction("Index");
            AddErrors(result);
            return View(model);
        }

        // POST: RoleAdmin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");
            var role = await RoleManager.FindByIdAsync(id);
            if (role != null)
            {
                await RoleManager.DeleteAsync(role);
            }
            return RedirectToAction("Index");
        }

        // POST: RoleAdmin/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkDelete(string[] roleIds)
        {
            if (roleIds == null || roleIds.Length == 0) return RedirectToAction("Index");
            foreach (var id in roleIds)
            {
                var role = await RoleManager.FindByIdAsync(id);
                if (role != null)
                {
                    await RoleManager.DeleteAsync(role);
                }
            }
            return RedirectToAction("Index");
        }

        private void AddErrors(Microsoft.AspNet.Identity.IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}
