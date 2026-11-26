# LightToSerialRelight
A Beat Saber mod that allows you to control your Arduino LED Strip based on Beat Saber events.

**Disclaimer**: AI was used to patch this mod up to 1.40.8, and more AI to add chroma and modify the arduino code.

This mod is yet another updated version of the mod created by [D3rPole](https://github.com/D3rPole/BS-LightToSerial).   
It was forked from [MyLegIsPotato](https://github.com/MyLegIsPotato/LightToSerialRelight), in order to update it. It's not my code, far from it.

It allows you to communicate with Arduino based on events to display preset animations and patterns on your wb2812 adressable LED strip. The mod automatically gets custom colors from Beat Saber and makes them fully saturated before reaching arduino, it may be the reason if some colors on LED strip get displayed incorrectly. Most of the times it works as intended.

**WARNING**: Once the mod is installed, it **will** blindly send serial data over whatever open port it finds. This could lead to *bad stuff* happening depending on what you have plugged into your computer! 
I am NOT responsible for any hardware destroyed this way. Please just be careful!

[Gameplay(Outdated)](https://www.youtube.com/watch?v=9OCgU-t3w0Y)  New video hopefully soon

## Mod installation
Just download the .dll file from [the releases](https://github.com/SupertoineFR/LightToSerialRelight/releases) and drop it into your BeatSaber folder in /Plugins.

The mod needs four other dependencies installed in Beat Saber, next to the name is the minimum required version.

BSIPA	4.3.0
BeatSaberMarkupLanguage (BSML)	1.12.0
BS Utils	1.14.0
SiraUtil	3.2.0

These can all be installed inside of [BSManager](https://github.com/Zagrios/bs-manager).

## Setup

Before you play Beat Saber you will need to download arduino program and upload it to your board. Set all the settings in the top part to match your setup, that includes:
```
#define LED_PIN     9
#define NUM_LEDS    120
```
and probably:
```
#define LED_TYPE    WS2811
#define COLOR_ORDER GRB
```
It has not been tested with with other types of LEDs. Only WS2811

You will also need to install FastLED library. To do so go to your Arduino IDE -> Tools -> Manage Libraries -> Search for: *FastLED* -> Select version **3.3.0** -> Click install

The strip starts with this pattern: Green for 8 LEDs, Red for 8 LEDs, and then Blue for the rest. If these do not match up, change the color order in line 10 of the arduino program. 

The LED on pin 13 also starts blinking at 200ms, that means that it's waiting for the first command. 

Default baudrate is 115200. The baudrate selected in the game settings must match `Serial.begin(...)` in `LedMainSerial.ino`. After selecting your port in game setting (you can find which one in Arduino IDE) and clicking **apply** the LED strip should start with a yellow block (8 LEDs iirc) moving back and forth. This effect shows up whenever you're in the lobby.

If it does not change from the default pattern, and the LED on pin 13 (look at the arduino) doesn't stop blinking, make sure you have the correct settings ingame (port and baud rate).

## Other information
Remember to report any bugs. If you have any question regarding modding don't hestitate to ask, it wasn't easy at the start for me too.

Special thanks to:
- [D3rPole](https://github.com/D3rPole), for idea and old code that got basically rewritten from beginning.
- [Shadnix-was-taken](https://github.com/Shadnix-was-taken) whos code was used for MyLegisPotato's learning.
- [MyLegIsPotato](https://github.com/MyLegIsPotato), for the original game mod.
