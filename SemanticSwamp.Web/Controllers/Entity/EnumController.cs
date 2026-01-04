// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared;
using static SemanticSwamp.Shared.Enums;

[ApiController]
[Route("api/[controller]")]
public class EnumController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public EnumController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("UploadLocalFileType")]
    public async Task<List<string>> UploadLocalFileType()
    {
        var result = new List<string>();
        result.Add(LocalFileTypes.Top5Movies.ToString());
        result.Add(LocalFileTypes.SportsHistory.ToString());
        result.Add(LocalFileTypes.TheOdyssey.ToString());
        return result;
    }
}