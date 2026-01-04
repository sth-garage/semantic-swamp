// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using SemanticSwamp.Shared.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private SemanticSwampDBContext _context;
    private IFileManager _fileManager;

    public FileController(SemanticSwampDBContext context, IFileManager fileManager)
    {
        _context = context; 
        _fileManager = fileManager;
    }


    //// GET api/files/download?filename=example.txt
    //[HttpGet("download")]
    //public async Task<IActionResult> Download([FromQuery] string filename)
    //{
    //    // Basic security: Only allow downloading files from a specific folder
    //    var folder = Path.Combine(Directory.GetCurrentDirectory(), "Files");
    //    var filePath = Path.Combine(folder, filename);

    //    if (!System.IO.File.Exists(filePath))
    //        return NotFound();

    //    var memory = new MemoryStream();
    //    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    //    {
    //        await stream.CopyToAsync(memory);
    //    }
    //    memory.Position = 0;
    //    var contentType = "application/octet-stream";
    //    return File(memory, contentType, filename);
    //}


    [HttpPost("upload")]
    public async Task<IActionResult> Upload(FileUploadDTO input)

    {
        var returnMsg = "";

        string existing = null; // _context.DocumentUploads.FirstOrDefault(x => x.IsActive && x.FileName == input.file.FileName);

        if (existing == null)
        {
            if (input != null
                && input.file != null
                && input.file.Length != 0)
            {

                DocumentUpload documentUpload = await _fileManager.ProcessUpload(input);

                
                returnMsg = input.file.FileName + " was successfully uploaded";
            }
            else
            {
                returnMsg = "There was a problem with the input file or type provided";
            }
        }
        else
        {
            returnMsg = input.file.FileName + " has already been added";
        }

        return Ok(returnMsg);

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