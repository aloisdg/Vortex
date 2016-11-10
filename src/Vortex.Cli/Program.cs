using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Vortex.Cli {
    public class Program {
        public static void Main(string[] args) {
            Client client = new Client ();

            //var content = File.ReadAllText (@"C:\Users\alois\Downloads\PF\Assistance divine - Wikis Pathfinder-fr.htm");
            //var spells = client.Parse (content);
            //Console.WriteLine (spells);

            Task.Run (async () => {
                var spellbook = await client.ScrapAllSpellsAsync ();
                Console.WriteLine (spellbook);
            }).Wait ();
            Console.ReadLine ();
        }
    }
}
