using System.Collections.Generic;

namespace Vortex {
    public class Spell {
        public string Name { get; set; }
        public string School { get; set; }
        public string SchoolFull { get; set; }
        public IDictionary<string, int> Level { get; set; }
        public string CastingTime { get; set; }
        public string Components { get; set; }
        public string Range { get; set; }
        public string Zone { get; set; }
        public string Effect { get; set; }
        public string Target { get; set; }
        public string Duration { get; set; }
        public string SavingThrow { get; set; }
        public string SpellResistance { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string Source { get; set; }

    }
}