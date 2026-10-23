using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Services;

public interface IHostApplicationService
{
    Task<(bool Success, string Message)> ApproveAsync(int applicationId, string adminId);
    Task<(bool Success, string Message)> RejectAsync(int applicationId, string adminId, string reason);
}

public class HostApplicationService : IHostApplicationService
{
    public const string GuestRole = "Guest";
    public const string HostRole = "Host";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly ILogger<HostApplicationService> _applicationLogger;

    public HostApplicationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger,
        INotificationService notifications,
        ILogger<HostApplicationService> applicationLogger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _notifications = notifications;
        _applicationLogger = applicationLogger;
    }

    /// <summary>
    /// Approves a host application. This is the heart of the rubric:
    /// the applicant becomes a Host, the Guest role is removed in the same
    /// operation, the property goes live, the change is logged and the applicant
    /// is notified.
    /// </summary>
    public async Task<(bool Success, string Message)> ApproveAsync(int applicationId, string adminId)
    {
        var application = await _db.HostApplications
            .Include(a => a.Property).ThenInclude(p => p.Images)
            .Include(a => a.ApplicationUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return (false, "Application not found.");

        if (application.Status != ApplicationStatus.Pending)
            return (false, "Only a pending application can be approved.");

        var user = await _userManager.FindByIdAsync(application.ApplicationUserId);
        if (user is null)
            return (false, "The applicant could not be loaded.");

        if (application.Property.HostId != application.ApplicationUserId ||
            !application.Property.Images.Any())
            return (false, "The application is missing a valid applicant-owned property image.");

        var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;

        try
        {
            application.Status = ApplicationStatus.Approved;
            application.ReviewedById = adminId;
            application.ReviewedAt = DateTime.UtcNow;
            application.Property.IsActive = true;

            await _db.SaveChangesAsync();

            // 1) Add the Host role.
            if (!await _userManager.IsInRoleAsync(user, HostRole))
            {
                var addResult = await _userManager.AddToRoleAsync(user, HostRole);
                if (!addResult.Succeeded)
                {
                    await RollbackAsync(transaction);
                    return (false, "Could not grant the Host role: " + DescribeErrors(addResult));
                }
            }

            // 2) Remove the Guest role so Guest and Host stay mutually exclusive.
            if (await _userManager.IsInRoleAsync(user, GuestRole))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, GuestRole);
                if (!removeResult.Succeeded)
                {
                    await RollbackAsync(transaction);
                    return (false, "Could not remove the Guest role: " + DescribeErrors(removeResult));
                }
            }

            // 3) Force the claims refresh so the new role applies to the current session.
            //    A failure here must not undo a role change that already succeeded;
            //    it only means the applicant's cookie is refreshed on their next sign-in.
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                _applicationLogger.LogWarning(
                    "Host role granted to {UserId} but the security stamp could not be refreshed: {Errors}",
                    user.Id, DescribeErrors(stampResult));
            }

            // 4) Audit trail and notification are part of the same transaction as the role change.
            await _logger.LogAsync(adminId, $"Approved host application #{application.Id}", ActivityType.Application);
            await _logger.LogAsync(user.Id, "Role changed Guest -> Host", ActivityType.RoleChange);

            await _notifications.NotifyAsync(
                user.Id,
                NotificationType.Application,
                "You're now a host!",
                $"Your application for \"{application.Property.Title}\" was approved. Your property is live.",
                "/HostDashboard/Index");

            if (transaction is not null)
                await transaction.CommitAsync();

            return (true, $"Application approved. {user.FullName} is now a Host.");
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction);
            _applicationLogger.LogError(ex, "Could not approve host application {ApplicationId}.", applicationId);
            return (false, "Approval failed. No role or application changes were committed.");
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<(bool Success, string Message)> RejectAsync(int applicationId, string adminId, string reason)
    {
        var application = await _db.HostApplications
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return (false, "Application not found.");

        if (application.Status != ApplicationStatus.Pending)
            return (false, "Only a pending application can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            return (false, "A rejection reason is required.");

        var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;

        try
        {
            application.Status = ApplicationStatus.Rejected;
            application.ReviewedById = adminId;
            application.ReviewedAt = DateTime.UtcNow;
            application.RejectionReason = reason.Trim();
            application.Property.IsActive = false;

            await _db.SaveChangesAsync();
            await _logger.LogAsync(adminId, $"Rejected host application #{application.Id}", ActivityType.Application);
            await _notifications.NotifyAsync(
                application.ApplicationUserId,
                NotificationType.Application,
                "Host application declined",
                reason.Trim(),
                "/HostApplication/Status");

            if (transaction is not null)
                await transaction.CommitAsync();

            return (true, "Application rejected.");
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction);
            _applicationLogger.LogError(ex, "Could not reject host application {ApplicationId}.", applicationId);
            return (false, "Rejection failed. No application changes were committed.");
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static async Task RollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction)
    {
        if (transaction is not null)
            await transaction.RollbackAsync();
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
