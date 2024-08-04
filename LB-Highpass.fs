/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Color Adjustment",
		"LB"
	],
	"DESCRIPTION": "Subjective analog to a highpass filter, kills darkest pixels first then pulls everything towards black",
	"INPUTS": [
		{
			"NAME": "inputImage",
			"TYPE": "image"
		},
		{
			"NAME": "level",
			"TYPE": "float",
			"MIN": 0.0,
			"MAX": 1.0,
			"DEFAULT": 0.0
		}
	]
}*/

void main() {
    float rampWidth = 0.3;
    // 0 should map to -rampWidth so that every pixel is above the top of the ramp (passthrough).
    // 1 Should map to 1 so that all pixels are <= bottom of the ramp (blackout).
    float iLevel = level * (1.0 + rampWidth) - rampWidth;
	float thisBrightness = (IMG_THIS_PIXEL(inputImage).r + IMG_THIS_PIXEL(inputImage).g + IMG_THIS_PIXEL(inputImage).b) / 3.0;  // 0-1
	float rampBottom = iLevel;
	float rampTop = iLevel + rampWidth;
	if (thisBrightness <= rampBottom) {
		gl_FragColor = IMG_THIS_PIXEL(inputImage) * vec4(0.0, 0.0, 0.0, 1.0);
	} else if (thisBrightness >= rampTop) {
	    gl_FragColor = IMG_THIS_PIXEL(inputImage);
	} else {
	    float perc = (thisBrightness - rampBottom) / rampWidth;
	    //perc = smoothstep(0.0, 1.0, perc);
		//float targetBrightness = rampBottom + 
		float adjust = perc;
		gl_FragColor = IMG_THIS_PIXEL(inputImage) * vec4(adjust, adjust, adjust, 1.0);
	}
}