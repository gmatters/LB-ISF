/*{
    "CATEGORIES": [
        "Test"
    ],
    "CREDIT": "by VIDVOX",
    "DESCRIPTION": "Buffers 8 recent frames",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Buffer",
            "MAX": 9,
            "MIN": 0,
            "NAME": "inputDelay",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "LABEL": "Buffer Lag",
            "MAX": 20,
            "MIN": 0,
            "NAME": "inputRate",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "Single",
                "Double",
                "Triple"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1,
                2
            ]
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "DESCRIPTION": "this buffer stores the last frame's odd / even state",
            "HEIGHT": "3",
            "PERSISTENT": true,
            "TARGET": "lastRow",
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
            "TARGET": "buffer6"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer5"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer4"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer3"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer2"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer1"
        },
        {
        }
    ]
}
*/

void main()
{
    vec4 fade = vec4(0.8, 0.8, 0.8, 1.0);  // Debug buffer chain by visible degrading it each link
    vec2 shouldAdvancePixel = vec2(1);
    int lagMultiplier = 3;
	//	first pass: read the "buffer7" into "buffer8"
	//	apply lag on each pass
	if (PASSINDEX == 0)	{
		vec4		srcPixel = IMG_PIXEL(lastRow,shouldAdvancePixel);
		//	i'm only using the X and Y components
		if (inputRate == 0.0)	{
			srcPixel.x = 0.0;
			srcPixel.y = 0.0;
		}
		else if (inputRate <= 1.0)	{
			srcPixel.x = (srcPixel.x) > 0.5 ? 0.0 : 1.0;
			srcPixel.y = 0.0;
		}
		else {
			srcPixel.x = srcPixel.x + 1.0 / inputRate + srcPixel.y;
			if (srcPixel.x > 1.0)	{
				srcPixel.y = mod(srcPixel.x, 1.0);
				srcPixel.x = 0.0;
			}
		}
		srcPixel.a = 1.0;
		// Because above math doesn't work, force it every Nth frame
		/*
		if (mod(float(FRAMEINDEX), float(lagMultiplier)) == 0.0) {
		  srcPixel = vec4(0.0);
		} else {
		  srcPixel = vec4(1.0);
		}*/
		gl_FragColor = srcPixel;
	}
	vec4 lastPix = IMG_PIXEL(lastRow,shouldAdvancePixel);
	bool advanceFrame = lastPix.x == 0.0;
	if (PASSINDEX == 1)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer7) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer8);
		}
	}
	else if (PASSINDEX == 2)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer6) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer7);
		}
	}
	else if (PASSINDEX == 3)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer5) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer6);
		}
	}
	else if (PASSINDEX == 4)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer4) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer5);
		}
	}
	else if (PASSINDEX == 5)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer3) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer4);
		}
	}
	else if (PASSINDEX == 6)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer2) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer3);
		}
	}
	else if (PASSINDEX == 7)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer1) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer2);
		}
	}
	else if (PASSINDEX == 8)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(inputImage) * fade;
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer1);
		}
	}
	else if (PASSINDEX == 9)	{
		//	Figure out which section I'm in and draw the appropriate buffer there
		vec2 tex = isf_FragNormCoord;
		vec4 returnMe = vec4(0.0);
		vec4 color = vec4(0.0);
		float pixelBuffer = inputDelay;

		if (pixelBuffer < 1.0)	{
			color = IMG_NORM_PIXEL(inputImage, tex);
		}
		else if (pixelBuffer < 2.0)	{
			color = IMG_NORM_PIXEL(buffer1, tex);
		}
		else if (pixelBuffer < 3.0)	{
			color = IMG_NORM_PIXEL(buffer2, tex);
		}
		else if (pixelBuffer < 4.0)	{
			color = IMG_NORM_PIXEL(buffer3, tex);
		}
		else if (pixelBuffer < 5.0)	{
			color = IMG_NORM_PIXEL(buffer4, tex);
		}
		else if (pixelBuffer < 6.0)	{
			color = IMG_NORM_PIXEL(buffer5, tex);
		}
		else if (pixelBuffer < 7.0)	{
			color = IMG_NORM_PIXEL(buffer6, tex);
		}
		else if (pixelBuffer < 8.0)	{
			color = IMG_NORM_PIXEL(buffer7, tex);
		}
		else	{
			color = IMG_NORM_PIXEL(buffer8, tex);
		}
		
		returnMe = color;
		
		bool debugAdvance = true;
		if (debugAdvance) {
		  vec4 zlastRow = IMG_PIXEL(lastRow,shouldAdvancePixel);
	      bool zadvanceFrame = zlastRow.x == 0.0;
		  returnMe = zadvanceFrame ? vec4(0.0, 1.0, 0.0, 1.0) : vec4(1.0, 0.0, 0.0, 1.0);
		}

		gl_FragColor = returnMe;
	}
}
