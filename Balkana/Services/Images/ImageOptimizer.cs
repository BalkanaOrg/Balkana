using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Balkana.Services.Images
{
    public static class ImageOptimizer
    {
        /// <summary>
        /// Saves an uploaded image as WebP, optionally resizing to fit max dimensions.
        /// </summary>
        /// <returns>Web-root relative url (starting with /uploads/...)</returns>
        public static async Task<string> SaveWebpAsync(
            IFormFile file,
            string webRootPath,
            string relativeFolder,
            int? maxWidth = null,
            int? maxHeight = null,
            int quality = 85)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Length == 0) throw new InvalidOperationException("Empty file.");

            var uploadsPath = Path.Combine(webRootPath, relativeFolder);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}.webp";
            var finalPath = Path.Combine(uploadsPath, fileName);

            using var image = await Image.LoadAsync(file.OpenReadStream());

            // resize if larger than allowed while preserving aspect ratio
            if (maxWidth.HasValue || maxHeight.HasValue)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth ?? 0, maxHeight ?? 0)
                };
                image.Mutate(x => x.Resize(resizeOptions));
            }

            var encoder = new WebpEncoder
            {
                FileFormat = WebpFileFormatType.Lossy,
                Quality = quality
            };

            await image.SaveAsync(finalPath, encoder);

            var normalizedFolder = relativeFolder.Replace('\\', '/').Trim('/');
            return $"/{normalizedFolder}/{fileName}";
        }
    }
}

