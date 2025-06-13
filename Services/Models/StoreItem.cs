namespace VenturaBot.Models
{
    public class StoreItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int BasePrice { get; set; }
        public int Stock { get; set; }
        public int Demand { get; set; }
        public string ImageUrl { get; set; }
        public bool IsHidden { get; set; } = false;
    }
}
