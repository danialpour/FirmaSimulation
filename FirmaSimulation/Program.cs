using FirmaSimualation;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace FirmaSim;

public static class Program
{
    private const int Spieldauer = 12;
    private static readonly Random rng = new Random();
    private static Unternehmen u = null!;

    public static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch { }
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        Console.WriteLine("----------------------------------------------");
        Console.WriteLine("---- UNTERNEHMENSSIMULATION - Online Shop ----");
        Console.WriteLine("----------------------------------------------");
        Console.WriteLine("\nWie soll dein Unternehmen heißen?");
        string name = Console.ReadLine()?.Trim() is { Length: > 0 } s ? s : "Mein Shop";

        u = Unternehmen.Neu(name);
        Regeln();

        while (true)
        {
            if (u.Monat > Spieldauer) { Schlussbilanz("Die 12 Monate sind vorbei."); return; }
            if (u.Eigenkapital < 0) { Schlussbilanz("Deine Firma ist Insolvent."); return; }

            Kopfzeile();
            Console.WriteLine("[1] Kennzahlen & Sortiment      [6] Sortiment anpassen");
            Console.WriteLine("[2] Produkte einkaufen          [7] MONAT ABSCHLIESSEN");
            Console.WriteLine("[3] Preise festlegen                                  ");
            Console.WriteLine("[4] Werbebudget setzen                                ");
            Console.WriteLine("[5] Personal                    [0] Beenden");
            Console.Write("\nDeine Wahl: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": Kennzahlen(); break;
                case "2": Einkaufen(); break;
                case "3": PreiseSetzen(); break;
                case "4": WerbungSetzen(); break;
                case "5": Personal(); break;
                case "6": Sortiment(); break;
                case "7": MonatAbschliessen(); break;
                case "0": Console.WriteLine("Dein Unternehmen vermisst dich!"); return;
                default: Console.WriteLine("Ungültige Eingabe."); break;
            }
        }
    }



    //------------------------ Anzeige --------------------------------
    private static void Regeln()
    {
        Console.WriteLine("""
            SO FUNKTIONIERT ES:
            - Werbung bringt neue Kunden, aber mit abnehmender Wirkung.
              (500 Euro = 134 Kunden, 2000 Euro = 268 Kunden)
            - 60% der Kunden kommen im folgendem Monat wieder.
            - Der Preis steuert die Nachfrage.
            - Jeder Mitarbeiter schafft 120 Bestellungen pro Monat. Mehr
              Nachfrage als Kapazität = verlorene Umsätze und schlechter Ruf.
            - Leeres Lager kostet Ruf. Ruf wirkt auf die Werbung.
            - Ziel: nach 12 Monaten möglichst viel Eigenkapital haben.
            """);
    }

    private static void Kopfzeile()
    {
        Console.WriteLine();
        Console.WriteLine(new string('_', 64));
        Console.WriteLine(new string('-', 64));
        Console.WriteLine($" {u.Name} | Monat {u.Monat}/{Spieldauer}");
        Console.WriteLine($" Kapital {Geld(u.Kapital)} | Lager {Geld(u.Lagerwert)}");
        Console.WriteLine($" Eigenkapital {Geld(u.Eigenkapital)} | Mitarbeiter {u.Mitarbeiter} " +
            $"| Kunden {Math.Floor(u.Kundenstamm)} | Ruf {u.Ruf:P0}");
        Console.WriteLine(new string('_', 64));
        Console.WriteLine(new string('-', 64));
    }

    private static void Kennzahlen()
    {
        Console.WriteLine();
        Console.WriteLine($"{"#",-3}" +
            $"{"Produkt",-24}" +
            $"{"EK",11}" +
            $"{"VK",12}" +
            $"{"Bestand",11}" +
            $"{"Marge",12}");
        for (int i = 0; i < u.Produkte.Count; i++)
        {
            var p = u.Produkte[i];
            string sor = p.ImSortiment ? "" : " (ausgelistet)";
            Console.WriteLine($"{i + 1,-3}" +
                $"{p.Name + sor,-24}" +
                $"{Geld(p.Einkaufspreis),11}" +
                $"{Geld(p.Verkaufspreis),12}" +
                $"{p.Lagerbestand,11}" +
                $"{Geld(p.Marge),12}");
        }

        decimal pk = u.Mitarbeiter * Simulation.GehaltProMitarbeiter;
        decimal fk = Simulation.FixKosten;
        decimal lk = u.Produkte.Sum(p => p.Lagerbestand) * Simulation.LagerkostenProStück;

        decimal fmk = pk + fk + lk;
        Console.WriteLine($"\nKapazität: {u.Kapazität} Bestellungen/Monat " +
                         $"({u.Mitarbeiter} * {Simulation.KapazitätProMitarbeiter})");
        Console.WriteLine($"Fixe Monatskosten: Personal {Geld(u.Mitarbeiter * Simulation.GehaltProMitarbeiter)} " +
                          $"+ Betrieb {Geld(Simulation.FixKosten)} " +
                          $"+ Lager {Geld(u.Produkte.Sum(p => p.Lagerbestand) * Simulation.LagerkostenProStück)}" +
                          $" = {Geld(fmk)}");

        if (u.Historie.Count > 0)
        {
            Console.WriteLine("\nBisherige Monate:");
            Console.WriteLine($"{"Monat",-7}{"Umsatz",12}{"Gewinn",12}{"Kapital",14}");
            foreach (var h in u.Historie.TakeLast(6))
            {
                Console.WriteLine($"{h.Monat,-7}{Geld(h.Umsatz),12}{Geld(h.Gewinn),12}{Geld(h.KapitalDanach),14}");
            }
        }
    }

    //------------------------- AKTIONEN ------------------------------
    private static void Einkaufen()
    {
        var p = WähleProdukt("Welches Produkt einkaufen?");
        if (p is null) return;

        Console.WriteLine($"\n{p.Name}: Einkaufspreis {Geld(p.Einkaufspreis)}, Bestand {p.Lagerbestand}");
        Console.WriteLine($"Ab 100 Stück gibt es 5 % Mengenrabatt, ab 300 Stück 10 %.");
        int menge = LiesInt("Menge (0 = abbrechen): ", 0, 100000);
        if (menge is 0) return;

        decimal stückpreis = p.Einkaufspreis * (menge >= 300 ? 0.90m : menge >= 100 ? 0.95m : 1.0m);
        decimal kosten = Math.Round(stückpreis * menge, 2);

        if (kosten > u.Kapital)
        {
            Console.WriteLine($"Zu teuer: {Geld(kosten)} nötig, du hast {Geld(u.Kapital)}.");
            return;
        }

        Console.WriteLine($"{menge} * {Geld(stückpreis)} = {Geld(kosten)}. Kaufen? (j/n)");
        if (!JaNein()) return;

        u.Kapital -= kosten;
        p.Einbuchen(menge, stückpreis);
        Console.WriteLine($"Eingebucht. Neuer Bestand: {p.Lagerbestand}, Stückkosten {Geld(p.Stückkosten)}.");
    }

    private static void PreiseSetzen()
    {
        var p = WähleProdukt("Bei welchem Produkt den Preis ändern?");
        if (p is null) return;

        Console.WriteLine($"\n{p.Name}: aktuell {Geld(p.Verkaufspreis)}, Stückkosten {Geld(p.Stückkosten)}, " +
            $"Marktüblich {Geld(p.Referenzpreis)}");
        decimal neu = LiesDecimal("Neuer Verkaufspreis (0 = abbrechen): ", 0m, 1000000m);
        if (neu == 0m) return;

        p.Verkaufspreis = neu;
        if (neu <= p.Stückkosten)
        {
            Console.WriteLine("Achtung! Du verkaufst unter dem Einstandspreis. Du machst Verlust.");
        }
        Console.WriteLine($"Neue Marge: {Geld(p.Marge)} pro Stück");
    }

    private static void WerbungSetzen()
    {
        Console.WriteLine($"\nAktuelles Werbebudget für diesen Monat: {Geld(u.Werbebudget)}");
        decimal b = LiesDecimal($"Werbebudget (max. {Geld(u.Kapital)}):", 0m, Math.Max(0m, u.Kapital));
        u.Werbebudget = b;

        double prognose = Math.Round(Simulation.Werbeeffizienz * Math.Sqrt((double)b) * u.Ruf);
        Console.WriteLine($"Ungefähr erwartete neue Kunden: {prognose}. (es gibt noch andere Faktoren)");
    }

    private static void Personal()
    {
        Console.WriteLine($"\nMitarbeiter: {u.Mitarbeiter} │ Kapazität {u.Kapazität} Bestellungen/Monat");
        Console.WriteLine($"Gehalt {Geld(Simulation.GehaltProMitarbeiter)}/Monat, " +
                          $"Einstellung einmalig {Geld(Simulation.Einstellungskosten)}, " +
                          $"Kündigung {Geld(Simulation.Kündigungskosten)}.");
        Console.WriteLine("[1] Einstellen   [2] Entlassen   [0] Zurück");

        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                int ein = LiesInt("wie viele Einstellen? ", 0, 50);
                decimal kosten = ein * Simulation.Einstellungskosten;
                if (kosten > u.Kapital) { Console.WriteLine("Dafür reicht das Kapital nicht aus."); return; }
                u.Kapital -= kosten;
                u.Mitarbeiter += ein;
                Console.WriteLine($"{ein} neue Mitarbeiter. Neue Kapazität {u.Kapazität}.");
                break;

            case "2":
                int ent = LiesInt("Wie viele entlassen? ", 0, u.Mitarbeiter);
                decimal entK = ent * Simulation.Kündigungskosten;
                if (entK > u.Kapital) { Console.WriteLine("Dafür reicht das Kapital nicht aus."); return; }
                u.Kapital -= entK;
                u.Mitarbeiter -= ent;
                Console.WriteLine($"{ent} Mitarbeiter entlassen. Neue Kapazität {u.Kapazität}.");
                break;
        }
    }

    private static void Sortiment()
    {
        var p = WähleProdukt("Welches Produkt möchtest du umstellen?");
        if (p is null) return;

        p.ImSortiment = !p.ImSortiment;
        Console.WriteLine(p.ImSortiment ? $"{p.Name} ist wieder im Sortiment." :
            $"{p.Name} ist ausgelistet. {(p.Lagerbestand > 0 ? $"Der Restbestand ({p.Lagerbestand} Stück) verursacht weiter Lagerkosten." : $"")}");
    }

    //-------------------------- MONATSABSCHLUSS -----------------------------
    private static void MonatAbschliessen()
    {
        if (u.Werbebudget > u.Kapital)
        {
            u.Werbebudget = Math.Max(0m, u.Kapital);
        }

        int monat = u.Monat;
        var s = Simulation.SimuliereMonat(u, rng);

        Console.WriteLine($"\n⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪⟪ ABSCHLUSS MONAT {monat} " + new string('⟫', 30));
        if (s.Ereignis is not null) Console.WriteLine($"\n  ⚡{s.Ereignis}");

        Console.WriteLine($"\n  Neue Kunden: {s.NeueKunden} | Aktive Kunden: {s.AktiveKunden}");
        Console.WriteLine($"  {"Produkt",-24}" +
                $"{"Nachfrage",12}" +
                $"{"Verkauft",12}" +
                $"{"Umsatz",15}" +
                $"{"DB",15}");

        foreach (var pr in s.Produkte)
        {
            Console.WriteLine($"  {pr.Name,-24}" +
                $"{pr.Nachfrage,12}" +
                $"{pr.Verkauft,12}" +
                $"{Geld(pr.Umsatz),15}" +
                $"{Geld(pr.Deckungsbeitrag),15}");
        }


        int verloren = s.Produkte.Sum(x => x.NichtLieferbar);
        Console.WriteLine();
        if (verloren > 0) Console.WriteLine($"  {verloren} nicht lieferbare Bestellungen (kein Lagerbestand)");
        if (s.KapazitätsEngpass > 0) Console.WriteLine($"  {s.KapazitätsEngpass} Bestellungen wegen Personalmangel verloren (zu wenige Mitarbeiter)");

        Console.WriteLine($"\n  {"Umsatz",-22}{Geld(s.Umsatz),14}");
        Console.WriteLine($"  {"− Wareneinsatz",-22}{Geld(s.Wareneinsatz),14}");
        Console.WriteLine($"  {"− Werbung",-22}{Geld(s.Werbung),14}");
        Console.WriteLine($"  {"− Personal",-22}{Geld(s.Personal),14}");
        Console.WriteLine($"  {"− Betriebskosten",-22}{Geld(s.Fixkosten),14}");
        Console.WriteLine($"  {"− Lagerkosten",-22}{Geld(s.Lagerkosten),14}");
        if (s.Sonderkosten > 0) Console.WriteLine($"  {"− Sonderkosten",-22}{Geld(s.Sonderkosten),14}");
        Console.WriteLine("  " + new string('-', 36));
        Console.WriteLine($"  {"= GEWINN",-22}{Geld(s.Gewinn),14}");
        Console.WriteLine($"\n  Kapital: {Geld(s.KapitalDanach)}");
        Console.WriteLine(new string('⟪', 35) + new string('⟫', 35));
        Console.WriteLine("\n(Enter für den nächsten Monat)");
        Console.ReadLine();
    }

    private static void Schlussbilanz(string grund)
    {
        Console.WriteLine($"\nSPIELENDE: {grund}");
        Console.WriteLine($"EigenKapital: {Geld(u.Eigenkapital)} (Start: {Geld(10000m)})");
        decimal umsatzGesamt = u.Historie.Sum(h => h.Umsatz);
        Console.WriteLine($"Gesamtumsatz: {Geld(umsatzGesamt)} über {u.Historie.Count} Monate");

        string note = u.Eigenkapital switch
        {
            < 0 => "Versuche es nochmal.",
            < 10000m => "Kapital verbrannt, aber du hast es versucht.",
            < 20000m => "Solides Unternehmertum🫡",
            < 50000m => "Klasse💪",
            _ => "Zeit für die Großen Ligen 🏆",
        };
        Console.WriteLine(note);
    }

    //------------------------ HELFER FUNKTIONEN -------------------------------
    private static string Geld(decimal betrag) => betrag.ToString("N2", CultureInfo.CurrentCulture) + " €";

    private static int LiesInt(string prompt, int min, int max, bool bereitsGefragt = false)
    {
        while (true)
        {
            if (!bereitsGefragt) Console.Write(prompt);
            bereitsGefragt = true;

            if (int.TryParse(Console.ReadLine()?.Trim(), out int wert) && wert >= min && wert <= max)
            {
                return wert;
            }

            Console.WriteLine($"Bitte eine ganze Zahl zwischen {min} und {max} angeben: ");
            bereitsGefragt = true;
        }
    }
    private static Produkt? WähleProdukt(string frage)
    {
        Console.WriteLine();
        for (int i = 0; i < u.Produkte.Count; i++)
        {
            Console.WriteLine($" [{i + 1}] {u.Produkte[i].Name}");
        }
        Console.WriteLine($"{frage} (0 = abbrechen) ");

        int wahl = LiesInt("", 0, u.Produkte.Count, bereitsGefragt: true);
        return wahl is 0 ? null : u.Produkte[wahl - 1];
    }

    private static bool JaNein()
    {
        string a = (Console.ReadLine() ?? "").Trim().ToLower();
        return a is "j" or "ja" or "y" or "yes" or "ye";
    }

    private static decimal LiesDecimal(string prompt, decimal min, decimal max)
    {
        while (true)
        {
            Console.Write(prompt);
            string eingabe = (Console.ReadLine() ?? "").Trim().Replace("€", "").Trim().Replace(".", ",");

            if (decimal.TryParse(eingabe, NumberStyles.Any, CultureInfo.GetCultureInfo("de-De"), out decimal wert))
            {
                if (wert >= min && wert <= max) return Math.Round(wert, 2);
            }
            Console.WriteLine($"Bitte gib einen Betrag zwischen {Geld(min)} und {Geld(max)} and.");
        }
    }
}