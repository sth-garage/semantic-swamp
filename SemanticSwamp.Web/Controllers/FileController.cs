// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    // GET api/files/download?filename=example.txt
    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string filename)
    {
        // Basic security: Only allow downloading files from a specific folder
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        var filePath = Path.Combine(folder, filename);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        var contentType = "application/octet-stream";
        return File(memory, contentType, filename);
    }
}


/*
 * <!DOCTYPE html>
<html>
<head>
    <title>Download File via Fetch</title>
</head>
<body>
    <button id="downloadBtn">Download File via JS</button>

<script>
document.getElementById('downloadBtn').addEventListener('click', async () => {
    const filename = "example.txt"; // change as needed
    try {
        // Fetch the file. Adjust the URL as needed for your API.
        const response = await fetch(`/api/files/download?filename=${encodeURIComponent(filename)}`);
        if (!response.ok) {
            alert("Failed to download file!");
            return;
        }

        // Get filename from the Content-Disposition header if present
        let downloadFilename = filename;
        const disposition = response.headers.get('Content-Disposition');
        if (disposition && disposition.indexOf('filename=') !== -1) {
            let match = disposition.match(/filename="?([^"]+)"?/);
            if (match && match[1]) downloadFilename = match[1];
        }

        // Convert response to Blob
        const blob = await response.blob();

        // For modern browsers: create a link and trigger click
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = downloadFilename;
        document.body.appendChild(a);
        a.click();
        setTimeout(() => {
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        }, 100);

    } catch (err) {
        alert("Error: " + err);
    }
});
</script>
</body>
</html>
 */