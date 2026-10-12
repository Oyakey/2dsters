shader_type canvas_item;

uniform int texture_scale = 1;

void fragment() {
    COLOR = texture(TEXTURE, UV / float(texture_scale));
}
