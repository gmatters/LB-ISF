/*{
	"DESCRIPTION": "RGB all white, varying alpha",
	"CREDIT": "Lance Blisters",
	"ISFVSN": "2.0",
	"CATEGORIES": [
		"TEST-GLSL"
	]
}*/

void main() {
	gl_FragColor = vec4(1.0,1.0,1.0,isf_FragNormCoord.x);
}
