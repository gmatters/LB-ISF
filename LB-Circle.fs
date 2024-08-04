/*{
    "CATEGORIES": [
        "LB"
    ],
    "CREDIT": "by Geoff Matters",
    "DESCRIPTION": "Flexible Circle Wrap",
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "DEFAULT": 1,
            "MAX": 4,
            "MIN": 0,
            "NAME": "size",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "MAX": 1,
            "MIN": 0,
            "NAME": "hCrop",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "MAX": 1,
            "MIN": 0,
            "NAME": "vCrop",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "MAX": 1,
            "MIN": 0,
            "NAME": "dilate",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "MAX": 4,
            "MIN": 1,
            "NAME": "mirrors",
            "TYPE": "long"
        },
        {
            "LABEL": "Edge Blend",
            "DEFAULT": 0.1,
            "MAX": 0.5,
            "MIN": 0,
            "NAME": "blend",
            "TYPE": "float"
        },
        {
            "LABEL": "Twist",
            "DEFAULT": 0,
            "MAX": 5,
            "MIN": -5,
            "NAME": "twist",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Center from",
            "LABELS": [
                "Bottom",
                "Top",
                "Left",
                "Right"
            ],
            "NAME": "mode",
            "TYPE": "long",
            "VALUES": [
                0,
                1,
                2,
                3
            ]
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
        }
    ]
}
*/

// XXX: there is a little bit of an artifact at the seam which can be seen as a dark line on bright footage. Related to 0.5 offset to pixel center? It persists no matter how high I turn edge blend, but disappears when hCrop is reduced to 99. Or is this only VDMX? Can't seem to repro in ISFEditor
// TODO: a 'smooth' control which does non-linear radius that expands center area, thus giving it more area in the output to prevent it from disappearing.
// TODO: see notes in keep for other ideas
// TODO: consider a blur or blend at the center point to reduce the digitalness of the singularity
// TODO: consider option to anti-alias edge of circle (notably jagged when isfedit is at low resolution)

float map(float n, float i1, float i2, float o1, float o2){
  return o1 + (o2-o1) * (n-i1)/(i2-i1);
}

const float pi = 3.14159265359;
const float tau = 6.28318530718;

vec4 getCroppedPixel(vec2 src) {
    // Dilate gives a non-linear mapping which makes the center of output take
    // up a larger area. It counteracts the way that wrapping dedicates much
    // smaller area to the half of footage packed into center.  TODO: this
    // could be done as an operation on r, it is just tricker to keep the scale
    // constant becase r is not normalized to [0,1] the way that x and y are.
    if (mode == 0) {
      src.y *= pow(src.y, pow(dilate, 2.0));
    } else if (mode == 1) {
      src.y = 1.0 - src.y;
      src.y *= pow(src.y, pow(dilate, 2.0));
      src.y = 1.0 - src.y;
    } else if (mode == 2) {
      src.x *= pow(src.x, pow(dilate, 2.0));
    } else if (mode == 3) {
      src.x = 1.0 - src.x;
      src.x *= pow(src.x, pow(dilate, 2.0));
      src.x = 1.0 - src.x;
    }
    // Use a subset of the input 
    src.x = map(src.x, 0.0, 1.0, 0.5 - hCrop/2.0, 0.5+hCrop/2.0);
    src.y = map(src.y, 0.0, 1.0, 0.5 - vCrop/2.0, 0.5+vCrop/2.0);
    return IMG_NORM_PIXEL(inputImage, src);
}

void main()     {
  vec2 src = isf_FragNormCoord.xy;
  vec2 center = vec2(0.5, 0.5);

  // Normalization is [0-1] on each axis even if they differ in size. Counter
  // that so we get a perfect circle.
  src.x = src.x * RENDERSIZE.x/RENDERSIZE.y;
  center.x = center.x * RENDERSIZE.x/RENDERSIZE.y;

  // translate from polar coords back to cart for this point
  // the effect translates (x,y) to (r,theta) such that top maps to center
  float inputAngle = mode == 0 ? 0.0 : 0.5;
  if (mirrors == 2 || mirrors == 4) {
    inputAngle += 0.5/float(mirrors); // Keep center centered
  }
  float r = distance(src,center);
  float theta = (inputAngle * tau + pi + atan(src.x-center.x,src.y-center.y))/tau;
  theta += r * twist;
  theta = mod(theta, 1.0);
  src = vec2(theta, (r * 2.0)/size);

  if ((src.x < 0.0)||(src.x > 1.0)||(src.y < 0.0)||(src.y > 1.0))
    // Areas outside of source rectangle are transparent
    gl_FragColor = vec4(0.0);
  else    {
    // At this point we are working in cartesian, changing
    // which part of the input rectangle we are targetting.

    /* "Bottom", "Top", "Left", "Right" */
    if (mode == 0) {
      src = vec2(src.x, src.y);
    } else if (mode == 1) {
      src = vec2(src.x, 1.0 - src.y);
    } else if (mode == 2) {
      src = vec2(src.y, src.x);
    } else if (mode == 3) {
      src = vec2(1.0 - src.y, src.x);
    }

    if (mirrors > 1)     {
      if (mode == 0 || mode == 1) {
        src.x = mod(src.x * float(mirrors), 2.0); // mod 2.0 for odd/even
        if (src.x > 1.0) {
          src.x = 2.0 - src.x; // flip the evens
        }
      } else {
        src.y = mod(src.y * float(mirrors), 2.0); // mod 2.0 for odd/even
        if (src.y > 1.0) {
          src.y = 2.0 - src.y; // flip the evens
        }
      }
    }

    gl_FragColor = getCroppedPixel(src);

    // Edge blending is implemented by taking mirror across the seam, because
    // unless we are xCropped there aren't any extra real pixels.
    if (mode == 0 || mode == 1) {
      if (src.x < blend) {
        vec2 srcSmooth = vec2(1.0 - src.x, src.y);
        float mixAmount = map(src.x, 0.0, blend, 0.5, 0.0);
        gl_FragColor = mix(gl_FragColor, getCroppedPixel(srcSmooth), mixAmount);
      }
      else if (1.0-blend < src.x) {
        vec2 srcSmooth = vec2(1.0 - src.x, src.y);
        float mixAmount = map(src.x, 1.0-blend, 1.0, 0.0, 0.5);
        gl_FragColor = mix(gl_FragColor, getCroppedPixel(srcSmooth), mixAmount);
      }
    } else { 
      if (src.y < blend) {
        vec2 srcSmooth = vec2(src.x, 1.0 - src.y);
        float mixAmount = map(src.y, 0.0, blend, 0.5, 0.0);
        gl_FragColor = mix(gl_FragColor, getCroppedPixel(srcSmooth), mixAmount);
      }
      else if (1.0-blend < src.y) {
        vec2 srcSmooth = vec2(src.x, 1.0 - src.y);
        float mixAmount = map(src.y, 1.0-blend, 1.0, 0.0, 0.5);
        gl_FragColor = mix(gl_FragColor, getCroppedPixel(srcSmooth), mixAmount);
      }
    }
  }
}

