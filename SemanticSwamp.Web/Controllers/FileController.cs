// FilesController.cs
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared;
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


    [HttpPost("upload")]
    public async Task<IActionResult> Upload(FileUploadDTO input)

    {
        var returnMsg = "";

        DocumentUpload existing = _context.DocumentUploads.FirstOrDefault(x => x.IsActive && x.FileName == input.file.FileName);

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

    [HttpPost("UploadLocalFileType")]
    public async Task<IActionResult> UploadLocalFileType(UploadLocalPayload localFile)
    {
        var fileType = Enums.LocalFileTypes.Top5Movies;
        var localFileName = localFile.LocalFileName;

        switch (localFileName.ToLowerInvariant())
        {
            case "theodyssey":
                fileType = Enums.LocalFileTypes.TheOdyssey;
                break;
            case "sportshistory":
                fileType = Enums.LocalFileTypes.SportsHistory;
                break;
            case "top5movies":
            default:
                fileType = Enums.LocalFileTypes.Top5Movies;
                break;
        }

        var result = await _fileManager.GetTextFileSummaryFromPath(fileType);

        return Ok(result);
    }



}