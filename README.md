# XiPHiAS.CalcLauncher

Kleine C# Windows-app zonder console, bedoeld voor de calculator-toets van de Logitech MX Keys S. Vereist Windows 11 met Microsoft Windows Calculator. Start de app als gewone gebruiker.

## Openen in VS Code

Open de map `D:\Users\michael\GitHub\xiphias-calclauncher` via **File > Open Folder**. Een .NET 10 SDK is nodig om te bouwen. C#-ondersteuning in VS Code is handig voor bewerken; bouwen kan rechtstreeks in de geïntegreerde PowerShell-terminal.

Alle onderstaande opdrachten voer je uit in die projectmap.

```powershell
dotnet build .\XiPHiAS.CalcLauncher.csproj -c Release
```

## Publiceren

Kleine, enkele EXE voor een x64-pc waarop de .NET 10 runtime aanwezig is:

```powershell
dotnet publish .\XiPHiAS.CalcLauncher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\win-x64
```

Gebruik daarna `publish\win-x64\XiPHiAS.CalcLauncher.exe`. Deze EXE bevat geen eigen .NET runtime. De .NET 10 SDK installeert ook een geschikte runtime.

Voor een pc zonder .NET: onderstaande variant bundelt de runtime en is daardoor aanzienlijk groter. Native runtimebestanden worden bij gebruik uitgepakt onder de standaard .NET extractiemap van de gebruiker.

```powershell
dotnet publish .\XiPHiAS.CalcLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\standalone-win-x64
```

Voor Windows op ARM vervang je `win-x64` door `win-arm64` en kies je een aparte uitvoermap. Publiceren kan internettoegang vereisen om runtimepacks op te halen. Bij runtime-updates moet je de standalone variant opnieuw publiceren.

## Logitech-toets instellen

Selecteer in Logi Options+ de MX Keys S en de calculator-toets. Kies, als jouw versie die actie aanbiedt, het openen van een toepassing en selecteer de gepubliceerde `XiPHiAS.CalcLauncher.exe` via het volledige pad. De benaming van die actie kan verschillen per Options+-versie. Gebruik de algemene toewijzing en controleer eventuele app-specifieke toewijzingen.

De toets moet uitsluitend deze launcher starten. Laat geen tweede toewijzing actief die daarnaast rechtstreeks Calculator opent. Kies de EXE zelf, zonder een PowerShell- of terminalvenster ertussen. Bewaar de publicatiemap op een vaste locatie. Er draait geen achtergrondservice; de launcher sluit na iedere toetsdruk af.

## Werking en grenzen

- Een sessiegebonden mutex laat maar één launcher tegelijk starten. Tijdens het wachten op Calculator worden volgende launcher-aanroepen genegeerd.
- De launcher zoekt zichtbare hoofdvensters op proces-app-identiteit (`Microsoft.WindowsCalculator_8wekyb3d8bbwe!App`), onafhankelijk van de venstertitel of Windows-taal. Hij onderzoekt ook kindvensters: bij oudere Store-hosting kan het hoofdvenster van `ApplicationFrameHost.exe` zijn en het kindvenster van Calculator. Het hoofdvenster wordt geactiveerd.
- Bij meerdere bestaande Calculator-vensters wordt de instantie met de nieuwste processtarttijd hergebruikt. De launcher leest ook de app-identiteit van het hostvenster zelf, slaat door DWM verborgen (cloaked) vensters over en koppelt hostvensters via kindvensters of owned helpervensters aan het Calculator-proces. Bij gelijke/onbekende starttijd geldt de bestaande venstervolgorde. Als een Calculator-versie meerdere vensters in hetzelfde proces onderbrengt, kan hun onderlinge aanmaaktijd hiermee niet worden bepaald. De launcher sluit geen vensters en bewaart bestaande berekeningen.
- Een bestaand venster wordt hersteld indien geminimaliseerd en via `SetForegroundWindow` geactiveerd. Windows stuurt toetsenbordinvoer vervolgens naar de actieve app. Er worden geen cijfers of andere toetsen gesimuleerd en de bestaande invoerfocus binnen Calculator wordt behouden. Als bijvoorbeeld een zoekveld in Calculator actief was, kan dat veld actief blijven.
- Als geen venster bestaat, controleert hij of Calculator nog start. Anders start hij precies eenmaal de systeem-`calc.exe` en wacht maximaal 15 seconden op het echte venster. Het proces van `calc.exe` hoeft zelf geen hoofdvenster te krijgen: het kan alleen een doorverwijzer zijn.
- Windows beslist of focusoverdracht is toegestaan. De launcher probeert kort opnieuw en controleert de daadwerkelijke voorgrond. Bij weigering knippert de taakbalkknop en wordt een logregel geschreven. Automatische focus is daardoor niet onder alle omstandigheden gegarandeerd, bijvoorbeeld vanuit Options+, bij actieve menu's of een vergrendelde sessie. Er worden geen focusinstellingen in het register aangepast. `AttachThreadInput` wordt bewust vermeden omdat gekoppelde invoerwachtrijen kunnen vastlopen.
- De mutex voorkomt dubbele starts door deze launcher. Hij kan niet voorkomen dat een andere toepassing of een gelijktijdige handmatige actie ook Calculator opent. Een vastgelopen of vensterloos Calculator-proces wordt niet beëindigd; sluit het zelf via Taakbeheer als het starten blijft hangen.
- Virtuele bureaubladen en beveiligde bureaubladen worden niet beheerd. Test op hetzelfde gewone bureaublad. Voer ook Options+, Calculator en de launcher op hetzelfde normale rechtenniveau uit. Er is geen administratorrecht nodig.

## Handmatig testen

Voer eerst de gepubliceerde EXE uit via Verkenner, daarna via de toegewezen Logitech-toets. Typ pas nadat Calculator zichtbaar vooraan staat.

| Situatie | Actie en verwacht resultaat |
| --- | --- |
| Geen Calculator open | Sluit alle Calculator-vensters. Druk eenmaal: één venster verschijnt. Typ `12+3` en Enter: resultaat 15. |
| Calculator achter een andere app | Laat een herkenbare berekening staan, schakel naar Kladblok, druk de toets: hetzelfde venster en dezelfde berekening komen vooraan. Typen gaat naar Calculator. |
| Geminimaliseerd | Minimaliseer Calculator en druk de toets: hetzelfde venster wordt hersteld en ontvangt invoer. |
| Gemaximaliseerd | Maximaliseer Calculator, wissel van app en druk de toets: de venstergrootte blijft behouden. |
| Snel herhalen | Begin zonder Calculator en druk 10 keer snel: één Calculator-venster. Herhaal met een bestaand venster. |
| Meerdere vensters vooraf | Open drie Calculator-vensters na elkaar met verschillende berekeningen. Breng het oudste naar voren, wissel naar Kladblok en druk de toets: de laatst geopende Calculator komt vooraan, zonder vierde venster. Minimaliseer die en herhaal. Sluit vervolgens de nieuwste: de eerstvolgende nieuwste wordt gebruikt. |
| Andere taal of rekenmodus | Test bijvoorbeeld Wetenschappelijk en een andere Windows-taal: herkenning blijft werken zonder titelvergelijking. |
| Focus geweigerd | Test vanuit een app met een geopend menu. Windows mag de focus weigeren: er komt geen extra Calculator; controleer taakbalk en log. Sluit het menu en probeer opnieuw. |
| Koude start | Herhaal na afmelden/herstarten, met Options+ actief. Controleer vooral de eerste toetsdruk. |

Als cijfers in Kladblok blijven verschijnen, is de test mislukt: controleer het log en de directe Logitech-toewijzing. Een geslaagde build bewijst de focusoverdracht op jouw machine niet.

Log: `%LOCALAPPDATA%\XiPHiAS.CalcLauncher\launcher.log`. Normaal worden alleen fouten gelogd. Met `XiPHiAS.CalcLauncher.exe --diagnose` schrijft de launcher de gevonden hostvensters, gekoppelde proces-ID's, starttijden en gekozen venster naar de log, zonder Calculator te starten of de focus te veranderen. De log wordt bij ongeveer 256 KB opnieuw begonnen. Exitcodes: 0 = focus gelukt, diagnose voltooid of overlappende aanroep genegeerd; 1 = start/detectie/fout; 2 = Windows heeft focus niet verleend.

## Technische bronnen

- [Microsoft: SetForegroundWindow en de focusrestricties](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
- [Microsoft: app-identiteiten / AUMID](https://learn.microsoft.com/en-us/windows/configuration/store/find-aumid)
- [Microsoft: risico's van AttachThreadInput](https://devblogs.microsoft.com/oldnewthing/20080801-00/?p=21393)
- [Microsoft: publiceren als enkele EXE](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/)
