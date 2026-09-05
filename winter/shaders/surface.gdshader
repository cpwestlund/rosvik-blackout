shader_type spatial;
render_mode diffuse_burley;
uniform vec4 base_color : source_color = vec4(0.7,0.72,0.74,1.0);
uniform float grain = 0.10;
uniform int surface_kind = 0;
varying vec3 world;
float hash(vec3 p) { return fract(sin(dot(p, vec3(127.1,311.7,74.7)))*43758.5453); }
float noise(vec3 p) {
 vec3 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}
void vertex(){world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
void fragment(){
 float n=noise(world*7.0)*0.55+noise(world*45.0)*0.25+noise(world*0.6)*0.2;
 vec3 c=base_color.rgb*(1.0+(n-0.5)*grain);
 if(surface_kind==1){
  float row=floor(world.y/0.09);
  vec2 b=vec2((world.x+world.z)/0.25+mod(row,2.0)*0.5,world.y/0.09);
  vec2 f=fract(b); float seam=max(step(f.x,0.035),step(f.y,0.07));
  c=mix(c*(0.96+hash(vec3(floor(b),0.0))*0.08),vec3(0.25,0.25,0.23),seam*0.3);
 }
 if(surface_kind==2){float seam=pow(abs(sin((world.x+world.z)*34.0)),18.0);c*=1.0-seam*0.16;}
 ALBEDO=c; ROUGHNESS=surface_kind==3?0.48:0.92;
}
