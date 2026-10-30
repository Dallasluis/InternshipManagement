namespace InternshipManagement.Web.Services
{
    public sealed class UploadedFileStorage
    {
        private const int SignatureLength = 12;
        private readonly IWebHostEnvironment _environment;

        public UploadedFileStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(
            IFormFile file,
            string folder,
            IReadOnlyCollection<string> allowedExtensions,
            long maxFileSize,
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (file.Length <= 0 || file.Length > maxFileSize || !allowedExtensions.Contains(extension))
                throw new InvalidDataException("Choose a supported file within the allowed size limit.");

            var signature = new byte[SignatureLength];
            await using (var input = file.OpenReadStream())
            {
                if (input.Length < SignatureLength)
                    throw new InvalidDataException("The selected file is incomplete.");

                await input.ReadExactlyAsync(signature, cancellationToken);
                if (!HasValidSignature(extension, signature))
                    throw new InvalidDataException("The selected file does not match its file type.");
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, fileName);
            await using var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(output, cancellationToken);

            return $"/uploads/{folder}/{fileName}";
        }

        private static bool HasValidSignature(string extension, ReadOnlySpan<byte> signature) =>
            extension switch
            {
                ".pdf" => signature.StartsWith("%PDF-"u8),
                ".jpg" or ".jpeg" => signature.Length >= 3
                    && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
                ".png" => signature.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => signature.Length >= 12
                    && signature[..4].SequenceEqual("RIFF"u8)
                    && signature[8..12].SequenceEqual("WEBP"u8),
                ".doc" => signature.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
                ".docx" => signature.StartsWith("PK\u0003\u0004"u8),
                _ => false
            };
    }
}
