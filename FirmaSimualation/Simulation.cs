using FirmaSim;

namespace FirmaSimualation;

public static class Simulation
{
    public const int KapazitätProMitarbeiter = 120;
    public const decimal GehaltProMitarbeiter = 600m;
    public const decimal Einstellungskosten = 400m;
    public const decimal Kündigungskosten = 800m;
    public const decimal FixKosten = 400m;
    public const decimal LagerkostenProStück = 0.50m;
    public const double Kundenbindung = 0.60;
    public const double Werbeeffizienz = 6.0; 

    
    public static MonatsErgebnis SimuliereMonat(Unternehmen u, Random rng)
    {
        var Merg = new MonatsErgebnis { Monat = u.Monat };

        //----------- 1. Zufallseregnis -----------------
        double nachfrageMod = 1.0, werbeMod = 1.0;
        var e = Ereignisse.Ziehe(rng);
        if (e != null)
        {
            Merg.Ereignis = e.Beschreibung;
            nachfrageMod *= e.NachfrageFaktor;
            werbeMod *= e.WerbeFaktor;
            Merg.Sonderkosten += e.Sonderkosten;
            foreach(var p in u.Produkte)
            {
                p.Einkaufspreis = Math.Round(p.Einkaufspreis * (decimal)e.EinkaufspreisFaktor, 2);
            }
            
        }

        //------------- 2. Marketing (wie viele Kunden werden erreicht) -------------------
        int aktiveProdukte = u.Produkte.Count(p => p.ImSortiment);

        double neueKunden = Werbeeffizienz
            * Math.Sqrt((double)u.Werbebudget)
            * werbeMod
            * u.Ruf
            * (1.0 + 0.08 * Math.Max(0, aktiveProdukte - 1))
            * (0.85 + rng.NextDouble() * 0.30);

        Merg.NeueKunden = (int)Math.Floor(Math.Max(0, neueKunden));
        u.Kundenstamm = u.Kundenstamm * Kundenbindung + Merg.NeueKunden;
        Merg.AktiveKunden = (int)Math.Floor(Math.Max(0, u.Kundenstamm) * (0.40 + rng.NextDouble() * 0.80));

        //-------------- 3. Nachfrage je Produkt (Preis-Absatz-Funktion) -------------------
        var nachfrage = new Dictionary<Produkt, int>();
        foreach (var p in u.Produkte.Where(p => p.ImSortiment))
        {
            if (p.Verkaufspreis <= 0) { nachfrage[p] = 0; }

            double preisFaktor = Math.Pow((double)(p.Referenzpreis / p.Verkaufspreis), p.Elastizität);
            preisFaktor = Math.Clamp(preisFaktor, 0.02, 3.0);

            double menge = Merg.AktiveKunden
                * p.Nachfrage
                * preisFaktor
                * nachfrageMod
                * (0.90 + rng.NextDouble() * 0.20);

            nachfrage[p] = (int)Math.Round(Math.Max(0, menge));
        }

        //--------------- 4. Kapazitätsgrenze der Mitarbeiter ---------------------
        int gesamtNachfrage = nachfrage.Values.Sum();
        double kapaFaktor = 1.0;
        if (gesamtNachfrage > u.Kapazität && gesamtNachfrage > 0)
        {
            kapaFaktor = (double)u.Kapazität / gesamtNachfrage;
            Merg.KapazitätsEngpass = gesamtNachfrage - u.Kapazität;
        }

        //-------------- 5. Verkaufen ----------------------------------
        int nichtLieferbarerGesamt = 0;
        foreach(var (p, val) in nachfrage)
        {
            int bedarf = (int)Math.Floor(val * kapaFaktor);
            int verkauft = Math.Min(bedarf, p.Lagerbestand);
            int fehlt = bedarf - verkauft;
            nichtLieferbarerGesamt += fehlt;

            decimal umsatz = verkauft * p.Verkaufspreis;
            decimal warenEinsatz = p.Ausbuchen(verkauft);

            Merg.Umsatz += umsatz;
            Merg.Wareneinsatz += warenEinsatz;
            Merg.Produkte.Add(new ProduktErgebnis
            {
                Name = p.Name,
                Nachfrage = val,
                Verkauft = verkauft,
                NichtLieferbar = fehlt,
                Umsatz = umsatz,
                Wareneinsatz = warenEinsatz,
            });
        }

        //----------------- 6. Ruf anpassen -------------------------
        if (gesamtNachfrage > 0 && (nichtLieferbarerGesamt + Merg.KapazitätsEngpass) > gesamtNachfrage * 0.10)
        {
            u.Ruf -= 0.08;
        }
        else
        {
            u.Ruf += 0.04;
        }
        u.Ruf = Math.Clamp(u.Ruf, 0.50, 1.20);

        //----------------- 7. Kosten ---------------------
        Merg.Werbung = u.Werbebudget;
        Merg.Personal = u.Mitarbeiter * GehaltProMitarbeiter;
        Merg.Fixkosten = FixKosten;
        Merg.Lagerkosten = Math.Round(u.Produkte.Sum(p => p.Lagerbestand) * LagerkostenProStück, 2);

        //---------------- 8. Abschluss ----------------
        u.Kapital = Math.Round(u.Kapital + Merg.Liquidität, 2);
        u.Werbebudget = 0m;
        u.Monat++;

        Merg.KapitalDanach = u.Kapital;
        u.Historie.Add(Merg);
        return Merg;
    }
}