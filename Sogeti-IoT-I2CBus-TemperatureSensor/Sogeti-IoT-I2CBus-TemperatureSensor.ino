// ----------------------------------------------------------------------------
// <copyright file="Sogeti-IoT-I2CBus-TemperatureSensor.ino" company="SOGETI Spain">
//     Copyright © 2016 SOGETI Spain. All rights reserved.
//     Basic example of I2C communication between Raspberry & Arduino by Osc@rNET.
// </copyright>
// ----------------------------------------------------------------------------
#include <Wire.h>

#define TEMPERATURE_SENSOR_LED  A0

#define I2C_SLAVE_ADDRESS     0x0D
#define I2C_FASS_MODE_400KHZ  400000L

byte currentTemperature = 0;

const long interval = 1000;
unsigned long previousMillis = 0;

// Runs once (for setup purposes).
void setup()
{
  Wire.begin(I2C_SLAVE_ADDRESS);
  Wire.setClock(I2C_FASS_MODE_400KHZ);
  Wire.onRequest(requestHandler);
}

// Runs repeatedly (main code).
void loop()
{
  unsigned long currentMillis = millis();
  
  if ((currentMillis - previousMillis) >= interval)
  {
    previousMillis = currentMillis;
    
    int sensorVal = analogRead(TEMPERATURE_SENSOR_LED);
    float voltage = (sensorVal / 1024.0) * 5.0;
    float temperature = (voltage - .5) * 100;    
    
    currentTemperature = (byte)temperature;
  }
}

// Runs when request data from master (I2C BUS).
void requestHandler()
{
  Wire.write(currentTemperature);
}
