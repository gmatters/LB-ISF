# LB-ISF
ISF Filters

## Musical

LB-Lowpass: designed as visual match to sweeping a lowpass filter down

LB-Highpass: same for highpass

LB-Drill: visual equivalent of small audio loops, with delay specified in seconds. Toggles two frames at exactly the delay specified. To express audio delays smaller than one frame, you can apply "subframe" to multiply the frame contents.

LB-Hold: visual equivalent of a spectral freeze or quick infinite reverb hold. Freezes a frame and slowly stretches it into abstractness.

## Drawing

LB-Graffiti and LB-Brush take brush position as input and draw onto the output

## Color

LB-HueDistort is a hue rotate that operates in perceptually uniform color space, with options for flipping and biasing the color space in order to tease out nice distributions of output colors.

LB-HueRange limits the range of hues by mapping colors to avoid part of the color wheel. Options to choose between smooth output with possible color collisions or a unique mapping with a discontinuity.

LB-Palettize allows you to specify, in the filter code, a set of 9 colors. Input colors will be posterized to the nearest palette color. There are some options to force different color mappings in order to find a useful output.

## Time

LB-PosterizeTime reduces the frame rate by taking occasional input frames and fading between them. Can make anything chill.

## Util

LB-Cutout attempts to erase the "empty" space from around a subject shot on top of a solid background. Works best when the subject is completely surrounded by background. It isn't magic, but can work well for things like simple drawings on white paper.

LB-KeyStatus visualizes the alpha channel as black for transparent, white for opaque, and grey for anything in between. Useful to check the output of a color or luma key.

## Misc

LB-RecordLabel turns anything into a spinning vinyl record label. 33RPM is still too fast for many things to read, so reduce to taste.
