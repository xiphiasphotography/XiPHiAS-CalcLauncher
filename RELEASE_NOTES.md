# CalcLauncher 0.9

Eerste GitHub-release van XiPHiAS CalcLauncher, een kleine Windows-app voor de calculator-toets van de Logitech MX Keys S.

## Functies

- Start Microsoft Windows Calculator of brengt een bestaand venster naar voren.
- Herstelt geminimaliseerde vensters en behoudt bestaande berekeningen.
- Herkent Calculator via app-identiteit, onafhankelijk van venstertitel en Windows-taal.
- Voorkomt overlappende starts via een sessiegebonden mutex.
- Werkt zonder consolevenster, achtergrondservice of administratorrechten.
- Logt fouten en geweigerde focusoverdracht in `%LOCALAPPDATA%\XiPHiAS.CalcLauncher\launcher.log`.

## Downloads en installatie

Vereist Windows 11 x64 met Microsoft Windows Calculator.

- **XiPHiAS.CalcLauncher-0.9.0-win-x64.zip**: kleine EXE; vereist de .NET 10 runtime.
- **XiPHiAS.CalcLauncher-0.9.0-standalone-win-x64.zip**: bevat de .NET-runtime; geen afzonderlijke .NET-installatie nodig.
- **SHA256SUMS.txt**: SHA-256-controlesommen van beide ZIP-bestanden.

Pak een ZIP uit op een vaste locatie. Wijs in Logi Options+ de calculator-toets rechtstreeks toe aan `XiPHiAS.CalcLauncher.exe`. Zie de README voor de instellingen en handmatige testscenario's.

## Bekende beperkingen

Windows kan voorgrondfocus weigeren. De launcher laat dan de taakbalkknop knipperen en schrijft een logregel. Een vastgelopen Calculator-proces wordt niet afgesloten. Virtuele en beveiligde bureaubladen worden niet beheerd.

## Validatie

Release-build en publicatie van beide x64-varianten gecontroleerd. De interactieve focusoverdracht en Logitech-toewijzing moeten op de doelcomputer worden getest volgens de README.
