/*--------------------------------------
Assembly kit for Action games
© 2023 SigmoidButton
----------------------------------------
-------------------
Description
-------------------
Recommended Render Pipeline : URP
Each of the models have the number of Material is 1.
Each of the models have the number of Textures is 0~5.
Texture size 1024 x 1024 pixels.
Texture type : BaseColor, Metallic, Smoothness, Occlusion, Normal, Emission, Mask

-------------------
About Shader
-------------------
Shader file requires "Shader Graph".
Would you please install "Shader Graph" from Package Manager.

Would you please set the appropriate texture on item of texture in surface inputs of the Shader file.

-------------------
About Scripts
-------------------
Scripts in the package is for "TestStage" scene only.
It is not recommended for any other use.

- Portal Script
Destination : This is value for moving the player character.

- Switch Button Script
Button Color : The color of the buttons can be changed.
Mesh : This value is required for color change.
Off Delay Time : This value is the time it takes to turn off.

- Switch Button Script
Lever Color : The color of the levers can be changed.
Mesh : This value is required for color change.
Off Delay Time : This value is the time it takes to turn off.
Off Delay Time Flg : Checking this will automatically turn off the lever.If not checked, it will remain on.

- Tile Move Script
Moving_Speed : This value is speed to move a character.
Moving_Time : This value is time to move a character.

- Tile Needle Script
Off Delay Time : This value is the time it takes to turn off.

- Treasure Script
Item : The item available from treasure chests.

- Wall Script
Wall Color : The color of the walls can be changed.
Button : Specify a button to be linked to this object.
Open Delay time : Specifies when this object will operate.

-------------------
Polygon
-------------------
- Character
Angel 18320 triangles
Devil 16004 triangles
Ghost 6096 triangles
Reaper 11938 triangles
Dragon 10216 triangles
Minotaur 11032 triangles
Angel_lowpoly 2974 triangles
Devil_lowpoly 2966 triangles
Ghost_lowpoly 2116 triangles
Reaper_lowpoly 2378 triangles
Dragon_lowpoly 2992 triangles
Minotaur_lowpoly 2994 triangles

- Effects
EffectSphere 224 triangles
EffectSquare 8 triangles
Exclamation 264 triangles
Exclamation_lowpoly 32 triangles
PortalCylinder 64 triangles
Question 840 triangles
Question_lowpoly 236 triangles
Smoke 156 triangles

- Blocks
Block1 44 triangles
Block2 108 triangles
Block_Stairs 44 triangles
BlockBox 108 triangles
StairsDoor 1952 triangles

- Decorations
Cactus 2040 triangles
Cactus_lowpoly 930 triangles
Iceberg 220 triangles
Pillar00 3960 triangles
Pillar00_lowpoly 980 triangles
Pillar01 2804 triangles
Pillar01_lowpoly 418 triangles
Pillar10 2816 triangles
Pillar10_lowpoly 406 triangles
Pillar11 2624 triangles
Pillar11_lowpoly 644 triangles
Pillar20 44 triangles
Rock 219 triangles
SkullObj 2580 triangles
Torch 176 triangles
TorchPalace 140 triangles
Tree 5900 triangles
Tree_lowpoly 1416 triangles
TreeDry 5852 triangles
TreeDry_lowpoly 1382 triangles

- Gimmicks
Lock 2838 triangles
Lock_lowpoly 1054 triangles
LockBreak 3402 triangles
SwitchButton 738 triangles
SwitchButton_lowpoly 266 triangles
SwitchLever 718 triangles
SwitchLever_lowpoly 242 triangles
TileMove 36 triangles
TileNeedle 990 triangles
TileNeedle_lowpoly 290 triangles
Treasure 4864 triangles
Treasure_lowpoly 1204 triangles
Wall 110 triangles

- Items
Apple 291 triangles
Bell 746 triangles
Bomb 486 triangles
Bone 224 triangles
Book 132 triangles
Coffee 826 triangles
Coin 764 triangles
Crown 702 triangles
Fire 220 triangles
Glasses 404 triangles
Heart 400 triangles
Ice 1800 triangles
Key 1080 triangles
Leaf 88 triangles
Lightning 166 triangles
Meat 528 triangles
PaperScroll 159 triangles
Ring 512 triangles
Shield 112 triangles
Shoes 518 triangles
Star 100 triangles
Stone 160 triangles
Switch 266 triangles
Sword 122 triangles
Watch 1700 triangles

-------------------
Animation
-------------------
- Character
attack1
attack1_shift
attack2
attack2_shift
attack3
attack3_shift
dissolve
idle
run
surprised

- Effects
Clear
exclamation
question

- Items
Key_get

- LockBreak
lock_break

- StairsDoor
StairsDoor_close
StairsDoor_default
StairsDoor_open

- SwitchButton
SwitchButton_default
SwitchButton_off
SwitchButton_on

- SwitchLever
SwitchLever_default
SwitchLever_off_l
SwitchLever_off_r
SwitchLever_on_l
SwitchLever_on_r

- TileNeedle
TileNeedle_default
TileNeedle_stab

- Treasure
Treasure_idle
Treasure_open

- Wall
Wall_default
Wall_open

----------------------------------------*/
