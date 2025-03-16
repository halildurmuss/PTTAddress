using PTTAddress.Models;

namespace PTTAddress.Models
{
    public class Subdistrict
    {
        public int SemtId { get; set; }
        public string SemtAdi { get; set; }
        public virtual List<Neighborhood> Mahalleler { get; set; }
    }
}