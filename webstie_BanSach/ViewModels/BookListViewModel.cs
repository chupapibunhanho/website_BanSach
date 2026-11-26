using System.Collections.Generic;
using webstie_BanSach.Models;

namespace webstie_BanSach.ViewModels
{
    public class CategoryCountViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
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
    }
}
