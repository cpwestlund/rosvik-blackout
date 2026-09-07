shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, filter_linear;
void fragment(){
 vec3 c=texture(screen_texture,SCREEN_UV).rgb;
 vec2 p=SCREEN_UV*2.0-1.0;
 float vignette=smoothstep(0.35,1.5,length(p));
 c*=1.0-vignette*0.25;
 COLOR=vec4(c,1.0);
}
