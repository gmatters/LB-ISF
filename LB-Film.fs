/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Color Adjustment",
		"LB"
	],
	"DESCRIPTION": "Film-style retro grunge",
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

// Lots of inspiration from texturelabs https://www.youtube.com/watch?v=pIr64mLJtdU

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

float taylorInvSqrt(in float r) { return 1.79284291400159 - 0.85373472095314 * r; }
vec2 taylorInvSqrt(in vec2 r) { return 1.79284291400159 - 0.85373472095314 * r; }
vec3 taylorInvSqrt(in vec3 r) { return 1.79284291400159 - 0.85373472095314 * r; }
vec4 taylorInvSqrt(in vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

#if !defined(saturate)
#define saturate(V) clamp(V, 0.0, 1.0)
#endif


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

float snoise(in vec3 v) {
    const vec2  C = vec2(1.0/6.0, 1.0/3.0) ;
    const vec4  D = vec4(0.0, 0.5, 1.0, 2.0);

    // First corner
    vec3 i  = floor(v + dot(v, C.yyy) );
    vec3 x0 =   v - i + dot(i, C.xxx) ;

    // Other corners
    vec3 g = step(x0.yzx, x0.xyz);
    vec3 l = 1.0 - g;
    vec3 i1 = min( g.xyz, l.zxy );
    vec3 i2 = max( g.xyz, l.zxy );

    //   x0 = x0 - 0.0 + 0.0 * C.xxx;
    //   x1 = x0 - i1  + 1.0 * C.xxx;
    //   x2 = x0 - i2  + 2.0 * C.xxx;
    //   x3 = x0 - 1.0 + 3.0 * C.xxx;
    vec3 x1 = x0 - i1 + C.xxx;
    vec3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    vec3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

    // Permutations
    i = mod289(i);
    vec4 p = permute( permute( permute(
                i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
            + i.y + vec4(0.0, i1.y, i2.y, 1.0 ))
            + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float n_ = 0.142857142857; // 1.0/7.0
    vec3  ns = n_ * D.wyz - D.xzx;

    vec4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

    vec4 x_ = floor(j * ns.z);
    vec4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

    vec4 x = x_ *ns.x + ns.yyyy;
    vec4 y = y_ *ns.x + ns.yyyy;
    vec4 h = 1.0 - abs(x) - abs(y);

    vec4 b0 = vec4( x.xy, y.xy );
    vec4 b1 = vec4( x.zw, y.zw );

    //vec4 s0 = vec4(lessThan(b0,0.0))*2.0 - 1.0;
    //vec4 s1 = vec4(lessThan(b1,0.0))*2.0 - 1.0;
    vec4 s0 = floor(b0)*2.0 + 1.0;
    vec4 s1 = floor(b1)*2.0 + 1.0;
    vec4 sh = -step(h, vec4(0.0));

    vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
    vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

    vec3 p0 = vec3(a0.xy,h.x);
    vec3 p1 = vec3(a0.zw,h.y);
    vec3 p2 = vec3(a1.xy,h.z);
    vec3 p3 = vec3(a1.zw,h.w);

    //Normalise gradients
    vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
    m = m * m;
    return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1),
                                dot(p2,x2), dot(p3,x3) ) );
}

#ifndef FBM_NOISE_FNC
#define FBM_NOISE_FNC(UV) snoise(UV)
#endif

#ifndef FBM_NOISE2_FNC
#define FBM_NOISE2_FNC(UV) FBM_NOISE_FNC(UV)
#endif

#ifndef FBM_NOISE3_FNC
#define FBM_NOISE3_FNC(UV) FBM_NOISE_FNC(UV)
#endif

#ifndef FBM_NOISE_TILABLE_FNC
#define FBM_NOISE_TILABLE_FNC(UV, TILE) gnoise(UV, TILE)
#endif

#ifndef FBM_NOISE3_TILABLE_FNC
#define FBM_NOISE3_TILABLE_FNC(UV, TILE) FBM_NOISE_TILABLE_FNC(UV, TILE)
#endif

#ifndef FBM_NOISE_TYPE
#define FBM_NOISE_TYPE float
#endif

#ifndef FBM_VALUE_INITIAL
#define FBM_VALUE_INITIAL 0.0
#endif

#ifndef FBM_SCALE_SCALAR
#define FBM_SCALE_SCALAR 2.0
#endif

#ifndef FBM_AMPLITUD_INITIAL
#define FBM_AMPLITUD_INITIAL 0.5
#endif

#ifndef FBM_AMPLITUD_SCALAR
#define FBM_AMPLITUD_SCALAR 0.5
#endif

#ifndef FNC_FBM
#define FNC_FBM
FBM_NOISE_TYPE fbm(in vec2 st, in int octaves) {
    // Initial values
    FBM_NOISE_TYPE value = FBM_NOISE_TYPE(FBM_VALUE_INITIAL);
    float amplitud = FBM_AMPLITUD_INITIAL;

    // Loop of octaves
    for (int i = 0; i < octaves; i++) {
        value += amplitud * FBM_NOISE2_FNC(st);
        st *= FBM_SCALE_SCALAR;
        amplitud *= FBM_AMPLITUD_SCALAR;
    }
    return value;
}

FBM_NOISE_TYPE fbm(in vec3 pos, in int octaves) {
    // Initial values
    FBM_NOISE_TYPE value = FBM_NOISE_TYPE(FBM_VALUE_INITIAL);
    float amplitud = FBM_AMPLITUD_INITIAL;

    // Loop of octaves
    for (int i = 0; i < octaves; i++) {
        value += amplitud * FBM_NOISE3_FNC(pos);
        pos *= FBM_SCALE_SCALAR;
        amplitud *= FBM_AMPLITUD_SCALAR;
    }
    return value;
}

float blendScreen(in float base, in float blend) {
    return 1. - ((1. - base) * (1. - blend));
}

vec3 blendScreen(in vec3 base, in vec3 blend) {
    return vec3(blendScreen(base.r, blend.r),
                blendScreen(base.g, blend.g),
                blendScreen(base.b, blend.b));
}
#endif

#define RANDOM_SCALE vec4(443.897, 441.423, .0973, .1099)
float random(in float x) {
    x = fract(x * RANDOM_SCALE.x);
    x *= x + 33.33;
    x *= x + x;
    return fract(x);
}
float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}
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
vec3 random3(float p) {
    vec3 p3 = fract(vec3(p) * RANDOM_SCALE.xyz);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.xxy + p3.yzz) * p3.zyx); 
}

float compositeSourceAtop(float src, float dst) {
    return src * dst + dst * (1.0 - src);
}

vec3 compositeSourceAtop(vec3 srcColor, vec3 dstColor, float srcAlpha, float dstAlpha) {
    return srcColor * dstAlpha + dstColor * (1.0 - srcAlpha);
}

vec4 compositeSourceAtop(vec4 srcColor, vec4 dstColor) {
    vec4 result = vec4(0.0);

    result.rgb = compositeSourceAtop(srcColor.rgb, dstColor.rgb, srcColor.a, dstColor.a);
    result.a = compositeSourceAtop(srcColor.a, dstColor.a);

    return result;
}
// END: lygia.xyz


float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

float lerp(float val, float out_a, float out_b) {
  return lerp(val, 0., 1., out_a, out_b);
}

vec3 scratch(vec2 xyW, vec2 whW, float resolutionScale) {
  float time24 = floor(TIME * 24.);
  // Jitter and tilt offset the sampling of noise
  vec2 random24 = random2(time24);
  float random24Centered = random24.x - 0.5;
  vec2 jitter = vec2(10, 0) * random24;
  float tiltScale = 0.01;
  vec2 tilt = vec2(xyW.y, 0);
  if (random24.y < 0.5) { // half the time
    tilt.x = (whW.y - xyW.y); // move bottom edge
  }
  tilt.x *= tiltScale * random24Centered;
  // Sample noise to generate lines
  float noise = fbm(vec3((xyW + jitter + tilt)/(resolutionScale * vec2(25., 20000.)), TIME / 3.), 2);
  noise = 1.51 - noise;  // 1.55 is very few scratches, 1.4 is so many that the illusion falls apart
  noise = clamp(noise, 0.8, 1.);  // Prevent oversaturation, also limit depth of scratches
  vec3 rv = vec3(noise);
  return rv;
}

vec3 toner3(vec3 colorLow, vec3 colorMid, vec3 colorHigh, float brightness) {
  if (brightness < 0.5) {
    return mix(colorLow, colorMid, brightness * 2.);
  }
  return mix(colorMid, colorHigh, (brightness-0.5) * 2.);
}

vec3 toner3(vec3 colorLow, vec3 colorMid, vec3 colorHigh, vec3 inPixel) {
  float brightness = (inPixel.r + inPixel.b + inPixel.g) / 3.;
  return toner3(colorLow, colorMid, colorHigh, brightness);
}

vec3 leak(vec2 xyW, float resolutionScale) {
  // Fractal Noise
  float resolutionScale480 = resolutionScale * 1080. / 480.;  // Historical reasons
  float noise = 0.2 + 0.4 * fbm(vec3(xyW/(resolutionScale480*vec2(2000., 4000.)), TIME), 2);
  vec3 rv = vec3(noise);
  // Toner
  float time = TIME * 5.;  // 5hz
  vec3 colorA = random3(floor(time));
  vec3 colorB = random3(floor(time)+1.);
  vec3 color = mix(colorA, colorB, fract(time));
  vec3 colorDark = vec3(0.19, 0.23, 0.29);
  rv =  noise < 0.25 ? mix(vec3(0.), colorDark, noise * 4.) : mix (colorDark, color, (noise - 0.25)*4.);
  float noise2 = 0.3 + 0.8 * fbm(vec3(xyW/(resolutionScale480*vec2(1500., 3000.)), TIME/4.), 1);
  rv *= noise2;
  return rv;
}

vec4 damage(vec2 xyW, vec2 whW, float resolutionScale) {
  float time24 = floor(TIME * 24.);
  float seed = TIME * 30.;  // Crank up time to simulate random  // samples: 750, 1050
  float noise = fbm(vec3(xyW/(resolutionScale * vec2(300., 500.)), seed), 7);
  noise = smoothstep(0.7, 0.75, noise);
  noise = clamp(noise, 0., 1.);  // Prevent oversaturation, also limit depth of scratches
  // Texturelabs uses a second noise to create texture inside the spots, but that didn't seem necessary
  vec3 damageRGB = toner3(vec3(0.,0.,0.), vec3(0.184, 0.765, 0.369), vec3(1.,1.,1.), noise);
  vec4 rv = vec4(damageRGB, smoothstep(0.,0.1,noise));
  return rv;
}

vec4 film(vec4 inPixel, vec2 xyW, vec2 whW) {
  float resolutionScale = whW.y/1080.; // Normalize vs render resolution. e.g. 4k will be 2.0
  vec3 leakRBG = leak(xyW, resolutionScale);
  vec3 scratchRBG = scratch(xyW, whW, resolutionScale);
  vec4 damageRBGA = damage(xyW, whW, resolutionScale); //return damageRBGA;

  vec3 rv = blendScreen(inPixel.rgb, leakRBG);
  rv = compositeSourceAtop(damageRBGA, vec4(rv, inPixel.a)).rgb;
  rv *= scratchRBG;
  return vec4(rv, inPixel.a); // Don't ever turn transparent input into non-transparent output.
}

void main() {
  vec2 xyW = gl_FragCoord.xy;  // W variables are working space -> pixel coords
  vec2 whW = RENDERSIZE;  // W variables are working space -> pixel coords

  float fps = 6. + 6. * step(0.5, jitter);  // Update 6 or 12 fps
  float jitterTime = floor(TIME * fps) / fps;


  vec4 inPixel = sampleInputBoundedW(xyW);
  gl_FragColor = film(inPixel, xyW, whW);
}
