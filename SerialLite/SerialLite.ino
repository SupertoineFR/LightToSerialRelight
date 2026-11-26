#include <FastLED.h>

// LED strip configuration
#define LED_PIN     9
#define NUM_LEDS    120
#define LED_TYPE    WS2811
#define COLOR_ORDER BRG
CRGB leds[NUM_LEDS];

// Brightness
int BRIGHTNESS = 121;
int SelectedBRIGHTNESS = 255;

// Palettes & blending
CRGBPalette16 currentPalette;
TBlendType    currentBlending;
CRGBPalette16 primaryColorPalette = CRGBPalette16(CRGB::Blue);
CRGBPalette16 secondaryColorPalette = CRGBPalette16(CRGB::Red);
CRGBPalette16 receivedPalette;

// Colors
CRGB primaryColor = CRGB::Blue;
CRGB secondaryColor = CRGB::Red;

// State
int menuPos = 1;
int currentAnimation = 0;
char inputChar;

// Scrolling effect state (AnimationBurst equivalent)
int animationSpeedIndex = 10;
float speedsArray[] = {1 / 1000, 1 / 500, 1 / 300, 1 / 200, 1 / 100, 1 / 50, 1 / 20, 1 / 10, 1 / 2, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
bool animationReverse = true;
int animationTimer = 0;

// Fade effect state (on -> fade off)
unsigned long effectStartTime = 0;
const unsigned long FADE_DURATION_MS = 200; // Duration of fade effect in milliseconds
bool fadeActive = false;

// Color receiving state (Beat Saber mod sends: $ + 6 bytes RGBRGB + %)
bool receivingColors = false;
byte colorData[6];
int colorByteIndex = 0;
bool enableShowing = true;

///////////////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////////////

void setup() {
  // Default palette
  currentPalette = RainbowColors_p;
  currentBlending = NOBLEND;
  SetupBSPalettes();

  FastLED.addLeds<LED_TYPE, LED_PIN, COLOR_ORDER>(leds, NUM_LEDS).setCorrection(TypicalLEDStrip);
  FastLED.setBrightness(BRIGHTNESS);
  FastLED.setTemperature(UncorrectedTemperature);

  pinMode(LED_BUILTIN, OUTPUT);

  Serial.begin(115200); // Must match the baud rate selected in the Beat Saber mod settings
  FastLED.setMaxRefreshRate(120);
}

///////////////////////////////////////////////////////////////////////////////
// LOOP
///////////////////////////////////////////////////////////////////////////////

void loop() {
  // Process EVERY byte waiting in the buffer on every frame.
  // This keeps the Arduino responsive to serial messages at all times,
  // even while an effect is running. No blocking delays are used.
  while (Serial.available() > 0) {
    ProcessSerialByte(Serial.read());
  }

  // Run the current effect based on the last command received
  switch (menuPos) {
    case 0: Blackout(); break;
    case 1: StaticPalette(); break;   // Received static palette from PC
    case 10: StaticPalette(); break;
    case 20: StaticPalette(); break;  // '#' command - lobby / static
    case 21: RightTurnOn(); break;    // 'q' command
    case 22: LeftTurnOn(); break;     // 'w' command
    case 23: ScrollEffect(primaryColorPalette); break;    // 'e' - scrolling, stays on
    case 24: ScrollEffect(secondaryColorPalette); break;  // 'r' - scrolling, stays on
    case 25: FadeOnOffEffect(primaryColor); break;        // 't' - on then fade off
    case 26: FadeOnOffEffect(secondaryColor); break;      // 'y' - on then fade off
    case 29: TurnOff(); break;        // 'p' command
  }

  ShowLED();
}

///////////////////////////////////////////////////////////////////////////////
// SERIAL CONTROL
///////////////////////////////////////////////////////////////////////////////

void ProcessSerialByte(uint8_t b) {
  inputChar = (char)b;

  // If we're receiving color data from the Beat Saber mod, accumulate bytes
  if (receivingColors) {
    if (inputChar == '%') {
      // End of color data - apply and exit receiving mode
      applyColorData();
      receivingColors = false;
      enableShowing = true;
      return;
    }
    if (colorByteIndex < 6) {
      colorData[colorByteIndex++] = b;
      if (colorByteIndex == 6) {
        // All 6 bytes received - apply immediately
        applyColorData();
        receivingColors = false;
        enableShowing = true;
      }
      return; // Don't process as a command while receiving color bytes
    }
  }

  // Feedback byte for LED Controller / Beat Saber mod
  if (inputChar == '-') {
    Serial.write('a');
  }

  // Single-character Arduino commands
  switch (inputChar) {
    case 'a': Serial.write('a'); break;            // Acknowledge / feedback
    case '#': menuPos = 20; break;                 // Static / lobby palette
    case '$': receivingColors = true; colorByteIndex = 0; enableShowing = false; break;  // Start receiving Beat Saber colors
    case 'q': menuPos = 21; break;                 // Right turn on (primary color)
    case 'w': menuPos = 22; break;                 // Left turn on (secondary color)
    case 'e': menuPos = 23; break;                 // Scrolling effect, primary (stays on)
    case 'r': menuPos = 24; break;                 // Scrolling effect, secondary (stays on)
    case 't': menuPos = 25; fadeActive = true; effectStartTime = millis(); break;  // On then fade off, primary
    case 'y': menuPos = 26; fadeActive = true; effectStartTime = millis(); break;  // On then fade off, secondary
    case 'p': menuPos = 29; break;                 // Turn off / blackout
  }
}

// Applies received color data to primary/secondary colors and rebuilds palettes
void applyColorData() {
  primaryColor.r = colorData[0];
  primaryColor.g = colorData[1];
  primaryColor.b = colorData[2];
  secondaryColor.r = colorData[3];
  secondaryColor.g = colorData[4];
  secondaryColor.b = colorData[5];
  SetupBSPalettes();
  BRIGHTNESS = 255;
  enableShowing = true;
}

///////////////////////////////////////////////////////////////////////////////
// LED OUTPUT
///////////////////////////////////////////////////////////////////////////////

void ShowLED() {
  FastLED.show();
}

///////////////////////////////////////////////////////////////////////////////
// PALETTE FUNCTIONS (all static)
///////////////////////////////////////////////////////////////////////////////

void StaticPalette() {
  currentPalette = receivedPalette;
}

void RightTurnOn() {
  currentBlending = NOBLEND;
  currentPalette = primaryColorPalette;
  FastLED.setBrightness(SelectedBRIGHTNESS);
}

void LeftTurnOn() {
  currentBlending = NOBLEND;
  currentPalette = secondaryColorPalette;
  FastLED.setBrightness(SelectedBRIGHTNESS);
}

void TurnOff() {
  Blackout();
}

void Blackout() {
  currentPalette = CRGBPalette16(CRGB::Black);
  FastLED.setBrightness(0);
}

void SetupBSPalettes() {
  primaryColorPalette = CRGBPalette16(
    primaryColor, CRGB::Black, primaryColor, CRGB::Black,
    primaryColor, CRGB::Black, primaryColor, CRGB::Black,
    primaryColor, CRGB::Black, primaryColor, CRGB::Black,
    primaryColor, CRGB::Black, primaryColor, CRGB::Black
  );
  secondaryColorPalette = CRGBPalette16(
    CRGB::Black, secondaryColor, CRGB::Black, secondaryColor,
    CRGB::Black, secondaryColor, CRGB::Black, secondaryColor,
    CRGB::Black, secondaryColor, CRGB::Black, secondaryColor,
    CRGB::Black, secondaryColor, CRGB::Black, secondaryColor
  );
  BRIGHTNESS = 200;
}

///////////////////////////////////////////////////////////////////////////////
// EFFECT 1: SCROLLING (e = primary, r = secondary)
// Non-blocking: scrolls the palette index back and forth each frame.
// Responsive to serial at all times (no delay()).
// ///////////////////////////////////////////////////////////////////////////////

void ScrollEffect(CRGBPalette16 palette) {
  currentBlending = NOBLEND;
  currentPalette = palette;
  FastLED.setBrightness(SelectedBRIGHTNESS);

  // Move the palette index back and forth each frame
  if (animationReverse) {
    animationTimer += (1 * speedsArray[animationSpeedIndex]);
  } else {
    animationTimer -= (1 * speedsArray[animationSpeedIndex]);
  }

  if (animationTimer < -255 || animationTimer > 255) {
    animationReverse = !animationReverse;
  }

  FillLEDsFromPaletteColors(animationTimer);
}

///////////////////////////////////////////////////////////////////////////////
// EFFECT 2: ON THEN FADE OFF (t = primary, y = secondary)
// Turns on full brightness, then fades to black over FADE_DURATION_MS.
// Non-blocking: uses millis() so serial stays responsive during the fade.
// ///////////////////////////////////////////////////////////////////////////////

void FadeOnOffEffect(CRGB color) {
  unsigned long elapsed = millis() - effectStartTime;

  if (elapsed >= FADE_DURATION_MS) {
    // Fade complete - turn off and reset fade state
    Blackout();
    fadeActive = false;
  } else {
    // Fade from 255 down to 0 over the duration
    uint8_t brightness = 255 - (uint8_t)((elapsed * 255) / FADE_DURATION_MS);
    fill_solid(leds, NUM_LEDS, color);
    FastLED.setBrightness(brightness);
  }
}

///////////////////////////////////////////////////////////////////////////////
// HELPERS
// ///////////////////////////////////////////////////////////////////////////////

void FillLEDsFromPaletteColors(int colorIndex) {
  uint8_t brightness = 255;
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = ColorFromPalette(currentPalette, colorIndex, brightness, currentBlending);
    colorIndex += (255 / NUM_LEDS);
  }
}