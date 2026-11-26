namespace webstie_BanSach.Models
{
    public partial class Book
    {
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public long? SoldCount { get; set; }
        public int? SalesRank { get; set; }
        public string AuthorNames { get; set; }
    }
}
