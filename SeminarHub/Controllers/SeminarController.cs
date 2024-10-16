﻿using Microsoft.AspNetCore.Authorization;
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
