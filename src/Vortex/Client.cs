using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;

namespace Vortex {
    public class Client {

        private readonly IConfiguration _configuration = Configuration.Default.WithDefaultLoader();

        /// <summary>
        /// Get All spells from PF
        /// </summary>
        /// <returns></returns>
        public async Task<string> ScrapAllSpellsAsync() {
            // get all 3 pages' contents
            var lines = await GetSpellsAsync ().ConfigureAwait (false);

            var spells = lines.Select (x => {
                //var line = spells.SingleOrDefault (x => x.TextContent.StartsWith (name + " ("))/*.TextContent*/;
                var line = x.TextContent;
                var summary = line.Substring (line.IndexOf ("). ") + "). ".Length);

                // Pathfinder-RPG.Abondance de munitions.ashxPathfinder-RPG.Abondance%20de%20munitions.ashx

                var href = x.QuerySelectorAll ("a").First ().Attributes["href"].Value;
                var source = (href.StartsWith ("http")
                    ? string.Empty
                    : "www.pathfinder-fr.org/Wiki/") + href;
                return new Spell { Summary = summary, Source = source };
            });


            var tasks = spells.Select (GetSpellAsync);

            var spellBook = await Task
                .WhenAll (tasks)
                .ConfigureAwait (false);

            return NetJSON.NetJSON.Serialize (spellBook);
        }

        /// <summary>
        /// GetSpellsAsync downloads all 3 pages (A-D, E-O, P-Z) async
        /// Then scrap them all one by one to extract all spell
        /// </summary>
        /// <returns>All spells as a collection of html's li element</returns>
        private async Task<IEnumerable<IElement>> GetSpellsAsync() {
            string[] pageArguments = { string.Empty, " (suite)", " (fin)" };
            var tasks = pageArguments
                .Select (t => BrowsingContext
                    .New (_configuration)
                    //.OpenNewAsync()
                    .OpenAsync ($"http://www.pathfinder-fr.org/Wiki/Pathfinder-RPG.Liste%20des%20sorts{t}.ashx")
                ).ToList ();
            var res = await Task.WhenAll (tasks).ConfigureAwait (false);

            var l = new List<IElement> ();
            foreach (var task in res) {
                var document = task;

                var subspells = document
                    .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li > ul > li");

                document
                    .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li > ul > li")
                    .ToList ().ForEach (x => x.Remove ());

                var spells = document
                    .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li")
                    .Concat (subspells);

                l.AddRange (spells);
            }
            return l;
        }

        /// <summary>
        /// Download a specific spell's page async
        /// Then Parse it
        /// </summary>
        /// <param name="spell">An almost empty spell. It should contain a source and a summary.</param>
        /// <returns>An async filled spell</returns>
        private Task<Spell> GetSpellAsync(Spell spell) {
            Debugger.Break ();
            return Task.Run (async () => {
                var document = await BrowsingContext
                    .New (_configuration)
                    .OpenAsync (spell.Source)
                    .ConfigureAwait (false);

                return ParseSpellFromDocument (document, spell);
            });
        }

        /// <summary>
        /// parse a string content for the sake of debugging
        /// we should keep it for testing
        /// </summary>
        /// <param name="content">html page as string</param>
        /// <returns>A spell json serialized</returns>
        public string Parse(string content) {
            var document = new HtmlParser (_configuration).Parse (content);

            var spell = ParseSpellFromDocument (document, new Spell ());

            return NetJSON.NetJSON.Serialize (spell);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document">the html page</param>
        /// <param name="spell">An almost empty spell</param>
        /// <returns>A filled spell</returns>
        private static Spell ParseSpellFromDocument(IParentNode document, Spell spell) {
            spell.Name = document.QuerySelector (".pagetitle").TextContent;

            var div = document.QuerySelector ("#frmPrint");
            div.RemoveChild (div.LastChild); // rm script

            var html = div.InnerHtml.Split ("<br><br>");

            var parser = new HtmlParser ();
            var content = html
                .FirstOrDefault ()?.Trim ()
                .Split ("<br>", StringSplitOptions.RemoveEmptyEntries)
                .Select (x => x.Substring (x.IndexOf ("</b> ") + 5))
                .Select (x => parser.Parse (x).DocumentElement.TextContent)
                .ToArray ();

            var firstline = content[0].Split (" ; Niveau ", StringSplitOptions.RemoveEmptyEntries);
            spell.School = firstline[0].Contains (" ") ? firstline[0].Remove (firstline[0].IndexOf (' ')) : firstline[0];
            spell.SchoolFull = firstline[0];
            //spell.Level = firstline[1]
            //    .Split (", ", StringSplitOptions.RemoveEmptyEntries)
            //    .Select (x => x.Split (' ')) // no space sometime Bard6
            //    .ToDictionary (x => x[0], x => int.Parse (x[1]));
            spell.CastingTime = content[1];
            spell.Components = content[2];
            spell.Range = content[3];
            spell.Target = Find (content, "Cibles ", "Cible "); // should be line 4
            spell.Effect = Find (content, "Effet "); // should be line 4
            spell.Zone = Find (content, "Zone d'effet "); // should be line 4
            spell.Duration = content[5];
            var lastline = content[6].Split (" ; Résistance à la magie ", StringSplitOptions.RemoveEmptyEntries);
            spell.SavingThrow = lastline[0];
            spell.SpellResistance = lastline[1];

            spell.Description = string
                .Concat (html?.Skip (1).Select (x => parser.Parse (x).DocumentElement.TextContent)).Trim ();
            return spell;
        }

        public static string Find(IReadOnlyCollection<string> source, params string[] elements) {
            foreach (var s in source)
                foreach (var element in elements.Where (x => s.StartsWith (x)))
                    return s.Substring (element.Length);
            return string.Empty;
        }

        //public Task<Spell> ScrapSpellAsync(string name) {

        //    var firstLetter = char.ToLower (name[0]); // maybe useless => all uppercase

        //    // http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net
        //    // http://stackoverflow.com/questions/6286284/c-sharp-remove-accent-from-character

        //    // because é != e
        //    //Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);
        //    //firstLetter = Encoding.ASCII.GetString (Encoding.GetEncoding ("windows-1254").GetBytes (firstLetter.ToString()))[0];
        //    // http://stackoverflow.com/questions/33579661/encoding-getencoding-cant-work-in-uwp-app


        //    var page = firstLetter >= 'e' ? $"%20({(firstLetter >= 'o' ? "fin" : "suite")})" : string.Empty;
        //    string alphaspellUrl = $"http://www.pathfinder-fr.org/Wiki/Pathfinder-RPG.Liste%20des%20sorts{page}.ashx";

        //    //var document = await BrowsingContext
        //    //    .New (_configuration)
        //    //    .OpenAsync (alphaspellUrl)
        //    //    .ConfigureAwait (false);

        //    var document = new HtmlParser (_configuration).Parse ("source");

        //    var subspells = document
        //    .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li > ul > li");

        //    document
        //        .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li > ul > li")
        //        .ToList ().ForEach (x => x.Remove ());

        //    var spells = document
        //        .QuerySelectorAll ("#PageContentDiv > h2.separator + ul > li")
        //        .Concat (subspells);

        //    var line = spells.SingleOrDefault (x => x.TextContent.StartsWith (name + " (")).TextContent;
        //    var summary = line.Substring (line.IndexOf ("). ") + "). ".Length);

        //    return new Spell {
        //        Summary = summary
        //    };
        //}
    }
}
