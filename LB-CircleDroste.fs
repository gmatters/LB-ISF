/*
{
    "CREDIT": "by Geoff Matters via shadertoy Xs3SWj",
    "ISFVSN": "2",
    "CATEGORIES": [
        "LB"
    ],
    "DESCRIPTION": "Circular droste with variable zoom and spin. Derived from https://www.shadertoy.com/view/Xs3SWj by roywig, with the help of article:\nhttp://www.josleys.com/article_show.php?id=82",
    "INPUTS": [
            {
                    "NAME": "inputImage",
                    "TYPE": "image"
            },
            {
                    "NAME": "zoomSpeed",
                    "TYPE": "float",
                    "MIN": -1.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.0
            },
            {
                    "NAME": "spinSpeed",
                    "TYPE": "float",
                    "MIN": -1.0,
                    "MAX": 1.0,
                    "DEFAULT": 0.0
            }
    ],
    "PASSES": [
        {
            "DESCRIPTION": "Accumulate zoom and rotation state",
            "FLOAT": true,
            "PERSISTENT": true,
            "TARGET": "dataBuffer",
            "WIDTH": "1",
            "HEIGHT": "1"
        },
        {
        }
    ]
}

*/

/*
  reference:
  https://www.shadertoy.com/view/Xs3SWj
  https://www.josleys.com/article_show.php?id=82
*/

#define PI 3.14159

#define ZTIME r
#define STIME g

vec2 accumulatorAddress = vec2(0.5, 0.5);  // Target center of pixel at 0,0 to avoid subpixel sampling math

vec4 getData() {
  return IMG_PIXEL(dataBuffer, accumulatorAddress);
}

vec2 cInverse(vec2 a) { return	vec2(a.x,-a.y)/dot(a,a); }
vec2 cMul(vec2 a, vec2 b) {	return vec2( a.x*b.x -  a.y*b.y,a.x*b.y + a.y * b.x); }
vec2 cDiv(vec2 a, vec2 b) {	return cMul( a,cInverse(b)); }
vec2 cExp(vec2 z) {	return vec2(exp(z.x) * cos(z.y), exp(z.x) * sin(z.y)); }
vec2 cLog(vec2 a) {	float b =  atan(a.y,a.x); if (b>0.0) b-=2.0*PI;return vec2(log(length(a)),b);}

float lerp(float val, float in_a, float in_b, float out_a, float out_b) {
  return ((val - in_a) / (in_b - in_a)) * (out_b - out_a) + out_a;
}

void main() {
    vec4 data = getData();
    float zTime = data.ZTIME;
    float sTime = data.STIME;
    if (PASSINDEX == 0) {
      // Data accumulator accumulates 'time' used for zoom and speed.
      // Accumulating 'time' means that the dataBuffer isn't entagled with
      // implementation details like angles and twist.  Calculating zoom/spin
      // directly from TIME causes the output to jump around when adjusting
      // zoomSpeed and spinSpeed.
      zTime +=  TIMEDELTA * lerp(zoomSpeed, -1., 1., -10., 10.);
      sTime +=  TIMEDELTA * lerp(spinSpeed, -1., 1., -5., 5.);
      gl_FragColor.ZTIME = zTime;
      gl_FragColor.STIME = sTime;
      gl_FragColor.a = 1.0;  // If we don't set alpha on our data pixel, we might be subject to premultiply that corrupts the data in .rgb
      return;
    }
    vec2 z = gl_FragCoord.xy;
    z = (z.xy - RENDERSIZE.xy/2.)/RENDERSIZE.y;
    float r1 = 0.1, r2 = 1.0,
        scale = log(r2/r1),angle = atan(scale/(2.0*PI));
    // Droste transform here
    z = cLog(z);  // Transform by log: circle becomes vertical line
    z.y -= sTime;  // This part is equivalent to rotating the input image such that down can become up
    z = cDiv(z, cExp(vec2(0,angle))*cos(angle)); // Offsets each copy to turn rings into spiral
    z.x = mod(z.x-zTime,scale);
    z = cExp(z)*r1; // Undo the log transform; vertical lines back to circles
    // Draw output color
    gl_FragColor = IMG_NORM_PIXEL(inputImage,mod(.5+.5*z,1.0)); 
    // This code from the original causes round bright and dark overlays. Unclear what the intent was.
    //z = sin(z*25.);
    //gl_FragColor = vec4(mix(vec3(z.x*z.y),gl_FragColor.xyz,.85),1.);
}
