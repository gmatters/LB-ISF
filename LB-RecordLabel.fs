/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Turn input into a record label.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 33,
            "MAX": 60,
            "MIN": 0,
            "NAME": "rpm",
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

const float pi = 3.14159265359;
const float tau = 6.28318530718;

void main()
{
  if (PASSINDEX == 0) { // Render output	
    vec4 sourceCoord = gl_FragCoord;
    vec2 size = IMG_SIZE(inputImage);
    vec2 center = size / 2.0;
    float distance = distance(gl_FragCoord.xy, center);

    // Rotate.
    // Angle is calcuted directly from time, so changes in speed cause it to
    // jump sporadically. Supporting smooth changes in speed requires tracking
    // the current angle between frames, which in ISF would require an
    // additional rendering pass (even if a single buffer). An extra pass seems
    // wasteful, when the concept of the filter is that it would usually be
    // running at constant 33 or 45rpm.
    float rps = rpm / 60.0;
    float angle = mod(TIME * rps, 1.0);
    // Polar coord angle
    float a = atan ((sourceCoord.y-center.y),(sourceCoord.x-center.x));
    // Back to cartesian
    float s = sin(a + tau * angle);
    float c = cos(a + tau * angle);
    sourceCoord.x = (distance * c) + center.x;
    sourceCoord.y = (distance * s) + center.y;

    gl_FragColor = IMG_PIXEL(inputImage, sourceCoord.xy);

    // Record shape
    float radius = min(size.x, size.y) / 2.0; // Fit to frame TODO: actually crops 1/2 of the antialias
    float antialiasPixels = 3.0;
    // Outer edge
    float alpha = smoothstep(1.0, 0.0, (distance - radius) / antialiasPixels);
    // Inner edge
    alpha *= smoothstep(0.0, 1.0, (distance - radius/16.0) / antialiasPixels);
    gl_FragColor.a *= alpha;
  }
}
