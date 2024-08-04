/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Draw over video using a paintbrush as a cursor.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
          "NAME" : "cursor",
          "TYPE" : "point2D",
          "DEFAULT" : [
            0,
            0
          ],
          "MIN" : [
            0,
            0
          ],
          "MAX" : [
            1,
            1
          ]
        },
        {
            "DEFAULT": 30,
            "MAX": 300,
            "MIN": 0,
            "NAME": "brushSize",
            "TYPE": "float"
        },
        {
            "DEFAULT": 3,
            "MAX": 60,
            "MIN": 0,
            "NAME": "decayTimeSec",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "PERSISTENT": true,
            "FLOAT": true,
            "TARGET": "lifeBuffer"
        },
        {
            "DESCRIPTION": "Coordinates of cursor stored in [1,1].xy",
            "FLOAT": true,
            "HEIGHT": "1",
            "PERSISTENT": true,
            "TARGET": "LastFrameCoord",
            "WIDTH": "1"
        },
        {
        }
    ]
}
*/

const float pi = 3.14159265359;

// from https://stackoverflow.com/questions/63491296/calculating-point-to-line-distance-in-glsl
float distanceFromPointToLine(in vec3 a, in vec3 b, in vec3 c) {
  vec3 ba = a - b;
  vec3 bc = c - b;
  float d = dot(ba, bc);
  float len = length(bc);
  float param = 0.0;
  if (len != 0.0) {
    param = clamp(d / (len * len), 0.0, 1.0);
  }
  vec3 r = b + bc * param;
  return distance(a, r);
}


// Value Noise code by Inigo Quilez ported by @colin_movecraft
float hash(vec2 p, float seed)  // replace this by something better
{
    p  = 50.0*fract(seed*1.17921+p*0.3183099 + vec2(0.71,0.113));
    return -1.0+2.0*fract( p.x*p.y*(p.x+p.y) );
}

float noise( in vec2 p , in float seed)
{
    vec2 i = floor( p );
    vec2 f = fract( p );

    vec2 u = f*f*(3.0-2.0*f);

    float v1 = mix( mix( hash( i + vec2(0.0,0.0) , floor(seed) ),
                     hash( i + vec2(1.0,0.0) , floor(seed) ), u.x),
                mix( hash( i + vec2(0.0,1.0) , floor(seed) ),
                     hash( i + vec2(1.0,1.0) , floor(seed) ), u.x), u.y);
    float v2 = mix( mix( hash( i + vec2(0.0,0.0) , ceil(seed) ),
                     hash( i + vec2(1.0,0.0) , ceil(seed) ), u.x),
                mix( hash( i + vec2(0.0,1.0) , ceil(seed) ),
                     hash( i + vec2(1.0,1.0) , ceil(seed) ), u.x), u.y);
    return mix(v1,v2,fract(seed));
}

float map(float n, float i1, float i2, float o1, float o2){
        return o1 + (o2-o1) * (n-i1)/(i2-i1);

}

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

vec4 rand4(vec4 co)     {
        vec4    returnMe = vec4(0.0);
        returnMe.r = rand(co.rg);
        returnMe.g = rand(co.gb);
        returnMe.b = rand(co.ba);
        returnMe.a = rand(co.rb);
        return returnMe;
}
// End Value Noise



// Buffer is [lifespan, phase, unused, unusable]


vec4 color1 = vec4(1.0, 1.0, 1.0, 1.0);
vec4 color2 = vec4(0.945, 0.353, 0.141, 1.0);  // Roadmap orange
vec4 color3 = vec4(0.0, 0.0, 1.0, 1.0);  // Roadmap blue


void main()
{
  float size1 = 0.5;  // Portion of brush size
  float size2 = 0.7;  // Portion of brush size

  if (PASSINDEX == 0)	{  // write into lifeBuffer
      vec3 pointA = vec3(gl_FragCoord.x, gl_FragCoord.y, 0);
      vec2 cursorDeNormalized = cursor * IMG_SIZE(inputImage);
      vec2 lastCursorDeNormalized = IMG_PIXEL(LastFrameCoord, vec2(0,0)).xy * IMG_SIZE(inputImage);
      vec3 lineEndB = vec3(cursorDeNormalized.x, cursorDeNormalized.y, 0);
      vec3 lineEndC = vec3(lastCursorDeNormalized.x, lastCursorDeNormalized.y, 0);
      vec4 bufferColor = IMG_THIS_PIXEL(lifeBuffer);

      // Bleed downwards
      float y_offset = 1.0;
      vec4 pixelAbove = IMG_PIXEL(lifeBuffer, vec2(gl_FragCoord.x, gl_FragCoord.y+y_offset));
      bufferColor = pixelAbove;

      // age existing content so that it eventually disappears
      bufferColor.r -= TIMEDELTA / decayTimeSec;
      bufferColor.a = 1.0; // Avoid premultiply messing up our data RGB values.

      // Draw in new contents
      if (distanceFromPointToLine(pointA, lineEndB, lineEndC) <= size1 * brushSize) {
        if (rand(isf_FragNormCoord.xy * TIME) > 0.5) {
          bufferColor.r = 1.0;
          bufferColor.g = rand(isf_FragNormCoord.yx * TIME);  // Randomish phase
        }
      }
      gl_FragColor = bufferColor;
  }
  if (PASSINDEX == 1)	{  // write into LastFrameCoord
          // TODO: writing to all pixels. Should only need one pixel to store data.
          gl_FragColor.x = cursor.x;
          gl_FragColor.y = cursor.y;
          gl_FragColor.a = 1.0;  // Avoid premultiply
  }
  else if (PASSINDEX == 2)	{
    // Render output	
    gl_FragColor = IMG_THIS_PIXEL(inputImage) / 3.0; // XXX: forcing dim for convenience while working
    vec4 bufferPixel = IMG_THIS_PIXEL(lifeBuffer);
    if (bufferPixel.r > 0.0) {  // If pixel is alive
      float pixelPhase = bufferPixel.g;
      float twinkleSpeed = 1.0;
      float alpha = max(sin(twinkleSpeed*TIME + pixelPhase*2.0*pi), 0.0);  // max cuts off lower half of sin
      alpha = alpha * alpha * alpha; // Sharper, shorter peaks
      vec4 twinkleColor = mix(color2, color1, bufferPixel.r);
      gl_FragColor = mix(gl_FragColor, twinkleColor, alpha);
    }
  }
}
