/*{
  "CATEGORIES": ["LB", "Distortion", "Transform"],
  "DESCRIPTION": "Simply add motion via Various animated tranforms and deforms.",
  "CREDIT": "by Geoff Matters (with ChatGPT)",
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },

    { "NAME": "sinAmount",    "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.2 },
    { "NAME": "sinSpeed",     "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0, "MAX": 5.0 },

    { "NAME": "noiseAmount",  "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.2 },
    { "NAME": "noiseSpeed",   "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0, "MAX": 5.0 },

    { "NAME": "moveAmount",   "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.5 },
    { "NAME": "moveSpeed",    "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 20.0 },

    { "NAME": "scaleAmount",  "TYPE": "float", "DEFAULT": 0.02,  "MIN": 0.0, "MAX": 0.3 },
    { "NAME": "scaleSpeed",   "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 10.0 },

    { "NAME": "rotateAmount", "TYPE": "float", "DEFAULT": 0.01,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "rotateSpeed",  "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 2.0 },

    {
      "NAME": "wrapMode",
      "TYPE": "long",
      "LABELS": ["wrap", "reflect", "clamp", "transparent"],
      "VALUES": [0, 1, 2, 3],
      "DEFAULT": 3
    }
  ],
  "PASSES": [
    {
      "TARGET": "ControlsBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 10,
      "HEIGHT": 1
    },
    {
      "TARGET": "AccumBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 5,
      "HEIGHT": 1
    },
    {}
  ]
}*/

/* 

Write a GLSL shader in the ISF format. The shader uses multiple passes into persistent float buffers to smooth changes in user-adjustable ISF controls, and to accumulate time-driven values so that they don't jump around when control values change. The persistent float buffers store one value per pixel, in the red channel. The alpha channel must always be 1.0. The persistent float buffers have a height of 1 and a width equal to the number of values being stored in each. There is a single main() method which switches on PASSINDEX. The shader applies sin/cos moving spatial distortion on the input. It also applies perlin-noise based spatial distortion. It also applies time-varying position offset that causes the image to move around smoothly but unpredictably. It also applies uniform scale which causes the image to grow and shrink smoothly but unpredictable. It also applies a 2D rotation that causes the image to rotate clockwise and counterclockwise smoothly but unpredictably. There are user-adjustable ISF controls for amount and movement speed for each type of distortion, movement, scale, and rotation. There is a user-adjustable ISF control of type long with value 0, 1, 2, 3 and labels "wrap", "reflect", "clamp", "transparent" which controls how out-of-bounds samples are handled.

Then fix:

isf_FragCoord to gl_FragCoord

sampleWrapped(vec2 uv) assumes inputImage

fix sampleWrapped reflect mode (1.0 - blah)

greater speed for movement and zoom

rename variables

tune input defaults, ranges, labels

TODO: rotate in real vs nominal space?

*/

// === Perlin-style Noise ===
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f*f*(3.0 - 2.0*f);
    return mix(
        mix(hash(i + vec2(0.0, 0.0)), hash(i + vec2(1.0, 0.0)), u.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
        u.y
    );
}

// === Sample with wrap mode ===
vec4 sampleWrapped(vec2 uv) {
    if (wrapMode == 0) {
        return IMG_NORM_PIXEL(inputImage, fract(uv));
    } else if (wrapMode == 1) {
        return IMG_NORM_PIXEL(inputImage, vec2(1.0) - abs(fract(uv * 0.5) * 2.0 - 1.0));
    } else if (wrapMode == 2) {
        return IMG_NORM_PIXEL(inputImage, clamp(uv, 0.0, 1.0));
    } else {
        if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) {
            return vec4(0.0);
        } else {
            return IMG_NORM_PIXEL(inputImage, uv);
        }
    }
}

void main() {
    if (PASSINDEX == 0) {
        // === Pass 0: Smooth Controls into ControlsBuffer ===
        int i = int(floor(gl_FragCoord.x));
        float val = 0.0;
        if      (i == 0) val = sinAmount;
        else if (i == 1) val = sinSpeed;
        else if (i == 2) val = noiseAmount;
        else if (i == 3) val = noiseSpeed;
        else if (i == 4) val = moveAmount;
        else if (i == 5) val = moveSpeed;
        else if (i == 6) val = scaleAmount;
        else if (i == 7) val = scaleSpeed;
        else if (i == 8) val = rotateAmount;
        else if (i == 9) val = rotateSpeed;

        float prev = IMG_PIXEL(ControlsBuffer, vec2(float(i) + 0.5, 0.5)).r;
        float smoothed = mix(prev, val, 0.02);
        gl_FragColor = vec4(smoothed, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // === Pass 1: Accumulate phase values into AccumBuffer ===
        float sinSpeed    = IMG_PIXEL(ControlsBuffer, vec2(1.5, 0.5)).r;
        float noiseSpeed  = IMG_PIXEL(ControlsBuffer, vec2(3.5, 0.5)).r;
        float moveSpeed   = IMG_PIXEL(ControlsBuffer, vec2(5.5, 0.5)).r;
        float scaleSpeed  = IMG_PIXEL(ControlsBuffer, vec2(7.5, 0.5)).r;
        float rotateSpeed = IMG_PIXEL(ControlsBuffer, vec2(9.5, 0.5)).r;

        int i = int(floor(gl_FragCoord.x));
        float prev = IMG_PIXEL(AccumBuffer, vec2(float(i) + 0.5, 0.5)).r;
        float delta = TIMEDELTA;
        float updated = prev;

        if      (i == 0) updated += sinSpeed * delta;
        else if (i == 1) updated += noiseSpeed * delta;
        else if (i == 2) updated += moveSpeed * delta;
        else if (i == 3) updated += scaleSpeed * delta;
        else if (i == 4) updated += rotateSpeed * delta;

        gl_FragColor = vec4(updated, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 2) {
        // === Final Render Pass ===
        vec2 uv = isf_FragNormCoord;
        vec2 center = vec2(0.5);

        // === Get Smoothed Values and Phases ===
        float sinAmt    = IMG_PIXEL(ControlsBuffer, vec2(0.5, 0.5)).r;
        float sinPhase  = IMG_PIXEL(AccumBuffer, vec2(0.5, 0.5)).r;

        float noiseAmt  = IMG_PIXEL(ControlsBuffer, vec2(2.5, 0.5)).r;
        float noisePhase= IMG_PIXEL(AccumBuffer, vec2(1.5, 0.5)).r;

        float moveAmt   = IMG_PIXEL(ControlsBuffer, vec2(4.5, 0.5)).r;
        float movePhase = IMG_PIXEL(AccumBuffer, vec2(2.5, 0.5)).r;

        float scaleAmt   = IMG_PIXEL(ControlsBuffer, vec2(6.5, 0.5)).r;
        float scalePhase = IMG_PIXEL(AccumBuffer, vec2(3.5, 0.5)).r;

        float rotateAmt   = IMG_PIXEL(ControlsBuffer, vec2(8.5, 0.5)).r;
        float rotatePhase = IMG_PIXEL(AccumBuffer, vec2(4.5, 0.5)).r;

        // === Sin/Cos distortion ===
        vec2 sinOffset = vec2(
            sin(uv.y * 10.0 + sinPhase),
            cos(uv.x * 10.0 + sinPhase)
        ) * sinAmt;

        // === Noise distortion ===
        float n = noise(uv * 4.0 + vec2(noisePhase));
        vec2 noiseOffset = (vec2(n) - 0.5) * 2.0 * noiseAmt;

        // === Movement ===
        vec2 moveDir = vec2(
            sin(movePhase) + noise(vec2(movePhase, 0.3)),
            cos(movePhase * 1.3) + noise(vec2(movePhase, 0.7))
        );
        vec2 moveOffset = moveDir * 0.5 * moveAmt;

        // === Scale ===
        float scaleWave = sin(scalePhase * 1.5) + noise(vec2(scalePhase));
        float scale = 1.0 + (scaleWave - 0.5) * 2.0 * scaleAmt;
        vec2 scaledUV = (uv - center) / scale + center;

        // === Rotation ===
        float angle = sin(rotatePhase * 1.1) + noise(vec2(rotatePhase));
        float theta = angle * 3.14159 * rotateAmt;
        float cosT = cos(theta);
        float sinT = sin(theta);
        vec2 offset = scaledUV - center;
        vec2 rotatedUV = vec2(
            offset.x * cosT - offset.y * sinT,
            offset.x * sinT + offset.y * cosT
        ) + center;

        // === Combine All Distortions ===
        vec2 finalUV = rotatedUV + sinOffset + noiseOffset + moveOffset;

        // === Sample Image ===
        gl_FragColor = sampleWrapped(finalUV);
    }
}

