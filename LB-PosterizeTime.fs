/*{
    "CATEGORIES": [
        "LB",
        "Time"
    ],
    "CREDIT": "by Geoff Matters using Micro Buffer by VIDVOX ",
    "DESCRIPTION": "Posterizes time with blending between frames",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 0,
            "LABELS": [
                "Cut",
                "Blend"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1
            ]
        },
        {
            "DEFAULT": 60,
            "LABEL": "Frames per Frame",
            "MAX": 200,
            "MIN": 1,
            "NAME": "inputRate",
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

#define CUT_MODE 0
#define BLEND_MODE 1

// Accumulate fractional values.
bool accumulate(inout float accumulator, in float increment);

bool accumulate(inout float accumulator, in float increment) {
  accumulator += increment;
  if (accumulator >= 1.0) {
    accumulator = mod(accumulator, 1.0);
    return true;
  }
  return false;
}

// Accumulate fractional values using a vec4 as a data store.
void accumulateInVec4(inout vec4 dataVec, in float increment);

#define ACCUMULATOR r
#define WRAPPED g
void accumulateInVec4(inout vec4 dataVec, in float increment) {
	float accumulator = dataVec.ACCUMULATOR;
	bool wrapped = accumulate(accumulator, increment);
	dataVec.ACCUMULATOR = accumulator;
	dataVec.WRAPPED = wrapped ? 1.0 : 0.0;
	dataVec.a = 1.0;         // Without this values don't persist (pre-multiplied away?)
}

void main()
{
	vec2 accumulatorAddress = vec2(0);  // Which pixel from the metadata we use to track interframe progress
	// If lastRow.r is 0, advance the frame. 
	if (PASSINDEX == 0)	{
	    float increment = 1.0 / inputRate;
		vec4 dataVec = IMG_PIXEL(dataBuf,accumulatorAddress);
		accumulateInVec4(dataVec, increment);
		gl_FragColor = dataVec;  // Write the updated value back to the persisted data
		return;
	}
	// Read back out the calculation from the first pass.
	// It is actually unclear whether it is more performant to an additional pass to pre-calculate the accumulator, but that is how it is written.
	float accumulator = IMG_PIXEL(dataBuf,accumulatorAddress).ACCUMULATOR;
	bool advanceFrame = IMG_PIXEL(dataBuf,accumulatorAddress).WRAPPED == 1.0;
	if (PASSINDEX == 1)	{
	    // write to buffer2
		gl_FragColor = advanceFrame ? IMG_THIS_NORM_PIXEL(buffer1) : IMG_THIS_NORM_PIXEL(buffer2);
	}
	else if (PASSINDEX == 2)	{
	    // write to buffer1
		gl_FragColor = advanceFrame ? IMG_THIS_NORM_PIXEL(inputImage) : IMG_THIS_NORM_PIXEL(buffer1);
	}
	else if (PASSINDEX == 3)	{
	    // Write to output
		float percentThroughBuffer = mode == CUT_MODE ? 1.0 : accumulator;		
		gl_FragColor = mix(IMG_THIS_NORM_PIXEL(buffer2), IMG_THIS_NORM_PIXEL(buffer1), percentThroughBuffer);;

	}
}
