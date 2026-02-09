namespace SeniorProject.Models
{
    public class ProductGroupItem
    {
        public int Id { get; set; }

        public int ProductGroupId { get; set; }
        public int RawProductId { get; set; }
        public string MappedName { get; set; } = "";
    }
}
