/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Kaleidoscope-style radial mirroring.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "LABEL": "Divisions",
            "DEFAULT": 6,
            "MAX": 16,
            "MIN": 1,
            "NAME": "_divisions",
            "TYPE": "float"
        },
        {
            "LABEL": "Edge Blend",
            "NAME": "blend",
            "DEFAULT": 0.2,
            "MIN": 0,
            "MAX": 1,
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Blend Mode",
            "LABELS": [
                "XFade",
                "Max2",
                "Max3",
                "Min",
                "Over (source alpha)"
            ],
            "NAME": "_mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1,
                2,
                3,
                4
            ]
        },
        {
            "NAME": "center",
            "TYPE": "point2D",
            "DEFAULT": [0.3,0.5],
            "IDENTITY": [0.5,0.5],
            "MIN": [0,0],
            "MAX": [1,1]
        },
        {
            "NAME": "angle",
            "TYPE": "float",
            "MIN": 0,
            "MAX": 1,
            "DEFAULT": 0
        },
        {
            "NAME": "scale",
            "TYPE": "float",
            "MIN": 0,
            "MAX": 3,
            "DEFAULT": 1
        },
        {
            "NAME": "radialScale",
            "TYPE": "float",
            "MIN": 0,
            "MAX": 3,
            "DEFAULT": 1
        },
        {
            "DEFAULT": 0,
            "LABEL": "When you hit the edge of the input image",
            "LABELS": [
                "Mirror",
                "Transparent",
                "Smear edge pixel"
            ],
            "NAME": "_out_of_bounds",
            "TYPE": "long",
            "VALUES": [
                0,
                1,
                2
            ]
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
        }
    ]
}
*/

#define MODE_XFADE 0
#define MODE_MAX2 1
#define MODE_MAX3 2
#define MODE_MIN 3
#define MODE_OVER 4

#define BOUNDS_MIRROR 0
#define BOUNDS_TRANSPARENT 1
#define BOUNDS_SMEAR 2

// TODO: a 'smooth' control which does non-linear radius that expands center area, thus giving it more area in the output to prevent it from disappearing.
// TODO: see notes in keep for other ideas
// TODO: consider a blur or blend at the center point to reduce the digitalness of the singularity
// TODO: consider option to anti-alias edge of circle (notably jagged when isfedit is at low resolution)

float map(float n, float i1, float i2, float o1, float o2){
  return o1 + (o2-o1) * (n-i1)/(i2-i1);
}

const float M_PI = 3.14159265359;
const float pi = 3.14159265359;
const float tau = 6.28318530718;

// COMMON
// Percent: 0 is edge == faded out, 1 is center == faded in
// By its nature, whites will fade in much faster and reach full opacity long before 1
vec4 applyNoEdge(vec4 texel, float percent)
{   
    float amount = 1.0;  // "how much" fading
    // Leave center half+ untouched, ramp down outer n%
    float fadeoutSize = 0.5 * amount;
    // Whites fades out less aggressively
    float fadeoutSizeHighs = 0.2 * amount; 
    float spatialMapLows = smoothstep(0.0, fadeoutSize, percent);
    float spatialMapHighs = smoothstep(0.0, fadeoutSizeHighs, percent);
    /*** Make it dynamic from pixel content ***/
    // Get a measure of pixel brightness
    // TODO: perceptual brightness (ala desaturate)? All channels? One channel?
    float brightness;
    brightness = max(texel.r, max(texel.g, texel.b)); 
    brightness = mix(spatialMapLows, spatialMapHighs, brightness);
    // Apply final mask value to input
    texel.a *= brightness;
    return texel;
}


// Determine output pixel for some part of a slice
// Because each slice has its center rendered as thisSlice, and its edges rendered as otherSlice, the formula needs to be symmetrical

float linearstep( float A, float B, float X )
{
   float t = ( X - A ) / ( B - A );

   return clamp( t, 0., 1. );
}

vec4 compPixelLinearBlend(vec4 thisSliceValue, vec4 prevSliceValue, vec4 nextSliceValue, float percentThroughSlice, float blendAmt)
{
    float halfBlend = blendAmt/2.0;
    if (percentThroughSlice > 1.0 - halfBlend) {
        return mix(thisSliceValue, nextSliceValue, linearstep(1.0-halfBlend, 1.0+halfBlend, percentThroughSlice));
    } else if (percentThroughSlice < halfBlend){
        return mix(prevSliceValue, thisSliceValue, linearstep(-halfBlend, halfBlend, percentThroughSlice));
    }
    return thisSliceValue;
}


vec4 compPixelMin(vec4 thisSliceValue, vec4 prevSliceValue, vec4 nextSliceValue, float percentThroughSlice)
{
    // We actually have enough data to crossfade between prev and next to generate a
    // second pixel to compare with this slice's
    vec4 otherValue = mix(prevSliceValue, nextSliceValue, percentThroughSlice);
    return min(thisSliceValue, otherValue);
}

vec4 compPixelMax2(vec4 thisSliceValue, vec4 prevSliceValue, vec4 nextSliceValue, float percentThroughSlice)
{
    vec4 otherValue = mix(prevSliceValue, nextSliceValue, percentThroughSlice);
    return max(thisSliceValue, otherValue);
}

vec4 premultiply(vec4 pixel) {
  return vec4(pixel.rgb * pixel.a, pixel.a);
}

vec4 compPixelMax3(vec4 thisSliceValue, vec4 prevSliceValue, vec4 nextSliceValue, float percentThroughSlice)
{
    prevSliceValue = premultiply(applyNoEdge(prevSliceValue, 1.0-percentThroughSlice));
    nextSliceValue = premultiply(applyNoEdge(nextSliceValue, percentThroughSlice));
    return max(prevSliceValue, max(thisSliceValue, nextSliceValue));
}

// Over mostly makes sense if there is alpha involved
vec4 compPixelOver(vec4 thisSliceValue, vec4 prevSliceValue, vec4 nextSliceValue, float percentThroughSlice)
{
    // TODO: handling of alpha is probably wrong, should use common
    if (percentThroughSlice > 0.5) {
        return mix(nextSliceValue, thisSliceValue, thisSliceValue.a);  // This covers start of next
    } else {
        return mix(thisSliceValue, prevSliceValue, prevSliceValue.a);  // This is covered by overrun of prev
    }
}

// lygia.xyz
vec2 mirror(in vec2 v) {
    vec2 f = fract(v);
    vec2 m = floor(mod(v, 2.));
    vec2 fm = f * m;
    return f + m - fm * 2.;
}

// lygia.xyz
vec2 cart2polar(in vec2 st) {
    return vec2(atan(st.y, st.x), length(st));
}
vec2 polar2cart(in vec2 polar) {
    return vec2(cos(polar.x), sin(polar.x)) * polar.y;
}

vec4 sampleInput(vec2 sourcePointW) {
    if (_out_of_bounds == BOUNDS_MIRROR) {
      // VDMX shows artifacts, crop out the outer pixel on all sides when mirroring
      vec2 sourcePointMirrored = mirror(sourcePointW / RENDERSIZE) * (RENDERSIZE-2.) + vec2(1.);
      return IMG_PIXEL(inputImage, sourcePointMirrored);
    }
    if (_out_of_bounds == BOUNDS_TRANSPARENT) {
      if (any(lessThan(sourcePointW, vec2(0., 0.))) || any(greaterThan(sourcePointW, RENDERSIZE))) {
        return vec4(0.);
      }
    }
    // Default pixel lookup smears edge pixels
    return IMG_PIXEL(inputImage, sourcePointW);
}

void main()
{

    vec2 _center = center * RENDERSIZE; // Center in exact pixel coords
    int mode = int(_mode);
    vec2 xyW = gl_FragCoord.xy;  // W variables are working space -> pixel coords
    vec2 centerPointW = RENDERSIZE / 2.0;

    float angleRads = angle * 2.0*M_PI; // Which direction to sample, scaled 0-1
    float distanceScale = 1.0 / scale;  // Zoom in (helps avoid underflow)... making the scale appear larger requires sampling pixels from a smaller source area
    vec2 sampleSourcePoint = vec2(_center.x, _center.y); // Where to start the sampling of the input image

    float divisions = floor(_divisions);
    float radsPerSlice = 2.0*M_PI/floor(_divisions);

    // Convert cartesian x,y to polar coordinates relative to center.
    float destRads = atan(xyW.y - centerPointW.y, xyW.x - centerPointW.x);
    float destDist = distance(centerPointW, xyW) * distanceScale;

    destRads -= radsPerSlice / 2.0;  // Shifts so slice is centered along X axis of output (rather than starting at it)
    destRads = mod(destRads, radsPerSlice);

    // Calculate where we are taking the pixels from
    float radsThis = destRads - radsPerSlice / 2.0;  // Shifts the pixels we are going to look up to be along X axis of source
    radsThis += angleRads;
    float radsPrev = radsThis + radsPerSlice;
    float radsNext = radsThis - radsPerSlice;

    // Add some radial compression (squeeze in wider source angle)
    radsThis *= radialScale;
    radsPrev *= radialScale;
    radsNext *= radialScale;

    // Convert polar to cartesian
    vec2 sourcePointW = polar2cart(vec2(radsThis, destDist));
    vec2 sourcePointPrev = polar2cart(vec2(radsPrev, destDist));
    vec2 sourcePointNext = polar2cart(vec2(radsNext, destDist));

    sourcePointW += sampleSourcePoint;
    sourcePointPrev += sampleSourcePoint;
    sourcePointNext += sampleSourcePoint;

    // Get the pixel values for each slice
    // TODO: we don't always use all slices, it would perform better to only calculate what we need
    vec4 thisSliceValue = sampleInput(sourcePointW);
    vec4 prevSliceValue = sampleInput(sourcePointPrev);
    vec4 nextSliceValue = sampleInput(sourcePointNext);

    // Composite
    vec4 outValue;
    float percentThroughSlice = destRads / radsPerSlice;

    if (mode == MODE_MAX2)
        outValue = compPixelMax2(thisSliceValue, prevSliceValue, nextSliceValue, percentThroughSlice);
    else if (mode == MODE_MAX3)
        outValue = compPixelMax3(thisSliceValue, prevSliceValue, nextSliceValue, percentThroughSlice);
    else if (mode == MODE_MIN)
        outValue = compPixelMin(thisSliceValue, prevSliceValue, nextSliceValue, percentThroughSlice);
    else if (mode == MODE_OVER)
        outValue = compPixelOver(thisSliceValue, prevSliceValue, nextSliceValue, percentThroughSlice);
    else
        outValue = compPixelLinearBlend(thisSliceValue, prevSliceValue, nextSliceValue, percentThroughSlice, blend);

    gl_FragColor = outValue;
}

