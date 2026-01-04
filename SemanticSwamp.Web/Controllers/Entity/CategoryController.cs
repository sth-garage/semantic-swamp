// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public CategoriesController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("Simple")]
    public async Task<List<Category>> GetSimple()
    {
        var result = _context.Categories;
        return result.ToList();
    }
}