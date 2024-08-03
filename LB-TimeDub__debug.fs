/*{
    "CATEGORIES": [
        "Feedback",
        "LB"
    ],
    "CREDIT": "by Geoff Matters using Micro Buffer by VIDVOX ",
    "DESCRIPTION": "Buffers 8 recent frames and feedbacks",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Level",
            "MAX": 1,
            "MIN": 0,
            "NAME": "level",
            "TYPE": "float"
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
            "DEFAULT": 6,
            "LABEL": "Delay Multiplier",
            "MAX": 20,
            "MIN": 0,
            "NAME": "inputRate",
            "TYPE": "float"
        },
        {
            "DEFAULT": false,
            "LABEL": "Flip Horizontal",
            "NAME": "hFlip",
            "TYPE": "bool"
        },
        {
            "DEFAULT": 1,
            "IDENTITY": 1,
            "MAX": 2,
            "MIN": 0.5,
            "NAME": "zoom",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Debug Buffer",
            "MAX": 8,
            "MIN": 0,
            "NAME": "debugBuffer",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "DESCRIPTION": "this buffer stores the last frame's odd / even state",
            "FLOAT": true,
            "HEIGHT": "3",
            "PERSISTENT": true,
            "TARGET": "lastRow",
            "WIDTH": "3"
        },
        {
            "PERSISTENT": true,
            "TARGET": "buffer9"
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

vec4 rgb2hsv(vec4 c)	{
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
	vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);
	
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec4(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x,c.a);
}

vec4 hsv2rgb(vec4 c)	{
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return vec4(c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y),c.a);
}

vec4 linearAdd(vec4 a, vec4 b) {
  return vec4(a.rgb + b.rgb, a.a);
}

vec4 saturatingAdd(in vec4 a, in vec4 b) {
  //float headroom = 1.0 - max(max(a.r, a.g), a.b);  // 1 if a is black, 0 if b is saturated
  float headroom = 1.0 - (a.r + a.g + a.b) / 3.0;
  return vec4(a.rgb + b.rgb * headroom, a.a);
}

vec4 add(vec4 a, vec4 b) {
  //if (isf_FragNormCoord.x < 0.5) {
  //  return linearAdd(a, b);
  //}
  return saturatingAdd(a, b);
}

// TODO: circular buffer might be more efficient? Although calculating which buffer every frame might cost more than simply copying

void main()
{
	vec2 accumulatorPixel = vec2(0);
	//	apply lag on each pass
	//	if this is the first pass, i'm going to read the position from the "lastRow" image, and write a new position based on this and the hold variables
	// If lastRow.x is 0, advance the frame. 
        int debug_buffer = int(debugBuffer);
	if (PASSINDEX == 0)	{
		vec4 srcPixel = IMG_PIXEL(lastRow,accumulatorPixel);
		//	i'm only using the X and Y components
		//  X is 0 (advance frame) or >0 accumulating (don't)
		//  Y is leftover from last frame wrap
		if (inputRate == 0.0)	{
			srcPixel.x = 0.0;  // Force advance every frame
			srcPixel.y = 0.0;
		}
		else if (inputRate <= 1.0)	{
			srcPixel.x = (srcPixel.x) > 0.5 ? 0.0 : 1.0;  // Toggle
			srcPixel.y = 0.0;
		}
		else {
			srcPixel.x = srcPixel.x + 1.0 / inputRate + srcPixel.y;
			srcPixel.y = 0.0; // Only inlude the overflow once
			if (srcPixel.x > 1.0)	{
				srcPixel.y = mod(srcPixel.x, 1.0);
				srcPixel.x = 0.0;
			}
		}
		srcPixel.a = 1.0; // Without this values don't persist (pre-multiplied away?)
		gl_FragColor = srcPixel;
	}
	float accumulator = IMG_PIXEL(lastRow,accumulatorPixel).x;
	bool advanceFrame = accumulator == 0.0;
        if (debug_buffer > 0) {
          advanceFrame = false;
        }
	if (PASSINDEX == 1)	{
            //	Buffer 9 exists just to hold a copy of buffer8 during frame
            //	advance, so we can later copy it back into 1. If we don't have
            //	the extra buffer, buffer8 gets overwritten by buffer7 and lost
            //	forever.
	//	if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer8);
	//	}
	}
	if (PASSINDEX == 2)	{
	    //	read the "buffer7" into "buffer8"
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer7);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer8);
		}
	}
	else if (PASSINDEX == 3)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer6);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer7);
		}
	}
	else if (PASSINDEX == 4)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer5);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer6);
		}
	}
	else if (PASSINDEX == 5)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer4);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer5);
		}
	}
	else if (PASSINDEX == 6)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer3);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer4);
		}
	}
	else if (PASSINDEX == 7)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer2);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer3);
		}
	}
	else if (PASSINDEX == 8)	{
		if (advanceFrame)	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer1);
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer2);
		}
	}
	else if (PASSINDEX == 9)	{
                // Write into buffer 1
		if (advanceFrame)	{
                        vec2 normSrcCoord = isf_FragNormCoord;
                        if (hFlip) {
                                normSrcCoord.x = (1.0-normSrcCoord.x);  // HFlip the feedback portion
                        }
                        normSrcCoord = (normSrcCoord - vec2(0.5,0.5)) / zoom + vec2(0.5, 0.5);  // XXX: misses input
                        vec4 bufferPixel = IMG_NORM_PIXEL(buffer9, normSrcCoord);
                        vec4 inputPixel = IMG_NORM_PIXEL(inputImage, normSrcCoord);
                        if (mode == 0) {
                                        float feedbackLevel = 0.6 + 0.4 * level;
                                vec4 color = vec4(feedbackLevel) * bufferPixel;
                                // Add luminanance from input
                                color = add(color, inputPixel * inputPixel.a * level);  // Note that we premultiply incoming pixel because add() doesn't honor second pixel's alpha (at time of writing)
                                color.a = 1.0;
                                gl_FragColor = color;
                        } else if (mode == 1) {
                            gl_FragColor = mix(inputPixel, bufferPixel, level);
                        } else if (mode == 2) {
                            // Feedback runs away a bit when maxed out
                                gl_FragColor = (1.0-level) * inputPixel + level * 1.1 * bufferPixel;
                        } else if (mode == 3) {
                            // Classic algo before matching effectDub
                                vec4 color = mix(inputPixel, bufferPixel, level);
                                color = rgb2hsv(color);
                                color.y += 0.2;
                                color = hsv2rgb(color);
                                gl_FragColor = color;
                        }
		}
		else	{
			gl_FragColor = IMG_THIS_NORM_PIXEL(buffer1);
		}
	}
	else if (PASSINDEX == 10)	{
		//	Figure out which section I'm in and draw the appropriate buffer there
		vec2 tex = isf_FragNormCoord;
		vec4 returnMe = vec4(0.0);
		vec4 bufferFrame = vec4(0.0);
		
		float percentThroughBuffer = 0.0; // XXX: mod(float(FRAMEINDEX), framesPerBuffer) / framesPerBuffer;
		percentThroughBuffer = accumulator;
		
		bufferFrame = mix(IMG_THIS_NORM_PIXEL(buffer8), IMG_THIS_NORM_PIXEL(buffer7), percentThroughBuffer); // XXX: how does this interplay with buffer9?
		
		if (mode == 0) {
		  	// Output frame always includes input frame so that we have some portion of full framerate motion.
			returnMe = IMG_THIS_NORM_PIXEL(inputImage);
			returnMe = add(returnMe * returnMe.a, bufferFrame);  // Note that we premultiply incoming pixel because add() does
			returnMe.a = 1.0;  // necessary to honor luminance of feedback loop
		} else if (mode == 1) {
		  	returnMe = mix(IMG_THIS_NORM_PIXEL(inputImage), bufferFrame, level);
		} else if (mode == 2) {
		  	returnMe = mix(IMG_THIS_NORM_PIXEL(inputImage), bufferFrame, level);
		} else if (mode == 3) {
		  	returnMe = IMG_THIS_NORM_PIXEL(inputImage) + level * bufferFrame;
		}

                if (debug_buffer == 1) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer1); 
                } else if (debug_buffer == 2) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer2); 
                } else if (debug_buffer == 3) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer3); 
                } else if (debug_buffer == 4) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer4); 
                } else if (debug_buffer == 5) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer5); 
                } else if (debug_buffer == 6) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer6); 
                } else if (debug_buffer == 7) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer7); 
                } else if (debug_buffer == 8) {
                  returnMe = IMG_THIS_NORM_PIXEL(buffer8); 
                }


		gl_FragColor = returnMe;
		

	}
}
