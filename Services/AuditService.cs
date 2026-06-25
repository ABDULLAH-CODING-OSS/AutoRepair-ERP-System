using AutoRepairERD.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AutoRepairERD.Services
{
    /// <summary>
    /// Service for logging audit trail of CRUD operations and important actions
    /// </summary>
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Log an action to the audit trail
        /// </summary>
        public async Task LogActionAsync(string tableName, int recordId, string actionType, string oldValues = null, string newValues = null)
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                var userId = httpContext?.Session?.GetInt32("UserID");
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    TableName = tableName,
                    RecordId = recordId,
                    ActionType = actionType,
                    OldValues = oldValues,
                    NewValues = newValues,
                    ActionDate = DateTime.Now,
                    Ipaddress = ipAddress
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silently fail - audit logging should never break the application
            }
        }

        /// <summary>
        /// Log a CREATE action
        /// </summary>
        public async Task LogCreateAsync(string tableName, int recordId, string summary = null)
        {
            await LogActionAsync(tableName, recordId, "Create", null, summary);
        }

        /// <summary>
        /// Log an UPDATE action
        /// </summary>
        public async Task LogUpdateAsync(string tableName, int recordId, string oldValues = null, string newValues = null)
        {
            await LogActionAsync(tableName, recordId, "Update", oldValues, newValues);
        }

        /// <summary>
        /// Log a DELETE action
        /// </summary>
        public async Task LogDeleteAsync(string tableName, int recordId, string summary = null)
        {
            await LogActionAsync(tableName, recordId, "Delete", summary, null);
        }

        /// <summary>
        /// Log a custom action
        /// </summary>
        public async Task LogCustomActionAsync(string tableName, int recordId, string actionType, string details = null)
        {
            await LogActionAsync(tableName, recordId, actionType, null, details);
        }
    }
}
