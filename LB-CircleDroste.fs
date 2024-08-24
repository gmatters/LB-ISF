/*
{
    "CREDIT": "by Geoff Matters via shadertoy Xs3SWj",
    "ISFVSN": "2",
    "CATEGORIES": [
        "LB"
    ],
    "DESCRIPTION": "Circular droste with variable zoom and spin. Derived from https://www.shadertoy.com/view/Xs3SWj by roywig, with the help of article:\nhttp://www.josleys.com/article_show.php?id=82",
    "INPUTS": [
            {
                    "NAME": "inputImage",
                    "TYPE": "image"
            },
            {
                    "NAME": "zoomSpeed",
                    "LABEL": "Zoom Speed",
                    "TYPE": "float",
                    "MIN": -1.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.0
            },
            {
                    "NAME": "spinSpeed",
                    "LABEL": "Spin Speed",
                    "TYPE": "float",
                    "MIN": -1.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.0
            },
            {
                    "NAME": "innerRadius",
                    "LABEL": "Inner Radius",
                    "TYPE": "float",
                    "MIN": 0.02,
                    "MAX": 0.99,
                    "DEFAULT": 0.1
            },
            {
                    "NAME": "edgeBlend",
                    "LABEL": "Edge Blend",
                    "TYPE": "float",
                    "MIN": 0.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.005
            },
            {
                    "NAME": "dimCenter",
                    "LABEL": "Dim Center",
                    "TYPE": "float",
                    "MIN": 0.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.00
            },
            {
                    "DEFAULT": false,
                    "LABEL": "Clockwise",
                    "NAME": "clockwise",
                    "TYPE": "bool"
            },
            {
                    "DEFAULT": false,
                    "LABEL": "Debug Wrap",
                    "NAME": "debugWrap",
                    "TYPE": "bool"
            }
    ],
    "PASSES": [
        {
            "DESCRIPTION": "Accumulate zoom and rotation state",
            "FLOAT": true,
            "PERSISTENT": true,
            "TARGET": "dataBuffer",
            "WIDTH": "1",
            "HEIGHT": "1"
        },
        {
        }
    ]
}

*/

/*
  reference:
  https://www.shadertoy.com/view/Xs3SWj
  https://www.josleys.com/article_show.php?id=82
  http://roy.red/posts/fractal-droste-images/
  http://roy.red/posts/infinite-regression/
*/

#define PI 3.14159

#define ZTIME r
#define STIME g

vec2 accumulatorAddress = vec2(0.5, 0.5);  // Target center of pixel at 0,0 to avoid subpixel sampling math

vec4 getData() {
  return IMG_PIXEL(dataBuffer, accumulatorAddress);
}

vec2 cInverse(vec2 a) { return	vec2(a.x,-a.y)/dot(a,a); }
vec2 cMul(vec2 a, vec2 b) {	return vec2( a.x*b.x -  a.y*b.y,a.x*b.y + a.y * b.x); }
vec2 cDiv(vec2 a, vec2 b) {	return cMul( a,cInverse(b)); }
vec2 cExp(vec2 z) {	return vec2(exp(z.x) * cos(z.y), exp(z.x) * sin(z.y)); }
vec2 cLog(vec2 a) {	float b =  atan(a.y,a.x); if (b>0.0) b-=2.0*PI;return vec2(log(length(a)),b);}

float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

void main() {
    vec4 data = getData();
    float zTime = data.ZTIME;
    float sTime = data.STIME;
    float r1 = innerRadius;
    float r2 = 1.0;
    float scale = log(r2/r1);
    if (PASSINDEX == 0) {
      // Data accumulator accumulates 'time' used for zoom and speed.
      // Accumulating 'time' means that the dataBuffer isn't entagled with
      // implementation details like angles and twist.  Calculating zoom/spin
      // directly from TIME causes the output to jump around when adjusting
      // zoomSpeed and spinSpeed.
      zTime +=  TIMEDELTA * lerp(zoomSpeed, -1., 1., -10., 10.);
      sTime +=  TIMEDELTA * lerp(spinSpeed, -1., 1., -5., 5.);
      // Keeping values near 0 might prevent float accuracy issues of adding huge numbers then subtracting them again
      zTime = mod(zTime, scale); // Has no effect on output, but limits range of z.x to stay near 0
      sTime = mod(sTime, 2.0 * PI); // Has no effect on output, but limits range of z.y to stay near 0
      gl_FragColor.ZTIME = zTime;
      gl_FragColor.STIME = sTime;
      // If we don't set alpha on our data pixel, we might be subject to
      // premultiply that corrupts the data in .rgb
      gl_FragColor.a = 1.0;
      return;
    }
    vec2 z = gl_FragCoord.xy;
    z = (z.xy - RENDERSIZE.xy/2.)/RENDERSIZE.y;  // Map so that 0 is in center. Assumes y is shorter
    float angle = atan(scale/(2.0*PI)); // CCW from integral left edge
    if (clockwise) {
      angle = -1. * angle;  // CW from integral left edge
      // Jos Leys article indicates two alternate variations PI+angle and
      // PI-angle, I find those to give identical output to angle and -angle
    }
    // Droste transform here
    z = cLog(z);  // Transform by log: circle becomes vertical strip
    z.y -= sTime;  // Vertical shift in log space, equiv to rotating the input image (down can become up)
    z = cDiv(z, cExp(vec2(0,angle))*cos(angle)); // Offsets each copy to turn rings into spiral
    z.x -= zTime;  // Horizontal shift in log space, equiv to zoom

    // While we are in log-space, calculate a float measurement of how far
    // 'down' the spiral this pixel is, with 0 representing the outer edge, 1.0
    // one wrap in from the outer edge, 1.5 a half turn further than that, etc.
    // z.x seems to range from -inf to 0 (clockwise), but zoom and twist will impact the range
    float wrapCount = -1.0 * z.x; // 0 -> infinity but practical visible limit depends on closeness of r1 r2
    if (dimCenter > 0.0 || debugWrap) {
      wrapCount = wrapCount / scale; // 0 -> infinity, scaled so that it increases 1.0 by every wrap
      if (clockwise) { wrapCount += 1.0; } // Empirical
      wrapCount = floor(wrapCount);  // 0 -> inf as integers aligned with the wrap boundaries
      wrapCount -= zTime/scale; // Causes each wrap of the spiral to shift as it moves in/out, so that the overall range doesn't drift toward +-infinity
      // Now that we have a count for turns, we need to add the progress within each turn
      float wrapProgress = z.y; // Empirically, y ranges from -2PI to 0  (clockwise)
      wrapProgress /= 2.0 * PI; // range -1 to 0  (clockwise)
      if (clockwise) { wrapProgress *= -1.0; }
      if (!clockwise) { wrapProgress += 1.0; } // range 0 to 1
      wrapCount += wrapProgress; // add in the intra-wrap progress, 0 -> inf smoothly around spiral
      if (debugWrap) {
        wrapCount = wrapCount / 5.0;  // visibility hack
        gl_FragColor = vec4(vec3(wrapCount), 1.0);
        return;
      }
    }

    z.x = mod(z.x,scale);  // Tiling
    vec2 zBlend = z;  // Identify correct source pixel to blend for feathering
    float blend = 0.0;
    float feather = edgeBlend * scale;
    if (z.x > scale - feather) {  // Right edge of log space strip -> outer edge of original circle
      blend = (z.x - (scale - feather)) / feather;
      zBlend.x = z.x - scale; // blending pixels are left of strip -> inside inner edge of orig circle
    }
    z = cExp(z)*r1; // Undo the log transform; vertical lines back to circles
    zBlend = cExp(zBlend)*r1; // Undo the log transform; vertical lines back to circles
    // 0.5 + 0.5z maps back to '0 in the corner' normalized coords
    z = mod(.5+.5*z,1.0);
    zBlend = mod(.5+.5*zBlend,1.0);
    // Draw output color
    vec4 color = IMG_NORM_PIXEL(inputImage,z);
    vec4 blendColor = IMG_NORM_PIXEL(inputImage, zBlend);
    if (dimCenter > 0.0) {
      float beforeFade = 1.0; // The first wrap at full brightness before fadeout starts applying
      wrapCount = max(wrapCount-beforeFade, 0.0); // Don't let wrap go negative from beforeFade
      float blendWrapCount = max(wrapCount-1.0, 0.0); // One turn outwards, clamp at 0
      float brightness = 1.0 - dimCenter * wrapCount;
      color *= vec4(vec3(brightness), 1.0);
      float blendBrightness = 1.0 - dimCenter * blendWrapCount;
      blendColor *= vec4(vec3(blendBrightness), 1.0);
    }

    gl_FragColor = mix(color, blendColor, blend);
}
