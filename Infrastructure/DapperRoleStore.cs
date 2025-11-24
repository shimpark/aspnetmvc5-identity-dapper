using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNet.Identity;
using WebApp.Models;

namespace WebApp.Infrastructure
{
    public class DapperRoleStore : IRoleStore<ApplicationRole, string>
    {
        private readonly string _connectionString;

        public DapperRoleStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task CreateAsync(ApplicationRole role)
        {
            const string sql = @"
                INSERT INTO AspNetRoles (Id, Name)
                VALUES (@Id, @Name)";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, role);
            }
        }

        public async Task UpdateAsync(ApplicationRole role)
        {
            const string sql = @"
                UPDATE AspNetRoles SET Name = @Name WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, role);
            }
        }

        public async Task DeleteAsync(ApplicationRole role)
        {
            const string sql = @"
                DELETE FROM AspNetRoles WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                await conn.ExecuteAsync(sql, new { role.Id });
            }
        }

        public async Task<ApplicationRole> FindByIdAsync(string roleId)
        {
            const string sql = "SELECT * FROM AspNetRoles WHERE Id = @Id";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationRole>(sql, new { Id = roleId });
            }
        }

        public async Task<ApplicationRole> FindByNameAsync(string roleName)
        {
            const string sql = "SELECT * FROM AspNetRoles WHERE Name = @Name";

            using (var conn = CreateConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<ApplicationRole>(sql, new { Name = roleName });
            }
        }

        public void Dispose()
        {
            // no-op
        }
    }
}
