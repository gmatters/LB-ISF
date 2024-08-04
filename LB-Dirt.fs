/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Various degradations",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0.0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "drive",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.5,
            "MAX": 1,
            "MIN": 0,
            "NAME": "crush",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
        }
    ]
}
*/


float map(float n, float i1, float i2, float o1, float o2){
  return o1 + (o2-o1) * (n-i1)/(i2-i1);
}

// Assumes 0-1 output range
float mapFrom(float n, float i1, float i2){
  return map(n, i1, i2, 0.0, 1.0);
}

// Assumes 0-1 input range
float mapTo(float n, float o1, float o2){
  return map(n, 0.0, 1.0, o1, o2);
}

void main()     {
  //vec4 sourceCoord = gl_FragCoord;
  //sourceCoord += drive * 20.0 * sin(sourceCoord.x / map(drive, 0.0, 1.0, 10.0, 1.0));
  //gl_FragColor = IMG_PIXEL(inputImage, sourceCoord.xy);
  vec2 src = isf_FragNormCoord.xy;

  // Drive coordinate
  // This has to come before crush to keep the detail in the output
  float inScale = 100.0;
  inScale = mapTo(drive, 10.0, 100.0);
  float xOffset = drive * 10.0 * sin((src.x - 0.5) * inScale) / inScale;
  float yOffset = drive * 10.0 * sin((src.y - 0.5) * inScale *  10.0) / (inScale * 10.0);
  src.x += xOffset / 3.0;
  src.y += yOffset;

  // crush coordinate
  // TODO: avoid stretching effect of continuously variable bits, perhaps by calculating color of floor and ceiling bit and crossfading. To avoid multiple color loopup, could try crossfading source coordinate too, although that probably breaks boxiness and more
  float cBits = mapTo(crush, 9.0, 5.0);
  if (cBits < 8.0) { // XXX: fade in crush from 9 -> 8
    float cScale = pow(2.0, cBits);
    src.x = float(int(cScale * src.x)) / cScale;
    src.y = float(int(cScale * src.y)) / cScale;
  }

  // Lookup color from coordinate
  gl_FragColor = IMG_NORM_PIXEL(inputImage, src);

  // Crush color
  vec4 fragCrushed = gl_FragColor;
  float bits_color = mapTo(crush, 8.0, 1.0); // TODO: continuously variable bit depth looks odd, try full -> 4 bit -> 2 bit?
  fragCrushed.r = float(int(fragCrushed.r * bits_color)) / bits_color;
  fragCrushed.g = float(int(fragCrushed.g * bits_color)) / bits_color;
  fragCrushed.b = float(int(fragCrushed.b * bits_color)) / bits_color;
  gl_FragColor = mix(gl_FragColor, fragCrushed, crush);

}

