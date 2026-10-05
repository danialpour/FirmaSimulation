using FirmaSimualation;

namespace FirmaSim;

public class Unternehmen
{
    public string Name { get; set; } = "";
    public int Monat { get; set; } = 1;
    public decimal Kapital { get; set; } = 10000m;
    public int Mitarbeiter { get; set; } = 1;
    public double Kundenstamm { get; set; }
    public double Ruf { get; set; } = 1.0;
    public decimal Werbebudget { get; set; }

    public List<Produkt> Produkte { get; set; } = new();
    public List<MonatsErgebnis> Historie { get; } = new();

    public decimal Lagerwert => Produkte.Sum(p => p.Lagerwert);
    public decimal Eigenkapital => Kapital + Lagerwert;
    public int Kapazität => Mitarbeiter * Simulation.KapazitätProMitarbeiter;

    public static Unternehmen Neu(string name)
    {
        var u = new Unternehmen { Name = name };

        u.Produkte.Add(new Produkt
        {
            Name = "Kopfhörer",
            Einkaufspreis = 25m,
            Verkaufspreis = 49m,
            Referenzpreis = 49m,
            Nachfrage = 0.62,
            Elastizität = 1.6,
        });
        u.Produkte.Add(new Produkt
        {
            Name = "USB-C-Kabel",
            Einkaufspreis = 3m,
            Verkaufspreis = 9m,
            Referenzpreis = 9m,
            Nachfrage = 0.85,
            Elastizität = 2.2,
        });
        u.Produkte.Add(new Produkt
        {
            Name = "Lautsprecher",
            Einkaufspreis = 40m,
            Verkaufspreis = 79m,
            Referenzpreis = 79m,
            Nachfrage = 0.33,
            Elastizität = 1.3,
        });
        u.Produkte.Add(new Produkt
        {
            Name = "Fernseher",
            Einkaufspreis = 190m,
            Verkaufspreis = 359m,
            Referenzpreis = 359m,
            Nachfrage = 0.25,
            Elastizität = 1.2,
        });

        u.Produkte[0].Einbuchen(40, u.Produkte[0].Einkaufspreis);
        u.Produkte[1].Einbuchen(100, u.Produkte[1].Einkaufspreis);
        u.Produkte[2].Einbuchen(20, u.Produkte[2].Einkaufspreis);

        return u;
    }
}

public class Produkt
{
    public string Name { get; init; } = "";
    public decimal Einkaufspreis { get; set; }
    public decimal Verkaufspreis { get; set; }
    public decimal Referenzpreis { get; set; }
    public int Lagerbestand { get; set; }
    public decimal Lagerwert { get; set; }
    public bool ImSortiment { get; set; } = true;
    public double Nachfrage { get; set; }
    public double Elastizität {  get; set; }
    public decimal Stückkosten => Lagerbestand > 0 ? Math.Round(Lagerwert / Lagerbestand, 2) : Einkaufspreis;
    public decimal Marge => Verkaufspreis - Stückkosten;

    public void Einbuchen(int menge, decimal preisProStück)
    {
        Lagerbestand += menge;
        Lagerwert += menge * preisProStück;
    }

    public decimal Ausbuchen(int menge)
    {
        if (menge <= 0) return 0m;
        if (menge >= Lagerbestand)
        {
            decimal ganz = Lagerwert;
            Lagerbestand = 0;
            Lagerwert = 0m;
            return ganz;
        }
        decimal kosten = Math.Round(Lagerwert / Lagerbestand * menge, 2);
        Lagerbestand -= menge;
        Lagerwert -= kosten;
        return kosten;
    }
}

public class ProduktErgebnis
{
    public string Name { get; set; } = "";
    public int Nachfrage { get; set; }
    public int Verkauft { get; set; }
    public int NichtLieferbar { get; set; }
    public decimal Umsatz { get; set; }
    public decimal Wareneinsatz { get; set; }
    public decimal Deckungsbeitrag => Umsatz - Wareneinsatz;
}

public class MonatsErgebnis
{
    public int Monat { get; init; }
    public string? Ereignis { get; set; }

    public int NeueKunden { get; set; }
    public int AktiveKunden { get; set; }
    public int KapazitätsEngpass { get; set; }
    public List<ProduktErgebnis> Produkte { get; set; } = new();
    public decimal Umsatz { get; set; }
    public decimal Wareneinsatz { get; set; }
    public decimal Werbung { get; set; }
    public decimal Personal { get; set; }
    public decimal Fixkosten { get; set; }
    public decimal Lagerkosten { get; set; }
    public decimal Sonderkosten { get; set; }
    
    public decimal GesamtKosten => Wareneinsatz + Werbung + Personal + Fixkosten
                                   + Lagerkosten + Sonderkosten;

    public decimal Gewinn => Umsatz - GesamtKosten;

    public decimal Liquidität => Umsatz - Werbung - Personal - Fixkosten - Lagerkosten - Sonderkosten;

    public decimal KapitalDanach {  get; set; }
}