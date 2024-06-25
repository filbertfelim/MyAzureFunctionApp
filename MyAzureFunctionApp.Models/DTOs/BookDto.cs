using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Models.DTOs
{
    public class BookDto
    {
        public string Title { get; set; }
        public int AuthorId { get; set; }
        public List<int> CategoryIds { get; set; }
    }
}