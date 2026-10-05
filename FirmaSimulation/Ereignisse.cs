namespace FirmaSimualation;

public class Ereignis
{
    public string Beschreibung { get; init; } = "";
    public double NachfrageFaktor { get; init; } = 1.0;
    public double WerbeFaktor { get; init; } = 1.0;
    public double EinkaufspreisFaktor { get; init; } = 1.0;
    public decimal Sonderkosten { get; init; } = 0m;
}

public static class Ereignisse
{
    private static readonly Ereignis[] Alle =
    {
        new() {Beschreibung = "Ein Video über deinen Shop geht viral! Die Werbung wirkt doppelt.", WerbeFaktor = 2.0,},
        new() { Beschreibung = "Elektronik ist gerade gefragt (+30 % Nachfrage).", NachfrageFaktor = 1.30 },
        new() { Beschreibung = "Die Inflation macht die Leute Sparsam (-25 % Nachfrage).", NachfrageFaktor = 0.75 },
        new() { Beschreibung = "Dein Lieferant erhöht die Preise um 15 %.", EinkaufspreisFaktor = 1.15 },
        new() { Beschreibung = "Einkaufspreise fallen um 20 %.", EinkaufspreisFaktor = 0.80 },
        new() { Beschreibung = "Du hast einen Wasserschaden im Lager. Zusätzliche Abwicklungskosten von 600 €.", Sonderkosten = 600m },
        new() { Beschreibung = "Du hast ein defektes Lagerregal. Zusätzliche Abwicklungskosten von 350 €.", Sonderkosten = 350m },
        new() { Beschreibung = "Ein neuer Wettbewerber drängt in deine Nische (-15 % Nachfrage).", NachfrageFaktor = 0.85 },
    };

    public static Ereignis? Ziehe(Random rng) => rng.NextDouble() <= 0.30 ? Alle[rng.Next(Alle.Length)] : null;
}