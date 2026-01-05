using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public CollectionsController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("Simple")]
    public async Task<List<Collection>> GetSimple()
    {
        var result = _context.Collections;
        return result.ToList();
    }
}