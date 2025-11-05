using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SGR.Api.Controllers.Backoffice;

[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController : ControllerBase
{
    [HttpPost("avatar")]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo não enviado" });

        if (file.ContentType != "image/png" && file.ContentType != "image/jpeg")
            return BadRequest(new { message = "Apenas PNG ou JPG" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return BadRequest(new { message = "Extensão inválida" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var name = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(uploadsDir, name);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/avatars/{name}";
        return Ok(new { url });
    }

    [HttpDelete("avatar")]
    public IActionResult DeleteAvatar([FromQuery] string? url, [FromQuery] string? name)
    {
        var fileName = name;
        if (string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(url))
        {
            try { fileName = Path.GetFileName(new Uri(url).AbsolutePath); } catch { }
        }
        if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(new { message = "Parâmetro inválido" });

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") return BadRequest(new { message = "Extensão inválida" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
        var path = Path.Combine(uploadsDir, fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return NoContent();
    }
}
