// ----------------------------------------------------------------------------
// <copyright file="Sogeti-IoT-I2CBus-LED.ino" company="SOGETI Spain">
//     Copyright © 2016 SOGETI Spain. All rights reserved.
//     Basic example of I2C communication between Raspberry & Arduino by Osc@rNET.
// </copyright>
// ----------------------------------------------------------------------------
#include <Wire.h>

#define EXTERNAL_LED_PIN  2
#define INTERNAL_LED_PIN  13

#define I2C_SLAVE_ADDRESS     0x0A     // 0x0A = Red | 0x0B = Yellod | 0x0C = Blue
#define I2C_FASS_MODE_400KHZ  400000L

const long interval = 1000;
unsigned long previousMillis = 0;

int externalLEDState = LOW;
int internalLEDState = LOW;

// Runs once (for setup purposes).
void setup()
{
  pinMode(EXTERNAL_LED_PIN, OUTPUT);
  pinMode(INTERNAL_LED_PIN, OUTPUT);
  
  digitalWrite(EXTERNAL_LED_PIN, LOW);
  digitalWrite(INTERNAL_LED_PIN, LOW);
  
  Wire.begin(I2C_SLAVE_ADDRESS);
  Wire.setClock(I2C_FASS_MODE_400KHZ);
  Wire.onReceive(receiveHandler);
}

// Runs repeatedly (main code).
void loop()
{
  unsigned long currentMillis = millis();
  
  if ((currentMillis - previousMillis) >= interval)
  {
    previousMillis = currentMillis;
    
    if (externalLEDState == HIGH)
    {
      if (internalLEDState == LOW)
      {
        internalLEDState = HIGH;
      }
      else
      {
        internalLEDState = LOW;
      }
      
      digitalWrite(INTERNAL_LED_PIN, internalLEDState);
    }
    else if (internalLEDState == HIGH)
    {
      internalLEDState = LOW;
      digitalWrite(INTERNAL_LED_PIN, internalLEDState);
    }
  }
}

// Runs when receive data from master (I2C BUS).
void receiveHandler(int numberOfBytes)
{
  if (Wire.available() > 0)
  {
    byte turnOnOffByte = Wire.read();
    
    if (turnOnOffByte == 0x00)
    {
      digitalWrite(EXTERNAL_LED_PIN, LOW);
      externalLEDState = LOW;
    }
    else if (turnOnOffByte == 0x01)
    {
      digitalWrite(EXTERNAL_LED_PIN, HIGH);
      externalLEDState = HIGH;
    }
  }
}
