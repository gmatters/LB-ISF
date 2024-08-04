/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters using Micro Buffer by VIDVOX ",
    "DESCRIPTION": "Tracks center of brightness and replaces with a brush point.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "Default",
                "StableLoop",
                "SaturatedLoop",
                "Classic"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1,
                2,
                3
            ]
        },
        {
            "NAME": "debug",
            "TYPE": "bool"
        },
        {
            "DEFAULT": 30,
            "MAX": 300,
            "MIN": 1,
            "NAME": "brushSize",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "DESCRIPTION": "Coordinates of cursor stored in [1,1].xy",
            "FLOAT": true,
            "HEIGHT": "3",
            "PERSISTENT": true,
            "TARGET": "LastFrameCoord",
            "WIDTH": "3"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer8"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer7"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer2"
        },
        {
            "DESCRIPTION": "Coordinates of center brightness stored in [0,0].xy",
            "FLOAT": true,
            "HEIGHT": "3",
            "PERSISTENT": true,
            "TARGET": "MaxCoordData",
            "WIDTH": "3"
        },
        {
        }
    ]
}
*/

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

void main()
{

	if (PASSINDEX == 0)	{
	    // Preserve the previous frame's cursor position so we can use it to interpolate later
		gl_FragColor = IMG_THIS_PIXEL(MaxCoordData);
	}
	if (PASSINDEX == 1)	{
	    //	first pass: read the "buffer7" into "buffer8"
	}
	else if (PASSINDEX == 2)	{
	}
	else if (PASSINDEX == 3)	{  // Write into buffer2, only the edges will contain data
	    vec2 sourcePixel;
	    // Left edge collects rows
	    if (gl_FragCoord.x <= 1.0) {
	        float brightness = 0.0;
	        sourcePixel.y = gl_FragCoord.y;
	        for (sourcePixel.x = 0.0; sourcePixel.x < IMG_SIZE(inputImage).x; sourcePixel.x++) {
	          vec4 sourceColor = IMG_PIXEL(inputImage, sourcePixel);
	          brightness += (sourceColor.r + sourceColor.g + sourceColor.b) / (3.0 * IMG_SIZE(inputImage).x);
	        }
			gl_FragColor = vec4(brightness, brightness, brightness, 1.0);
		}
		// Bottom edge collects columns
	    if (gl_FragCoord.y <= 1.0) {
	        float brightness = 0.0;
	        sourcePixel.x = gl_FragCoord.x;
	        for (sourcePixel.y = 0.0; sourcePixel.y < IMG_SIZE(inputImage).y; sourcePixel.y++) {
	          vec4 sourceColor = IMG_PIXEL(inputImage, sourcePixel);
	          brightness += (sourceColor.r + sourceColor.g + sourceColor.b) / (3.0 * IMG_SIZE(inputImage).y);
	        }
			gl_FragColor = vec4(brightness, brightness, brightness, 1.0);
		}
	}
	else if (PASSINDEX == 4)	{  // Write into MaxCoordData
	    if (gl_FragCoord.x < 1.0 && gl_FragCoord.y < 1.0) {  // TODO: doing all X and Y accumulation for a single frag might increase latency because it can't be parallelized
			float brightestXBrightness = 0.0;
		    float brightestXCoord = 0.0;
		    for (float someX = 0.0; someX < IMG_SIZE(inputImage).x; someX++) {
		      vec4 thisColume = IMG_PIXEL(buffer2, vec2(someX, 0.0));
		      if (thisColume.r > brightestXBrightness) {
		      	brightestXBrightness = thisColume.r;  // r is proxy for total brightness
		      	brightestXCoord = someX;
		      }
		    }
		    float brightestYBrightness = 0.0;
		    float brightestYCoord = 0.0;
		    for (float someY = 0.0; someY < IMG_SIZE(inputImage).y; someY++) {
		      vec4 thisRow = IMG_PIXEL(buffer2, vec2(0.0, someY));
		      if (thisRow.r > brightestYBrightness) {
		      	brightestYBrightness = thisRow.r;  // r is proxy for total brightness
		      	brightestYCoord = someY;
		      }
		    }
		    gl_FragColor = vec4(brightestXCoord, brightestYCoord, 1.0, 1.0);		    
	    }
	}
	else if (PASSINDEX == 5)	{
		// Render output	
		vec2 peakBrightnessCoord = IMG_PIXEL(MaxCoordData, vec2(0.0, 0.0)).xy;

		gl_FragColor = vec4(0.0);
		// Debug rows and columns
		if (debug) {
	        vec2 rowSourcePixel = vec2(0.0, gl_FragCoord.y);
	        vec2 columnSourcePixel = vec2(gl_FragCoord.x, 0.0);
			gl_FragColor = mix(IMG_PIXEL(buffer2, rowSourcePixel), IMG_PIXEL(buffer2, columnSourcePixel), 0.5);
			// Debug brightness peak
			if (abs(gl_FragCoord.x - peakBrightnessCoord.x) < 10.0 || abs(gl_FragCoord.y - peakBrightnessCoord.y) < 10.0) {
				gl_FragColor = vec4(0.0, 0.0, 1.0, 1.0);
			} else {
				gl_FragColor = vec4(gl_FragColor.r, 0.0, 0.0, 1.0);
			}
		}
		// Render spot
		if (distance(gl_FragCoord.xy, peakBrightnessCoord) <= brushSize) {
			gl_FragColor = vec4(1.0);
		}
		// Render interpolated
		vec3 pointA = vec3(gl_FragCoord.x, gl_FragCoord.y, 0);
		vec3 lineEndB = vec3(peakBrightnessCoord.x, peakBrightnessCoord.y, 0);
		vec3 lineEndC = vec3(IMG_PIXEL(LastFrameCoord, vec2(0.0, 0.0)).xy, 0);
		if (distanceFromPointToLine(pointA, lineEndB, lineEndC) <= brushSize) {
			gl_FragColor = vec4(1.0);
		}
	}
}
