/*{
  "DESCRIPTION": "Break input image into pieces and collage back together.",
  "CREDIT": "by Geoff Matters (with ChatGPT)",
  "CATEGORIES": ["LB"],
  "INPUTS":[
    { "NAME":"inputImage","TYPE":"image" },
    { "NAME":"baseOffsetRange","LABEL":"Offset","TYPE":"float","DEFAULT":0.02,"MIN":0.0,"MAX":0.2 },
    { "NAME":"baseRotation","LABEL":"Rotation","TYPE":"float","DEFAULT":2.0,"MIN":0.0,"MAX":180.0 },
    { "NAME":"borderColor","LABEL":"Border Color","TYPE":"color","DEFAULT":[1.0,1.0,1.0,1.0] },
    { "NAME":"borderSize","LABEL":"Border Thickness","TYPE":"float","DEFAULT":0.01,"MIN":0.0,"MAX":0.05 },
    { "NAME":"backgroundColor","LABEL":"Background Color","TYPE":"color","DEFAULT":[0.0,0.0,0.0,1.0] },
    { "NAME":"randomSeed","LABEL":"Random Seed","TYPE":"float","DEFAULT":0.0,"MIN":0.0,"MAX":1000.0 },
    { "NAME":"positionJitter","LABEL":"Jitter Position","TYPE":"float","DEFAULT":1.0,"MIN":0.0,"MAX":100.0 },
    { "NAME":"rotationJitter","LABEL":"Jitter Rotation","TYPE":"float","DEFAULT":1.0,"MIN":0.0,"MAX":45.0 }
  ]
}*/


/*

write a glsl shader in ISF format which takes an input image and outputs a collage of 20 subimage tiles, each with independent position and bounds. Each tile samples from the corresponding part of the input image, with a slight offset (+-5% of image size) and rotation (+-2 degrees). Sampling is calculated from center to avoid a directional bias in the output. Ensure rotation is calculated so the corners remain 90 degrees and there is no skew of the tile shape or of the sampled image. Each tile has an antialiased border, with the border color defined as user-adjustable ISF controls. The border size is a user-adjustable ISF control in the range 0 - 0.05 and a default of 0.01. If the border size is 0, the application of the border mask is bypassed entirely. Each tile fills between 30 and 50% of the output image. The edges of the tiles are antialiased to avoid jagged edges, calculating lower tiles as needed or blending into the background color if there is no lower tile. Give each tile a drop shadow. There is a user-adjustable control for a random seed, which is incorporated into the rand hash functions. Each tile's position and rotation has additional jitter, updated 12 times per second, with the jitter amounts exposed as user-adjustable controls. Calculate in non-normalized space.

Then fix:

replace texture2D with IMG_ NORM_ PIXEL

inverted border by swapping smoothstep terms

delete uniforms for input args

export jitterOffsetPx

fix calculation of tileUV and shadowUV to apply rotate in pixel space, avoiding skew

fix calculation of sampleUV, which was skewed version of full image

rename variables

expose baseOffsetRange, rotation  as user args

avoid out-of-bounds

*/

#ifdef GL_ES
precision mediump float;
#endif

const int NUM_TILES = 20;
const float minScale = 0.3;
const float maxScale = 0.5;
const float edgeAASize = 2.0;  // in pixels
const vec2 shadowOffset = vec2(10.0);
const float shadowAlpha = 0.3;
const float shadowFeather = 6.0;

float steppedTime(float t){ return floor(t * 12.0) / 12.0; }
float rand(float x){ return fract(sin(x + randomSeed)*43758.5453); }
vec2 rand2(float x){ return vec2(rand(x), rand(x+1.234)); }
float randBetween(float x,float a,float b){ return mix(a,b,rand(x)); }
mat2 rotate(float a){ float c=cos(a), s=sin(a); return mat2(c,-s,s,c); }

void getTile(int i, float t, out vec2 centerPx, out vec2 sizePx, out vec2 offsetUV, out float angle, out vec2 jitterOffsetPx){
    float fi = float(i);
    vec2 centerUV = 0.1 + 0.8 * rand2(fi*1.1);
    float scaleUV = randBetween(fi*2.2, minScale, maxScale);
    sizePx = RENDERSIZE * scaleUV;
    offsetUV = (rand2(fi*3.3) - 0.5)*2.0*baseOffsetRange;
    if (centerUV.x + scaleUV/2.0 + offsetUV.x > 1.0) {
      //offsetUV.x = 1.0 - centerUV.x - scaleUV/2.0;  // Prevent OOB by cheating offset
      centerUV.x = 1.0 - offsetUV.x - scaleUV/2.0;  // Prevent OOB by cheating center
    }
    if (centerUV.y + scaleUV/2.0 + offsetUV.y > 1.0) {
      centerUV.y = 1.0 - offsetUV.y - scaleUV/2.0;  // Prevent OOB by cheating center
    }
    if (centerUV.x - scaleUV/2.0 + offsetUV.x < 0.0) {
      centerUV.x = 0.0 - offsetUV.x + scaleUV/2.0;  // Prevent OOB by cheating center
    }
    if (centerUV.y - scaleUV/2.0 + offsetUV.y < 0.0) {
      centerUV.y = 0.0 - offsetUV.y + scaleUV/2.0;  // Prevent OOB by cheating center
    }
    float baseAng = radians(randBetween(fi*4.4, -baseRotation, baseRotation));
    float seed = fi*10.0 + t*100.0;
    jitterOffsetPx = (rand2(seed)-0.5)*2.0*positionJitter;  // TODO: is input arg pix or percent or fraction?
    float jitAng = radians((rand(seed+5.0)-0.5)*2.0*rotationJitter);
    centerPx = centerUV * RENDERSIZE;
    angle = baseAng + jitAng;
}

void main(){
    vec2 thisPx = gl_FragCoord.xy;
    vec4 accum = backgroundColor;
    float t = steppedTime(TIME);

    for(int i = 0; i < NUM_TILES; i++){
        vec2 centerPx, sizePx, offsetUV, jitterOffsetPx;
        float angle;
        getTile(i, t, centerPx, sizePx, offsetUV, angle, jitterOffsetPx);
        centerPx += jitterOffsetPx;

        // Drop shadow
        vec2 shadowCenterPx = thisPx - (centerPx + shadowOffset);
        vec2 shadowUV = (rotate(-angle) * shadowCenterPx) / sizePx + 0.5;
        if(all(greaterThanEqual(shadowUV, vec2(0.0))) && all(lessThanEqual(shadowUV, vec2(1.0)))){
            float e = min(min(shadowUV.x,1.0-shadowUV.x),min(shadowUV.y,1.0-shadowUV.y));
            float mask = smoothstep(0.0, shadowFeather/length(sizePx), e);
            vec4 sh = vec4(0.0,0.0,0.0, shadowAlpha * mask);
            accum = mix(accum, sh, sh.a);
        }

        // Main tile
        vec2 localCenterPx = thisPx - centerPx;
        vec2 tileUV = (rotate(-angle) * localCenterPx) / sizePx + 0.5;  // Normalized position WRT tile; 0-1 are tile bounds
        if(all(greaterThanEqual(tileUV, vec2(0.0))) && all(lessThanEqual(tileUV, vec2(1.0)))){
            vec2 samplePx = rotate(-angle) * localCenterPx + centerPx;
            vec2 sampleUV = samplePx / RENDERSIZE + offsetUV - jitterOffsetPx / RENDERSIZE;
            vec4 col = IMG_NORM_PIXEL(inputImage, sampleUV);
            //col = vec4(vec3(0.05, 0.05, 0.05) * float(i), 1.0);  // Debug tiles as solid greyscale
            //if (any(lessThan(sampleUV, vec2(0.0))) || any(greaterThan(sampleUV, vec2(1.0)))){
            //  col = vec4(1.0, 0.0, 1.0, 1.0);  // Debug out-of-bounds samples
            //}

            float e = min(min(tileUV.x,1.0-tileUV.x),min(tileUV.y,1.0-tileUV.y));
            float edgeMask = smoothstep(0.0, edgeAASize/length(sizePx), e);

            vec4 outC = col;
            if(borderSize > 0.0){
                float bmask = smoothstep(borderSize + edgeAASize/length(sizePx), borderSize, e);
                outC = mix(col, borderColor, bmask);
            }
            outC.a *= edgeMask;
            accum = mix(accum, outC, outC.a);
        }
    }

    gl_FragColor = vec4(accum.rgb, 1.0);
}

