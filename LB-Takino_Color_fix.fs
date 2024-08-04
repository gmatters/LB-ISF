/*{
	"CREDIT": "by Geoff Matters",
	"ISFVSN": "2",
	"CATEGORIES": [
		"Color Adjustment",
		"LB"
	],
	"DESCRIPTION": "Color fix hacks for a specific piece of footage",
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
        gl_FragColor = IMG_THIS_PIXEL(inputImage);
        //gl_FragColor.r = 0.5;
        gl_FragColor.g = (gl_FragColor.r + gl_FragColor.g) / 2.0;
        gl_FragColor.r = gl_FragColor.g;
}
