// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;

[ApiController]
[Route("api/[controller]")]
public class DocumentUploadsController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public DocumentUploadsController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("Simple")]
    public async Task<List<DocumentUpload>> GetSimple()
    {
        var result = _context.DocumentUploads;
        return result.ToList();
    }
}