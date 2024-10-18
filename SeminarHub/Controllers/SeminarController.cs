using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeminarHub.Data;
using SeminarHub.Data.Models;
using SeminarHub.Models;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Claims;
using static SeminarHub.Common.ModelConstants;

namespace SeminarHub.Controllers
{
    [Authorize]
    public class SeminarController : Controller
    {
        private readonly SeminarHubDbContext context;

        public SeminarController(SeminarHubDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var model = new SeminarViewModel();
            model.Categories = await GetCategories();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(SeminarViewModel model)
        { 
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            DateTime dateAndTime;

            if (!DateTime.TryParseExact(model.DateAndTime, DateAndTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateAndTime))
            {
                throw new InvalidOperationException("Invalid date or time format");
            }

            Seminar seminar = new Seminar
            {
                Topic = model.Topic,
                Lecturer = model.Lecturer,
                Details = model.Details,
                DateAndTime = dateAndTime,
                Duration = model.Duration.Value,
                CategoryId = model.CategoryId,
                OrganizerId = GetCurrentUserId() ?? string.Empty
            };

            await context.Seminars.AddAsync(seminar);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> All()
        {
            var model = await context.Seminars
                .Where(s => s.IsDeleted == false)
                .Select(s => new SeminarInfoViewModel
                {
                    Id = s.Id,
                    Topic = s.Topic,
                    Lecturer = s.Lecturer,
                    Details = s.Details,
                    DateAndTime = s.DateAndTime.ToString(DateAndTimeFormat),
                    Category = s.Category.Name,
                    Organizer = s.Organizer.UserName ?? string.Empty
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Joined()
        {
           string currentUserId = GetCurrentUserId() ?? string.Empty;
            var model = await context.SeminarsParticipants
                    .Where(sp => sp.ParticipantId == currentUserId)
                    .Include(sp => sp.Seminar)
                    .Where(sp => sp.Seminar.IsDeleted == false)
                    .Select(sp => new SeminarInfoViewModel
                    {
                        Id = sp.Seminar.Id,
                        Topic = sp.Seminar.Topic,
                        Lecturer = sp.Seminar.Lecturer,
                        Details = sp.Seminar.Details,
                        DateAndTime = sp.Seminar.DateAndTime.ToString(DateAndTimeFormat),
                        Category = sp.Seminar.Category.Name,
                        Organizer = sp.Seminar.Organizer.UserName ?? string.Empty
                    })
                    .AsNoTracking()
                    .ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Join(int id)
        {
           var seminar = await context.Seminars
                .Where(s => s.IsDeleted == false)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seminar == null)
            {
                return NotFound();
            }

            string currentUserId = GetCurrentUserId() ?? string.Empty;

            if (await context.SeminarsParticipants.AnyAsync(sp => sp.SeminarId == id && 
                sp.ParticipantId == currentUserId && 
                sp.Seminar.IsDeleted == false))
            {
                return NotFound();
            }

            var seminarParticipant = new SeminarParticipant
            {
                SeminarId = id,
                ParticipantId = currentUserId
            };

            await context.SeminarsParticipants.AddAsync(seminarParticipant);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Joined));
        }

        public async Task<IActionResult> Leave(int id)
        {
            var seminar = await context.Seminars
                .Where(s => s.IsDeleted == false)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seminar == null)
            {
                return NotFound();
            }

            string currentUserId = GetCurrentUserId() ?? string.Empty;

            var seminarParticipant = await context.SeminarsParticipants
                .FirstOrDefaultAsync(sp => sp.SeminarId == id && sp.ParticipantId == currentUserId && sp.Seminar.IsDeleted == false);

            if (seminarParticipant == null)
            {
                return NotFound();
            }

            context.SeminarsParticipants.Remove(seminarParticipant);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Joined));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await context.Seminars
                .Where(s => s.Id == id)
                .Where(s => s.IsDeleted == false)
                .AsNoTracking()
                .Select(s => new SeminarViewModel
                {
                    Topic = s.Topic,
                    Lecturer = s.Lecturer,
                    Details = s.Details,
                    DateAndTime = s.DateAndTime.ToString(DateAndTimeFormat),
                    Duration = s.Duration,
                    CategoryId = s.CategoryId,
                    Categories = GetCategories().Result
                })
                .FirstOrDefaultAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, SeminarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            DateTime dateAndTime;

            if (!DateTime.TryParseExact(model.DateAndTime, DateAndTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateAndTime))
            {
                ModelState.AddModelError(nameof(model.DateAndTime), "Invalid date or time format");

                return View(model);
            }

            Seminar? seminar = await context.Seminars
                .Where(s => s.IsDeleted == false)
                .FirstOrDefaultAsync(s => s.Id == id);

            seminar.Topic = model.Topic;
            seminar.Lecturer = model.Lecturer;
            seminar.Details = model.Details;
            seminar.DateAndTime = dateAndTime;
            seminar.Duration = model.Duration.Value;
            seminar.CategoryId = model.CategoryId;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await context.Seminars
                .Where(s => s.Id == id)
                .Where(s => s.IsDeleted == false)
                .AsNoTracking()
                .Select(s => new SeminarDetailsViewModel
                {
                    Id = s.Id,
                    Topic = s.Topic,
                    Lecturer = s.Lecturer,
                    Details = s.Details,
                    DateAndTime = s.DateAndTime.ToString(DateAndTimeFormat),
                    Duration = s.Duration,
                    Category = s.Category.Name,
                    Organizer = s.Organizer.UserName ?? string.Empty
                })
                .FirstOrDefaultAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await context.Seminars
                .Where(s => s.Id == id)
                .Where(s => s.IsDeleted == false)
                .Select(s => new SeminarDeleteViewModel
                {
                    Id = s.Id,
                    Topic = s.Topic,
                    DateAndTime = s.DateAndTime
                })
                .FirstOrDefaultAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(SeminarDeleteViewModel model)
        {
            var seminar = await context.Seminars
                .Where(s => s.Id == model.Id)
                .Where(s => s.IsDeleted == false)
                .FirstOrDefaultAsync();

            if (seminar == null)
            {
                return NotFound();
            }

            seminar.IsDeleted = true;
         
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        private async Task<List<Category>> GetCategories()
        {
            return await context.Categories.ToListAsync();
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
