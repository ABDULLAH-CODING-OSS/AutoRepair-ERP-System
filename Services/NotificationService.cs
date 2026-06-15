using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateForUserAsync(int userId, string type, string title, string message, int? createdByUserId = null, string? relatedType = null, int? relatedId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required", nameof(title));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message is required", nameof(message));

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new ArgumentException("Invalid user", nameof(userId));

            var n = new Notification
            {
                UserId = userId,
                NotificationType = type,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();
            return n;
        }

        public async Task<Notification> CreateForRoleAsync(int roleId, string type, string title, string message, int? userId = null, string? relatedType = null, int? relatedId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required", nameof(title));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message is required", nameof(message));

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) throw new ArgumentException("Invalid role", nameof(roleId));
            // Create per-user notifications for each user in the role
            var userIds = await _context.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync();
            Notification? created = null;
            foreach (var uid in userIds)
            {
                var n = new Notification
                {
                    UserId = uid,
                    NotificationType = type,
                    Title = title,
                    Message = message,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(n);
                created = n;
            }
            await _context.SaveChangesAsync();
            return created!;
        }

        public async Task MarkReadAsync(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return;
            n.IsRead = true;
            // ReadAt not present in DB model; do not set
            // Entity is tracked by the context; no explicit Update required
            await _context.SaveChangesAsync();
        }

        public async Task MarkUnreadAsync(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return;
            n.IsRead = false;
            // ReadAt not present in DB model; do not set
            await _context.SaveChangesAsync();
        }

        public async Task ArchiveAsync(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return;
            // Archive is not supported by current DB schema. No-op to avoid SQL errors. Consider DB migration to add IsArchived.
            return;
        }

        public async Task<int> GetUnreadCountForUserAsync(int userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        public async Task<List<Notification>> GetLatestForUserAsync(int userId, int limit = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
