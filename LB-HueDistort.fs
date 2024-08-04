/*{
    "CATEGORIES": [
        "Color Adjustment",
        "LB"
    ],
    "CREDIT": "by Geoff Matters using HSLUV GLSL port by William Malo",
    "DESCRIPTION": "Hue distortion. Defaults to perceptually uniform HSV but reduce the |perceptual| fader to get pure math SV. |bias| squishes the spectrum left or right, allowing access to other colors. Half the color wheel will be stretched and the other half squished from this mapping. Then, an optional rotate is applied. With these two controls, any duotone input can be mapped to any duotone output. Try finding a midpoint somewhere between 0.25 and 0.75 to keep the distortion subtle. Extreme midpoint values may cause haloing in the anti-aliased values at color boundaries.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "rotate",
            "TYPE": "float"
        },
        {
            "DEFAULT": false,
            "LABEL": "reverse color spectrum",
            "NAME": "flipSpectrum",
            "TYPE": "bool"
        },
        {
            "DEFAULT": 1,
            "IDENTITY": 1,
            "MAX": 1,
            "MIN": 0,
            "NAME": "range",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.5,
            "LABEL": "bias",
            "MAX": 1,
            "MIN": 0,
            "NAME": "midpoint",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "LABEL": "perceptual",
            "NAME": "love",
            "TYPE": "float"
        },
        {
            "DEFAULT": false,
            "LABEL": "show hue mapping",
            "NAME": "showMapping",
            "TYPE": "bool"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "HEIGHT": "64",
            "TARGET": "histogramColumns"
        },
        {
            "FLOAT": true,
            "HEIGHT": "1",
            "TARGET": "histogram",
            "WIDTH": "64"
        },
        {
        }
    ]
}
*/

//#define COMPARE_COLOR_SPACES  // debug with pure HSV manipulation in bottom half

#define PASS_HISTOGRAM_COLUMNS 0
#define PASS_HISTOGRAM 1
#define PASS_RENDER 2
#define COLOR_SPACE_HSLUV 0
#define COLOR_SPACE_HSLB 1
#define COLOR_SPACE_HSV 2
#define HISTOGRAM_BINS 64.0   // Must match height of histogram buffer


// If you have footage with pure R and G, no matter now much you rotate the hue
// those two color values will always be e.g. 120 degrees apart. Instead, remap
// the hue in a non-linear fashion so that part of the wheel is squished and
// part is stretched.


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

/*
HSLUV-GLSL v4.2
HSLUV is a human-friendly alternative to HSL. ( http://www.hsluv.org )
GLSL port by William Malo ( https://github.com/williammalo )
Put this code in your fragment shader.
*/

vec3 hsluv_intersectLineLine(vec3 line1x, vec3 line1y, vec3 line2x, vec3 line2y) {
    return (line1y - line2y) / (line2x - line1x);
}

vec3 hsluv_distanceFromPole(vec3 pointx,vec3 pointy) {
    return sqrt(pointx*pointx + pointy*pointy);
}

vec3 hsluv_lengthOfRayUntilIntersect(float theta, vec3 x, vec3 y) {
    vec3 len = y / (sin(theta) - x * cos(theta));
    if (len.r < 0.0) {len.r=1000.0;}
    if (len.g < 0.0) {len.g=1000.0;}
    if (len.b < 0.0) {len.b=1000.0;}
    return len;
}

float hsluv_maxSafeChromaForL(float L){
    mat3 m2 = mat3(
         3.2409699419045214  ,-0.96924363628087983 , 0.055630079696993609,
        -1.5373831775700935  , 1.8759675015077207  ,-0.20397695888897657 ,
        -0.49861076029300328 , 0.041555057407175613, 1.0569715142428786  
    );
    float sub0 = L + 16.0;
    float sub1 = sub0 * sub0 * sub0 * .000000641;
    float sub2 = sub1 > 0.0088564516790356308 ? sub1 : L / 903.2962962962963;

    vec3 top1   = (284517.0 * m2[0] - 94839.0  * m2[2]) * sub2;
    vec3 bottom = (632260.0 * m2[2] - 126452.0 * m2[1]) * sub2;
    vec3 top2   = (838422.0 * m2[2] + 769860.0 * m2[1] + 731718.0 * m2[0]) * L * sub2;

    vec3 bounds0x = top1 / bottom;
    vec3 bounds0y = top2 / bottom;

    vec3 bounds1x =              top1 / (bottom+126452.0);
    vec3 bounds1y = (top2-769860.0*L) / (bottom+126452.0);

    vec3 xs0 = hsluv_intersectLineLine(bounds0x, bounds0y, -1.0/bounds0x, vec3(0.0) );
    vec3 xs1 = hsluv_intersectLineLine(bounds1x, bounds1y, -1.0/bounds1x, vec3(0.0) );

    vec3 lengths0 = hsluv_distanceFromPole( xs0, bounds0y + xs0 * bounds0x );
    vec3 lengths1 = hsluv_distanceFromPole( xs1, bounds1y + xs1 * bounds1x );

    return  min(lengths0.r,
            min(lengths1.r,
            min(lengths0.g,
            min(lengths1.g,
            min(lengths0.b,
                lengths1.b)))));
}

float hsluv_maxChromaForLH(float L, float H) {

    float hrad = radians(H);

    mat3 m2 = mat3(
         3.2409699419045214  ,-0.96924363628087983 , 0.055630079696993609,
        -1.5373831775700935  , 1.8759675015077207  ,-0.20397695888897657 ,
        -0.49861076029300328 , 0.041555057407175613, 1.0569715142428786  
    );
    float sub1 = pow(L + 16.0, 3.0) / 1560896.0;
    float sub2 = sub1 > 0.0088564516790356308 ? sub1 : L / 903.2962962962963;

    vec3 top1   = (284517.0 * m2[0] - 94839.0  * m2[2]) * sub2;
    vec3 bottom = (632260.0 * m2[2] - 126452.0 * m2[1]) * sub2;
    vec3 top2   = (838422.0 * m2[2] + 769860.0 * m2[1] + 731718.0 * m2[0]) * L * sub2;

    vec3 bound0x = top1 / bottom;
    vec3 bound0y = top2 / bottom;

    vec3 bound1x =              top1 / (bottom+126452.0);
    vec3 bound1y = (top2-769860.0*L) / (bottom+126452.0);

    vec3 lengths0 = hsluv_lengthOfRayUntilIntersect(hrad, bound0x, bound0y );
    vec3 lengths1 = hsluv_lengthOfRayUntilIntersect(hrad, bound1x, bound1y );

    return  min(lengths0.r,
            min(lengths1.r,
            min(lengths0.g,
            min(lengths1.g,
            min(lengths0.b,
                lengths1.b)))));
}

float hsluv_fromLinear(float c) {
    return c <= 0.0031308 ? 12.92 * c : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}
vec3 hsluv_fromLinear(vec3 c) {
    return vec3( hsluv_fromLinear(c.r), hsluv_fromLinear(c.g), hsluv_fromLinear(c.b) );
}

float hsluv_toLinear(float c) {
    return c > 0.04045 ? pow((c + 0.055) / (1.0 + 0.055), 2.4) : c / 12.92;
}

vec3 hsluv_toLinear(vec3 c) {
    return vec3( hsluv_toLinear(c.r), hsluv_toLinear(c.g), hsluv_toLinear(c.b) );
}

float hsluv_yToL(float Y){
    return Y <= 0.0088564516790356308 ? Y * 903.2962962962963 : 116.0 * pow(Y, 1.0 / 3.0) - 16.0;
}

float hsluv_lToY(float L) {
    return L <= 8.0 ? L / 903.2962962962963 : pow((L + 16.0) / 116.0, 3.0);
}

vec3 xyzToRgb(vec3 tuple) {
    const mat3 m = mat3( 
        3.2409699419045214  ,-1.5373831775700935 ,-0.49861076029300328 ,
       -0.96924363628087983 , 1.8759675015077207 , 0.041555057407175613,
        0.055630079696993609,-0.20397695888897657, 1.0569715142428786  );
    
    return hsluv_fromLinear(tuple*m);
}

vec3 rgbToXyz(vec3 tuple) {
    const mat3 m = mat3(
        0.41239079926595948 , 0.35758433938387796, 0.18048078840183429 ,
        0.21263900587151036 , 0.71516867876775593, 0.072192315360733715,
        0.019330818715591851, 0.11919477979462599, 0.95053215224966058 
    );
    return hsluv_toLinear(tuple) * m;
}

vec3 xyzToLuv(vec3 tuple){
    float X = tuple.x;
    float Y = tuple.y;
    float Z = tuple.z;

    float L = hsluv_yToL(Y);
    
    float div = 1./dot(tuple,vec3(1,15,3)); 

    return vec3(
        1.,
        (52. * (X*div) - 2.57179),
        (117.* (Y*div) - 6.08816)
    ) * L;
}


vec3 luvToXyz(vec3 tuple) {
    float L = tuple.x;

    float U = tuple.y / (13.0 * L) + 0.19783000664283681;
    float V = tuple.z / (13.0 * L) + 0.468319994938791;

    float Y = hsluv_lToY(L);
    float X = 2.25 * U * Y / V;
    float Z = (3./V - 5.)*Y - (X/3.);

    return vec3(X, Y, Z);
}

vec3 luvToLch(vec3 tuple) {
    float L = tuple.x;
    float U = tuple.y;
    float V = tuple.z;

    float C = length(tuple.yz);
    float H = degrees(atan(V,U));
    if (H < 0.0) {
        H = 360.0 + H;
    }
    
    return vec3(L, C, H);
}

vec3 lchToLuv(vec3 tuple) {
    float hrad = radians(tuple.b);
    return vec3(
        tuple.r,
        cos(hrad) * tuple.g,
        sin(hrad) * tuple.g
    );
}

vec3 hsluvToLch(vec3 tuple) {
    tuple.g *= hsluv_maxChromaForLH(tuple.b, tuple.r) * .01;
    return tuple.bgr;
}

vec3 lchToHsluv(vec3 tuple) {
    tuple.g /= hsluv_maxChromaForLH(tuple.r, tuple.b) * .01;
    return tuple.bgr;
}

vec3 hpluvToLch(vec3 tuple) {
    tuple.g *= hsluv_maxSafeChromaForL(tuple.b) * .01;
    return tuple.bgr;
}

vec3 lchToHpluv(vec3 tuple) {
    tuple.g /= hsluv_maxSafeChromaForL(tuple.r) * .01;
    return tuple.bgr;
}

vec3 lchToRgb(vec3 tuple) {
    return xyzToRgb(luvToXyz(lchToLuv(tuple)));
}

vec3 rgbToLch(vec3 tuple) {
    return luvToLch(xyzToLuv(rgbToXyz(tuple)));
}

vec3 hsluvToRgb(vec3 tuple) {
    return lchToRgb(hsluvToLch(tuple));
}

vec3 rgbToHsluv(vec3 tuple) {
    return lchToHsluv(rgbToLch(tuple));
}

vec3 hpluvToRgb(vec3 tuple) {
    return lchToRgb(hpluvToLch(tuple));
}

vec3 rgbToHpluv(vec3 tuple) {
    return lchToHpluv(rgbToLch(tuple));
}

vec3 luvToRgb(vec3 tuple){
    return xyzToRgb(luvToXyz(tuple));
}

// allow vec4's
vec4   xyzToRgb(vec4 c) {return vec4(   xyzToRgb( vec3(c.x,c.y,c.z) ), c.a);}
vec4   rgbToXyz(vec4 c) {return vec4(   rgbToXyz( vec3(c.x,c.y,c.z) ), c.a);}
vec4   xyzToLuv(vec4 c) {return vec4(   xyzToLuv( vec3(c.x,c.y,c.z) ), c.a);}
vec4   luvToXyz(vec4 c) {return vec4(   luvToXyz( vec3(c.x,c.y,c.z) ), c.a);}
vec4   luvToLch(vec4 c) {return vec4(   luvToLch( vec3(c.x,c.y,c.z) ), c.a);}
vec4   lchToLuv(vec4 c) {return vec4(   lchToLuv( vec3(c.x,c.y,c.z) ), c.a);}
vec4 hsluvToLch(vec4 c) {return vec4( hsluvToLch( vec3(c.x,c.y,c.z) ), c.a);}
vec4 lchToHsluv(vec4 c) {return vec4( lchToHsluv( vec3(c.x,c.y,c.z) ), c.a);}
vec4 hpluvToLch(vec4 c) {return vec4( hpluvToLch( vec3(c.x,c.y,c.z) ), c.a);}
vec4 lchToHpluv(vec4 c) {return vec4( lchToHpluv( vec3(c.x,c.y,c.z) ), c.a);}
vec4   lchToRgb(vec4 c) {return vec4(   lchToRgb( vec3(c.x,c.y,c.z) ), c.a);}
vec4   rgbToLch(vec4 c) {return vec4(   rgbToLch( vec3(c.x,c.y,c.z) ), c.a);}
vec4 hsluvToRgb(vec4 c) {return vec4( hsluvToRgb( vec3(c.x,c.y,c.z) ), c.a);}
vec4 rgbToHsluv(vec4 c) {return vec4( rgbToHsluv( vec3(c.x,c.y,c.z) ), c.a);}
vec4 hpluvToRgb(vec4 c) {return vec4( hpluvToRgb( vec3(c.x,c.y,c.z) ), c.a);}
vec4 rgbToHpluv(vec4 c) {return vec4( rgbToHpluv( vec3(c.x,c.y,c.z) ), c.a);}
vec4   luvToRgb(vec4 c) {return vec4(   luvToRgb( vec3(c.x,c.y,c.z) ), c.a);}
// allow 3 floats
vec3   xyzToRgb(float x, float y, float z) {return   xyzToRgb( vec3(x,y,z) );}
vec3   rgbToXyz(float x, float y, float z) {return   rgbToXyz( vec3(x,y,z) );}
vec3   xyzToLuv(float x, float y, float z) {return   xyzToLuv( vec3(x,y,z) );}
vec3   luvToXyz(float x, float y, float z) {return   luvToXyz( vec3(x,y,z) );}
vec3   luvToLch(float x, float y, float z) {return   luvToLch( vec3(x,y,z) );}
vec3   lchToLuv(float x, float y, float z) {return   lchToLuv( vec3(x,y,z) );}
vec3 hsluvToLch(float x, float y, float z) {return hsluvToLch( vec3(x,y,z) );}
vec3 lchToHsluv(float x, float y, float z) {return lchToHsluv( vec3(x,y,z) );}
vec3 hpluvToLch(float x, float y, float z) {return hpluvToLch( vec3(x,y,z) );}
vec3 lchToHpluv(float x, float y, float z) {return lchToHpluv( vec3(x,y,z) );}
vec3   lchToRgb(float x, float y, float z) {return   lchToRgb( vec3(x,y,z) );}
vec3   rgbToLch(float x, float y, float z) {return   rgbToLch( vec3(x,y,z) );}
vec3 hsluvToRgb(float x, float y, float z) {return hsluvToRgb( vec3(x,y,z) );}
vec3 rgbToHsluv(float x, float y, float z) {return rgbToHsluv( vec3(x,y,z) );}
vec3 hpluvToRgb(float x, float y, float z) {return hpluvToRgb( vec3(x,y,z) );}
vec3 rgbToHpluv(float x, float y, float z) {return rgbToHpluv( vec3(x,y,z) );}
vec3   luvToRgb(float x, float y, float z) {return   luvToRgb( vec3(x,y,z) );}
// allow 4 floats
vec4   xyzToRgb(float x, float y, float z, float a) {return   xyzToRgb( vec4(x,y,z,a) );}
vec4   rgbToXyz(float x, float y, float z, float a) {return   rgbToXyz( vec4(x,y,z,a) );}
vec4   xyzToLuv(float x, float y, float z, float a) {return   xyzToLuv( vec4(x,y,z,a) );}
vec4   luvToXyz(float x, float y, float z, float a) {return   luvToXyz( vec4(x,y,z,a) );}
vec4   luvToLch(float x, float y, float z, float a) {return   luvToLch( vec4(x,y,z,a) );}
vec4   lchToLuv(float x, float y, float z, float a) {return   lchToLuv( vec4(x,y,z,a) );}
vec4 hsluvToLch(float x, float y, float z, float a) {return hsluvToLch( vec4(x,y,z,a) );}
vec4 lchToHsluv(float x, float y, float z, float a) {return lchToHsluv( vec4(x,y,z,a) );}
vec4 hpluvToLch(float x, float y, float z, float a) {return hpluvToLch( vec4(x,y,z,a) );}
vec4 lchToHpluv(float x, float y, float z, float a) {return lchToHpluv( vec4(x,y,z,a) );}
vec4   lchToRgb(float x, float y, float z, float a) {return   lchToRgb( vec4(x,y,z,a) );}
vec4   rgbToLch(float x, float y, float z, float a) {return   rgbToLch( vec4(x,y,z,a) );}
vec4 hsluvToRgb(float x, float y, float z, float a) {return hsluvToRgb( vec4(x,y,z,a) );}
vec4 rgbToHslul(float x, float y, float z, float a) {return rgbToHsluv( vec4(x,y,z,a) );}
vec4 hpluvToRgb(float x, float y, float z, float a) {return hpluvToRgb( vec4(x,y,z,a) );}
vec4 rgbToHpluv(float x, float y, float z, float a) {return rgbToHpluv( vec4(x,y,z,a) );}
vec4   luvToRgb(float x, float y, float z, float a) {return   luvToRgb( vec4(x,y,z,a) );}

/*
END HSLUV-GLSL
*/

/*  distort by mapping 0.5 to e.g. "target" 0.25

0 0
0.1 0.05  0.25 * 0.2  target * 2 * hue
0.2 0.1
0.3 0.15
0.4 0.2
0.5 0.25  target * 2 * hue
0.6 0.25 + 0.75 * 0.2  (1.0 - target) * (hue - 0.5) * 2
0.7 0.25 + 0.75 * 0.4
0.8 0.25 + 0.75 * 0.6
0.9 0.25 + 0.75 * 0.8
1.0 0.25 + 0.75 + 1.0 target + (1.0 - target) * (hue - 0.5)
*/

// Hue: 0 is red, 0.5 is cyan



#define UNIT_TO_HSLUV vec4(360.0, 100.0, 100.0, 1)

// WCS is "working color space" which is HSV / HSL / HSLUV etc scaled to unit ranges. Prefer the accessors .xyz

vec4 rgb2wcs(vec4 rgb, int color_space) {
  vec4 wcs;
  if (color_space == COLOR_SPACE_HSLUV || color_space == COLOR_SPACE_HSLB) {
    wcs = rgbToHsluv(rgb);
    wcs /= UNIT_TO_HSLUV;
  } else {
    wcs = rgb2hsv(rgb);
  }
  return wcs;
}

vec4 wcs2rgb(vec4 wcs, int color_space) {
  vec4 rgb;
  if (color_space == COLOR_SPACE_HSLUV || color_space == COLOR_SPACE_HSLB) {
    rgb = hsluvToRgb(wcs * UNIT_TO_HSLUV);
  } else {
    rgb = hsv2rgb(wcs);
  }
  return rgb;
}

float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

float limitHue(float hue, float width) {
  int MODE_REFLECT = 0;
  int MODE_CUT = 1;
  int mode = MODE_REFLECT;
  float center = 0.5;
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

float mapHue(float hue) {
  hue = flipSpectrum ? 1.0 - hue : hue;
  hue = limitHue(hue, range);
  hue = mod(hue + rotate, 1.0);
  // Bias
  if (hue < 0.5) {
    hue = hue * midpoint / 0.5;
  } else {
    hue = midpoint + (1.0 - midpoint) * (hue - 0.5) * 2.0;
  }
  return hue;
}

// Processes an RGB value according to all the current filter settings
#define colorSpace COLOR_SPACE_HSLB
vec4 mapRgb(vec4 rgb) {
  // Rendering content
#ifdef COMPARE_COLOR_SPACES
  vec4 wcs = rgb2wcs(rgb, isf_FragNormCoord.y < 0.5 ? COLOR_SPACE_HSV : colorSpace);
  wcs.x = mapHue(wcs.x);
  vec4 rgb_out = wcs2rgb(wcs, isf_FragNormCoord.y < 0.5 ? COLOR_SPACE_HSV : colorSpace);
#else
  vec4 wcs = rgb2wcs(rgb, colorSpace);
  wcs.x = mapHue(wcs.x);
  vec4 rgb_out = wcs2rgb(wcs, colorSpace);
#endif
  if (colorSpace == COLOR_SPACE_HSLB) {
    //HSLB takes the RGB output of HSLUV, determines the hue, then restores the brightness and saturation from the input pixel.
    vec4 rgb_from_hsluv = rgb_out;
    vec4 hsv_from_hsluv = rgb2hsv(rgb_from_hsluv);
    vec4 hsv_from_input = rgb2hsv(rgb);
    hsv_from_input.x = hsv_from_hsluv.x;
    rgb_out = hsv2rgb(hsv_from_input);
    rgb_out = mix(rgb_out, rgb_from_hsluv, love);
#ifdef COMPARE_COLOR_SPACES
  	if (isf_FragNormCoord.y < 0.5) {
      rgb_out = wcs2rgb(wcs, COLOR_SPACE_HSV);   // Undo the HSLB logic for the lower half (needed?)
    }
#endif
  }
  return rgb_out;
}

// TODO: histogram rounding error, on test card histogram is missing pure red - rounded to 0 index bin?

void main() { 
    vec2 loc = isf_FragNormCoord;
    
    if (showMapping && PASSINDEX == PASS_HISTOGRAM_COLUMNS) {
      // x is column, y is hue bin
      int hue_bin = int(gl_FragCoord.y);
      int height = int(IMG_SIZE(inputImage).y);
      vec4 out_color = vec4(0, 0, 0, 1);
      for (int row = 0; row < height; row++) {  // For each pixel in column
        vec4 hsv = rgb2hsv(IMG_PIXEL(inputImage, vec2(gl_FragCoord.x, row)));
        if (int(hsv.x * HISTOGRAM_BINS) == hue_bin && hsv.s > 0.0) {
          out_color.r += 0.01;
        }
      }
      gl_FragColor = out_color;
      return;
    }
    if (showMapping && PASSINDEX == PASS_HISTOGRAM) {
      // X is bin
      int width = int(IMG_SIZE(inputImage).x);
      float amount_of_this_bin = 0.0;
      for (int column = 0; column < width; column++) {
        amount_of_this_bin += IMG_PIXEL(histogramColumns, vec2(column, gl_FragCoord.x)).r;
      }
      gl_FragColor = vec4(amount_of_this_bin, amount_of_this_bin, amount_of_this_bin, 1.0);
      return;
    }
    if (PASSINDEX == PASS_RENDER) {
      if (showMapping && 0.95 > loc.y && loc.y > 0.93) {
        // Render histogram directly to RBG sidestepping colorspace used for content.
        int which_bin = int(loc.x * HISTOGRAM_BINS);
        float level = IMG_PIXEL(histogram, vec2(which_bin, 0)).r;
        level = log(level)/5.0; // Looking for useful range. Should probably be based on image size, or histogram max value at display time.
        gl_FragColor = vec4(level, level, level, 1.0);
      } else if (showMapping && loc.y > 0.88) {
        // Chroma spectra
        vec4 spectrumRgb = hsv2rgb(vec4(loc.x, 1.0, 1.0, 1.0));
        if (loc.y > 0.94) {
          gl_FragColor = spectrumRgb;  // Render input in classic maxed-out RGB
        } else {
          // Put the RGB value of the input spectrum through the same process as the content
          gl_FragColor = mapRgb(spectrumRgb);
        }
      } else {
        // Rendering content
        gl_FragColor = mapRgb(IMG_THIS_PIXEL(inputImage));
      }
    }
}
