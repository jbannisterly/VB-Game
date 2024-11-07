Option Strict On
Imports NEA.OpenGLImporter
'// Class containing constant strings of shaders
'// Shaders are written in GLSL and will be compiled at runtime
Public Class Shaders

    Public Const VERTEX_SHADER_HEALTH As String = "
#version 400
layout(location = 0) in vec2 vPos;
layout(location = 1) in vec3 vCol;
out vec3 fCol;
 
void main(){
  fCol = vCol;
  gl_Position = vec4(vPos,0,1);
}
"

    Public Const FRAGMENT_SHADER_HEALTH As String = "
#version 400
in vec3 fCol;
out vec4 colour;
 
void main(){
  colour = vec4(fCol,1);
}
"

    Public Const VERTEX_SHADER_ANIMATED_MODEL As String = "
#version 400
layout(location = 0) in vec3 vp;
layout(location = 1) in vec4 vweight;
layout(location = 2) in vec3 vn;
layout(location = 3) in vec3 vTan;
layout(location = 4) in uvec4 vjoint;
layout(location = 5) in vec2 vTex;
uniform mat4 perspectiveMatrix;
uniform mat4 relativeMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;
uniform mat4 animateMatrix[200];
out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec3 worldPos;
out vec2 texPos;
out vec3 col;
void main() {
  float colour[10];
  int joint[4];
  mat4 skinMatrix = mat4(0);
  int i;
  for(i = 0; i < 4; i++){
    skinMatrix += animateMatrix[vjoint[i]] * vweight[i];
  }
    
  worldPos = vec3(modelMatrix * skinMatrix * vec4(vp,1));
  normal = mat3(modelMatrix) * mat3(skinMatrix) * vn;
  tangent = mat3(modelMatrix) * mat3(skinMatrix) * vTan;
  normal = normalize(normal);
  
  if(dot(tangent,tangent) < 0.001){tangent = vec3(1,0,0);}
  // Fix broken tangents if texture references are the same
  
  tangent = normalize(tangent);
  bitangent = cross(normal, tangent);
  texPos = vTex;
  col = vec3(1,1,1);
  gl_Position = perspectiveMatrix * vec4(worldPos,1);
}
"

    Public Const FRAGMENT_SHADER_GLTF_DEFER As String = "
#version 420
 
layout(early_fragment_tests) in;
 
in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec3 worldPos;
in vec2 texPos;
in vec3 col;
 
uniform sampler2D albedo;
uniform sampler2D normalMap;
uniform sampler2D metalMap;
uniform sampler2D specularMap;
uniform int reflection;
uniform vec3 playerPos;
uniform vec3 highlightCol;
 
layout (location = 0) out vec4 out_normal;
layout (location = 1) out vec4 world_pos;
layout (location = 2) out vec4 frag_colour;
layout (location = 3) out vec4 out_reflection;
layout (location = 4) out vec4 out_id;
 
void main() {
  vec3 normalTexture;
  float specularTexture;
  vec3 normalModified;
  float metalTexture;
  float highlight;
  normalTexture = texture(normalMap,texPos).xyz;
  normalTexture = normalTexture * 2 - vec3(1,1,1);
  metalTexture = texture(metalMap, texPos).x;
  specularTexture = texture(specularMap, texPos).x;
    
  normalModified = normalize(mat3(normalize(tangent), normalize(bitangent), normalize(normal)) * normalTexture);
  normalModified = normalModified * 0.5 + vec3(0.5,0.5,0.5);
 
  if (reflection == 1 && worldPos.y < 40.1){discard;}
  
  highlight = pow(1 - abs(dot(normalize(playerPos - worldPos),normalize(normal))),3);
 
  frag_colour = texture(albedo,texPos) * vec4(col,1) + vec4(highlightCol,0) * highlight * 2;
  world_pos = vec4(worldPos * 0.0001,1);
  out_normal = vec4(normalModified,1);
  out_reflection = vec4(metalTexture,specularTexture,1,1);
  out_id = vec4(1,0,0,0);
}
"

    Public Const FRAGMENT_SHADER_GLTF As String = "
#version 400
in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec3 worldPos;
in vec2 texPos;
in vec3 col;
 
uniform sampler2D albedo;
uniform sampler2D normalMap;
uniform sampler2D metalMap;
uniform sampler2D specularMap;
uniform int reflection;
uniform vec3 light;
 
out vec4 frag_colour;
 
void main() {  
  float diffuse;
  vec3 baseColour;
  vec3 normalTexture;
  vec3 normalModified;
  vec3 outputColour;
  baseColour = texture(albedo,texPos).xyz;
  normalTexture = texture(normalMap,texPos).xyz;
  normalTexture = normalTexture * 2 - vec3(1,1,1);
  baseColour = pow(baseColour, vec3(2.2));
  
  normalModified = normalize(mat3(normalize(tangent), normalize(bitangent), normalize(normal)) * normalTexture);
 
  diffuse = dot(normalModified, normalize(light));
  
  outputColour = baseColour * clamp(diffuse + 0.1,0.2,1.0);
  outputColour = pow(outputColour, vec3(1/2.2));
  frag_colour = vec4(outputColour,1);
}
"

    Public Const VERTEX_SHADER_GRASS As String = "
#version 400
layout(location = 0) in vec3 vPos;
layout(location = 1) in vec2 chunkPos;
 
uniform mat4 perspectiveMatrix;
uniform sampler2D heightmap;
uniform sampler2D normalmap;
uniform vec3 playerPos;
uniform vec2 terrainOffset;
 
out vec3 worldPosV;
out vec3 normalV;
 
void main() {
  float height;
  vec3 vPosModified; //= vPos + 32 * chunkPos.xxy;
  //vPosModified = vPos * 0.5 + 32 * chunkPos.xxy;
  vPosModified = vPos * 0.5 + floor(playerPos) - vec3(50,0,50);
  if(mod(vPosModified.x,2) == 0){vPosModified.z += 0.25;}
  height = texture(heightmap, (vPosModified.xz - terrainOffset) / 1024).x * 100;
  worldPosV = vec3(vPosModified.x,height-0.1,vPosModified.z);
  normalV = texture(normalmap, (vPosModified.xz - terrainOffset) / 1024).xyz * 2 - vec3(1,1,1);
  gl_Position = vec4(0,0,0,1);
}
"

    Public Const GEOMETRY_SHADER_GRASS As String = "
#version 400
layout (points) in;
layout (triangle_strip, max_vertices = 255) out;
 
in vec3 worldPosV[];
in vec3 normalV[];
 
uniform mat4 perspectiveMatrix;
uniform vec3 cameraPos;
uniform vec3 playerPos;
uniform float inputValue;
uniform int rain;
uniform vec3 cameraVector;
 
out vec3 worldPosF;
out vec2 texPosF;
out vec3 normalF;
out vec3 colourF;
out float distanceF;
 
void Square(vec3 position, vec3 basePos, float height, vec3 animation, mat3 hillMatrix){
  worldPosF = basePos + hillMatrix * -position;
  texPosF = vec2(0,1);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * position;
  texPosF = vec2(1,1);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * (-position + vec3(0,height,0) + animation);
  texPosF = vec2(0,0);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * (position + vec3(0,height,0) + animation);
  texPosF = vec2(1,0);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1);
  EmitVertex();
 
  EndPrimitive();
}
 
void GrassBlade(vec3 position, vec3 basePos, float height, vec3 animation, mat3 hillMatrix){
  vec3 newAnimation;
  newAnimation = animation + (vec3(1.0,1.0,1.0) * abs(fract(basePos.x * 1023.23 + basePos.z * 233.32 + inputValue * 1.43)-0.5) - vec3(0.25,0.25,0.25)) * 0.3;
  newAnimation.y = height;
 
  worldPosF = basePos - hillMatrix * position * 0.03;
  texPosF = vec2(fract(basePos.x * basePos.z * 0.23),1.0);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1.0);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * position * 0.03;
  texPosF = vec2(fract(basePos.x * basePos.z * 0.23 + 0.1),1.0);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1.0);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * newAnimation * 0.5 - position * 0.03;
  texPosF = vec2(fract(basePos.x * basePos.z * 0.23 + 0.05),0.6);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1.0);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * newAnimation * 0.5 + position * 0.03;
  texPosF = vec2(fract(basePos.x * basePos.z * 0.23 + 0.05),0.6);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1.0);
  EmitVertex();
 
  worldPosF = basePos + hillMatrix * (newAnimation * vec3(1.7,1,1.7));
  texPosF = vec2(fract(basePos.x * basePos.z * 0.23 + 0.05),0.2);
  gl_Position = perspectiveMatrix * vec4(worldPosF,1.0);
  EmitVertex();
 
  EndPrimitive();
}
 
" & SUBROUTINE_SMOOTH_VALUE_NOISE & "
 
void main(){
  vec3 norm = normalV[0];
  normalF = norm;
  vec3 basePos = worldPosV[0];
  float random = Rnd(basePos);
  basePos += random * vec3(1,0,1);
  vec3 animation;
  float height = (random * 0.5 + 0.6) * clamp(normalF.y*200-185,0,1);
  float rippleAmp;
  float distance = dot(basePos - playerPos, basePos - playerPos);
  mat3 hillMatrix;
  vec4 normalF4 = vec4(normalF, 0);
  float patchFactor;
    
  colourF = vec3(1,1,1) * distance * 0.005;
  colourF = vec3(0.1,0.1,0.1) * random + vec3(0.9,0.9,0.9);
  patchFactor = SmoothValueNoise(basePos.xz / 3).x;
  if (patchFactor < 0.5){patchFactor = 0;}
 
  if(normalF.y > 0.925 && basePos.y < 65 && basePos.y > 42){
    float theta = Rnd(basePos) * 6.28;
    float sine = sin(theta);
    float cosine = cos(theta);
    int i;
    vec3 moveAway = normalize(basePos - playerPos) / max(distance, 4);
    vec3 newPos;
    mat3 rotMatrix = mat3(
      cosine, 0, -sine,
      0, 1, 0,
      sine, 0, cosine);
    height *= min((3000-distance) * 0.01,1);
    height *= min(distance, 1);
    height *= patchFactor;
    if(height > 0.1){
      moveAway.y = 0;
      rippleAmp = Rnd(basePos + vec3(0,1,0)) * 0.2 + 0.2;
      animation = SmoothValueNoise(2 * inputValue * vec3(1,0,1) + basePos * 0.1).xxy * (rain*2+1) - (rain*2+1) * vec3(0.5,0,0.5);
      animation.y = 0;
      vec3 xVector = vec3(normalize(norm.yx) * vec2(1,-1),0);
      vec3 zVector = vec3(0,normalize(norm.zy) * vec2(-1,1));
      if(xVector.x < 0){xVector *= -1;}
      if(zVector.z < 0){zVector *= -1;}
      basePos.y += random * (xVector.y + zVector.y);
 
      hillMatrix = mat3(
        xVector,
        normalize(norm),
        zVector
      );
      distanceF = distance;
      if (dot(normalize(basePos - cameraPos),cameraVector) > 0.4 || distance < 16){
        if (distance > 144 && patchFactor > 0.5){
          Square(rotMatrix * vec3(0,0,0.5),basePos,height,animation+moveAway,hillMatrix);
          Square(rotMatrix * vec3(0.433,0,-0.25),basePos,height,animation+moveAway,hillMatrix);
          Square(rotMatrix * vec3(0.433,0,0.25),basePos,height,animation+moveAway,hillMatrix);
        }
        if (patchFactor > 0.9){
          Square(rotMatrix * vec3(0,0,0.5),basePos,height*2,animation+moveAway,hillMatrix);
          Square(rotMatrix * vec3(0.433,0,-0.25),basePos,height*2,animation+moveAway,hillMatrix);
          Square(rotMatrix * vec3(0.433,0,0.25),basePos,height*2,animation+moveAway,hillMatrix);
        }
        if (distance < 169){
          for(i = 0; i < int(clamp(patchFactor * 50.0,0.0,50.0)); i++){
            newPos = basePos+RndVec3(basePos.xz + vec2(i,0) * 34.21);
            newPos.y += (newPos.x-basePos.x) * xVector.y + (newPos.z-basePos.z) * zVector.y;
            moveAway = normalize(newPos - playerPos) / max(distance, 1);
            moveAway.y = 0;
            GrassBlade(normalize(vec3(newPos.z - cameraPos.z,0,cameraPos.x - newPos.x)),newPos,height,animation+moveAway,hillMatrix);
          }
        }
      }
    }
  }
}
"

    Public Const FRAGMENT_SHADER_GRASS As String = "
#version 400
in vec3 worldPosF;
in vec2 texPosF;
in vec3 normalF;
in vec3 colourF;
in float distanceF;
 
uniform sampler2D grassTexture;
 
layout (location = 0) out vec4 normal;
layout (location = 1) out vec4 world_pos;
layout (location = 2) out vec4 frag_colour;
layout (location = 3) out vec4 out_reflection;
 
" & SUBROUTINE_SMOOTH_VALUE_NOISE & "
 
void main(){
  vec3 col = texture(grassTexture,texPosF).xyz;
  if(col.x > col.y){discard;}
  vec2 patchy = SmoothValueNoise(worldPosF.xz * 0.05) + SmoothValueNoise(worldPosF.xz * 0.03);
  normal = vec4(normalF * 0.5 + vec3(0.5,0.5,0.5),1);
  world_pos = vec4(worldPosF * 0.0001,1);
 
  frag_colour = vec4(col * colourF * vec3(patchy.x * 0.75,1,patchy.y * 0.5) * 
    (1 + clamp((0.4 - texPosF.y) * clamp((600 - distanceF) * 0.01,0,1),-0.2,0.2))
  ,1);
  out_reflection = vec4(0,0,0,0);
}
"

    Public Const VERTEX_SHADER_TERRAIN As String = "
#version 400
layout(location = 0) in vec3 vPos;
layout(location = 1) in vec2 chunkPos;
uniform mat4 perspectiveMatrix;
uniform mat4 viewMatrix;
uniform mat4 relativeMatrix;
uniform mat4 modelMatrix;
uniform sampler2D heightmap;
uniform sampler2D normalmap;
uniform vec2 terrainOffset;
out vec3 col;
out vec3 norm;
out vec3 relPos;
out vec3 worldPos;
void main() {
  float height;
  vec3 vPosModified = vPos + 63 * chunkPos.xxy;
  height = texture(heightmap, (vPosModified.xz -  terrainOffset) / 1024).x * 100;
  norm = texture(normalmap, (vPosModified.xz - terrainOffset) / 1024).xyz;
  col = vec3(chunkPos.x,0,gl_InstanceID * 0.1);
  worldPos = vec3(vPosModified.x,height,vPosModified.z);
  relPos = vec3(relativeMatrix * vec4(worldPos,1));
  gl_Position = perspectiveMatrix * vec4(worldPos,1);
}
"

    Public Const VERTEX_SHADER_DEFERRED As String = "
#version 400
layout(location = 0) in vec3 vPos;
out vec2 position;
void main() {
  gl_Position = vec4(vPos,1);
  position = vPos.xy;
}
"

    Public Const FRAGMENT_SHADER_DEFERRED As String = "
#version 400
const float shadowSize[3] = float[3](16,64,256);
uniform sampler2D shadow[4];
uniform samplerCube sky;
uniform sampler2D heightmap;
uniform sampler2D reflectionTexture;
uniform vec3 light;
uniform vec3 lightColour;
uniform vec2 focusPoint;
uniform float inputValue;
uniform vec3 playerPos;
uniform float playerRot;
uniform int reflection;
uniform vec2 inverseScreenCoord;
uniform vec3 splash[8];
uniform float saturation;
uniform float oofFactor;
uniform mat4 cameraMatrix;
 
uniform sampler2D INnormal;
uniform sampler2D INworld;
uniform sampler2D INcolour;
uniform sampler2D INreflection;
uniform sampler2D INrain;
uniform sampler2D INsky;
uniform sampler2D INid;
uniform int rain;
uniform float time;
vec3 norm;
 vec3 relPos;
 vec3 worldPos;
 vec3 baseColour;
in vec2 position;
out vec4 frag_colour;
" & SUBROUTINE_FOG & SUBROUTINE_SPECULAR & SUBROUTINE_SMOOTH_VALUE_NOISE & "
 
float ShadowMaskSimple(){
  float depthTexture;
  float distanceToCentre;
  float surroundShadow;
  vec2 relativeCheck;
  vec2 distance;
  vec2 mainTargetSurround;
  int shadowUsed;
  int i;
  vec2 targetCheck = worldPos.xz - light.xz / light.y * worldPos.y - focusPoint;
  depthTexture = 100;
  distanceToCentre = max(abs(targetCheck.x),abs(targetCheck.y));
  for(i = 2; i >= 0; i--){
    if(distanceToCentre < shadowSize[i]){shadowUsed = i;}
  }
  relativeCheck = targetCheck * 0.5 / shadowSize[shadowUsed] + vec2(0.5,0.5);
 
  depthTexture = texture(shadow[shadowUsed],relativeCheck).x;
  if (depthTexture * 100 <= worldPos.y + 0.01){
    return 1;
  }
  return 0;
}
 
float ShadowMask(){
  float depthTexture;
  float distanceToCentre;
  float surroundShadow;
  vec2 relativeCheck;
  vec2 distance;
  vec2 mainTargetSurround;
  int shadowUsed;
  int i;
  vec2 targetCheck = worldPos.xz - light.xz / light.y * worldPos.y - focusPoint;
  depthTexture = 100;
  distanceToCentre = max(abs(targetCheck.x),abs(targetCheck.y));
  for(i = 2; i >= 0; i--){
    if(distanceToCentre < shadowSize[i]){shadowUsed = i;}
  }
  relativeCheck = targetCheck * 0.5 / shadowSize[shadowUsed] + vec2(0.5,0.5);
 
 
    for (i = 1; i <= 4; i++){
      depthTexture = texture(shadow[shadowUsed],relativeCheck + vec2(Rnd(worldPos * i) - 0.5,Rnd(worldPos * i * 1.23) - 0.5) * 0.004).x;
      if (depthTexture * 100 <= worldPos.y + 0.01){
        surroundShadow += 1;
      }
    }
    if (surroundShadow == 4){return 1;}
    if (surroundShadow == 0){return 0;}
    surroundShadow = 0;
 
    for (i = -4; i <= 4; i++){
      for(int j = -4; j <= 4; j++){
        depthTexture = texture(shadow[shadowUsed],relativeCheck + 0.0005 * (vec2(i,j) + 0 * vec2(Rnd(worldPos * i),Rnd(worldPos * 1.2 * j)))).x;
        if (depthTexture * 100 <= worldPos.y + 0.01){
         surroundShadow += (1 / 81.0);
        }
      }
    }
  return surroundShadow;
}
 
" & SUBROUTINE_WATER & "
 
void main() {
  float grey;
  float diffuse;
  vec3 mainCol;
  vec3 reflectionVector;
  vec3 metalColour;
  float shadowFactor;
  vec2 reflectionTexture;
  float specularFactor;
  vec3 specular;
  float reflectionFactor;
  vec3 reflectionColour;
  vec2 newPosition;
  float id;
 
  newPosition = position * 0.5 + vec2(0.5,0.5);
  norm = normalize(texture(INnormal,newPosition).xyz) * 2 - vec3(1,1,1);
  worldPos = texture(INworld,newPosition).xyz *10000;
  reflectionTexture = texture(INreflection,newPosition).xy;
  id = texture(INid, newPosition).x;
  reflectionFactor = reflectionTexture.x;
  specularFactor = reflectionTexture.y;
  baseColour = texture(INcolour,newPosition).xyz;
  baseColour = pow(baseColour, vec3(2.2));
 
  relPos = worldPos - playerPos;
  reflectionVector = reflect(normalize(relPos), norm);
 
  if (reflection == 1 && worldPos.y < 40.1){discard;}
 
  diffuse = clamp(dot(norm,normalize(light)),0.0,0.9);
  shadowFactor = @;
  diffuse *= shadowFactor;
 
  mainCol = vec3(baseColour * (0.1 + diffuse) * (1 - 0.9 * rain)) * lightColour;
  mainCol = Fog(mainCol, relPos);
 
  if((worldPos.y < 40.1) && reflection == 0){
    mainCol = Water(mainCol, light);
  }    
  specular = GetSpecular(norm, light, 300, vec3(200,200,200)) * shadowFactor;
  specular += 0.2 * rain * specular;
  mainCol += specular * pow(specularFactor,10);
 
  reflectionColour = pow(texture(sky, reflectionVector).xyz, vec3(2.2));
  if (rain == 1){
    reflectionColour = vec3(1,1,1)-reflectionColour;
    float greySky = dot(reflectionColour,vec3(0.33,0.33,0.33));
    reflectionColour = mix(greySky* vec3(1,1,1),reflectionColour,0.1);
  }
 
  metalColour = baseColour * (reflectionColour + specular);
  reflectionFactor += 0.15 * rain;
  mainCol = mix(mainCol, metalColour, reflectionFactor);
 
  mainCol = pow(mainCol, vec3(1/2.2));
  grey = dot(mainCol,vec3(0.33,0.33,0.33));
  mainCol = mix(grey * vec3(1,1,1),mainCol,saturation);
 
  float oof;
  oof = dot(position.xy,position.xy) * oofFactor;
  if(dot(relPos,relPos) > 40000000){mainCol = texture(INsky,newPosition).xyz;}
  mainCol *= vec3(1, 1 - oof, 1 - oof);
 
  float distance = dot(relPos,relPos);
  float hDistance = sqrt(dot(relPos.xz,relPos.xz));
  vec2 rainPosition;
  vec3 viewVector;
  viewVector = normalize(mat3(cameraMatrix) * vec3(position,1));
  rainPosition = vec2(atan(viewVector.x,viewVector.z),viewVector.y/hDistance*sqrt(distance));
 
  mainCol += texture(INrain, fract(vec2(rainPosition.x + 0.28 * floor(time * 1.17), rainPosition.y + time * 7.23))).xyz * rain * 0.4;
  if (distance > 15){
    mainCol += texture(INrain, fract(vec2(rainPosition.x * 3 + 0.28 * floor(time * 2.37), rainPosition.y * 3 + time * 5.23))).xyz * rain * 0.4;
  }
  if (distance > 80){
    mainCol += texture(INrain, fract(vec2(rainPosition.x * 9 + 0.34 * floor(time * 2.53), rainPosition.y * 7 + time * 4.23))).xyz * rain * 0.4;
  }
  if (distance > 300){
    mainCol += texture(INrain, fract(vec2(rainPosition.x * 32 + 0.14 * floor(time * 2.53), rainPosition.y * 12 + time * 3.23))).xyz * rain * 0.4;
  }
 
  frag_colour = vec4(mainCol, 1);
}
"

    Public Const FRAGMENT_SHADER_TERRAIN_DEFER As String = "
#version 400
 
uniform float inputValue;
uniform vec3 playerPos;
uniform float playerRot;
uniform int reflection;
uniform sampler2D ground;
uniform sampler2D groundNormal;
 
in vec3 col;
in vec3 norm;
in vec3 relPos;
in vec3 worldPos;
layout (location = 0) out vec4 normal;
layout (location = 1) out vec4 world_pos;
layout (location = 2) out vec4 frag_colour;
layout (location = 3) out vec4 out_reflection;
layout (location = 4) out vec4 out_id;
 
" & SUBROUTINE_SMOOTH_VALUE_NOISE & SUBROUTINE_TILE_SAMPLER & "
 
void main(){
  if(reflection == 1 && worldPos.y < 40.1){discard;}
 
  vec2 patchy;
  vec3 recolour = vec3(1,1,1);
  vec3 texCol;
  vec3 normalMapped;
  vec3 xVector;
  vec3 zVector;
  vec3 normNeg;
 
  normNeg = norm * 2 - vec3(1,1,1);
  xVector = vec3(normalize(normNeg.yx) * vec2(1,-1),0);
  zVector = vec3(0,normalize(normNeg.zy) * vec2(-1,1));
  if(xVector.x < 0){xVector *= -1;}
  if(zVector.z < 0){zVector *= -1;}
 
  texCol = GetTile2D(worldPos,ground,false);
 
  patchy = SmoothValueNoise(worldPos.xz * 0.05) + SmoothValueNoise(worldPos.xz * 0.03);
  if (norm.y > 0.92 && norm.y < 0.925){recolour = mix(vec3(1,0.2,0),vec3(1,1,1),(norm.y-0.92)*200);}
  if (norm.y < 0.92){recolour = vec3(1,0.2,0);}
  recolour *= vec3(patchy.x * 0.75,1,patchy.y * 0.5);
  
  normalMapped = GetTile2D(worldPos,groundNormal,true);
  normalMapped = mat3(xVector, normNeg, zVector) * normalMapped;
 
  normal = vec4(normalMapped * 0.5 + vec3(0.5,0.5,0.5),1);
  world_pos = vec4(worldPos * 0.0001,1);
  frag_colour = vec4(texCol * recolour * 0.8f, 1);
  out_reflection = vec4(0,0,0,0);
  out_id = vec4(0,1,0,0);
}
"

    'FOR THE SHADOW GENERATOR, FOCUS POINT IS 0,0 IN -1 TO 1
    'FOR THE TERRAIN DISPLAY IT IS 0,0 IN 0 TO 1

    Public Const VERTEX_SHADER_SHADOW_TERRAIN As String = "
#version 400
layout(location = 0) in vec3 vPos;
uniform sampler2D heightmap;
uniform vec2 lightGradient;
uniform vec2 focusPoint;
uniform vec2 terrainOffset;
uniform float scale;
out float depth;
 
void main() {
  float height;
  vec2 shadowPosition;
  height = texture(heightmap, (vPos.xz - terrainOffset) / 1024).x * 100 - 0.5;
  shadowPosition = vec2(vPos.xz - height * lightGradient) - focusPoint;
  gl_Position = vec4(shadowPosition * scale, height * 0.01,1);
  depth = height * 0.01;
}
"

    Public Const VERTEX_SHADER_SHADOW_ANIMATION As String = "
#version 400
layout(location = 0) in vec3 vp;
layout(location = 1) in vec4 vweight;
layout(location = 2) in uvec4 vjoint;
uniform mat4 modelMatrix;
uniform mat4 animateMatrix[200];
uniform vec2 lightGradient;
uniform vec2 focusPoint;
uniform float scale;
out float depth;
 
void main() {
  int joint[4];
  mat4 skinMatrix = mat4(0);
  float height;
  vec2 shadowPosition;
  vec3 worldPos;
 
  int i;
  for(i = 0; i < 4; i++){
    skinMatrix += animateMatrix[vjoint[i]] * vweight[i];
  }
 
  worldPos = vec3(modelMatrix * skinMatrix * vec4(vp,1));
 
  height = worldPos.y;
  shadowPosition = vec2(worldPos.xz - height * lightGradient) - focusPoint;
  gl_Position = vec4(shadowPosition * scale, height * 0.01,1);
  depth = height * 0.01;
}
"

    Public Const FRAGMENT_SHADER_SHADOW As String = "
#version 400
in float depth;
out vec4 i;
void main() {
  gl_FragDepth = depth;
  i = vec4(depth,1,depth,1);
}
"

    Public Const VERTEX_SHADER_TERRAIN_GENERATOR As String = "
#version 400
layout(location = 0) in vec3 vPos;
void main() {
  gl_Position = vec4(vPos,1);
}
"

    Public Const FRAGMENT_SHADER_TERRAIN_GENERATOR As String = "
#version 400
uniform vec2 offset;
uniform vec2 stride;
uniform sampler2D noise;
out vec4 i;
 
float Lerp(float a, float b, float distance){
  return (b - a) * distance + a;
}
 
float TexDot(vec2 coord,vec2 fraction){
  return dot(texture(noise,coord/128).xy*2-vec2(1,1),fraction);
}
 
float GetNoise(float xPos, float zPos, float scale){
  float baseValue;
  xPos += 0;
  zPos += 0;
  float fractX = fract(xPos / scale);
  float fractZ = fract(zPos / scale);
  float left = floor(xPos / scale);
  float bottom = floor(zPos / scale);
  fractX = fractX * fractX * fractX * ((6 * fractX - 15) * fractX + 10);
  fractZ = fractZ * fractZ * fractZ * ((6 * fractZ - 15) * fractZ + 10);
  float leftHeight = Lerp(TexDot(vec2(left,bottom),vec2(fractX,fractZ)),TexDot(vec2(left,bottom+1),vec2(fractX,fractZ-1)),fractZ);
  float rightHeight = Lerp(TexDot(vec2(left+1,bottom),vec2(fractX-1,fractZ)),TexDot(vec2(left+1,bottom+1),vec2(fractX-1,fractZ-1)),fractZ);
  baseValue = Lerp(leftHeight,rightHeight,fractX) * 0.6;
  return baseValue;
}
 
float GetHeight(float xPos, float zPos){
  float height = GetNoise(xPos,zPos,97) * 0.7 + GetNoise(xPos,zPos,31) * 0.2 + GetNoise(xPos,zPos,59) * 0.5 + GetNoise(xPos,zPos,11) * 0.02 + 0.5;
  float ocean = clamp(1 - ((xPos * xPos + zPos * zPos - 1000000) / 100000),0,1);
  return height * ocean;
 
}
 
 
void main(){
  vec2 pixelPosition;
  float depth;
  float gradientX;
  float gradientZ;
  vec3 normal;
  pixelPosition = stride.xy * gl_FragCoord.xy + offset.xy;
  pixelPosition = gl_FragCoord.xy + offset.xy;
  depth = GetHeight(pixelPosition.x,pixelPosition.y);
  gradientX = GetHeight(pixelPosition.x + 1,pixelPosition.y    ) - GetHeight(pixelPosition.x - 1,pixelPosition.y    );
  gradientZ = GetHeight(pixelPosition.x    ,pixelPosition.y + 1) - GetHeight(pixelPosition.x    ,pixelPosition.y - 1);
  normal = normalize(cross(vec3(2,gradientX * 100,0),vec3(0,gradientZ * 100,2)));
  if (normal.y < 0){normal *= -1;}
  normal = normal * 0.5 + vec3(0.5,0.5,0.5);
  gl_FragDepth = depth;
  i = vec4(normal, 1);
}
"

    Public Const VERTEX_SHADER_SKY As String = "
#version 400
layout(location = 0) in vec3 vPos;
uniform mat4 skyMatrix;
out vec3 position;
void main() {
  gl_Position = vec4(vPos.xy,0,1);
  position = vec3(skyMatrix * vec4(vPos,0));
}
"

    Public Const FRAGMENT_SHADER_SKY As String = "
#version 400
in vec3 position;
uniform samplerCube testCube;
uniform vec3 sun;
uniform vec3 moon;
uniform float time;
uniform float saturation;
uniform int rain;
out vec4 frag_colour;
void main(){
  float sunBright = dot(sun,normalize(position.xyz));
  float moonBright = dot(moon,normalize(position.xyz));
  float sunHeight = sun.y;
  float redness;
  vec4 colour;
  float grey;
  if(time < 1.67 || time > 4.61){
    colour = vec4(texture(testCube,position.xyz).xyz,1);
  }
  colour = rain * vec4(1,1,1,1) + (1-rain*2) * colour;
  if (sunHeight < 0.14 && sunHeight >= -0.1){
     redness = max(pow(sunBright,100),sunHeight - normalize(position).y + 0.7);
     colour = colour * (1-redness) + vec4(1,0.7,0.2,1) * (redness);
  }
  if(position.y < 0){
    sunBright = 0;
    moonBright = 0;
  }
  if(dot(position,sun) < 0){sunBright = 0;}
  if(dot(position,moon) < 0){moonBright = 0;}
  colour += vec4(1,1,1,1) * pow(sunBright,300) * 2;
  colour += vec4(1,1,1,1) * pow(moonBright,2000) * 3;
  grey = dot(colour.xyz,vec3(0.33,0.33,0.33));
  frag_colour = vec4(mix(grey* vec3(1,1,1),colour.xyz,saturation), 1);
  //frag_colour = colour;
}
"

    Public Const VERTEX_SHADER_GUI As String = "
#version 400
layout(location=0) in vec3 vPos;
layout(location=1) in vec2 vTex;
layout(location=2) in vec3 vCol;
out vec2 texPos;
out vec3 col;
void main(){
  gl_Position = vec4(vPos,1);
  texPos = vTex;
  col = vCol;
}
"

    Public Const FRAGMENT_SHADER_GUI As String = "
#version 400
in vec2 texPos;
in vec3 col;
uniform sampler2D fontTex;
out vec4 colour;
void main(){
  float display = texture(fontTex,texPos).x;
  if(display > 0.5 && (texPos.x + texPos.y) > 0) {discard;}
  colour = vec4(col,1);
}
"


    Public Const SUBROUTINE_SPECULAR As String = "
vec3 GetSpecular(vec3 normal, vec3 light, float shininess, vec3 reflective){
  vec3 reflected = reflect(normalize(light),normalize(normal));
  float specularHighlight;
  specularHighlight = dot(normalize(relPos),normalize(reflected));
  if(specularHighlight < 0){specularHighlight = 0;}
  return clamp(pow(specularHighlight,shininess) * reflective,0,1);
}
"
    Public Const SUBROUTINE_TILE_SAMPLER As String = "
vec3 GetTile(vec3 worldPos, sampler2D sampleTexture, bool sampleNormal){
  vec3 texColx0;
  vec3 texColx1;
  mat2 rotateSample;
  float rotation;
  float rsin;
  float rcos;
 
  rotation = Rnd(floor(worldPos.xz / 2)) * 6.28;
  rsin = sin(rotation);
  rcos = cos(rotation);
  rotateSample = mat2(rcos,rsin,-rsin,rcos); 
 
  texColx0 = vec3(texture(sampleTexture,rotateSample * vec2(fract(worldPos.xz/2))));
  if(sampleNormal){
    texColx0 = mat3(rcos,0,rsin,0,1,0,-rsin,0,rcos) * (texColx0.xzy * 2 - vec3(1,1,1));
  }
  texColx1 = texColx0;
  if (fract(worldPos.x/2) > 0.5){
    rotation = Rnd(floor(worldPos.xz / 2  + vec2(1,0))) * 6.28;
    rsin = sin(rotation);
    rcos = cos(rotation);
    rotateSample = mat2(rcos,rsin,-rsin,rcos); 
    texColx1 = vec3(texture(sampleTexture,rotateSample * (vec2(-1,0) + vec2(fract(worldPos.xz/2)))));
    if(sampleNormal){
      texColx1 = mat3(rcos,0,rsin,0,1,0,-rsin,0,rcos) * (texColx1.xzy * 2 - vec3(1,1,1));
    }
  }
  return mix(texColx0,texColx1,clamp(2 * (fract(worldPos.x/2) - 0.5),0,1));
}
 
vec3 GetTileA(vec3 worldPos, sampler2D sampleTexture, bool sampleNormal){
  vec3 texColx0;
  vec3 texColx1;
  mat2 rotateSample;
  float rotation;
  float rsin;
  float rcos;
 
  rotation = Rnd(floor(worldPos.xz / 2)) * 6.28;
  rsin = sin(rotation);
  rcos = cos(rotation);
  rotateSample = mat2(rcos,rsin,-rsin,rcos); 
 
  texColx0 = vec3(texture(sampleTexture,rotateSample * (vec2(0,-1) + vec2(fract(worldPos.xz/2)))));
  if(sampleNormal){
    texColx0 = mat3(rcos,0,rsin,0,1,0,-rsin,0,rcos) * (texColx0.xzy * 2 - vec3(1,1,1));
  }
  texColx1 = texColx0;
  if (fract(worldPos.x/2) > 0.5){
    rotation = Rnd(floor(worldPos.xz / 2  + vec2(1,0))) * 6.28;
    rsin = sin(rotation);
    rcos = cos(rotation);
    rotateSample = mat2(rcos,rsin,-rsin,rcos); 
    texColx1 = vec3(texture(sampleTexture,rotateSample * (vec2(-1,-1) + vec2(fract(worldPos.xz/2)))));
    if(sampleNormal){
      texColx1 = mat3(rcos,0,rsin,0,1,0,-rsin,0,rcos) * (texColx1.xzy * 2 - vec3(1,1,1));
    }
  }
  return mix(texColx0,texColx1,clamp(2 * (fract(worldPos.x/2) - 0.5),0,1));
}
 
vec3 GetTile2D(vec3 worldPos, sampler2D sampleTexture, bool sampleNormal){
  return mix(GetTile(worldPos,sampleTexture,sampleNormal),GetTileA(worldPos + vec3(0,0,2),sampleTexture,sampleNormal),clamp(2 * fract(worldPos.z/2) - 1,0,1));
}"

    Public Const SUBROUTINE_WATER As String = "
vec3 Water(vec3 mainCol, vec3 light){
  vec2 waterSample;
  vec2 noiseOffset;
  vec3 waveNormal;
  vec2 waterPos;
  float fresnel;
  vec3 specular;
  vec2 ripple;
  float rippleD;
  vec2 shadowTargetCheck;
  float inputValueScaled;
 
  inputValueScaled = inputValue + inputValue * 2 * rain;
 
  waterPos = worldPos.xz + relPos.xz * (40.1 - worldPos.y) / relPos.y;
  shadowTargetCheck = worldPos.xz - light.xz / light.y * worldPos.y - focusPoint;
 
  waterSample = gl_FragCoord.xy * inverseScreenCoord;
 
 
  noiseOffset = SmoothValueNoise(vec3(waterPos*0.4,inputValueScaled * 0.5) + vec3(inputValueScaled,0,0));
  noiseOffset += SmoothValueNoise(vec3(waterPos*5,inputValueScaled * 0.5) + vec3(inputValueScaled * 0.5,0,inputValueScaled)) * 0.4;
  noiseOffset += SmoothValueNoise(vec3(waterPos,-inputValueScaled * 0.65) + vec3(-inputValueScaled * 0.5,0,inputValueScaled * 0.3)) * 0.4;
  noiseOffset += SmoothValueNoise(vec3(waterPos * 4.7,-inputValueScaled * 0.5) + vec3(0,0,-inputValueScaled));
  noiseOffset += SmoothValueNoise(vec3(waterPos * 15,-inputValueScaled * 0.5) + vec3(0,0,-inputValueScaled)) * rain;
 
  noiseOffset = noiseOffset - 1.4;
 
  noiseOffset += noiseOffset * 10 * rain;
 
  for (int i = 0; i < 8; i++){
    ripple = waterPos - splash[i].xy;
    rippleD = sqrt(dot(ripple,ripple));
    noiseOffset += sin(clamp(rippleD * 5 - splash[i].z * 10,-6.28,0.0f)) * normalize(ripple) * pow(0.8,rippleD) * 4;
  }
 
 waterSample += mix(
  vec2(0,0),
  noiseOffset.yx * 0.007,
 clamp((40.1-worldPos.y)*3,0,1));
 
  fresnel = clamp(-normalize(relPos).y +1- clamp(0.5*(40.1-worldPos.y),0,1),0,1);
  fresnel = -normalize(relPos).y;
  fresnel = pow(fresnel,0.7);
 
  waveNormal = normalize(vec3(noiseOffset.x,5,noiseOffset.y));
  specular = GetSpecular(waveNormal,light,20,vec3(1,1,1) * 0.6) * (1-fresnel);
  if (texture(shadow[2],shadowTargetCheck / 512 + vec2(0.5,0.5)).x * 100 > 40.1){specular = vec3(0,0,0);}
  //return (dot(normalize(light),waveNormal) + specular) * vec3(1,1,1);
  return mix(texture(reflectionTexture, waterSample).xyz + specular,mainCol, fresnel);
}"

    Public Const SUBROUTINE_FOG As String = "
vec3 Fog(vec3 mainCol, vec3 relative){
  float distance;
  float height = (worldPos.y * 2 - relative.y) * 0.005;
  float foggy;
  distance = sqrt(dot(relative,relative));
  foggy = 1 - pow(0.5,distance * (0.005 + rain * 0.01));
  foggy = (1 - height * 0.7) * foggy * 1.5;
  if (foggy > 1){foggy = 1;}
  return mix(mainCol, lightColour * 0.5 * (1-rain*0.7*clamp(distance/30,0,1)),foggy);
}"

    Public Const SUBROUTINE_SMOOTH_VALUE_NOISE As String = "
float Rnd(vec3 seed){
  return fract(45623.3242 * sin(dot(seed,vec3(13.23243,11.63123,12.1223))));
}
 
float Rnd(vec2 seed){
  return fract(45623.3242 * sin(dot(seed,vec2(13.23243,11.6312))));
}
 
vec3 RndVec3(vec2 seed){
  float random;
  random = Rnd(seed);
  return vec3(random, 0, fract(random * 34.23));
}
 
vec2 ValueNoise(vec2 seed){
  return vec2(Rnd(floor(seed)), Rnd(floor(seed) + vec2(0.5,0.5)));
}
 
vec2 ValueNoise(vec3 seed){
  return vec2(Rnd(floor(seed)), Rnd(floor(seed) + vec3(0.5,0.5,0.5)));
}
 
vec2 SmoothValueNoise(vec2 seed){
  vec2 fractSeed = fract(seed);
  vec2 fractFactor = fractSeed * fractSeed * (3.0 - 2.0 * fractSeed);
  return mix(
  mix(ValueNoise(seed),ValueNoise(seed+vec2(1,0)),fractFactor.x),
  mix(ValueNoise(seed+vec2(0,1)),ValueNoise(seed+vec2(1,1)),fractFactor.x),
  fractFactor.y);
}
 
vec2 SmoothValueNoise(vec3 seed){
 /*vec4 y0z0 = vec4(
  ValueNoise(seed),
  ValueNoise(seed+vec3(1,0,0))
 );
 vec4 y1z0 = vec4(
  ValueNoise(seed+vec3(0,1,0)),
  ValueNoise(seed+vec3(1,1,0))
 );
 vec4 y0z1 = vec4(
  ValueNoise(seed+vec3(0,0,1)),
  ValueNoise(seed+vec3(1,0,1))
 );
 vec4 y1z1 = vec4(
  ValueNoise(seed+vec3(0,1,1)),
  ValueNoise(seed+vec3(1,1,1))
 );
 vec4 y0 = mix(y0z0,y0z1,fract(seed.z));
 vec4 y1 = mix(y1z0,y1z1,fract(seed.z));
 vec4 m = mix(y0,y1,fract(seed.y));
 return mix(m.xy,m.zw,fract(seed.x));*/
 
return mix(
 mix(
  mix(ValueNoise(seed),ValueNoise(seed+vec3(1,0,0)),fract(seed.x)),
  mix(ValueNoise(seed+vec3(0,1,0)),ValueNoise(seed+vec3(1,1,0)),fract(seed.x)),
  fract(seed.y)),
 mix(
  mix(ValueNoise(seed+vec3(0,0,1)),ValueNoise(seed+vec3(1,0,1)),fract(seed.x)),
  mix(ValueNoise(seed+vec3(0,1,1)),ValueNoise(seed+vec3(1,1,1)),fract(seed.x)),
  fract(seed.y)),
 fract(seed.z));
 
}
"
End Class

