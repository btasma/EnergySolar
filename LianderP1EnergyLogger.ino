/*
Energy monitor program.


This is an example message from the Liander energy meter:
0-0:96.1.1(4B414C37303035313738363336323133)
1-0:1.8.1(03007.806*kWh) Laag
1-0:1.8.2(03037.264*kWh) Hoog
1-0:2.8.1(01547.359*kWh) Terug Laag
1-0:2.8.2(03843.919*kWh) Terug Hoog
0-0:96.14.0(0002) Tarief?
1-0:1.7.0(0000.70*kW)Actueel gebruik
1-0:2.7.0(0000.00*kW)Actueel teruglevering
0-0:17.0.0(0999.00*kW) ?? Ampere?
0-0:96.3.10(1) ??
0-0:96.13.1()
0-0:96.13.0()
!
*/

#include <SoftwareSerial.h>
#define MESSAGE_INTERVAL 3

SoftwareSerial meter(3, 2); // RX is on pin 3
SoftwareSerial esp8266(11, 10); // RX is on pin 10, TX on pin 11
char messageLine[100]; // A temporary string to hold the entire line
int linePosition = 0;
int counter = 0;

long totalEnergyUsed = 0;
long totalEnergyProvided = 0;
long currentEnergyUsed = 0;
long currentEnergyProvided = 0;

void setup()
{
  Serial.begin(115200);
  esp8266.begin(115200);
  meter.begin(9600);
  Serial.println("Energy meter program started");
  delay(2000);
  esp8266.println("AT+CIFSR");
  delay(2000);
  esp8266.println("AT+CIPMUX=1");
}

void loop()
{
  if(meter.available())
  {
    char currentChar = meter.read() & 0x7F; // Use only the first 7 bits for data instead of the 8 bit default
    messageLine[linePosition++] = currentChar;

    if(currentChar == '\n')
    {
      parseLine();
      clearMessageLine();
    }
    // The ! is always the last character from the energy meter message
    else if(currentChar == '!')
    {
      Serial.println(totalEnergyUsed);
      Serial.println(totalEnergyProvided);
      Serial.println(currentEnergyUsed);
      Serial.println(currentEnergyProvided);

      if(++counter >= MESSAGE_INTERVAL)
      {
        sendMessage();
      }
    }
  }
}

void sendMessage()
{
        Serial.println("Sending to server");
        String dataToSend = "GET /energy.php?u=" + String(totalEnergyUsed) + "&p=" + String(totalEnergyProvided) + "&cu=" + String(currentEnergyUsed) + "&cp=" + String(currentEnergyProvided) + " HTTP/1.1\r\nHost: 192.168.1.206:5005\r\n\r\n\r\n\r\n";
       esp8266.println("AT+CIPSTART=0,\"TCP\",\"192.168.1.206\",5005"); 
       delay(500);
       esp8266.println("AT+CIPSEND=0,94");
       delay(500);
       esp8266.println(dataToSend);
       counter = 0;
       Serial.println("Data send");
}
void parseLine()
{
  long tl = 0;
  long tld = 0;

// Laag tarief
    if(sscanf(messageLine,"1-0:1.8.1(%ld.%ld" ,&tl, &tld) == 2)
    {
      totalEnergyUsed = tl * 1000 + tld;
    }
// Hoog tarief
    if(sscanf(messageLine,"1-0:1.8.2(%ld.%ld" ,&tl, &tld) == 2)
    {
      totalEnergyUsed = totalEnergyUsed + tl * 1000 + tld;
    }

        // Laag tarief
    if(sscanf(messageLine,"1-0:2.8.1(%ld.%ld" ,&tl, &tld) == 2)
    {
      totalEnergyProvided = tl * 1000 + tld;
    }
// Hoog tarief
    if(sscanf(messageLine,"1-0:2.8.2(%ld.%ld" ,&tl, &tld) == 2)
    {
      totalEnergyProvided = totalEnergyProvided + tl * 1000 + tld;
    }  
    
        // Used
    if(sscanf(messageLine,"1-0:1.7.0(%ld.%ld" ,&tl, &tld) == 2)
    {
      currentEnergyUsed = tl * 1000 + tld;
    }
// Provided
    if(sscanf(messageLine,"1-0:2.7.0(%ld.%ld" ,&tl, &tld) == 2)
    {
      currentEnergyProvided = tl * 1000 + tld;
    }  
}

void clearMessageLine()
{
       for (int i = 0; i < 100; i++)
      { 
        messageLine[i] = 0;
      }
      linePosition = 0; // Reset the counter
}
