/*
{
    "CATEGORIES": [
        "LB"
    ],
    "DESCRIPTION": "Posterize pixel colors to the nearest color within a specific palette. Optionally, pixels which are too far from any of the palette colors can be made transparent.\nBased on https://www.shadertoy.com/view/msXyz8",
    "IMPORTED": {
    },
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "NAME": "palette",
            "TYPE": "long",
            "DEFAULT": 0,
            "LABELS": [
                "SMB",
                "Forevents",
                "46: Tsukimi",
                "47: Shubun",
                "49: Soukou",
                "51: Shousetsu",
                "52: Taisetsu",
                "55: Shoukan",
                "57: Daikan",
                "73: Ryukyu",
                "86: Fuwafuwa",
                "89: Hirahira"
            ],
            "VALUES": [
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11
            ]
        },
        {
            "DEFAULT": 0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "blendColors",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "alphaThreshold",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "rotatePalette",
            "TYPE": "float"
        },
        {
            "NAME": "spreadDontMatch",
            "TYPE": "bool",
            "DEFAULT": false
        }
    ],
    "PASSES": [
        {
        }
    ]
}

*/

// TODO: palette scramble all 9! color orders?
// TODO: reduce input to 2-bit color and map to 8 of the colors. Or, reduce to 9 colors and map. No attempt to map nearest color. Or, assign palette colors to 8/9 color colors in a way that maximizes global similarity.
// TODO: rename alphaThreshold
// TODO: built in hue rotate?
// TODO: consider blending colors not linearly, e.g. something from https://www.shadertoy.com/view/lsdGzN

// CIEDE2000 
// reference: 
// https://github.com/yuki-koyama/color-util/blob/master/include/color-util/CIEDE2000.hpp

const float epsilon = 0.00001;

float my_sin(float x) { return sin(radians(x)); }
float my_cos(float x) { return cos(radians(x)); }
float my_atan(float y, float x) {
    float v = degrees(atan(y, x));
    return (v < 0.0) ? v + 360.0 : v;
}

float get_h(float a, float b) {
    bool a_and_b_are_zeros = (abs(a) < epsilon)&&(abs(b) < epsilon);
    return a_and_b_are_zeros ? 0.0 : my_atan(b, a);
}

float get_delta_h(float C1, float C2, float h1, float h2) {
    float diff = h2 - h1;
    return (C1 * C2 < epsilon) ? 0.0 :
    (abs(diff) <= 180.0) ? diff :
    (diff > 180.0) ? diff - 360.0 :
    diff + 360.0;
}

float get_h_bar(float C1, float C2, float h1, float h2) {
    float dist = abs(h1 - h2);
    float sum = h1 + h2;
    return (C1 * C2 < epsilon) ? h1 + h2 :
    (dist <= 180.0) ? 0.5 * sum :
    (sum < 360.0) ? 0.5 * (sum + 360.0) :
    0.5 * (sum - 360.0);
    
}

float calculate_CIEDE2000(vec3 Lab1, vec3 Lab2) {
    float L1 = Lab1.x;
    float a1 = Lab1.y;
    float b1 = Lab1.z;
    float L2 = Lab2.x;
    float a2 = Lab2.y;
    float b2 = Lab2.z;
    
    float C1_ab = sqrt(a1 * a1 + b1 * b1);
    float C2_ab = sqrt(a2 * a2 + b2 * b2);
    float C_ab_bar = 0.5 * (C1_ab + C2_ab);
    float G = 0.5 * (1.0 - sqrt(pow(C_ab_bar, 7.0) / (pow(C_ab_bar, 7.0) + pow(25.0, 7.0))));
    float a_1 = (1.0 + G) * a1;
    float a_2 = (1.0 + G) * a2;
    float C1 = sqrt(a_1 * a_1 + b1 * b1);
    float C2 = sqrt(a_2 * a_2 + b2 * b2);
    float h1 = get_h(a_1, b1);
    float h2 = get_h(a_2, b2);
    
    float delta_L = L2 - L1;
    float delta_C = C2 - C1;
    float delta_h = get_delta_h(C1, C2, h1, h2);
    float delta_H = 2.0 * sqrt(C1 * C2) * my_sin(0.5 * delta_h);
    
    float L_bar = 0.5 * (L1 + L2);
    float C_bar = 0.5 * (C1 + C2);
    float h_bar = get_h_bar(C1, C2, h1, h2);
    
    float T = 1.0 - 0.17 * my_cos(h_bar - 30.0) + 0.24 * my_cos(2.0 * h_bar) +
    0.32 * my_cos(3.0 * h_bar + 6.0) - 0.20 * my_cos(4.0 * h_bar - 63.0);
    
    float delta_theta = 30.0 * exp(-((h_bar - 275.0) / 25.0) * ((h_bar - 275.0) / 25.0));
    
    float R_C = 2.0 * sqrt(pow(C_bar, 7.0) / (pow(C_bar, 7.0) + pow(25.0, 7.0)));
    float S_L = 1.0 + (0.015 * (L_bar - 50.0) * (L_bar - 50.0)) / sqrt(20.0 + (L_bar - 50.0) * (L_bar - 50.0));
    float S_C = 1.0 + 0.045 * C_bar;
    float S_H = 1.0 + 0.015 * C_bar * T;
    float R_T = -my_sin(2.0 * delta_theta) * R_C;
    
    const float k_L = 1.0;
    const float k_C = 1.0;
    const float k_H = 1.0;
    
    float deltaL = delta_L / (k_L * S_L);
    float deltaC = delta_C / (k_C * S_C);
    float deltaH = delta_H / (k_H * S_H);
    
    float delta_E_squared = deltaL * deltaL + deltaC * deltaC + deltaH * deltaH + R_T * deltaC * deltaH;
    
    return sqrt(delta_E_squared);
}

//--- RGB2Lab
vec3 rgb2xyz(vec3 c) {
    vec3 tmp;
    tmp.x = (c.r > 0.04045) ? pow((c.r + 0.055) / 1.055, 2.4) : c.r / 12.92;
    tmp.y = (c.g > 0.04045) ? pow((c.g + 0.055) / 1.055, 2.4) : c.g / 12.92;
    tmp.z = (c.b > 0.04045) ? pow((c.b + 0.055) / 1.055, 2.4) : c.b / 12.92;
    return 100.0 * tmp * mat3(0.4124, 0.3576, 0.1805, 0.2126, 0.7152, 0.0722, 0.0193, 0.1192, 0.9505);
}
vec3 xyz2lab(vec3 c) {
    vec3 n = c / vec3(95.047, 100.0, 108.883);
    vec3 v;
    v.x = (n.x > 0.008856) ? pow(n.x, 1.0 / 3.0) : (7.787 * n.x) + (16.0 / 116.0);
    v.y = (n.y > 0.008856) ? pow(n.y, 1.0 / 3.0) : (7.787 * n.y) + (16.0 / 116.0);
    v.z = (n.z > 0.008856) ? pow(n.z, 1.0 / 3.0) : (7.787 * n.z) + (16.0 / 116.0);
    return vec3((116.0 * v.y) - 16.0, 500.0 * (v.x - v.y), 200.0 * (v.y - v.z));
}

vec3 rgb2lab(vec3 c) {
    vec3 lab = xyz2lab(rgb2xyz(c));
    return vec3(lab.x / 100.0, 0.5 + 0.5 * (lab.y / 127.0), 0.5 + 0.5 * (lab.z / 127.0));
}


float compare(vec3 rgb1, vec3 rgb2) {
    vec3 lab1 = rgb2lab(rgb1);
    vec3 lab2 = rgb2lab(rgb2);
    return calculate_CIEDE2000(lab1, lab2);
}

vec3 Normalize256(int r, int g, int b) {
    float r_float = float(r);
    float g_float = float(g);
    float b_float = float(b);
    return vec3(r_float/256.0, g_float/256.0, b_float/256.0);
}

#define PALETTE_SIZE 9
void palette_smb(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(255, 254, 207);   // Cloud
  colors[1] = Normalize256(90, 164, 233);  // Light Blue
  colors[2] = Normalize256(118, 190, 0);  // Light Green
  colors[3] = Normalize256(219, 147, 20);  // Coin
  colors[4] = Normalize256(143, 138, 240); // Sky
  colors[5] = Normalize256(172, 38, 11); // Mario Red
  colors[6] = Normalize256(0, 128, 1);  // Dark Green
  colors[7] = Normalize256(136, 65, 0); // Brick
  colors[8] = Normalize256(43, 15, 2);  // Black
}

void palette_forevents(inout vec3 colors[PALETTE_SIZE]) {  // https://www.pinterest.com/pin/164592561374214111/
  colors[0] = Normalize256(241, 232, 214);  // cream
  colors[1] = Normalize256(198, 223, 219);  // sky blue
  colors[3] = Normalize256(241, 200, 195);  // pink
  colors[2] = Normalize256(203, 191, 165);  // sand
  colors[4] = Normalize256(210, 183, 116);  // mustard
  colors[5] = Normalize256(133, 154, 128);  // olive
  colors[6] = Normalize256(219, 101, 58);   // pumpkin
  colors[7] = Normalize256(56, 82, 156);    // navy
  colors[8] = Normalize256(51, 51, 51);     // black
}

void palette_046(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(245, 243, 223);  // 2
  colors[1] = Normalize256(234, 230, 192);  // 1
  colors[2] = Normalize256(242, 233, 114);  // 6
  colors[3] = Normalize256(253, 211, 92);   // 3
  colors[4] = Normalize256(196, 154, 114);  // 9
  colors[5] = Normalize256(202, 162, 86);   // 4
  colors[6] = Normalize256(100, 70, 48);    // 5
  colors[7] = Normalize256(54, 54, 98);     // 7 
  colors[8] = Normalize256(52, 55, 58);     // 8
}

void palette_047(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(211, 67, 88);
  colors[1] = Normalize256(231, 131, 133);
  colors[2] = Normalize256(225, 167, 140);
  colors[3] = Normalize256(241, 143, 105);
  colors[4] = Normalize256(145, 172, 72);
  colors[5] = Normalize256(216, 213, 112);
  colors[6] = Normalize256(86, 96, 70);
  colors[7] = Normalize256(147, 92, 105);
  colors[8] = Normalize256(150, 68, 70);
}

void palette_049(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(149, 155, 169);
  colors[1] = Normalize256(94, 101, 102);
  colors[2] = Normalize256(211, 210, 191);
  colors[3] = Normalize256(36, 49, 70);
  colors[4] = Normalize256(62, 87, 112);
  colors[5] = Normalize256(217, 117, 86);
  colors[6] = Normalize256(241, 173, 95);
  colors[7] = Normalize256(225, 198, 192);
  colors[8] = Normalize256(224, 140, 122);
}

void palette_051(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(226, 69, 31);
  colors[1] = Normalize256(231, 117, 52);
  colors[2] = Normalize256(189, 53, 41);
  colors[3] = Normalize256(244, 164, 88);
  colors[4] = Normalize256(190, 150, 110);
  colors[5] = Normalize256(249, 241, 236);
  colors[6] = Normalize256(198, 175, 142);
  colors[7] = Normalize256(158, 140, 115);
  colors[8] = Normalize256(94, 77, 54);
}

void palette_052(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(244, 246, 241);
  colors[1] = Normalize256(242, 243, 234);
  colors[2] = Normalize256(220, 221, 210);
  colors[3] = Normalize256(87, 98, 105);
  colors[4] = Normalize256(135, 121, 109);
  colors[5] = Normalize256(202, 196, 172);
  colors[6] = Normalize256(57, 51, 44);
  colors[7] = Normalize256(197, 151, 99);
  colors[8] = Normalize256(148, 100, 83);
}

void palette_055(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(208, 219, 225);
  colors[1] = Normalize256(168, 187, 208);
  colors[2] = Normalize256(134, 160, 190);
  colors[3] = Normalize256(248, 240, 158);
  colors[4] = Normalize256(234, 237, 239);
  colors[5] = Normalize256(144, 139, 98);
  colors[6] = Normalize256(181, 177, 168);
  colors[7] = Normalize256(143, 139, 122);
  colors[8] = Normalize256(97, 93, 66);
}

void palette_057(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(232, 240, 235);
  colors[1] = Normalize256(219, 211, 229);
  colors[2] = Normalize256(188, 207, 228);
  colors[3] = Normalize256(200, 210, 221);
  colors[4] = Normalize256(233, 229, 219);
  colors[5] = Normalize256(214, 219, 224);
  colors[6] = Normalize256(65, 92, 126);
  colors[7] = Normalize256(89, 107, 128);
  colors[8] = Normalize256(161, 168, 156);
}

void palette_073(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(234, 85, 75);
  colors[1] = Normalize256(82, 177, 187);
  colors[2] = Normalize256(251, 203, 103);
  colors[3] = Normalize256(175, 98, 154);
  colors[4] = Normalize256(192, 184, 95);
  colors[5] = Normalize256(159, 192, 141);
  colors[6] = Normalize256(179, 120, 85);
  colors[7] = Normalize256(150, 205, 196);
  colors[8] = Normalize256(134, 107, 49);
}

void palette_086(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(248, 247, 240);
  colors[1] = Normalize256(245, 243, 223);
  colors[2] = Normalize256(246, 227, 231);
  colors[3] = Normalize256(230, 203, 219);
  colors[4] = Normalize256(228, 243, 245);
  colors[5] = Normalize256(181, 223, 226);
  colors[6] = Normalize256(221, 220, 214);
  colors[7] = Normalize256(151, 202, 208);
  colors[8] = Normalize256(209, 169, 188);
}

void palette_089(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = Normalize256(250, 246, 222);
  colors[1] = Normalize256(238, 240, 160);
  colors[2] = Normalize256(204, 226, 168);
  colors[3] = Normalize256(236, 244, 227);
  colors[4] = Normalize256(252, 223, 197);
  colors[5] = Normalize256(245, 217, 222);
  colors[6] = Normalize256(233, 181, 197);
  colors[7] = Normalize256(132, 204, 204);
  colors[8] = Normalize256(166, 212, 166);
}

void palette_black(inout vec3 colors[PALETTE_SIZE]) {
  colors[0] = vec3(0.0);
  colors[1] = vec3(0.0);
  colors[2] = vec3(0.0);
  colors[3] = vec3(0.0);
  colors[4] = vec3(0.0);
  colors[5] = vec3(0.0);
  colors[6] = vec3(0.0);
  colors[7] = vec3(0.0);
  colors[8] = vec3(0.0);
} 

void main() {
  vec3 colors[PALETTE_SIZE];
  int idx = 0;
  if (palette == idx) {
     palette_smb(colors);
  } else if (palette == ++idx) {
     palette_forevents(colors);
  } else if (palette == ++idx) {
     palette_046(colors);
  } else if (palette == ++idx) {
     palette_047(colors);
  } else if (palette == ++idx) {
     palette_049(colors);
  } else if (palette == ++idx) {
     palette_051(colors);
  } else if (palette == ++idx) {
     palette_052(colors);
  } else if (palette == ++idx) {
     palette_055(colors);
  } else if (palette == ++idx) {
      palette_057(colors);
  } else if (palette == ++idx) {
      palette_073(colors);
  } else if (palette == ++idx) {
      palette_086(colors);
  } else if (palette == ++idx) {
      palette_089(colors);
  } else {
      palette_black(colors);
  }

  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec4 inTexel = IMG_NORM_PIXEL(inputImage,mod(uv,1.0));
  vec3 col = IMG_NORM_PIXEL(inputImage,mod(uv,1.0)).rgb;

  float nearest_dist = 9999.0;
  int nearest_idx = 0;
  float second_dist = 9999.0;
  int second_idx = 0;
  float alpha = 1.0;

  int rotateAmount = int(rotatePalette * (float(PALETTE_SIZE)-epsilon));

  if (spreadDontMatch) {
    float fIndex = floor(inTexel.r * 1.99) + 2.0 * floor(inTexel.g * 1.99) + 4.0 * floor(inTexel.b * 1.99);
    nearest_idx = int(fIndex);
    nearest_idx = nearest_idx + rotateAmount;
    if (nearest_idx >= PALETTE_SIZE) { nearest_idx -= PALETTE_SIZE; }
    col = colors[nearest_idx];
    nearest_dist = 0.0;  // Consider this a perfect match (avoids any alpha fading)
  }
  else {

    for (int i = 0; i < PALETTE_SIZE; i++) {
      float dist = compare(colors[i], col);
      
      if (dist < nearest_dist) {
        if (nearest_dist < second_dist) {
          // Adopt previous best as second best
          second_dist = nearest_dist;
          second_idx = nearest_idx;
        }
        nearest_dist = dist;
        nearest_idx = i;
      } else if (dist < second_dist) {
        second_dist = dist;
        second_idx = i;
      }
    }

    // Optionally rotate palette. Colors won't be closest match to input
    // pixels, and because it might violate e.g. brightness order in a gradient
    // it might introduce noise along edges. But can also be useful to achieve
    // a pleasing overall tone.
    nearest_idx = nearest_idx + rotateAmount;
    if (nearest_idx >= PALETTE_SIZE) { nearest_idx -= PALETTE_SIZE; }
    second_idx = second_idx + rotateAmount;
    if (second_idx >= PALETTE_SIZE) { second_idx -= PALETTE_SIZE; }

    // Optionally fade out pixels which aren't a good match to anything in the palette
    // Empirically, observe differences from 0-200/256  (0.78125)
    float maxDistance = 1.0 - alphaThreshold;
    alpha = smoothstep(maxDistance, maxDistance-0.02, nearest_dist);

    // Optionally fade between two colors when they are equally good match.
    // Introduces colors which aren't exactly in the palette, and can reduces
    // flickering banding and harsh edges.
    // nearest_dist should always be smaller than second_dist
    // e.g. nearest_dist is 0.2 second best is 0.21, should have close to 50% blend
    // e.g. nearest_dist is 0.01 second is 0.5 there should be no blending
    float ratioSecond = second_dist / nearest_dist;  // 1 -> inf
    // e.g. close to 1 should be blend
    float maxRatioToBlend = 1.0 + 2.0 * blendColors;  // 1.0 - 11.0
    float blendAmount = smoothstep(maxRatioToBlend, 1.0, ratioSecond);  // range 0-1 where 1 means halfway between two colors

    vec3 res = colors[nearest_idx];
    vec3 second_res = colors[second_idx];
    col = mix(res, second_res, blendAmount * 0.5);

  }

  // Print color bars at left edge
  //col = uv.x < 0.03 ? colors[int(uv.y * float(PALETTE_SIZE))] : res;

  gl_FragColor = vec4(col, alpha * inTexel.a);
}
