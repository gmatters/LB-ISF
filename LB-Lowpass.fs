/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Color Adjustment",
		"LB"
	],
	"DESCRIPTION": "Subjective analog to a lowpass filter, clamps brightest pixels first then pulls everything towards black",
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
	//gl_FragColor = clamp(IMG_THIS_PIXEL(inputImage) + vec4(bright,bright,bright,0.0), 0.0, 1.0);
	// As "level" is increased, the lowpass clamps the output further and further down.
	float maxBrightness = (1.0 - level) * 3.0;
	float thisBrightness = IMG_THIS_PIXEL(inputImage).r + IMG_THIS_PIXEL(inputImage).g + IMG_THIS_PIXEL(inputImage).b;
	if (thisBrightness <= maxBrightness) {
		gl_FragColor = IMG_THIS_PIXEL(inputImage);
	} else {
		float adjust = maxBrightness / thisBrightness;
		gl_FragColor = IMG_THIS_PIXEL(inputImage) * vec4(adjust, adjust, adjust, 1.0);
	}
}