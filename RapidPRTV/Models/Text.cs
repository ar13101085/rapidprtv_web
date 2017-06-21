using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RapidPRTV.Models
{
    public class Text
    {
        public int TextId { get; set; }
        public string TextContent { get; set; }
        public DateTime TextUploadTime { get; set; }
        public DateTime TextPublishTime { get; set; }
        public bool IsPublishNow { get; set; }
    }
}