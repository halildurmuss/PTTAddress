using PTTAddress.Models;

namespace PTTAddress.Models
{
    public class Province
    {
        public int IlId { get; set; }
        public string IlAdi { get; set; }
        public virtual List<District> Ilceler { get; set; }
    }
}