# ROSVIK / BLACKOUT — målbild och leveransordning

Beslut med Patrik: behåll den uppskattade vinterversionens bildspråk och Godot.
Ett igenkännbart Rosvik, vardaglig realism, kall apokalyps och varma trygga rum.
Grafiken och stämningen är kvalitetskrav genom hela produktionen.

## Första färdiga spelet

En avgränsad enspelarkampanj i Rosvik med början, progression och avslut.
Långvarigt strömavbrott, vinter och utebliven samhällshjälp är utgångspunkten.
Spelaren återställer skolans reservkraft och bygger därefter upp en fungerande
fristad med värme, vatten, mat och kontakt med omvärlden. Orsaken till kollapsen
avslöjas genom miljön och fynd. Övernaturliga inslag ingår inte i grundbeslutet.

Kärnloopen: förbered dig i fristaden, utforska ett kvarter, sök verkliga saker,
prioritera vad du kan transportera, återvänd och använd fynden till konkret nytta.
Föremål ligger i rimliga miljöer: laddare i en låda, verktyg i garaget,
gräsklippare på golvet. En gräsklippare får inte plats i ryggsäcken.

## Arbetsordning

1. Säkra den godkända vinterversionens redigerbara källkod och byggkontroller.
2. Föremål, behållare och ryggsäck: massa, volym, skick och bestående fynd.
   Första miljöerna är servicebilen och ett välfyllt garage.
3. Spara/ladda hela spelvärlden; omstart, paus och inställningar fungerar.
4. Värme, mat, vatten och användning/reparation av saker bildar en hel spelloop.
5. Fler igenkännbara kvarter, interiörer, berättelse och kampanjens avslut.
6. Sammanhängande speltest från ny start till slut, Windows-test, prestanda,
   ljud/grafikinställningar och paketering. Därefter kan versionen kallas färdig.

## Loot och omfattning

Slutmålet är en mycket stor katalog av vardagliga ting. Antal poster är inget
framstegsmått om sakerna saknar rimlig plats, funktion eller presentation.
Katalogen skiljs från kod och från individuella föremåls skick och mängd.
Första katalogen ska visa att systemet kan växa utan en specialskriven funktion
för varje ny skruvmejsel, laddare eller trädgårdsmaskin. Massor/volymer är
spelmässiga uppskattningar, inte tillverkaruppgifter.

## Visuell och geografisk kontinuitet

Behåll kamera, vinterljus, snö, varm skolentré och befintlig geometri vid
systemändringar. Kartunderlag och platsbilder avgör igenkänningen; dokumentera
vad som är verifierat och vad som är tolkning. Utöka inte kartan med godtycklig
fantasyarkitektur. Bevara kartans källhänvisningar och licensinformation.

## Arbetssätt vid modellbyte eller avbrott

Små avslutade ändringar, versionshanterade och verifierade innan nästa del.
Läs denna fil och STATUS.md först. Återanvänd system och tester; gör inte om
motor, kamera eller konstnärlig riktning på eget initiativ. Källkod ska finnas
på fjärrgrenen efter varje avslutat steg. En spelbar zip ersätter inte källkod.
Var tydlig med vad som är byggt, testat och återstår; lova inte bakgrundsarbete
eller att användningsgränser kan kringgås.
