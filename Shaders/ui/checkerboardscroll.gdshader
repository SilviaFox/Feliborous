shader_type canvas_item;

uniform vec4 color1 : source_color;
uniform vec4 color2 : source_color;
//uniform sampler2D baseTexture : filter_nearest;
uniform vec2 speed;
uniform float size = 4.0;

void fragment() {
	vec2 realTime = vec2(TIME * speed.x, TIME * speed.y);
	vec2 scaledPos = vec2(VERTEX.x / size + realTime.x, VERTEX.y / size + realTime.y);
	
	
	
	float total = (floor(scaledPos.x)) + (floor (scaledPos.y));
	bool isEven = mod(total, 2.0) == 0.0;
	
	
	
	COLOR = (isEven)? color1 : color2;
}
