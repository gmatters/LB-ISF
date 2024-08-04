/*{
    "CATEGORIES": [
        "LB",
        "Time"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Freeze frame and incrementally modify it.",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "NAME": "freeze",
            "TYPE": "bool",
            "DEFAULT": 0.0
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "Default",
                "Fancy"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1
            ]
        },
        {
            "NAME": "level",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 1.0,
            "DEFAULT": 1.0
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
            "TARGET": "freezeBuffer"
        },
        {
        }
    ]
}
*/

#define MODE_A 0
#define MODE_B 1

#define DATA_FRAMES_FROZEN r

void main()
{
    vec2 dataAddress = vec2(0);
	vec4 dataVec = IMG_PIXEL(dataBuf,dataAddress);
	if (PASSINDEX == 0)	{
	    // Update any metadata calculations
		dataVec.DATA_FRAMES_FROZEN = freeze ? dataVec.DATA_FRAMES_FROZEN + 1.0 : 0.0;
		dataVec.a = 1.0;         // Without this values don't persist (pre-multiplied away?)
		gl_FragColor = dataVec;  // Write the updated value back to the persisted data
		return;
	}
	if (PASSINDEX == 1)	{
	    // write to freezeBuffer
	    if (freeze) {
	        // While frozen, apply any iterative destruction
	        // Location distortion        
        	vec2 loc = isf_FragNormCoord;  //      start with this pixel
        	float stretch_amount = dataVec.DATA_FRAMES_FROZEN * 0.00001;
        	//loc.x += 0.01;  // left shift // XXX: wrapping
        	loc.x = (loc.x - 0.5) * (1.0 - stretch_amount) + 0.5;  // Slow stretch from center
	        vec4 inColor = IMG_NORM_PIXEL(freezeBuffer, loc);
	        float normOffset = 0.03 * dataVec.DATA_FRAMES_FROZEN/IMG_SIZE(inputImage).x;
	        vec2 texOffset = vec2(normOffset, 0.0);
	        inColor = (IMG_NORM_PIXEL(freezeBuffer, loc) + IMG_NORM_PIXEL(freezeBuffer, loc-texOffset) + IMG_NORM_PIXEL(freezeBuffer, loc+texOffset)) / 3.0;
	        vec4 outColor = inColor;
	        // Color distortion
	        outColor.r = inColor.g;
	        outColor.g = inColor.b;
	        outColor.b = inColor.r;
	        outColor.r = 0.995 * inColor.r + 0.005 * inColor.g;
	        outColor.g = 0.995 * inColor.g + 0.005 * inColor.b;
	        outColor.b = 0.995 * inColor.b + 0.005 * inColor.r;
	        gl_FragColor = outColor;
	    } else {
		    gl_FragColor = IMG_THIS_NORM_PIXEL(inputImage);
		}
	}
	else {
	    // Write to output
		float fade = level;	
		gl_FragColor = mix(IMG_THIS_NORM_PIXEL(inputImage), IMG_THIS_NORM_PIXEL(freezeBuffer), fade);
	}
}
