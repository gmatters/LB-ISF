/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Display alpha as RGB to easily distinguish semi-transparent pixels (grey) from opaque (white) and perfectly transparent (black).",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
        }
    ]
}
*/


void main()
{
  vec4 inPixel = IMG_THIS_PIXEL(inputImage);

  gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
  if (inPixel.a > 0.0) {
    gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
    if (inPixel.a  < 1.0) {
      gl_FragColor = vec4(0.5, 0.5, 0.5, 1.0);
    }
  }
}
