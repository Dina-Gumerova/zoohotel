using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Зоогостиница_диплом_
{
    public class FullBooking
    {
        public object BookingId { get; internal set; }       
        public string OwnerName { get; set; }
        public string PetName { get; set; }
        public string Breed { get; set; }
        public string Size { get; set; }
        public decimal Weight { get; set; }
        public int Age { get; set; }
        public int Сell_number { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type_animal { get; set; }

    }

}
