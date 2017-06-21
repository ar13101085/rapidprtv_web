using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RapidPRTV.Models
{
    public class Advertise
    {
        public int AdvertiseId { get; set; }
        public DateTime AdvertiseUploadTime { get; set; }
        public DateTime AdvertisePublishTime { get; set; }
        public string AdvertiseName { get; set; }
        public int LiveDurationInSec { get; set; }
        public string BoxName { get; set; }
        public bool IsPublish { get; set; }

        
        public string GetPath()
        {
            string[] res = AdvertiseName.Split('.');
            return "Advertise/" + AdvertiseId+"."+res[1];
        }
    }
}