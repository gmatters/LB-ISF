/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Color Adjustment",
		"LB"
	],
	"DESCRIPTION": "Print-style retro grunge",
	"INPUTS": [
		{
			"NAME": "inputImage",
			"TYPE": "image"
		},
		{
			"NAME": "level",
			"TYPE": "float",
			"MIN": 0.0,
			"MAX": 1.0,
			"DEFAULT": 0.5
		},
		{
			"NAME": "jitter",
			"TYPE": "float",
			"MIN": 0.0,
			"MAX": 1.0,
			"DEFAULT": 0.2
		}
	]
}*/

// TODO: spatial distortion on each CMYK channel, rather than just input
// TODO: plate noise for each color (spots, scratches)
// TODO: look up some gradient, noise or grain based on color of input, meaning that fields of solid color will have a visually contiguous texture applied that breaks at the color boundaries
// TODO: consider snoise3 to get 3 dimensions of noise, use different combinations of dimensions to get rand for each channel etc
// TODO: a lot of the paper texture noise is invariant and could be cached in a persistent texture to boost performance
// TODO: reliance on black plate noise to simulate paper texture causes hue distortion, e.g. dark green of pkds mushroom loses lots of black and the cyan shows through more causes it to look bluer. Should probably use rbg lighten to simulate paper texture, and different plate noise for black.
// TODO: plate streaks should be calculated "through" randomoffset, so it gets the paper texture wiggle and doesn't look so digital

vec4 sampleInputW(vec2 sourcePointW) {  // 'Working' means XY with 0,0 at corner
    return IMG_PIXEL(inputImage, sourcePointW);
}

// Out-of-bounds pixels are empty.
// W means "working" coordinates, pixel count from 0,0 at corner
vec4 sampleInputBoundedW(vec2 sourcePointW) {
      if (any(lessThan(sourcePointW, vec2(0., 0.))) || any(greaterThan(sourcePointW, RENDERSIZE))) {
        return vec4(0.);
      }
  return sampleInputW(sourcePointW);
}

// START: lygia.xyz
float mmin(const float v) { return v; }
float mmin(in float a, in float b) { return min(a, b); }
float mmin(in float a, in float b, in float c) { return min(a, min(b, c)); }
float mmin(in float a, in float b, in float c, in float d) { return min(min(a,b), min(c, d)); }

float mmin(const vec2 v) { return min(v.x, v.y); }
float mmin(const vec3 v) { return mmin(v.x, v.y, v.z); }
float mmin(const vec4 v) { return mmin(v.x, v.y, v.z, v.w); }

float mod289(const in float x) { return x - floor(x * (1. / 289.)) * 289.; }
vec2 mod289(const in vec2 x) { return x - floor(x * (1. / 289.)) * 289.; }
vec3 mod289(const in vec3 x) { return x - floor(x * (1. / 289.)) * 289.; }
vec4 mod289(const in vec4 x) { return x - floor(x * (1. / 289.)) * 289.; }

float permute(const in float v) { return mod289(((v * 34.0) + 1.0) * v); }
vec2 permute(const in vec2 v) { return mod289(((v * 34.0) + 1.0) * v); }
vec3 permute(const in vec3 v) { return mod289(((v * 34.0) + 1.0) * v); }
vec4 permute(const in vec4 v) { return mod289(((v * 34.0) + 1.0) * v); }

#if !defined(saturate)
#define saturate(V) clamp(V, 0.0, 1.0)
#endif

vec4 rgb2cmyk(const in vec3 rgb) {
    float k = mmin(1.0 - rgb);
    float invK = 1.0 - k;
    vec3 cmy = (1.0 - rgb - k) / invK;
    cmy *= step(0.0, invK);
    return saturate(vec4(cmy, k));
}

vec3 cmyk2rgb(const in vec4 cmyk) {
    float invK = 1.0 - cmyk.w;
    return saturate(1.0-min(vec3(1.0), cmyk.xyz * invK + cmyk.w));
}

float snoise(in vec2 v) {
    const vec4 C = vec4(0.211324865405187,  // (3.0-sqrt(3.0))/6.0
                        0.366025403784439,  // 0.5*(sqrt(3.0)-1.0)
                        -0.577350269189626,  // -1.0 + 2.0 * C.x
                        0.024390243902439); // 1.0 / 41.0
    // First corner
    vec2 i  = floor(v + dot(v, C.yy) );
    vec2 x0 = v -   i + dot(i, C.xx);

    // Other corners
    vec2 i1;
    //i1.x = step( x0.y, x0.x ); // x0.x > x0.y ? 1.0 : 0.0
    //i1.y = 1.0 - i1.x;
    i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    // x0 = x0 - 0.0 + 0.0 * C.xx ;
    // x1 = x0 - i1 + 1.0 * C.xx ;
    // x2 = x0 - 1.0 + 2.0 * C.xx ;
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
    + i.x + vec3(0.0, i1.x, 1.0 ));

    vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
    m = m*m ;
    m = m*m ;

    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)

    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;

    // Normalise gradients implicitly by scaling m
    // Approximation of: m *= inversesqrt( a0*a0 + h*h );
    m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );

    // Compute final noise value at P
    vec3 g;
    g.x  = a0.x  * x0.x  + h.x  * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

vec2 snoise2( vec2 x ){
    float s  = snoise(vec2( x ));
    float s1 = snoise(vec2( x.y - 19.1, x.x + 47.2 ));
    return vec2( s , s1 );
}

float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

#define RANDOM_SCALE vec4(443.897, 441.423, .0973, .1099)
vec2 random2(float p) {
    vec3 p3 = fract(vec3(p) * RANDOM_SCALE.xyz);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.xx + p3.yz) * p3.zy);
}
vec2 random2(vec2 p) {
    vec3 p3 = fract(p.xyx * RANDOM_SCALE.xyz);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.xx + p3.yz) * p3.zy);
}
// END: lygia.xyz

// Start: ChatGPT
// Function to generate 2D Perlin noise
float noise(vec2 p) {
    // Simplex noise or Perlin noise can be used here
    // For the sake of this example, let's use a basic noise function
    // You can replace this with a more complex noise function for better results
    return fract(sin(dot(p * 12.9898, vec2(78.233, 151.718))) * 43758.5453);
}

// Function to create a fiber effect
float fiber(vec2 uv) {
    float n = snoise(uv * 130.37); // Scale the noise
    return smoothstep(0.55, 0.99, n); // Control the fiber appearance
}
// End: ChatGPT

float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

float lerp(float val, float out_a, float out_b) {
  return lerp(val, 0., 1., out_a, out_b);
}

void main() {
  vec2 xyW = gl_FragCoord.xy;  // W variables are working space -> pixel coords
  float resolutionScale = RENDERSIZE.y/480.; // Normalize vs render resolution

  float fps = 6. + 6. * step(0.5, jitter);  // Update 6 or 12 fps
  float jitterTime = floor(TIME * fps) / fps;

  float colorOffsetLevel = resolutionScale * smoothstep(0.3, 1.0, level);
  float blackOffsetLevel = resolutionScale * level;
  float jitterOffsetLevel = level;
  float randomOffsetScale = resolutionScale * level; // resolutionScale is also used to scale the noise itself, but if we don't include it here also the wiggles get lost at large resolutions
  float rgbDimLevel = level;  // Bigger specks
  float rgbFiberDimLevel = smoothstep(0., 0.8, level);  // Smaller texture at limit of resolution
  float paperBrightLevel = level * level; // Weak spots in ink / highlights from paper texture
  float mPlateNoiseLevel = level;  // Vertical streaks

  vec2 jitterPos = (jitter * 5. * random2(jitterTime)) * jitterOffsetLevel;  // resolutionScale is multiplied in via blackOffsetLevel
  vec2 randomOffset = snoise2(xyW / (resolutionScale*5.));  // Make the edges wiggly

  vec2 cOffset = vec2(-3., -1.) * colorOffsetLevel;
  vec2 mOffset = vec2(5., 1.) * colorOffsetLevel * mix(1.0, sin(jitterTime), jitter);
  vec2 yOffset = vec2(1., 5.) * colorOffsetLevel;
  vec2 kOffset = (vec2(-0., -2.) + jitterPos) * blackOffsetLevel;
  vec4 pixelC = sampleInputBoundedW(xyW + cOffset + randomOffset * randomOffsetScale);
  vec4 pixelM = sampleInputBoundedW(xyW + mOffset + randomOffset * randomOffsetScale);
  vec4 pixelY = sampleInputBoundedW(xyW + yOffset + randomOffset * randomOffsetScale);
  vec4 pixelK = sampleInputBoundedW(xyW + kOffset + randomOffset * randomOffsetScale);
  float thisAlpha = max(max(pixelC.a, pixelM.a), max(pixelY.a, pixelK.a));

  vec4 cmyk;
  cmyk.x = rgb2cmyk(pixelC.rgb).x * pixelC.a;
  cmyk.y = rgb2cmyk(pixelM.rgb).y * pixelM.a;
  cmyk.z = rgb2cmyk(pixelY.rgb).z * pixelY.a;
  float paperDim = smoothstep(0.5, 1.0, randomOffset.x)*0.35 + smoothstep(0.0, 1.0, randomOffset.y)*0.10;
  paperDim *= paperBrightLevel;
  cmyk.w = (rgb2cmyk(pixelK.rgb).w - paperDim) * pixelK.a;

  float mPlateNoise = noise(vec2(xyW.x) * 0.01);
  cmyk.y *= mix(1.0, smoothstep(0.0, 0.05*mPlateNoiseLevel, mPlateNoise), mPlateNoiseLevel);

  vec3 rgb = cmyk2rgb(cmyk);
  rgb -= 0.5 * rgbFiberDimLevel * fiber(xyW / resolutionScale);
  float rgbDim = 1.0 - 0.5 * smoothstep(1.0-0.4*rgbDimLevel, 1.0, randomOffset.y)*rgbDimLevel;
  rgb *= rgbDim;

  gl_FragColor = vec4(rgb, thisAlpha);
}
