using System.Collections.Generic;
using System.Web.Mvc;
using webstie_BanSach.Models;

namespace webstie_BanSach.ViewModels
{
    public class CategoryCountViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Count { get; set; }
    }

    public class BookListViewModel
    {
        public List<Book> Books { get; set; }
        public List<CategoryCountViewModel> Categories { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string SelectedCategoryName { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int AllBooksCount { get; set; }
        public string SelectedCategoryCode { get; set; }
        public List<Book> BannerBooks { get; set; }
        public string SearchTerm { get; set; }
        public int? AuthorId { get; set; }
        public int? PublisherId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public string Stock { get; set; }
        public string Sort { get; set; }
        public List<SelectListItem> Authors { get; set; }
        public List<SelectListItem> Publishers { get; set; }
    }

    public class QuickViewModel
    {
        public Book Book { get; set; }
        public string Authors { get; set; }
        public string Publisher { get; set; }
        public string Category { get; set; }
    }

    public class ReviewItemViewModel
    {
        public string ReviewerName { get; set; }
        public byte Rating { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public System.DateTime CreatedAtUtc { get; set; }
    }

    public class ReviewPageViewModel
    {
        public long BookId { get; set; }
        public List<ReviewItemViewModel> Reviews { get; set; }
        public int? Rating { get; set; }
        public string Sort { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class BookDetailViewModel
    {
        public Book Book { get; set; }
        public string Authors { get; set; }
        public string Category { get; set; }
        public string Publisher { get; set; }
        public int[] RatingCounts { get; set; }
        public ReviewPageViewModel ReviewPage { get; set; }
        public List<Book> RelatedBooks { get; set; }
        public List<Book> BoughtTogether { get; set; }
    }
}
