// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared;
using static SemanticSwamp.Shared.Enums;

[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public EnumsController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("UploadLocalFileType")]
    public async Task<string> UploadLocalFileType()
    {
        var result = "";
        result += LocalFileTypes.Top5Movies.ToString() + ",";
        result += LocalFileTypes.SportsHistory.ToString() + ",";
        result += LocalFileTypes.TheOdyssey.ToString() + ",";
        return result;
    }
}