using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;
using SeniorProject.Services;

namespace SeniorProject.Controllers
{
    [Authorize(Roles = "Admin, RetailManager")]
    public class RetailManagerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly ILogger<RetailManagerController> _logger;

        public RetailManagerController(ApplicationDbContext db, IBackgroundTaskQueue backgroundTaskQueue, ILogger<RetailManagerController> logger)
        {
            _db = db;
            _backgroundTaskQueue = backgroundTaskQueue;
            _logger = logger;
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile zipFile)
        {
            if (zipFile == null)
            {
                return View();
            }

            string root = Directory.GetCurrentDirectory();
            string uploadFolder = Path.Combine(root, "uploads");

            Directory.CreateDirectory(uploadFolder);

            string zipPath = Path.Combine(uploadFolder, "latest_upload.zip");

            using (var stream = new FileStream(zipPath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }

            _backgroundTaskQueue.QueueBackgroundWorkItem(zipPath);
            _logger.LogInformation($"File {zipFile.FileName} queued for background processing.");

            ViewBag.Message = "Upload successful!";
            return View();
        }
    }
}
