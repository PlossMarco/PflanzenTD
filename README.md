# 🌱 Pflanzen TD *(Arbeitstitel)*

Ein Tower-Defense-Spiel mit **echten Pflanzen** als Verteidigern und **echten Gartenschädlingen** als Gegnern — mit Garten zum Pflegen und einem Kompendium voller botanischer Fakten.

> **Status:** 🚧 Frühe Entwicklung — Meilenstein **M1a** (erster spielbarer Prototyp) in Arbeit.

## Spielidee
- **Kampf:** Pflanzen auf festen Feldern setzen, Schädlingswellen abwehren. Jede Pflanze hat echte Eigenschaften: Die Brennnessel brennt, der Kaktus sticht, der Apfelbaum wirft Äpfel.
- **Garten:** Pflanzen zwischen den Kämpfen pflegen und dauerhaft stärken — nach echten Standortbedürfnissen.
- **Sorten & Verwandte:** Beim Ausbau zwischen echten Sorten wählen, z. B. Apfelbaum → Boskoop oder Gravensteiner.
- **Kompendium:** Steckbriefe zu jeder Pflanze und jedem Schädling — mit Fakten, und bei legendären Pflanzen auch dem Mythos dazu.

## Technik
| | |
|---|---|
| Engine | Unity 6.3 LTS |
| Rendering | Universal Render Pipeline (2D), isometrische Sprites |
| Sprache | C# |
| Plattformen (geplant) | PC, Android, iOS |
| Sprachen (geplant) | Deutsch, Englisch |
| Versionskontrolle | Git + Git LFS |

## Roadmap
- [ ] **M1a** — Erster Prototyp: ein Pfad, ein Turm, ein Gegner, Wellen
- [ ] **M1b** — Greybox-Kampf: Samen & Nährstoffe, Ausbau, Leak-Limit, Sterne
- [ ] **M2** — Alle Kampfmechaniken von Kapitel 1
- [ ] **M3** — Garten
- [ ] **M4** — Fortschrittssysteme
- [ ] **M5** — Grafik & Lokalisierung
- [ ] **M6** — Kapitel 1 „Vorgarten“ komplett spielbar
- [ ] **M7+** — Mobile, weitere Kapitel

## Projektstruktur
```
Assets/_Project/
  Scenes/             Spielszenen
  Scripts/            C#-Code
  Prefabs/            Türme, Gegner, Projektile
  ScriptableObjects/  Daten zu Pflanzen, Gegnern, Wellen, Leveln
  Art/                Grafiken
```

## Über das Projekt
Mein erstes Unity-Projekt und gleichzeitig mein Lernprojekt auf dem Weg zur **Unity Certified User**-Zertifizierung. Die Commit-Historie zeigt den Weg von null an.

---

**English:** A tower defense game featuring real plants as defenders and real garden pests as enemies, with a garden care layer and a botanical compendium. Built with Unity 6.3 LTS (URP 2D, C#). Early development.

## Lizenz
© 2026 Marco Ploß. Alle Rechte vorbehalten — siehe [LICENSE](LICENSE).
