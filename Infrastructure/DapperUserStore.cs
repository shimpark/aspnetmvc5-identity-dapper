using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNet.Identity;
using WebApp.Models;

namespace WebApp.Infrastructure
{
    public class DapperUserStore :
        IUserStore<ApplicationUser>,
        IUserPasswordStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserLockoutStore<ApplicationUser, string>,
        IUserTwoFactorStore<ApplicationUser, string>,
        IUserLoginStore<ApplicationUser>,
        IUserRoleStore<ApplicationUser>,
        IUserClaimStore<ApplicationUser>,
        IUserPhoneNumberStore<ApplicationUser>
    {
        private readonly string _connectionString;

        public DapperUserStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region IUserStore Implementation

        public async Task CreateAsync(ApplicationUser user)
        {
            const string sql = @"
                INSERT INTO AspNetUsers (Id, UserName, Email, EmailConfirmed, PasswordHash, 
                    SecurityStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
                    LockoutEndDateUtc, LockoutEnabled, AccessFailedCount)
                VALUES (@Id, @UserName, @Email, @EmailConfirmed, @PasswordHash, 
                    @SecurityStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, 
                    @LockoutEndDateUtc, @LockoutEnabled, @AccessFailedCount)";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, user);
            }
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            const string sql = @"
                UPDATE AspNetUsers SET 
                    UserName = @UserName,
                    Email = @Email,
                    EmailConfirmed = @EmailConfirmed,
                    PasswordHash = @PasswordHash,
                    SecurityStamp = @SecurityStamp,
                    PhoneNumber = @PhoneNumber,
                    PhoneNumberConfirmed = @PhoneNumberConfirmed,
                    TwoFactorEnabled = @TwoFactorEnabled,
                    LockoutEndDateUtc = @LockoutEndDateUtc,
                    LockoutEnabled = @LockoutEnabled,
                    AccessFailedCount = @AccessFailedCount
                WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, user);
            }
        }

        public async Task DeleteAsync(ApplicationUser user)
        {
            const string sql = "DELETE FROM AspNetUsers WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new { user.Id });
            }
        }

        public async Task<ApplicationUser> FindByIdAsync(string userId)
        {
            const string sql = "SELECT * FROM AspNetUsers WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { Id = userId });
            }
        }

        public async Task<ApplicationUser> FindByNameAsync(string userName)
        {
            const string sql = "SELECT * FROM AspNetUsers WHERE UserName = @UserName";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { UserName = userName });
            }
        }

        #endregion

        #region IUserPasswordStore Implementation

        public Task SetPasswordHashAsync(ApplicationUser user, string passwordHash)
        {
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(ApplicationUser user)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        #endregion

        #region IUserSecurityStampStore Implementation

        public Task SetSecurityStampAsync(ApplicationUser user, string stamp)
        {
            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(ApplicationUser user)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        #endregion

        #region IUserEmailStore Implementation

        public Task SetEmailAsync(ApplicationUser user, string email)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(ApplicationUser user)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed)
        {
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            const string sql = "SELECT * FROM AspNetUsers WHERE Email = @Email";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { Email = email });
            }
        }

        #endregion

        #region IUserLockoutStore Implementation

        public Task<DateTimeOffset> GetLockoutEndDateAsync(ApplicationUser user)
        {
            var lockoutEndDate = user.LockoutEndDateUtc.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc))
                : new DateTimeOffset();

            return Task.FromResult(lockoutEndDate);
        }

        public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset lockoutEnd)
        {
            user.LockoutEndDateUtc = lockoutEnd == DateTimeOffset.MinValue ? (DateTime?)null : lockoutEnd.UtcDateTime;
            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(ApplicationUser user)
        {
            user.AccessFailedCount = 0;
            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(ApplicationUser user)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(ApplicationUser user)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled)
        {
            user.LockoutEnabled = enabled;
            return Task.FromResult(0);
        }

        #endregion

        #region IUserTwoFactorStore Implementation

        public Task SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled)
        {
            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        #endregion

        #region IUserPhoneNumberStore Implementation

        public Task SetPhoneNumberAsync(ApplicationUser user, string phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(ApplicationUser user)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        #endregion

        #region IUserLoginStore Implementation

        public async Task AddLoginAsync(ApplicationUser user, UserLoginInfo login)
        {
            const string sql = @"
                INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, UserId)
                VALUES (@LoginProvider, @ProviderKey, @UserId)";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new
                {
                    LoginProvider = login.LoginProvider,
                    ProviderKey = login.ProviderKey,
                    UserId = user.Id
                });
            }
        }

        public async Task RemoveLoginAsync(ApplicationUser user, UserLoginInfo login)
        {
            const string sql = @"
                DELETE FROM AspNetUserLogins 
                WHERE UserId = @UserId AND LoginProvider = @LoginProvider AND ProviderKey = @ProviderKey";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new
                {
                    UserId = user.Id,
                    LoginProvider = login.LoginProvider,
                    ProviderKey = login.ProviderKey
                });
            }
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user)
        {
            const string sql = @"
                SELECT LoginProvider, ProviderKey 
                FROM AspNetUserLogins 
                WHERE UserId = @UserId";

            using (var conn = CreateConnection())
            {
                var logins = await conn.QueryAsync<UserLoginInfo>(sql, new { UserId = user.Id });
                return logins.ToList();
            }
        }

        public async Task<ApplicationUser> FindAsync(UserLoginInfo login)
        {
            const string sql = @"
                SELECT u.* FROM AspNetUsers u
                INNER JOIN AspNetUserLogins l ON u.Id = l.UserId
                WHERE l.LoginProvider = @LoginProvider AND l.ProviderKey = @ProviderKey";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new
                {
                    LoginProvider = login.LoginProvider,
                    ProviderKey = login.ProviderKey
                });
            }
        }

        #endregion

        #region IUserRoleStore Implementation

        public async Task AddToRoleAsync(ApplicationUser user, string roleName)
        {
            const string getRoleSql = "SELECT Id FROM AspNetRoles WHERE Name = @RoleName";
            const string addRoleSql = @"
                INSERT INTO AspNetUserRoles (UserId, RoleId)
                VALUES (@UserId, @RoleId)";

            using (var conn = CreateConnection())
            {
                var roleId = await conn.QueryFirstOrDefaultAsync<string>(getRoleSql, new { RoleName = roleName });
                if (roleId != null)
                {
                    await conn.ExecuteAsync(addRoleSql, new { UserId = user.Id, RoleId = roleId });
                }
            }
        }

        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName)
        {
            const string sql = @"
                DELETE FROM AspNetUserRoles 
                WHERE UserId = @UserId AND RoleId = (SELECT Id FROM AspNetRoles WHERE Name = @RoleName)";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new { UserId = user.Id, RoleName = roleName });
            }
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            const string sql = @"
                SELECT r.Name 
                FROM AspNetRoles r
                INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId";

            using (var conn = CreateConnection())
            {
                var roles = await conn.QueryAsync<string>(sql, new { UserId = user.Id });
                return roles.ToList();
            }
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName)
        {
            const string sql = @"
                SELECT COUNT(1) 
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE ur.UserId = @UserId AND r.Name = @RoleName";

            using (var conn = CreateConnection())
            {
                var count = await conn.ExecuteScalarAsync<int>(sql, new { UserId = user.Id, RoleName = roleName });
                return count > 0;
            }
        }

        #endregion

        #region IUserClaimStore Implementation

        public async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            const string sql = @"
                SELECT ClaimType, ClaimValue 
                FROM AspNetUserClaims 
                WHERE UserId = @UserId";

            using (var conn = CreateConnection())
            {
                var claims = await conn.QueryAsync<dynamic>(sql, new { UserId = user.Id });
                return claims.Select(c => new Claim((string)c.ClaimType, (string)c.ClaimValue)).ToList();
            }
        }

        public async Task AddClaimAsync(ApplicationUser user, Claim claim)
        {
            const string sql = @"
                INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)
                VALUES (@UserId, @ClaimType, @ClaimValue)";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new
                {
                    UserId = user.Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
        }

        public async Task RemoveClaimAsync(ApplicationUser user, Claim claim)
        {
            const string sql = @"
                DELETE FROM AspNetUserClaims 
                WHERE UserId = @UserId AND ClaimType = @ClaimType AND ClaimValue = @ClaimValue";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new
                {
                    UserId = user.Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Dapper는 연결을 자동으로 관리하므로 특별한 정리 작업 불필요
        }

        #endregion
    }
}