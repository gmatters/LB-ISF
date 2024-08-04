/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Draw over video using a paintbrush as a cursor.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
          "NAME" : "cursor",
          "TYPE" : "point2D",
          "DEFAULT" : [
            0,
            0
          ],
          "MIN" : [
            0,
            0
          ],
          "MAX" : [
            1,
            1
          ]
        },
        {
            "DEFAULT": 30,
            "MAX": 300,
            "MIN": 0,
            "NAME": "brushSize",
            "TYPE": "float"
        },
        {
            "DEFAULT": 3,
            "MAX": 60,
            "MIN": 0,
            "NAME": "decayTimeSec",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.5,
            "MAX": 1,
            "MIN": 0,
            "NAME": "drawOver",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "Default",
                "Classic"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1
            ]
        },
        {
            "NAME": "debug",
            "TYPE": "bool"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "PERSISTENT": true,
            "FLOAT": true,
            "TARGET": "lifeBuffer"
        },
        {
            "DESCRIPTION": "Coordinates of cursor stored in [1,1].xy",
            "FLOAT": true,
            "HEIGHT": "1",
            "PERSISTENT": true,
            "TARGET": "LastFrameCoord",
            "WIDTH": "1"
        },
        {
        }
    ]
}
*/

  // from https://stackoverflow.com/questions/63491296/calculating-point-to-line-distance-in-glsl
  float distanceFromPointToLine(in vec3 a, in vec3 b, in vec3 c) {
    vec3 ba = a - b;
    vec3 bc = c - b;
    float d = dot(ba, bc);
    float len = length(bc);
    float param = 0.0;
    if (len != 0.0) {
      param = clamp(d / (len * len), 0.0, 1.0);
    }
    vec3 r = b + bc * param;
    return distance(a, r);
  }

// Buffer is [layer1, layer2, layer3, unusable] each float contains the
// remaining lifespan of that layer for that pixel.

// TODO: offset some layers, e.g. middle shadow layer, so e.g. lower right is thicker? Ala original tag.
// TODO: subtly fade out colors with age, e.g. as paint dries, and to create more clarity when painting over old stuff?

vec4 color1 = vec4(0.945, 0.353, 0.141, 1.0);  // Roadmap orange
vec4 color2 = vec4(0.0, 0.0, 0.0, 1.0);
vec4 color3 = vec4(0.0, 0.0, 1.0, 1.0);  // Roadmap blue

bool bleed = true;
bool drip = false;

bool dull = true;
float dullAmount1 = 0.00;
float dullAmount2 = 0.15;
float dullAmount3  = 0.25;
vec4 colorDull = vec4(1.0, 1.0, 1.0, 1.0);

vec4 getBufferPixel(in vec2 position, in float delta) {
    vec4 bufferData = IMG_PIXEL(lifeBuffer, position);
    vec4 bufferPixel = vec4(0.0);
    float colorFreshness = 0.0; // Inverse of age; freshest possible value is 1.0 which means just drawn.
    // Fade out alpha over this long (percent of lifespan).
    float aThresh = 0.001; // Must be > 0 to prevent divide by zero (or update code)
    // TODO: consider inter-color fades when freshnesses are very close? Perhaps no longer useful now that drawover inversion bug is fixed.
    // Prefer that inner color is drawn, so that it pools
    if (bufferData.r > 0.0 && bufferData.r > colorFreshness) {
        colorFreshness = bufferData.r;
        bufferPixel = color1;
        if (dull && colorFreshness + delta < 1.0) {
          bufferPixel = mix(colorDull, color1, (1.0 - dullAmount1) + dullAmount1 * (colorFreshness + delta));
        }
        if (bufferData.r < aThresh) {
          bufferPixel.a = bufferData.r / aThresh;
        } else {
          bufferPixel.a = 1.0;
        }
    }
    // But an outer color can draw over an inner color if it is as least delta newer.
    // E.G. .g of 0.8 could draw over .r of 0.6 but 0.61 wouldn't
    if (bufferData.g > 0.0 && (colorFreshness == 0.0 || bufferData.g - delta > colorFreshness)) {
        colorFreshness = bufferData.g; // If we have decided to draw over, we track the actual freshness, regardless of the delta used to decide whether to draw over. This is important to ensure that it is sufficiently hard to be drawn over.
        bufferPixel = color2;
        if (dull && colorFreshness + delta < 1.0) {
          bufferPixel = mix(colorDull, color2, (1.0 - dullAmount2) + dullAmount2 * (colorFreshness + delta));
        }
        if (bufferData.g < aThresh) {
          bufferPixel.a = bufferData.g / aThresh;
        } else {
          bufferPixel.a = 1.0;
        }
    }
    // For outermost color to draw over it must be delta newer than whatever the topmost color is. This means that .b of 0.8 could draw over .r of 0.6, but from the above example where .g of 0.8 drew over .r of 0.6, we would need a mucher fresher .b of e.g. 1.0 to draw over the 0.8.
    if (bufferData.b > 0.0 && (colorFreshness == 0.0 || bufferData.b - delta > colorFreshness)) {
        colorFreshness = bufferData.b;
        bufferPixel = color3;
        if (dull && colorFreshness + delta < 1.0) {
          bufferPixel = mix(colorDull, color3, (1.0 - dullAmount3) + dullAmount3 * (colorFreshness + delta));
        }
        if (bufferData.b < aThresh) {
          bufferPixel.a = bufferData.b / aThresh;
        } else {
          bufferPixel.a = 1.0;
        }
    }
    return bufferPixel;
}

// There are two desires in tension.
// We want the center color to be on top, so that it is contiguous.
// However, if we make it *always* on top, it will always be contiguous, but
// the cursor can become lost in a pool of c1 and it feels weird to come back
// much later and "draw into" a pool of c1 while losing c2 and c3.
// So, drawOver lets us control how soon we are able to draw over older
// material. It is implemented as an offset when comparing colors at different points in their lifespan.
// In order to preserve the drawover behaviour, colors must age at tha same rate.

void main()
{
  float size1 = 0.5;  // Portion of brush size
  float size2 = 0.7;  // Portion of brush size
  // Outer layers draw "under" inner one so that they make a continuous path
  // across time. We could also always paint the inner layers over the outer
  // ones, but that makes it impossible to ever "draw over" an older path.
  // If drawOver is close to 1, we apply a small displacement when drawing so that newer lines
  // are more likely to draw over an older one. Small value, we apply a big
  // displacement so that inner colors are almost always over outer colors.
  float heightDelta = (1.0 - drawOver) * 0.9;

  if (PASSINDEX == 0)	{  // write into lifeBuffer
      vec3 pointA = vec3(gl_FragCoord.x, gl_FragCoord.y, 0);
      vec2 cursorDeNormalized = cursor * IMG_SIZE(inputImage);
      vec2 lastCursorDeNormalized = IMG_PIXEL(LastFrameCoord, vec2(0,0)).xy * IMG_SIZE(inputImage);
      vec3 lineEndB = vec3(cursorDeNormalized.x, cursorDeNormalized.y, 0);
      vec3 lineEndC = vec3(lastCursorDeNormalized.x, lastCursorDeNormalized.y, 0);
      vec4 bufferColor = IMG_THIS_PIXEL(lifeBuffer);

      // Bleed outer color downwards
      if (bleed) {
        float bestB = bufferColor.b;  // This pixel
        float y_offset = 1.0;
        vec4 pixelAbove = IMG_PIXEL(lifeBuffer, vec2(gl_FragCoord.x, gl_FragCoord.y+y_offset));
        //if (drip && pixelAbove.b > 0.0 && pixelAbove.b > 0.0) { // only apply drip in earlier part of lifespan
        if (drip) {
          y_offset = 1.0 + sin(gl_FragCoord.x/11.0) / 4.0 + sin(gl_FragCoord.x/9.0) / 5.0; // Randomish wiggles
          pixelAbove = IMG_PIXEL(lifeBuffer, vec2(gl_FragCoord.x, gl_FragCoord.y+y_offset));
        }
        // TODO: consider phasing to get different drip locations, based on life/time such that the drip locations are related to the paint time and stay consistent, but things painted at different times have different drip locations.
        bestB = max(bufferColor.b, pixelAbove.b); // Never overwrite this pixel with older
        bufferColor.b = max(bufferColor.b, bestB); // Never overwrite this pixel with older
      }

      // age existing content so that it eventually disappears
      bufferColor.r -= TIMEDELTA / decayTimeSec;
      bufferColor.g -= TIMEDELTA / decayTimeSec;
      bufferColor.b -= TIMEDELTA / decayTimeSec;
      bufferColor.a = 1.0; // Avoid premultiply messing up our data RGB values.

      // Draw in new contents
      if (distanceFromPointToLine(pointA, lineEndB, lineEndC) <= size1 * brushSize) {
        bufferColor.r = 1.0;
      } else if (distanceFromPointToLine(pointA, lineEndB, lineEndC) <= size2 * brushSize) {
        bufferColor.g = 1.0;
      } else if (distanceFromPointToLine(pointA, lineEndB, lineEndC) <= brushSize) {
        bufferColor.b = 1.0;
      }
      gl_FragColor = bufferColor;
  }
  if (PASSINDEX == 1)	{  // write into LastFrameCoord
          // TODO: writing to all pixels. Should only need one pixel to store data.
          gl_FragColor.x = cursor.x;
          gl_FragColor.y = cursor.y;
          gl_FragColor.a = 1.0;  // Avoid premultiply
  }
  else if (PASSINDEX == 2)	{
    // Relative heights of paint layers (e.g. to pool foreground over background).
    float delta = heightDelta;
    // Render output	
    gl_FragColor = IMG_THIS_PIXEL(inputImage);
    // Drop Shadow
    vec4 displacedPixel = getBufferPixel(gl_FragCoord.xy + vec2(6, 6), delta);
    if (displacedPixel.a > 0.0) {
      gl_FragColor = mix(gl_FragColor, vec4(0.0, 0.0, 0.0, 1.0), displacedPixel.a * 0.2);
    }
    vec4 bufferPixel = getBufferPixel(gl_FragCoord.xy, delta);
    float pAlpha = bufferPixel.a;
    bufferPixel.a = 1.0;
    gl_FragColor = mix(gl_FragColor, bufferPixel, pAlpha);
  }
}
