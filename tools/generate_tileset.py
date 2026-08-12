#!/usr/bin/env python3
"""
「烟柳镇」像素瓦片地图 Tileset 生成器
16x16 tile, 32 columns x 16 rows = 512 x 256
"""
from PIL import Image, ImageDraw
import os

P = {
    'g1':(65,110,55),'g2':(80,130,68),'g3':(95,148,82),'g4':(110,165,95),
    'd1':(120,95,65),'d2':(140,115,80),'d3':(160,135,98),'d4':(100,80,55),
    'st1':(140,140,145),'st2':(165,165,170),'st3':(190,190,195),'st4':(110,110,118),
    'w1':(55,95,130),'w2':(70,115,150),'w3':(90,138,170),'w4':(110,158,188),
    'w5':(130,175,205),'w6':(45,78,110),
    'ww1':(220,218,212),'ww2':(235,233,228),'ww3':(248,246,242),
    'ww4':(200,198,192),'ww5':(180,178,172),
    'wd1':(130,85,45),'wd2':(155,105,60),'wd3':(175,125,78),'wd4':(100,65,32),'wd5':(190,140,90),
    'tf1':(70,80,90),'tf2':(85,98,110),'tf3':(100,115,130),'tf4':(55,65,75),'tf5':(115,130,148),
    'r1':(180,45,35),'r2':(210,65,50),'r3':(235,90,70),'r4':(150,30,22),
    'gd1':(200,170,80),'gd2':(225,195,100),'gd3':(240,215,130),
    'lw1':(50,95,40),'lw2':(65,120,52),'lw3':(80,145,65),'lw4':(95,168,78),
    'lk1':(90,65,35),'lk2':(110,80,45),
    'ht1':(235,170,185),'ht2':(245,195,208),'ht3':(255,220,230),
    'hl1':(50,120,55),'hl2':(65,140,70),'hl3':(80,158,82),
    'sk1':(160,185,210),'sk2':(180,200,220),
}
def px(d,x,y,n):
    if n in P: d.point((int(x),int(y)),fill=P[n])
def box(d,x1,y1,x2,y2,n):
    if n in P: d.rectangle([int(x1),int(y1),int(x2),int(y2)],fill=P[n])
def hr(d,y,x1,x2,n):
    if n in P:
        for x in range(int(x1),int(x2)+1): d.point((x,int(y)),fill=P[n])
def vr(d,x,y1,y2,n):
    if n in P:
        for y in range(int(y1),int(y2)+1): d.point((int(x),y),fill=P[n])
T=16

def tile_grass(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g2')
    for x,y,c in [(3,2,'g1'),(8,5,'g3'),(12,3,'g1'),(5,9,'g3'),(1,13,'g1'),(10,10,'g3'),(14,7,'g1'),(7,12,'g3')]: px(d,ox+x,oy+y,c)
    px(d,ox+4,oy+0,'g3'); px(d,ox+4,oy+1,'g4'); px(d,ox+12,oy+7,'g3'); px(d,ox+12,oy+8,'g4')
def tile_grass_dark(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g1')
    for x,y,c in [(2,3,'g2'),(9,6,'g1'),(13,2,'g2'),(6,11,'g1')]: px(d,ox+x,oy+y,c)
def tile_dirt(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'d2')
    for x,y,c in [(2,2,'d1'),(7,5,'d3'),(12,1,'d1'),(4,10,'d3'),(14,8,'d1'),(1,14,'d4')]: px(d,ox+x,oy+y,c)
def tile_stone_path(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'st1')
    hr(d,oy+7,ox,ox+15,'st4'); vr(d,ox+7,oy,oy+6,'st4')
    vr(d,ox+3,oy+8,oy+15,'st4'); vr(d,ox+11,oy+8,oy+15,'st4')
    px(d,ox+2,oy+2,'st3'); px(d,ox+10,oy+3,'st2'); px(d,ox+5,oy+10,'st3'); px(d,ox+13,oy+12,'st2')
def tile_water(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+1,ox+6,'w3'); hr(d,oy+3,ox+10,ox+14,'w4')
    hr(d,oy+8,ox+3,ox+9,'w4'); hr(d,oy+8,ox+12,ox+14,'w3')
    hr(d,oy+13,ox+0,ox+5,'w3'); hr(d,oy+13,ox+8,ox+13,'w4')
    px(d,ox+7,oy+1,'w1'); px(d,ox+14,oy+6,'w1'); px(d,ox+2,oy+11,'w1')
def tile_water_deep(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w1')
    hr(d,oy+4,ox+2,ox+8,'w2'); hr(d,oy+10,ox+5,ox+12,'w2')
    px(d,ox+0,oy+7,'w6'); px(d,ox+8,oy+1,'w6')
def tile_water_sparkle(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+1,ox+7,'w3'); hr(d,oy+9,ox+4,ox+11,'w4'); hr(d,oy+14,ox+0,ox+6,'w3')
    px(d,ox+5,oy+3,'w5'); px(d,ox+12,oy+8,'w5'); px(d,ox+3,oy+13,'w5'); px(d,ox+9,oy+5,'w5')

# Shore
def tile_shore_top(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+7,'w2'); hr(d,oy+3,ox+1,ox+6,'w3')
    box(d,ox,oy+8,ox+15,oy+15,'g2'); hr(d,oy+7,ox,ox+15,'g3')
    px(d,ox+2,oy+7,'g4'); px(d,ox+7,oy+7,'g1'); px(d,ox+12,oy+7,'g4')
def tile_shore_bottom(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+7,'g2'); box(d,ox,oy+8,ox+15,oy+15,'w2')
    hr(d,oy+8,ox,ox+15,'w3'); hr(d,oy+12,ox+2,ox+8,'w3')
    px(d,ox+4,oy+8,'g3'); px(d,ox+10,oy+8,'g1')
def tile_shore_left(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+7,oy+15,'w2'); box(d,ox+8,oy,ox+15,oy+15,'g2')
    vr(d,ox+7,oy,oy+15,'w3'); px(d,ox+8,oy+3,'g3'); px(d,ox+8,oy+10,'g1')
def tile_shore_right(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+7,oy+15,'g2'); box(d,ox+8,oy,ox+15,oy+15,'w2')
    vr(d,ox+8,oy,oy+15,'g3'); px(d,ox+8,oy+5,'w3'); px(d,ox+8,oy+12,'w3')
def tile_shore_corner_tl(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g2'); box(d,ox,oy,ox+7,oy+7,'w2')
    px(d,ox+7,oy+7,'w3'); px(d,ox+8,oy+7,'g3'); px(d,ox+7,oy+8,'g3')
def tile_shore_corner_tr(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g2'); box(d,ox+8,oy,ox+15,oy+7,'w2')
    px(d,ox+8,oy+7,'g3'); px(d,ox+7,oy+7,'w3')
def tile_shore_corner_bl(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g2'); box(d,ox,oy+8,ox+7,oy+15,'w2')
    px(d,ox+7,oy+8,'w3'); px(d,ox+8,oy+8,'g3')
def tile_shore_corner_br(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'g2'); box(d,ox+8,oy+8,ox+15,oy+15,'w2')
    px(d,ox+8,oy+8,'g3'); px(d,ox+7,oy+8,'w3')

# Walls
def tile_wall_white(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'ww2')
    hr(d,oy+7,ox,ox+15,'ww4'); hr(d,oy,ox,ox+15,'ww3'); hr(d,oy+15,ox,ox+15,'ww5')
def tile_wall_white_base(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+11,'ww2'); box(d,ox,oy+12,ox+15,oy+15,'st1')
    hr(d,oy+7,ox,ox+15,'ww4'); hr(d,oy+12,ox,ox+15,'st4')
    px(d,ox+3,oy+13,'st2'); px(d,ox+10,oy+14,'st2')
def tile_wall_wood(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'wd2')
    for y in [3,7,11,15]: hr(d,oy+y,ox,ox+15,'wd1' if y<15 else 'wd4')
    px(d,ox+4,oy+2,'wd3'); px(d,ox+8,oy+5,'wd3'); px(d,ox+12,oy+9,'wd3')
def tile_window(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'ww2'); hr(d,oy+7,ox,ox+15,'ww4')
    box(d,ox+3,oy+2,ox+12,oy+13,'wd4')
    box(d,ox+4,oy+3,ox+7,oy+6,'ww3'); box(d,ox+8,oy+3,ox+11,oy+6,'ww3')
    box(d,ox+4,oy+8,ox+7,oy+12,'ww2'); box(d,ox+8,oy+8,ox+11,oy+12,'ww2')
    vr(d,ox+7,oy+3,oy+12,'wd4'); hr(d,oy+7,ox+4,ox+11,'wd4')
def tile_door(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'ww2')
    box(d,ox+3,oy+1,ox+12,oy+14,'wd2')
    vr(d,ox+3,oy+1,oy+14,'wd4'); vr(d,ox+12,oy+1,oy+14,'wd4'); hr(d,oy+1,ox+3,ox+12,'wd4')
    vr(d,ox+7,oy+2,oy+13,'wd1')
    px(d,ox+6,oy+8,'gd1'); px(d,ox+9,oy+8,'gd1'); px(d,ox+6,oy+9,'gd2'); px(d,ox+9,oy+9,'gd2')
    box(d,ox+2,oy+14,ox+13,oy+15,'st2'); hr(d,oy+14,ox+2,ox+13,'st3')
def tile_door_open(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'ww2')
    box(d,ox+3,oy+1,ox+12,oy+15,(30,25,20))
    vr(d,ox+3,oy+1,oy+15,'wd4'); vr(d,ox+12,oy+1,oy+15,'wd4'); hr(d,oy+1,ox+3,ox+12,'wd4')

# Roof
def tile_roof(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'tf2')
    for row in range(4):
        y=oy+row*4; hr(d,y,ox,ox+15,'tf1'); hr(d,y+1,ox+1,ox+15,'tf3')
        for col in range(4): px(d,ox+col*4+2,y+2,'tf4'); px(d,ox+col*4+3,y+2,'tf4')
    px(d,ox+5,oy+3,'tf5'); px(d,ox+13,oy+7,'tf5')
def tile_roof_eave(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+10,'tf2')
    for row in range(3):
        y=oy+row*4; hr(d,y,ox,ox+15,'tf1'); hr(d,y+1,ox+1,ox+15,'tf3')
    box(d,ox,oy+11,ox+15,oy+13,'tf1'); hr(d,oy+11,ox,ox+15,'tf3')
    for col in range(4): px(d,ox+col*4+1,oy+14,'tf4'); px(d,ox+col*4+2,oy+14,'tf4')
def tile_roof_edge_l(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox+3,oy,ox+15,oy+15,'tf2'); box(d,ox,oy,ox+2,oy+15,'tf4')
    for row in range(4): y=oy+row*4; hr(d,y,ox+3,ox+15,'tf1'); hr(d,y+1,ox+4,ox+15,'tf3')
    vr(d,ox+3,oy,oy+15,'tf3')
def tile_roof_edge_r(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+12,oy+15,'tf2'); box(d,ox+13,oy,ox+15,oy+15,'tf4')
    for row in range(4): y=oy+row*4; hr(d,y,ox,ox+12,'tf1'); hr(d,y+1,ox,ox+11,'tf3')
    vr(d,ox+12,oy,oy+15,'tf3')
def tile_roof_ridge(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+6,'tf3'); box(d,ox,oy+7,ox+15,oy+15,'tf2')
    hr(d,oy,ox,ox+15,'tf5'); hr(d,oy+1,ox,ox+15,'tf3')
    for row in range(2): y=oy+8+row*4; hr(d,y,ox,ox+15,'tf1'); hr(d,y+1,ox+1,ox+15,'tf3')

# Decorations
def tile_willow(d,tx,ty):
    ox,oy=tx*T,ty*T
    box(d,ox+6,oy+6,ox+9,oy+15,'lk1'); px(d,ox+7,oy+5,'lk2'); px(d,ox+8,oy+5,'lk2')
    for x,y,c in [(4,1,'lw2'),(5,0,'lw3'),(6,0,'lw2'),(7,0,'lw3'),(8,0,'lw2'),(9,0,'lw3'),(10,0,'lw2'),(11,1,'lw3'),
                   (3,2,'lw1'),(4,2,'lw3'),(5,2,'lw2'),(6,2,'lw3'),(7,2,'lw2'),(8,2,'lw3'),(9,2,'lw2'),(10,2,'lw3'),(11,2,'lw2'),(12,2,'lw1'),
                   (3,3,'lw2'),(4,3,'lw3'),(5,3,'lw1'),(6,3,'lw2'),(7,3,'lw4'),(8,3,'lw3'),(9,3,'lw1'),(10,3,'lw3'),(11,3,'lw2'),
                   (5,5,'lw2'),(6,5,'lw1'),(7,5,'lw3'),(8,5,'lw2'),(9,5,'lw1')]:
        px(d,ox+x,oy+y,c)
    for x in [3,5,8,10,12]:
        for y in range(5,15):
            if y%2==0: px(d,ox+x,oy+y,'lw3')
            elif y%3==0: px(d,ox+x,oy+y,'lw4')
def tile_lotus(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+1,ox+7,'w3'); hr(d,oy+11,ox+4,ox+12,'w3')
    for x,y,c in [(1,6,'hl1'),(2,5,'hl2'),(3,5,'hl3'),(4,5,'hl2'),(5,6,'hl1'),
                   (9,8,'hl1'),(10,7,'hl2'),(11,7,'hl3'),(12,7,'hl2'),(13,8,'hl1')]: px(d,ox+x,oy+y,c)
    px(d,ox+7,oy+3,'ht2'); px(d,ox+8,oy+3,'ht1')
    px(d,ox+6,oy+4,'ht3'); px(d,ox+7,oy+4,'ht1'); px(d,ox+8,oy+4,'ht2')
    px(d,ox+7,oy+5,'ht1'); px(d,ox+7,oy+6,'hl1')
def tile_lantern(d,tx,ty):
    ox,oy=tx*T,ty*T
    px(d,ox+7,oy,'wd4'); px(d,ox+7,oy+1,'wd4')
    box(d,ox+5,oy+2,ox+10,oy+3,'gd1'); hr(d,oy+2,ox+5,ox+10,'gd2')
    box(d,ox+4,oy+4,ox+11,oy+10,'r2')
    hr(d,oy+4,ox+4,ox+11,'r3'); hr(d,oy+10,ox+4,ox+11,'r1')
    vr(d,ox+5,oy+4,oy+10,'r1'); vr(d,ox+7,oy+4,oy+10,'r3'); vr(d,ox+9,oy+4,oy+10,'r3'); vr(d,ox+11,oy+4,oy+10,'r1')
    box(d,ox+5,oy+11,ox+10,oy+12,'gd1'); hr(d,oy+11,ox+5,ox+10,'gd2')
    px(d,ox+7,oy+13,'r1'); px(d,ox+8,oy+13,'r1'); px(d,ox+7,oy+14,'r4'); px(d,ox+8,oy+14,'r4')
def tile_flag(d,tx,ty):
    ox,oy=tx*T,ty*T
    vr(d,ox+2,oy,oy+15,'wd4'); px(d,ox+2,oy,'wd5')
    box(d,ox+3,oy+2,ox+14,oy+9,'ww2'); hr(d,oy+2,ox+3,ox+14,'ww3'); hr(d,oy+9,ox+3,ox+14,'ww4')
    px(d,ox+7,oy+4,'r2'); px(d,ox+8,oy+4,'r2'); px(d,ox+7,oy+5,'r1'); px(d,ox+8,oy+5,'r1')
def tile_barrel(d,tx,ty):
    ox,oy=tx*T,ty*T
    box(d,ox+3,oy+4,ox+12,oy+13,'wd2'); hr(d,oy+5,ox+3,ox+12,'st4'); hr(d,oy+11,ox+3,ox+12,'st4')
    box(d,ox+4,oy+3,ox+11,oy+4,'wd3'); hr(d,oy+3,ox+4,ox+11,'wd5'); hr(d,oy+13,ox+3,ox+12,'wd4')
def tile_crate(d,tx,ty):
    ox,oy=tx*T,ty*T
    box(d,ox+2,oy+3,ox+13,oy+14,'wd2')
    hr(d,oy+3,ox+2,ox+13,'wd3'); hr(d,oy+14,ox+2,ox+13,'wd4')
    vr(d,ox+2,oy+3,oy+14,'wd4'); vr(d,ox+13,oy+3,oy+14,'wd4')
    for i in range(8): px(d,ox+4+i,oy+5+i,'wd1'); px(d,ox+11-i,oy+5+i,'wd1')

# Bridge
def tile_bridge(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'st2')
    hr(d,oy+7,ox,ox+15,'st4'); vr(d,ox+5,oy,oy+6,'st4'); vr(d,ox+10,oy,oy+6,'st4')
    vr(d,ox+3,oy+8,oy+15,'st4'); vr(d,ox+8,oy+8,oy+15,'st4'); vr(d,ox+13,oy+8,oy+15,'st4')
    px(d,ox+2,oy+2,'st3'); px(d,ox+8,oy+3,'st3'); px(d,ox+5,oy+10,'st3')
def tile_bridge_rail(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'st2')
    box(d,ox+1,oy+1,ox+3,oy+10,'st1'); box(d,ox+12,oy+1,ox+14,oy+10,'st1')
    box(d,ox+1,oy+1,ox+14,oy+2,'st3'); box(d,ox+1,oy+5,ox+14,oy+6,'st3')
    px(d,ox+2,oy,'st3'); px(d,ox+13,oy,'st3')

# Boat
def tile_boat(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+1,ox+7,'w3'); hr(d,oy+11,ox+4,ox+12,'w3')
    # 船身
    for x,y,c in [(2,8,'wd4'),(3,7,'wd2'),(4,7,'wd3'),(5,7,'wd2'),(6,7,'wd3'),(7,7,'wd2'),(8,7,'wd3'),
                   (9,7,'wd2'),(10,7,'wd3'),(11,7,'wd2'),(12,7,'wd3'),(13,7,'wd2'),
                   (3,8,'wd1'),(4,8,'wd2'),(5,8,'wd3'),(6,8,'wd2'),(7,8,'wd3'),(8,8,'wd2'),
                   (9,8,'wd3'),(10,8,'wd2'),(11,8,'wd3'),(12,8,'wd2'),
                   (4,9,'wd4'),(5,9,'wd1'),(6,9,'wd2'),(7,9,'wd1'),(8,9,'wd2'),
                   (9,9,'wd1'),(10,9,'wd2'),(11,9,'wd1')]:
        px(d,ox+x,oy+y,c)
    # 船头翘起
    px(d,ox+2,oy+7,'wd4'); px(d,ox+14,oy+7,'wd4')
    # 船篷
    for x,y,c in [(5,4,'wd4'),(6,4,'ww2'),(7,4,'ww3'),(8,4,'ww2'),(9,4,'ww3'),(10,4,'wd4'),
                   (5,5,'wd4'),(6,5,'ww3'),(7,5,'ww2'),(8,5,'ww3'),(9,5,'ww2'),(10,5,'wd4'),
                   (5,6,'wd4'),(6,6,'wd4'),(7,6,'wd4'),(8,6,'wd4'),(9,6,'wd4'),(10,6,'wd4')]:
        px(d,ox+x,oy+y,c)
def tile_dock(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+1,ox+6,'w3'); hr(d,oy+11,ox+5,ox+12,'w3')
    # 码头木板
    box(d,ox,oy+2,ox+11,oy+13,'wd2')
    hr(d,oy+5,ox,ox+11,'wd1'); hr(d,oy+8,ox,ox+11,'wd1'); hr(d,oy+11,ox,ox+11,'wd1')
    # 木桩
    px(d,ox+1,oy+14,'wd4'); px(d,ox+10,oy+14,'wd4')
    hr(d,oy+2,ox,ox+11,'wd3'); hr(d,oy+13,ox,ox+11,'wd4')

# Step stones
def tile_step_stone(d,tx,ty):
    ox,oy=tx*T,ty*T; box(d,ox,oy,ox+15,oy+15,'w2')
    hr(d,oy+3,ox+2,ox+8,'w3'); hr(d,oy+11,ox+5,ox+12,'w3')
    px(d,ox+5,oy+6,'st2'); px(d,ox+6,oy+6,'st1'); px(d,ox+7,oy+6,'st2')
    px(d,ox+5,oy+7,'st1'); px(d,ox+6,oy+7,'st2'); px(d,ox+7,oy+7,'st1')
    px(d,ox+10,oy+9,'st2'); px(d,ox+11,oy+9,'st1'); px(d,ox+12,oy+9,'st2')
    px(d,ox+10,oy+10,'st1'); px(d,ox+11,oy+10,'st2'); px(d,ox+12,oy+10,'st1')
    px(d,ox+2,oy+12,'st2'); px(d,ox+3,oy+12,'st1')
    px(d,ox+2,oy+13,'st1'); px(d,ox+3,oy+13,'st2')

def main():
    base = os.path.dirname(os.path.dirname(__file__))
    out = os.path.join(base, 'assets', 'tilesets')
    os.makedirs(out, exist_ok=True)

    COLS, ROWS = 32, 16
    img = Image.new('RGBA', (COLS*T, ROWS*T), (0,0,0,0))
    d = ImageDraw.Draw(img)

    print("🏘️  烟柳镇 — 像素瓦片地图 Tileset")
    print(f"   尺寸: {COLS*T}x{ROWS*T} ({COLS}x{ROWS} tiles, {T}x{T}px each)")

    # Row 0: 地形 (grass variants)
    tiles_r0 = [tile_grass]*6 + [tile_grass_dark]*4 + [tile_dirt]*4 + [tile_stone_path]*4 + [tile_grass]*14
    for i, fn in enumerate(tiles_r0[:COLS]):
        fn(d, i, 0)

    # Row 1: 水面
    tiles_r1 = [tile_water]*8 + [tile_water_deep]*4 + [tile_water_sparkle]*4 + [tile_water]*16
    for i, fn in enumerate(tiles_r1[:COLS]):
        fn(d, i, 1)

    # Row 2: 水岸过渡
    tiles_r2 = [tile_shore_top]*4 + [tile_shore_bottom]*4 + [tile_shore_left]*4 + [tile_shore_right]*4 + \
               [tile_shore_corner_tl, tile_shore_corner_tr, tile_shore_corner_bl, tile_shore_corner_br] + \
               [tile_shore_top]*16
    for i, fn in enumerate(tiles_r2[:COLS]):
        fn(d, i, 2)

    # Row 3: 更多岸过渡
    for i in range(COLS):
        tile_shore_top(d, i, 3) if i < 8 else tile_shore_bottom(d, i, 3) if i < 16 else \
        tile_shore_left(d, i, 3) if i < 24 else tile_shore_right(d, i, 3)

    # Row 4: 墙壁
    tiles_r4 = [tile_wall_white]*8 + [tile_wall_white_base]*4 + [tile_wall_wood]*4 + \
               [tile_window]*4 + [tile_door, tile_door_open] + [tile_wall_white]*10
    for i, fn in enumerate(tiles_r4[:COLS]):
        fn(d, i, 4)

    # Row 5: 更多墙
    tiles_r5 = [tile_wall_white]*8 + [tile_wall_wood]*4 + [tile_window]*4 + [tile_door]*4 + [tile_door_open]*4 + [tile_wall_white]*8
    for i, fn in enumerate(tiles_r5[:COLS]):
        fn(d, i, 5)

    # Row 6: 屋顶
    tiles_r6 = [tile_roof]*8 + [tile_roof_eave]*4 + [tile_roof_edge_l]*4 + [tile_roof_edge_r]*4 + \
               [tile_roof_ridge]*4 + [tile_roof]*4 + [tile_roof_eave]*4
    for i, fn in enumerate(tiles_r6[:COLS]):
        fn(d, i, 6)

    # Row 7: 更多屋顶
    tiles_r7 = [tile_roof]*6 + [tile_roof_eave]*6 + [tile_roof_ridge]*4 + [tile_roof]*4 + \
               [tile_roof_edge_l]*4 + [tile_roof_edge_r]*4 + [tile_roof_eave]*4
    for i, fn in enumerate(tiles_r7[:COLS]):
        fn(d, i, 7)

    # Row 8: 装饰
    tiles_r8 = [tile_willow]*4 + [tile_lotus]*4 + [tile_lantern]*4 + [tile_flag]*4 + \
               [tile_barrel]*4 + [tile_crate]*4 + [tile_willow]*4 + [tile_lotus]*4
    for i, fn in enumerate(tiles_r8[:COLS]):
        fn(d, i, 8)

    # Row 9: 更多装饰
    tiles_r9 = [tile_lantern]*4 + [tile_willow]*4 + [tile_lotus]*4 + [tile_flag]*4 + \
               [tile_barrel]*4 + [tile_crate]*4 + [tile_willow]*8
    for i, fn in enumerate(tiles_r9[:COLS]):
        fn(d, i, 9)

    # Row 10: 桥梁
    tiles_r10 = [tile_bridge]*8 + [tile_bridge_rail]*4 + [tile_bridge]*4 + \
                [tile_step_stone]*4 + [tile_bridge]*12
    for i, fn in enumerate(tiles_r10[:COLS]):
        fn(d, i, 10)

    # Row 11: 更多桥
    tiles_r11 = [tile_bridge_rail]*4 + [tile_bridge]*8 + [tile_step_stone]*4 + [tile_bridge]*16
    for i, fn in enumerate(tiles_r11[:COLS]):
        fn(d, i, 11)

    # Row 12: 船和码头
    tiles_r12 = [tile_boat]*6 + [tile_dock]*4 + [tile_boat]*4 + [tile_dock]*4 + [tile_boat]*6 + [tile_dock]*4 + [tile_boat]*4
    for i, fn in enumerate(tiles_r12[:COLS]):
        fn(d, i, 12)

    # Row 13: 更多船
    tiles_r13 = [tile_dock]*4 + [tile_boat]*6 + [tile_dock]*4 + [tile_boat]*6 + [tile_dock]*4 + [tile_boat]*8
    for i, fn in enumerate(tiles_r13[:COLS]):
        fn(d, i, 13)

    # Row 14-15: 留空（碰撞/透明占位）
    # 保持透明

    # 保存
    path = os.path.join(out, 'yanliu_town_tileset.png')
    img.save(path)
    print(f"   ✓ 保存: {path}")
    print(f"   文件大小: {os.path.getsize(path)} bytes")

    # 放大预览
    for scale in [2, 4, 8]:
        big = img.resize((img.width*scale, img.height*scale), Image.NEAREST)
        p = os.path.join(out, f'yanliu_town_tileset_{scale}x.png')
        big.save(p)
        print(f"   ✓ 预览 {scale}x: {p}")

    # 生成瓦片索引参考
    ref_path = os.path.join(out, 'tileset_reference.md')
    with open(ref_path, 'w') as f:
        f.write("# 烟柳镇瓦片地图索引\n\n")
        f.write(f"尺寸: {COLS}x{ROWS} tiles, {T}x{T}px each, 总计 {COLS*T}x{ROWS*T}px\n\n")
        rows_desc = [
            ("Row 0", "地形 — 草地/深草/泥土/石板路"),
            ("Row 1", "水面 — 浅水/深水/闪光"),
            ("Row 2", "水岸 — 上/下/左/右过渡 + 四角"),
            ("Row 3", "水岸 — 更多过渡变体"),
            ("Row 4", "墙壁 — 白墙/石基白墙/木墙/窗/门"),
            ("Row 5", "墙壁 — 更多变体"),
            ("Row 6", "屋顶 — 青瓦/飞檐/边缘/屋脊"),
            ("Row 7", "屋顶 — 更多变体"),
            ("Row 8", "装饰 — 柳树/荷花/灯笼/旗帜/桶/箱"),
            ("Row 9", "装饰 — 更多变体"),
            ("Row 10", "桥梁 — 石桥/栏杆/踏步石"),
            ("Row 11", "桥梁 — 更多变体"),
            ("Row 12", "船只 — 小船/码头"),
            ("Row 13", "船只 — 更多变体"),
            ("Row 14", "保留 — 碰撞/透明"),
            ("Row 15", "保留 — 碰撞/透明"),
        ]
        for row, desc in rows_desc:
            f.write(f"- **{row}**: {desc}\n")
        f.write(f"\n## 调色板\n\n")
        f.write(f"- 草地: 4色 ({P['g1']}→{P['g4']})\n")
        f.write(f"- 水面: 6色 ({P['w6']}→{P['w5']})\n")
        f.write(f"- 白墙: 5色 ({P['ww5']}→{P['ww3']})\n")
        f.write(f"- 木色: 5色 ({P['wd4']}→{P['wd5']})\n")
        f.write(f"- 青瓦: 5色 ({P['tf4']}→{P['tf5']})\n")
        f.write(f"- 柳树: 6色 (树冠4色 + 枝干2色)\n")
        f.write(f"- 荷花: 6色 (花3色 + 叶3色)\n")
    print(f"   ✓ 索引: {ref_path}")

    print("\n✅ 烟柳镇瓦片地图生成完成！")
    print(f"   Tileset: {path}")
    print(f"   瓦片总数: {COLS*ROWS} ({COLS}x{ROWS})")
    print(f"   有效瓦片: ~{COLS*14} (Row 0-13)")
    print(f"   瓦片类型: 草地/水/岸/墙/窗/门/屋顶/柳树/荷花/灯笼/旗帜/桥/船/码头")

if __name__ == '__main__':
    main()
