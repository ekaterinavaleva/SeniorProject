using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace SeniorProject.Controllers
{
    [Authorize(Roles = "Admin, RetailManager")]
    public class RetailManagerController : Controller
    {
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile zipFile)
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
                zipFile.CopyTo(stream);
            }

            string extractPath = Path.Combine(uploadFolder, "extracted");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);

            ViewBag.Message = "Upload successful!";
            return View();
        }
    }
}
