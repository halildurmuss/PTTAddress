using PTTAddress.Models;

namespace PTTAddress.Models
{
    public class District
    {
        public int IlceId { get; set; }
        public string IlceAdi { get; set; }
        public virtual List<Subdistrict> Semtler { get; set; }
    }
}