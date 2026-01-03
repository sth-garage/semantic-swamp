namespace SemanticSwamp.Web.DTOs
{
    public class FileUploadDTO
    {
        public IFormFile file { get; set; }
        public int categoryId { get; set; } = 1;
        public int collectionId { get; set; } = 1;
        public string newCollectionName { get; set; } = "";
        public string newCategoryName { get; set; } = "";
        public List<string> newTermNames { get; set; } = new List<string>();
        public List<int> termIds { get; set; } = new List<int>();
    }
}
