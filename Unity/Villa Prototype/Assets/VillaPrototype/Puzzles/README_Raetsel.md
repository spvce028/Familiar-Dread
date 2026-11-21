# Rätselkette – 2. OG / Dachboden

- Szene: Assets/Scenes/VillaPrototype.unity
- Hierarchie: VILLA_PROTOTYPE/Raetsel_2OG
- Notiz: rechter Bereich des Büroschreibtischs
- Familienfoto: Kommode im Elternschlafzimmer
- PC: vorhandener Monitor und Tastatur im Büro; VillaPuzzleObject als Instanz-Erweiterung
- Dachboden: drei Kisten; blaue Umzugskiste 2016 enthält Kassette
- Rekorder: bestehendes Gerät im dritten Schlafzimmer

- Bedienung: anvisieren, E; lesen/PC mit Maus; Esc schließt; Bewegung während Dokument/PC gesperrt
- Passwortlösung: Rügen2012; akzeptiert auch Rugen2012 und Ruegen2012, Groß-/Kleinschreibung und Leerzeichen egal
- PC-Funktionen nach Anmeldung: E-Mail lesen, ausschalten
- Kassette: erst finden, dann am Rekorder starten/stoppen; Transkript während Wiedergabe
- Kisten/Kassette lassen sich auch vor dem PC entdecken; kein künstlicher Fortschrittszwang
- Fortschritt gilt nur für den laufenden Spielstart; kein Speichersystem
- Bestehende geheime Kellertür wird nur angedeutet; keine neue automatische Türöffnung

- Assets/Scripts/Villa/Puzzles/VillaPuzzleState.cs: Texte, Passwort, Bild, Audio und Laufzeitfortschritt
- Assets/Scripts/Villa/Puzzles/VillaPuzzleObject.cs: Notiz, Foto, PC, Kistendeckel, Kassette
- Assets/Scripts/Villa/Puzzles/VillaPuzzleUI.cs: Dokumentansicht, Anmeldung, Mail und Transkript
- Assets/Scripts/Villa/VillaExplorer.cs: bestehende E-Ziele um Puzzleobjekte und Eingabesperre erweitert
- Assets/Scripts/Villa/VillaCassettePlayer.cs: optionaler Puzzle-Bezug; vorhandene MP3 bleibt zugewiesen
- Assets/VillaPrototype/Puzzles/: Materialien und einfaches Urlaubsbild
- Assets/VillaPrototype/Puzzles/Audio/Kassette_Papa_Prototyp.wav: synthetische deutsche Platzhalterstimme, später ersetzen
- Texte, Passwort, Foto und Audio im Inspector am VillaPuzzleState unter Raetsel_2OG ändern
- Bei geändertem Sprachinhalt auch Transkript und Audiodatei gemeinsam aktualisieren
