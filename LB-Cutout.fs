/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Mask",
		"LB"
	],
	"DESCRIPTION": "Remove outer pixels as long as they are roughly the same color.",
	"INPUTS": [
		{
			"NAME": "inputImage",
			"TYPE": "image"
		},
		{
			"NAME": "colorTolerance",
			"TYPE": "float",
			"MIN": 0.0,
			"MAX": 1.0,
			"DEFAULT": 0.3
		}
	],
        "PASSES": [
            {
                "DESCRIPTION": "two rows store top and bottom boundaries, two columns store l and r",
                "FLOAT": true,
                "TARGET": "boundaries",
                "PERSISTENT": true
            },
            {
            }
        ]
}*/

// XXX: data buffer could be 2 x width+height to save data

#define PIXEL_SKIP 1.0
#define TRANSPARENT 0.5
#define PACK_X r
#define PACK_Y g

float colorDiff(vec4 a, vec4 b) {
  return distance(a.rgb, b.rgb);
}

void main() {
        bool pass = true;
        float width = IMG_SIZE(inputImage).x;
        float height = IMG_SIZE(inputImage).y;
        float maxX = IMG_SIZE(inputImage).x - 1.0;
        float maxY = IMG_SIZE(inputImage).y - 1.0;
        // Must read from the center of the pixel to get back the exact data we put in, otherwise
        // it will sample with neighbor pixels
        float bottomDataRow = 0.5;
        float topDataRow = 1.5;
        float leftDataColumn = 0.5;
        float rightDataColumn = 1.5;

        if (PASSINDEX == 0) {
          gl_FragColor = vec4(0.0, 0.0, 1.0, 1.0);
          if (int(gl_FragCoord.x) == int(leftDataColumn)) {  // Left edge
            float x = 0.0;
            for (x=0.0; x < width; x += PIXEL_SKIP) {  // Skip transparent pixels to find the opaque edge
              vec2 someCoord = vec2(x, gl_FragCoord.y);
              if (IMG_PIXEL(inputImage, someCoord).a > TRANSPARENT) { break; }
            }
            vec2 edgeCoord = vec2(x, gl_FragCoord.y); // First non-transparent pixel is edge
            for (; x < width; x += PIXEL_SKIP) {  // Scan row from left
              vec2 someCoord = vec2(x, gl_FragCoord.y);
              if (IMG_PIXEL(inputImage, someCoord).a < TRANSPARENT) { continue; }
              if (colorDiff(IMG_PIXEL(inputImage, edgeCoord), IMG_PIXEL(inputImage, someCoord)) > colorTolerance) {
                break;
              }
            }
            // X is the percent at which a non-matching pixel was encountered
            gl_FragColor.PACK_X = x / width;
          }
          if (int(gl_FragCoord.x) == int(rightDataColumn)) {
            float x = maxX;
            for (x=maxX; x >= 0.0; x -= PIXEL_SKIP) {  // Scan row from right
              vec2 someCoord = vec2(x, gl_FragCoord.y);
              if (IMG_PIXEL(inputImage, someCoord).a > TRANSPARENT) { break; }
            }
            vec2 edgeCoord = vec2(x, gl_FragCoord.y); // First non-transparent pixel is edge
            for (; x >= 0.0; x -= PIXEL_SKIP) {  // Scan row from right
              vec2 someCoord = vec2(x, gl_FragCoord.y);
              if (IMG_PIXEL(inputImage, someCoord).a < TRANSPARENT) { continue; }
              if (colorDiff(IMG_PIXEL(inputImage, edgeCoord), IMG_PIXEL(inputImage, someCoord)) > colorTolerance) {
                break;
              }
            }
            // y is the percent at which a non-matching pixel was encountered. e.g. 0.9 means 10% from top
            gl_FragColor.PACK_X = x / width;
          }
          if (int(gl_FragCoord.y) == int(bottomDataRow)) {
            float y = 0.0;
            for (y=0.0; y < height; y += PIXEL_SKIP) {  // Scan column from bottom
              vec2 someCoord = vec2(gl_FragCoord.x, y);
              if (IMG_PIXEL(inputImage, someCoord).a > TRANSPARENT) { break; }
            }
            vec2 edgeCoord = vec2(gl_FragCoord.x, y);
            for (; y < height; y += PIXEL_SKIP) {  // Scan column from bottom
              vec2 someCoord = vec2(gl_FragCoord.x, y);
              if (IMG_PIXEL(inputImage, someCoord).a < TRANSPARENT) { continue; }
              if (colorDiff(IMG_PIXEL(inputImage, edgeCoord), IMG_PIXEL(inputImage, someCoord)) > colorTolerance) {
                break;
              }
            }
            // y is the percent at which a non-matching pixel was encountered
            gl_FragColor.PACK_Y = y / height;
          }
          if (int(gl_FragCoord.y) == int(topDataRow)) {
            float y = maxY;
            for (y=maxY; y >= 0.0; y -= PIXEL_SKIP) {  // Scan column from top
              vec2 someCoord = vec2(gl_FragCoord.x, y);
              if (IMG_PIXEL(inputImage, someCoord).a > TRANSPARENT) { break; }
            }
            vec2 edgeCoord = vec2(gl_FragCoord.x, y);
            for (; y >= 0.0; y -= PIXEL_SKIP) {  // Scan column from top
              vec2 someCoord = vec2(gl_FragCoord.x, y);
              if (IMG_PIXEL(inputImage, someCoord).a < TRANSPARENT) { continue; }
              if (colorDiff(IMG_PIXEL(inputImage, edgeCoord), IMG_PIXEL(inputImage, someCoord)) > colorTolerance) {
                break;
              }
            }
            // y is the percent at which a non-matching pixel was encountered. e.g. 0.9 means 10% from top
            gl_FragColor.PACK_Y = y / height;
          }
          return;
        }

	gl_FragColor = IMG_THIS_PIXEL(inputImage);

        float leftNormBoundary = IMG_PIXEL(boundaries, vec2(leftDataColumn, gl_FragCoord.y)).PACK_X;
        float rightNormBoundary = IMG_PIXEL(boundaries, vec2(rightDataColumn, gl_FragCoord.y)).PACK_X;
        float bottomNormBoundary = IMG_PIXEL(boundaries, vec2(gl_FragCoord.x, bottomDataRow)).PACK_Y;
        float topNormBoundary = IMG_PIXEL(boundaries, vec2(gl_FragCoord.x, topDataRow)).PACK_Y;
        if (isf_FragNormCoord.x < leftNormBoundary) { gl_FragColor.a = 0.0; }
        if (isf_FragNormCoord.x > rightNormBoundary) { gl_FragColor.a = 0.0; }
        if (isf_FragNormCoord.y < bottomNormBoundary) { gl_FragColor.a = 0.0; }
        if (isf_FragNormCoord.y > topNormBoundary) { gl_FragColor.a = 0.0; }
        return;
}
