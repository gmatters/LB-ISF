/*{
    "CATEGORIES": [
        "LB",
        "Time"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Freeze two frames and cut between them.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "NAME": "freeze",
            "TYPE": "bool"
        },
        {
            "DEFAULT": 1,
            "MAX": 1,
            "MIN": 1,
            "NAME": "level",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "MAX": 3,
            "MIN": 0,
            "NAME": "seconds",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "MAX": 8,
            "MIN": 0,
            "NAME": "subframe",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "DESCRIPTION": "this buffer stores the metastate",
            "FLOAT": true,
            "HEIGHT": "1",
            "PERSISTENT": true,
            "TARGET": "dataBuf",
            "WIDTH": "1"
        },
        {
            "PERSISTENT": true,
            "TARGET": "frameOlder"
        },
        {
            "PERSISTENT": true,
            "TARGET": "frameNewer"
        },
        {
        }
    ]
}
*/

#define MODE_A 0
#define MODE_B 1

#define DATA_FRAMES_FROZEN r  // 0 means not frozen, 1 is the first frozen frame, count up from there
#define DATA_WHICH_FRAME g    // Which frame to output
#define DATA_FREEZE_TIME b    // When the freeze was frozen
//#define DATA_HAVE_ALTERNATE_FRAME a    // Have we captured the alternate frame yet


void main()
{
    vec2 dataAddress = vec2(0);
	vec4 dataVec = IMG_PIXEL(dataBuf,dataAddress);
	if (PASSINDEX == 0)	{
	    // Update any metadata calculations
	    if (!freeze) {
	    	dataVec.DATA_FREEZE_TIME = 0.0;
	    } else if (dataVec.DATA_FREEZE_TIME == 0.0) { // freeze was just enabled
	    	dataVec.DATA_FREEZE_TIME = TIME;
	    }
		dataVec.DATA_FRAMES_FROZEN = freeze ? dataVec.DATA_FRAMES_FROZEN + 1.0 : 0.0;
		//dataVec.DATA_WHICH_FRAME = dataVec.DATA_WHICH_FRAME == 0.0 ? 1.0 : 0.0;  // Output toggles every frame
		float period = seconds; // Loop size in seconds  // XXX: base on something
		dataVec.DATA_WHICH_FRAME = mod((TIME-dataVec.DATA_FREEZE_TIME), period) > period * 0.5 ? 0.0 : 1.0;
		dataVec.a = 1.0;         // Without this values don't persist (pre-multiplied away?)
		gl_FragColor = dataVec;  // Write the updated value back to the persisted data
		return;
	}
	
	if (PASSINDEX == 1)	{
	    // write to frameOlder, the older of the two
	    if (freeze) {
	        gl_FragColor = IMG_THIS_NORM_PIXEL(frameOlder);  // Preserve old when frozen
	    } else {
		    gl_FragColor = IMG_THIS_NORM_PIXEL(inputImage);  // Copy input to old
		}
	}
    else if (PASSINDEX == 2)	{
	    // write to frameNewer
	    if (freeze) {
	        if ((TIME - dataVec.DATA_FREEZE_TIME) <= seconds/2.0) {
	          gl_FragColor = IMG_THIS_NORM_PIXEL(inputImage);
	        } else {
	          gl_FragColor = IMG_THIS_NORM_PIXEL(frameNewer);  // Preserve newer
	        }
	    } else {
		    gl_FragColor = IMG_THIS_NORM_PIXEL(inputImage);  // Copy input to newer just to have something in there at start of freeze
		}
	}
	else {
	    // Write to output
		float fade = level;	
		float copies = freeze ? pow(2.0, floor(subframe)) : 1.0;
		vec2 coord = isf_FragNormCoord * copies;
		coord = mod(coord, 1.0);
		bool whichFrame = dataVec.DATA_WHICH_FRAME == 0.0;
		if (whichFrame) {
		  gl_FragColor = mix(IMG_THIS_NORM_PIXEL(inputImage), IMG_NORM_PIXEL(frameNewer, coord), fade);
	    } else {
	      gl_FragColor = mix(IMG_THIS_NORM_PIXEL(inputImage), IMG_NORM_PIXEL(frameOlder, coord), fade);
        }
    }
}
