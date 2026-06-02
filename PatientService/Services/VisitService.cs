using Microsoft.EntityFrameworkCore;
using PatientService.Controllers;
using PatientService.Data;
using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class VisitService : IVisitService
{
    private readonly IVisitRepository _visitRepo;
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly ServicesDbContext _ctx;

    public VisitService(
        IVisitRepository visitRepo,
        IEnrollmentRepository enrollRepo,
        ServicesDbContext ctx)
    {
        _visitRepo = visitRepo;
        _enrollRepo = enrollRepo;
        _ctx = ctx;
    }

    public async Task<object> GetByEnrollmentAsync(Guid enrollmentId)
    {
        var all = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        var upcoming = all
            .Where(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED")
            .Select(v => MapToDto(v, all)).ToList();

        var history = all
            .Where(v => v.VisitStatus is "COMPLETED" or "MISSED" or "CANCELLED")
            .Select(v => MapToDto(v, all)).ToList();

        return new { Upcoming = upcoming, History = history };
    }

    public async Task<IEnumerable<VisitDto>> GetFilteredAsync(
        DateTime? date, Guid? protocolSiteId, string? status)
    {
        var matchedVisits = (await _visitRepo.GetFilteredAsync(date, protocolSiteId, status))
            .ToList();

        if (!matchedVisits.Any())
            return Enumerable.Empty<VisitDto>();

        var enrollmentIds = matchedVisits
            .Select(v => v.EnrollmentId)
            .Distinct()
            .ToList();

        var allSiblings = new Dictionary<Guid, List<Visit>>();
        foreach (var enrollmentId in enrollmentIds)
        {
            var siblings = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
                .OrderBy(v => v.VisitDate)
                .ToList();
            allSiblings[enrollmentId] = siblings;
        }

        return matchedVisits.Select(v =>
            MapToDto(v, allSiblings.TryGetValue(v.EnrollmentId, out var siblings)
                ? siblings
                : null));
    }

    public async Task<(bool Success, string? Error, VisitDto? Data)> AddAsync(
        Guid enrollmentId, string visitName, DateTime visitDate)
    {
        var enrollment = await _enrollRepo.GetByIdAsync(enrollmentId);
        if (enrollment is null)
            return (false, "Enrollment not found.", null);

        // ✅ Block visits for non-active enrollments
        if (enrollment.EnrollmentStatus != "ACTIVE")
            return (false, "Cannot add visits to a non-active enrollment.", null);

        // ✅ Block visits for inactive patients
        if (enrollment.Patient?.PatientStatus == "INACTIVE")
            return (false, "Cannot add visits to an inactive patient.", null);

        var visit = new Visit
        {
            EnrollmentId = enrollmentId,
            VisitName = visitName,
            VisitDate = visitDate,
            VisitStatus = "SCHEDULED"
        };

        var created = await _visitRepo.CreateAsync(visit);

        var siblings = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        return (true, null, MapToDto(created, siblings));
    }

    public async Task<(bool Success, string? Error, VisitDto? Data)> RescheduleAsync(
        Guid id, DateTime newDate)
    {
        var visit = await _visitRepo.GetByIdAsync(id);
        if (visit is null)
            return (false, "Visit not found.", null);

        visit.VisitDate = newDate;
        visit.VisitStatus = "RESCHEDULED";
        await _visitRepo.UpdateAsync(visit);

        var siblings = (await _visitRepo.GetByEnrollmentIdAsync(visit.EnrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        return (true, null, MapToDto(visit, siblings));
    }

    public async Task<(bool Success, string? Error)> CancelAsync(Guid id)
    {
        var visit = await _visitRepo.GetByIdAsync(id);
        if (visit is null)
            return (false, "Visit not found.");

        visit.VisitStatus = "CANCELLED";
        await _visitRepo.UpdateAsync(visit);
        return (true, null);
    }

    public async Task<string> SubmitReviewAsync(
        DateTime date, Guid? protocolSiteId, List<Guid> attendedVisitIds)
    {
        var allVisits = (await _visitRepo.GetFilteredAsync(
            date, protocolSiteId, "SCHEDULED")).ToList();

        foreach (var visit in allVisits)
        {
            visit.VisitStatus = attendedVisitIds.Contains(visit.VisitId)
                ? "COMPLETED"
                : "MISSED";
        }

        await _visitRepo.BulkUpdateAsync(allVisits);

        int completed = allVisits.Count(v => v.VisitStatus == "COMPLETED");
        int missed = allVisits.Count(v => v.VisitStatus == "MISSED");

        var affectedEnrollmentIds = allVisits
            .Select(v => v.EnrollmentId).Distinct().ToList();

        int autoCompleted = 0;

        foreach (var enrollmentId in affectedEnrollmentIds)
        {
            var enrollment = await _enrollRepo.GetByIdAsync(enrollmentId);
            if (enrollment is null || enrollment.EnrollmentStatus != "ACTIVE")
                continue;

            var allEnrollmentVisits = await _visitRepo
                .GetByEnrollmentIdAsync(enrollmentId);
            bool hasUpcoming = allEnrollmentVisits
                .Any(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED");

            if (!hasUpcoming)
            {
                enrollment.EnrollmentStatus = "COMPLETED";
                await _enrollRepo.UpdateAsync(enrollment);
                autoCompleted++;
            }
        }

        var message = $"{completed} marked COMPLETED · {missed} marked MISSED.";
        if (autoCompleted > 0)
            message += $" · {autoCompleted} enrollment(s) auto-completed.";

        return message;
    }

    public async Task<(bool Success, string? Error, int Count)> BulkScheduleAsync(
        Guid protocolId, List<BulkVisitItem> visits)
    {
        if (visits is null || visits.Count == 0)
            return (false, "No visits provided.", 0);

        var enrollments = await _ctx.PatientEnrollments
            .Where(e =>
                e.ProtocolSite.ProtocolId == protocolId &&
                e.EnrollmentStatus == "ACTIVE")
            .ToListAsync();

        if (enrollments.Count == 0)
            return (false, "No active patients found for this protocol.", 0);

        var toCreate = new List<Visit>();

        foreach (var enrollment in enrollments)
        {
            var existingVisits = (await _visitRepo
                .GetByEnrollmentIdAsync(enrollment.EnrollmentId))
                .ToList();

            foreach (var v in visits)
            {
                bool isDuplicate = existingVisits.Any(e =>
                    e.VisitName.Equals(v.VisitName, StringComparison.OrdinalIgnoreCase) &&
                    e.VisitDate.Date == v.VisitDate.Date);

                if (isDuplicate) continue;

                toCreate.Add(new Visit
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    VisitName = v.VisitName,
                    VisitDate = v.VisitDate,
                    VisitStatus = "SCHEDULED"
                });
            }
        }

        if (toCreate.Count == 0)
            return (false, "All visits already exist for all patients.", 0);

        await _visitRepo.BulkCreateAsync(toCreate);
        return (true, null, toCreate.Count);
    }

    private static VisitDto MapToDto(Visit v, List<Visit>? all)
    {
        string? prev = null, next = null;

        if (all is not null)
        {
            var idx = all.FindIndex(x => x.VisitId == v.VisitId);
            if (idx > 0) prev = all[idx - 1].VisitName;
            if (idx < all.Count - 1) next = all[idx + 1].VisitName;
        }

        return new VisitDto
        {
            VisitId = v.VisitId,
            EnrollmentId = v.EnrollmentId,
            VisitName = v.VisitName,
            VisitDate = v.VisitDate,
            VisitStatus = v.VisitStatus,
            PrevVisit = prev,
            NextVisit = next,
            PatientName = v.Enrollment?.Patient?.Name,
            ProtocolTitle = v.Enrollment?.ProtocolSite?.Protocol?.Title,
            SiteName = v.Enrollment?.ProtocolSite?.Site?.Name,
            EnrollmentStatus = v.Enrollment?.EnrollmentStatus
        };
    }
}