using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PomocneKlase
{ 
    //NE TREBA SERIALIZABLE NE SALJEM GA
    public class Parking
    {

        public int BrojMesta { get; set; }
        public int BrojZauzetih { get; set; }

        public double Cena { get; set; }

        public Parking(int brojMesta, int brojZauzetih, double cena)
        {
            BrojMesta = brojMesta;
            BrojZauzetih = brojZauzetih;
            Cena = cena;
        }

    }
}
