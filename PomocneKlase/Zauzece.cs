using System;
using System.Collections.Generic;

namespace PomocneKlase
{
    [Serializable]

    public class Zauzece
    {
        public int BrParkinga { get; set; } //to je ustvari koj parking bira kao id
        public int BrMesta { get; set; }
        public string VremeNapustanja { get; set; }

        public List<string> AutoInfo { get; set; }

        public int[] VremeDolaska { get; set; }

    }
}


