using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyCodeCamp.Models
{
    public class CampModel
    {        
        public string Url { get; set; }          // mapping urls instead of using primary keys

        [Required]
        [MinLength(3)]
        [MaxLength(20)]
        public string Moniker { get; set; }

        [Required]
        [MinLength(5)]
        [MaxLength(100)]
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        [MinLength(25)]
        [MaxLength(4095)]
        public string Description { get; set; }
        //public Location Location { get; set; }

        // Flatten out Location instance property in our CampModel AND Using AutoMapper
        public string LocationAddress1 { get; set; }
        public string LocationAddress2 { get; set; }
        public string LocationAddress3 { get; set; }
        public string LocationCityTown { get; set; }
        public string LocationStateProvince { get; set; }
        public string LocationPostalCode { get; set; }
        public string LocationCountry { get; set; }
    }
}
