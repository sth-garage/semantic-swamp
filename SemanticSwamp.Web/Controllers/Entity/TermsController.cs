using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;

[ApiController]
[Route("api/[controller]")]
public class TermsController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public TermsController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("Simple")]
    public async Task<List<Term>> GetSimple()
    {
        var result = _context.Terms;
        return result.ToList();
    }
}