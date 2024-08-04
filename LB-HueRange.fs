/*{
    "CATEGORIES": [
        "Color Adjustment",
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Limit the range of hues to a certain width around a center hue. 'reflect' ensures that smoothly varying input will smoothly vary in the ouput, by wrapping out-of-range hues back into the range. The downside is that there will be collisions, for instance the color opposite the center will also be mapped to the center. 'cut' mode ensures that unique input hues produce unique output hues, but there is a discontinuity at the color opposite the center. If your content has grain or noise, pixels might flicker between the low and high end of the allowed output range.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0.5,
            "MAX": 1,
            "MIN": 0,
            "NAME": "center",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1.0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "width",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "reflect",
                "cut"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1
            ]
        },
        {
            "DEFAULT": false,
            "LABEL": "show hue mapping",
            "NAME": "showMapping",
            "TYPE": "bool"
        }
    ],
    "ISFVSN": "2"
}
*/

#define MODE_CUT 1
#define MODE_REFLECT 0

vec4 rgb2hsv(vec4 c)	{
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
	vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);
	
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec4(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x,c.a);
}

vec4 hsv2rgb(vec4 c)	{
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return vec4(c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y),c.a);
}

float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

// Hue: 0 is red, 0.5 is cyan

float mapHue(float hue) {
  //return center + (hue - center) * width;  // Works, but cuts at red.
  if (mode == MODE_REFLECT) {
    float offset = (0.5-center);
    float myHue = mod(hue + offset, 1.0);
    float lower_boundary = 0.5 - width/2.0;
    if (myHue < lower_boundary) {
      myHue = lerp(myHue, 0.0, lower_boundary, 0.5, lower_boundary);
    }
    float upper_boundary = 0.5 + width/2.0;
    if (myHue > 0.5 + width/2.0) {
      myHue = lerp(myHue, upper_boundary, 1.0, upper_boundary, 0.5);
    }
    myHue = mod(myHue - offset, 1.0);
    return myHue;
  }
  // MODE_CUT
  float offset = (0.5-center);
  float myHue = mod(hue + offset, 1.0);
  myHue = 0.5 + (myHue - 0.5) * width;
  myHue = mod(myHue - offset, 1.0);
  return myHue;
}

void main() {
    vec4 hsv = rgb2hsv(IMG_THIS_PIXEL(inputImage));
    vec2 loc = isf_FragNormCoord;
    if (showMapping && loc.y > 0.88) {
      hsv.x = loc.y > 0.94 ? loc.x : mapHue(loc.x);
      hsv.y = 1.0;
      hsv.z = 1.0;
    } else {
      hsv.x = mapHue(hsv.x);
    }
    gl_FragColor = hsv2rgb(hsv);
}
