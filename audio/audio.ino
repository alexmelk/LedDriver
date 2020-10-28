#include <Adafruit_NeoPixel.h>

float brtn = 1000;
Adafruit_NeoPixel strip = Adafruit_NeoPixel(64, 2, NEO_RGB + NEO_KHZ800);
void setup() {
  Serial.begin(115200);
  strip.begin();
  strip.clear();
  strip.show();
  LCD(0,0,100,100,100);
  LCD(7,7,100,100,100);
  LCD(0,7,100,100,100);
  LCD(7,0,100,100,100);
  strip.show();
  delay(1000);
  strip.clear();
  strip.show();
}

void LCD(int i, int j, int R, int G, int B)
{
  if ((i > 7) || (j > 7) || (i < 0) || (j < 0)) {
    return;
  }

  if (i == 0)
  {
    strip.setPixelColor(j, G*((brtn/10)/100), R*((brtn/10)/100), B*((brtn/10)/100));
  }
  if ( (i % 2) == 0)
  {
    strip.setPixelColor(8 * i + j, G*((brtn/10)/100), R*((brtn/10)/100), B*((brtn/10)/100));
  }
  else
  {
    strip.setPixelColor(8 * i + (7 - j), G*((brtn/10)/100), R*((brtn/10)/100), B*((brtn/10)/100));
  }
}
byte r;
byte g;
byte b;
int charCounter = 0;
int j = 0;
void loop() {
  while(Serial.available())
  {
    strip.clear();
    int time = millis();
    
    for(int i=0;i<64;i++)
    {
      while (!Serial.available());
      r = Serial.read();
      while (!Serial.available());
      g = Serial.read();
      while (!Serial.available());
      b = Serial.read();
      strip.setPixelColor(i,g,r,b);
    }
    strip.show();
    if(millis()-time>10)
    {
      strip.clear();
      while(Serial.available()){Serial.read();}
      return;
    }
   }
  delayMicroseconds(100);
  
}
