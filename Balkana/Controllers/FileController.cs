using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    [Route("file")]
    [IgnoreAntiforgeryToken]
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public FileController(IWebHostEnvironment env) => _env = env;

        [HttpPost("upload")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Upload([FromForm] IFormFile upload)
        {
            Console.WriteLine("_env.WebRootPath = " + _env.WebRootPath);

            if (upload == null || upload.Length == 0)
                return BadRequest("No file uploaded");

            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "News");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(upload.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await upload.CopyToAsync(stream);

            var url = Url.Content($"~/uploads/news/{fileName}");
            return Json(new { url });
        }
    }
}
