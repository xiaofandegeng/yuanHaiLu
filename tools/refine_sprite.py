#!/usr/bin/env python3
"""
剑客「凌霜」精修版精灵表 — v2
32色武侠调色板 + 高精度像素细节
"""
from PIL import Image, ImageDraw
import os, sys

# 32色精修调色板
P = {
    'h1':(20,18,28),'h2':(35,30,42),'h3':(52,45,60),'h4':(72,62,82),'h5':(95,82,110),
    's1':(215,175,140),'s2':(230,190,155),'s3':(242,205,168),'s4':(252,218,182),'s5':(255,230,200),
    'c1':(45,52,65),'c2':(58,68,82),'c3':(75,88,105),'c4':(95,112,132),'c5':(115,135,158),'c6':(130,150,172),
    'i1':(200,200,210),'i2':(218,218,225),'i3':(235,235,240),
    'b1':(100,35,28),'b2':(140,50,40),'b3':(170,65,50),'b4':(195,85,60),
    'p1':(40,40,50),'p2':(55,55,65),'p3':(72,72,82),
    'bt1':(35,28,22),'bt2':(52,40,32),'bt3':(70,55,42),
    'sw1':(160,168,178),'sw2':(185,192,200),'sw3':(210,216,222),'sw4':(235,238,242),'sw5':(250,250,255),
    'g1':(140,115,55),'g2':(175,148,72),'g3':(200,175,95),
    'hi1':(80,55,35),'hi2':(105,75,48),'hi3':(130,95,60),
    't1':(120,28,22),'t2':(160,42,32),'t3':(195,60,45),
    'ed':(18,18,24),'ew':(248,248,248),'eh':(255,255,255),
    'mo':(200,155,130),
}

def px(d,x,y,n):
    if n and n in P: d.point((int(x),int(y)),fill=P[n])

def hr(d,y,x1,x2,n):
    if n in P:
        for x in range(int(x1),int(x2)+1): d.point((x,int(y)),fill=P[n])

def vr(d,x,y1,y2,n):
    if n in P:
        for y in range(int(y1),int(y2)+1): d.point((int(x),y),fill=P[n])

def box(d,x1,y1,x2,y2,n):
    if n in P: d.rectangle([int(x1),int(y1),int(x2),int(y2)],fill=P[n])

def draw_down(img, f):
    d=ImageDraw.Draw(img)
    cx=24; bob=[0,-1,0,-1][f]; al=[0,2,0,-2][f]; ar=[0,-2,0,2][f]
    ll=[0,1,0,-1][f]; lr=[0,-1,0,1][f]; hsw=[0,1,0,-1][f]; tsw=[0,2,0,-2][f]
    # 发髻
    box(d,cx-4,3,cx+3,5,'h1'); box(d,cx-3,2,cx+2,3,'h2'); box(d,cx-2,3,cx+1,4,'h4')
    px(d,cx-5,4,'g3'); hr(d,4,cx-4,cx+4,'g2'); px(d,cx+5,4,'g3')
    # 头两侧
    box(d,cx-6,7,cx-5,14,'h1'); box(d,cx-6,7,cx-5,10,'h3'); px(d,cx-6,8,'h4')
    box(d,cx+5,7,cx+6,14,'h1'); box(d,cx+5,7,cx+6,10,'h3'); px(d,cx+6,8,'h4')
    box(d,cx-5,6,cx+5,7,'h1'); hr(d,6,cx-4,cx+4,'h2'); px(d,cx-2,6,'h4')
    # 刘海
    hr(d,8,cx-5,cx+5,'h2'); hr(d,9,cx-4,cx+4,'h3')
    px(d,cx-3,8,'h4'); px(d,cx-1,8,'h5'); px(d,cx+2,8,'h4'); px(d,cx-4,9,'h4'); px(d,cx+3,9,'h4')
    # 马尾
    mx=cx+5+hsw
    for y in range(6,20+bob):
        px(d,mx,y,'h2' if y<12 else 'h3')
        if y<14: px(d,mx+1,y,'h4')
    px(d,mx,18+bob,'h4'); px(d,mx+1,19+bob,'h5')
    # 脸
    box(d,cx-4,10,cx+4,15,'s3'); hr(d,15,cx-3,cx+3,'s2'); px(d,cx-4,15,'s1'); px(d,cx+4,15,'s1')
    px(d,cx-3,12,'s4'); px(d,cx+3,12,'s4'); px(d,cx,11,'s5')
    # 眉
    px(d,cx-3,10,'h2'); px(d,cx-2,10,'h1'); px(d,cx+2,10,'h1'); px(d,cx+3,10,'h2')
    # 眼
    px(d,cx-3,11,'eh'); px(d,cx-2,11,'ed'); px(d,cx+2,11,'ed'); px(d,cx+3,11,'eh')
    # 嘴
    px(d,cx-1,14,'mo'); px(d,cx,14,'mo'); px(d,cx+1,14,'s2')
    # 身体
    by=16+bob
    box(d,cx-7,by,cx+7,by+12,'c3')
    vr(d,cx-7,by,by+12,'c1'); vr(d,cx+7,by,by+12,'c1')
    box(d,cx-6,by,cx-5,by+12,'c2'); box(d,cx+5,by,cx+6,by+12,'c2')
    box(d,cx-2,by+1,cx+2,by+6,'c4'); px(d,cx,by+2,'c5')
    # V领
    px(d,cx-1,by,'i2'); px(d,cx,by,'i3'); px(d,cx+1,by,'i2'); px(d,cx,by+1,'i2')
    px(d,cx-2,by,'c2'); px(d,cx+2,by,'c2')
    # 褶皱
    px(d,cx-4,by+4,'c1'); px(d,cx-3,by+5,'c2'); px(d,cx+4,by+4,'c1'); px(d,cx+3,by+5,'c2')
    px(d,cx-1,by+8,'c2'); px(d,cx+1,by+9,'c2')
    # 肩高光
    px(d,cx-5,by,'c5'); px(d,cx+5,by,'c5'); px(d,cx-6,by+1,'c6'); px(d,cx+6,by+1,'c6')
    # 腰带
    belt_y=by+12; hr(d,belt_y,cx-7,cx+7,'b2'); hr(d,belt_y+1,cx-7,cx+7,'b1')
    px(d,cx,belt_y,'g3'); px(d,cx-1,belt_y,'g2'); px(d,cx+1,belt_y,'g2')
    px(d,cx-4,belt_y,'b3'); px(d,cx+4,belt_y,'b3'); px(d,cx,belt_y,'b4')
    # 左臂
    la=cx-9+al
    for y in range(by+2,by+10): px(d,la,y,'c2'); px(d,la+1,y,'c3')
    px(d,la+1,by+2,'c5'); box(d,la-1,by+9,la+2,by+10,'c4')
    px(d,la,by+11,'s3'); px(d,la+1,by+11,'s3')
    # 右臂
    ra=cx+8+ar
    for y in range(by+2,by+10): px(d,ra,y,'c3'); px(d,ra+1,y,'c2')
    px(d,ra,by+2,'c5'); box(d,ra-1,by+9,ra+2,by+10,'c4')
    px(d,ra,by+11,'s3'); px(d,ra+1,by+11,'s3')
    # 裤子
    py=belt_y+2
    lx=cx-3+ll; rx=cx+1+lr
    for y in range(py,py+8):
        px(d,lx,y,'p1'); px(d,lx+1,y,'p2'); px(d,lx+2,y,'p2'); px(d,lx+3,y,'p1')
        px(d,rx,y,'p1'); px(d,rx+1,y,'p2'); px(d,rx+2,y,'p2'); px(d,rx+3,y,'p1')
    px(d,lx+1,py,'p3'); px(d,rx+1,py,'p3'); px(d,lx+2,py+2,'p3'); px(d,rx+2,py+2,'p3')
    # 靴子
    bty=py+8; blx=cx-4+ll; brx=cx+1+lr
    box(d,blx,bty,blx+4,bty+2,'bt2'); vr(d,blx,bty,bty+2,'bt1'); px(d,blx+2,bty,'bt3'); px(d,blx+1,bty+2,'bt1')
    box(d,brx,bty,brx+4,bty+2,'bt2'); vr(d,brx+4,bty,bty+2,'bt1'); px(d,brx+2,bty,'bt3'); px(d,brx+3,bty+2,'bt1')
    # 剑
    sx=cx+9
    px(d,sx,by-3,'hi2'); px(d,sx,by-4,'hi3'); px(d,sx,by-2,'hi1')
    px(d,sx-1,by-1,'g2'); px(d,sx,by-1,'g3'); px(d,sx+1,by-1,'g2')
    for y in range(by-9,by-4): px(d,sx,y,'sw2')
    px(d,sx,by-9,'sw4'); px(d,sx,by-8,'sw3'); px(d,sx,by-6,'sw3')
    px(d,sx-1,by-7,'c6')
    # 流苏
    tx=sx+1+tsw
    px(d,tx,by,'t3'); px(d,tx+1,by+1,'t2'); px(d,tx,by+2,'t1'); px(d,tx-1,by+3,'t1')

def draw_up(img, f):
    d=ImageDraw.Draw(img)
    cx=24; bob=[0,-1,0,-1][f]; al=[0,2,0,-2][f]; ar=[0,-2,0,2][f]
    ll=[0,1,0,-1][f]; lr=[0,-1,0,1][f]; tsw=[0,2,0,-2][f]
    # 头发背面
    box(d,cx-5,6,cx+5,15,'h2'); box(d,cx-4,5,cx+4,14,'h3')
    px(d,cx-3,7,'h4'); px(d,cx-1,8,'h5'); px(d,cx+2,7,'h4'); px(d,cx+3,9,'h4'); px(d,cx-2,10,'h4')
    hr(d,6,cx-4,cx+4,'h1'); vr(d,cx-5,7,15,'h1'); vr(d,cx+5,7,15,'h1')
    # 发髻
    box(d,cx-4,2,cx+3,5,'h1'); box(d,cx-3,2,cx+2,4,'h2'); box(d,cx-2,3,cx+1,3,'h4')
    hr(d,4,cx-5,cx+5,'g2'); px(d,cx-5,4,'g3'); px(d,cx+5,4,'g3')
    # 马尾
    mx=cx
    for y in range(6,24+bob):
        px(d,mx,y,'h2'); px(d,mx+1,y,'h3')
        if y<18: px(d,mx+2,y,'h4')
    px(d,mx+1,22+bob,'h4'); px(d,mx,23+bob,'h5'); px(d,mx+2,23+bob,'h4'); px(d,mx-1,22+bob,'h3')
    # 身体
    by=16+bob
    box(d,cx-7,by,cx+7,by+12,'c3')
    vr(d,cx-7,by,by+12,'c1'); vr(d,cx+7,by,by+12,'c1')
    box(d,cx-6,by,cx-5,by+12,'c2'); box(d,cx+5,by,cx+6,by+12,'c2')
    vr(d,cx,by,by+12,'c2'); px(d,cx,by+3,'c1'); px(d,cx,by+7,'c1')
    px(d,cx-5,by,'c5'); px(d,cx+5,by,'c5'); px(d,cx-3,by+1,'c4'); px(d,cx+3,by+1,'c4')
    # 腰带
    belt_y=by+12; hr(d,belt_y,cx-7,cx+7,'b2'); hr(d,belt_y+1,cx-7,cx+7,'b1')
    px(d,cx-3,belt_y,'b3'); px(d,cx+3,belt_y,'b3')
    # 手臂
    la=cx-9+al
    for y in range(by+2,by+10): px(d,la,y,'c2'); px(d,la+1,y,'c3')
    box(d,la-1,by+9,la+2,by+10,'c4')
    ra=cx+8+ar
    for y in range(by+2,by+10): px(d,ra,y,'c3'); px(d,ra+1,y,'c2')
    box(d,ra-1,by+9,ra+2,by+10,'c4')
    # 裤靴
    py=belt_y+2; lx=cx-3+ll; rx=cx+1+lr
    for y in range(py,py+8):
        px(d,lx,y,'p1'); px(d,lx+1,y,'p2'); px(d,lx+2,y,'p2'); px(d,lx+3,y,'p1')
        px(d,rx,y,'p1'); px(d,rx+1,y,'p2'); px(d,rx+2,y,'p2'); px(d,rx+3,y,'p1')
    px(d,lx+1,py,'p3'); px(d,rx+1,py,'p3')
    bty=py+8; blx=cx-4+ll; brx=cx+1+lr
    box(d,blx,bty,blx+4,bty+2,'bt2'); vr(d,blx,bty,bty+2,'bt1'); px(d,blx+2,bty,'bt3')
    box(d,brx,bty,brx+4,bty+2,'bt2'); vr(d,brx+4,bty,bty+2,'bt1'); px(d,brx+2,bty,'bt3')
    # 剑（背面完整）
    sx=cx+9
    for y in range(by-10,by+6): px(d,sx,y,'sw2')
    px(d,sx,by-11,'sw5'); px(d,sx,by-10,'sw4'); px(d,sx,by-8,'sw3'); px(d,sx,by-3,'sw3')
    px(d,sx-1,by+5,'g2'); px(d,sx,by+5,'g3'); px(d,sx+1,by+5,'g2')
    px(d,sx,by+6,'hi2'); px(d,sx,by+7,'hi1'); px(d,sx+1,by+7,'g3')
    tx=sx+1+tsw
    px(d,tx,by+8,'t3'); px(d,tx+1,by+9,'t2'); px(d,tx,by+10,'t1'); px(d,tx-1,by+11,'t1')

def draw_left(img, f):
    d=ImageDraw.Draw(img)
    cx=24; bob=[0,-1,0,-1][f]; ll=[0,2,0,-2][f]; lr=[0,-2,0,2][f]
    hsw=[0,-1,0,1][f]; tsw=[0,1,0,-1][f]
    # 发髻
    box(d,cx-4,2,cx+2,4,'h1'); box(d,cx-3,3,cx+1,4,'h2'); px(d,cx-2,3,'h4')
    # 头发
    box(d,cx-5,5,cx+3,7,'h1'); box(d,cx-4,6,cx+3,7,'h2')
    box(d,cx-5,7,cx-3,12,'h2'); px(d,cx-5,8,'h3'); px(d,cx-5,9,'h4')
    px(d,cx-4,8,'h3'); px(d,cx-4,10,'h4')
    hr(d,8,cx-5,cx-2,'h2'); px(d,cx-4,8,'h4'); px(d,cx-3,8,'h5')
    # 马尾
    mx=cx+4+hsw
    for y in range(6,18+bob): px(d,mx,y,'h2'); px(d,mx+1,y,'h3')
    px(d,mx+1,8,'h4'); px(d,mx+1,12,'h5'); px(d,mx,16+bob,'h4')
    # 脸
    box(d,cx-4,9,cx+2,14,'s3'); px(d,cx+2,12,'s2'); px(d,cx+2,13,'s1')
    hr(d,14,cx-3,cx+1,'s2')
    px(d,cx-5,11,'s2'); px(d,cx-5,12,'s1')
    px(d,cx-3,10,'ed'); px(d,cx-2,10,'ed'); px(d,cx-3,10,'eh')
    px(d,cx-2,13,'mo')
    # 身体
    by=15+bob
    box(d,cx-5,by,cx+4,by+13,'c3')
    vr(d,cx-5,by,by+13,'c1'); vr(d,cx+4,by,by+13,'c1')
    box(d,cx-4,by,cx-3,by+13,'c2'); box(d,cx+2,by,cx+3,by+13,'c2')
    px(d,cx-4,by,'i2'); px(d,cx-4,by+1,'i2'); px(d,cx-3,by,'i3')
    px(d,cx-2,by+5,'c1'); px(d,cx,by+7,'c2'); px(d,cx+1,by+4,'c2')
    px(d,cx-1,by+1,'c4'); px(d,cx,by+2,'c5'); px(d,cx+1,by,'c4')
    # 腰带
    belt_y=by+13; hr(d,belt_y,cx-5,cx+4,'b2'); hr(d,belt_y+1,cx-5,cx+4,'b1')
    px(d,cx-2,belt_y,'b3'); px(d,cx+2,belt_y,'b3')
    # 手臂（前臂 + 后臂）
    fa=cx-6  # 前臂
    for y in range(by+2,by+10): px(d,fa+ll//2,y,'c2'); px(d,fa+1+ll//2,y,'c3')
    box(d,fa-1+ll//2,by+9,fa+2+ll//2,by+10,'c4')
    px(d,fa+ll//2,by+11,'s3'); px(d,fa+1+ll//2,by+11,'s3')
    ba=cx+5  # 后臂
    for y in range(by+2,by+10): px(d,ba+lr//2,y,'c3'); px(d,ba+1+lr//2,y,'c2')
    box(d,ba-1+lr//2,by+9,ba+2+lr//2,by+10,'c4')
    # 裤子
    py=belt_y+2; flx=cx-3+ll; blx=cx-1-lr
    for y in range(py,py+8):
        px(d,flx,y,'p1'); px(d,flx+1,y,'p2'); px(d,flx+2,y,'p2'); px(d,flx+3,y,'p1')
        px(d,blx,y,'p1'); px(d,blx+1,y,'p2'); px(d,blx+2,y,'p2'); px(d,blx+3,y,'p1')
    px(d,flx+1,py,'p3'); px(d,blx+1,py,'p3')
    # 靴子
    bty=py+8; fbx=cx-4+ll; bbx=cx-2-lr
    box(d,fbx,bty,fbx+4,bty+2,'bt2'); vr(d,fbx,bty,bty+2,'bt1'); px(d,fbx+2,bty,'bt3')
    box(d,bbx,bty,bbx+4,bty+2,'bt2'); vr(d,bbx+4,bty,bty+2,'bt1'); px(d,bbx+2,bty,'bt3')
    # 剑（背后）
    sx=cx+6
    for y in range(by-6,by+3): px(d,sx,y,'sw2')
    px(d,sx,by-7,'sw4'); px(d,sx,by-5,'sw3')
    px(d,sx-1,by+3,'g2'); px(d,sx,by+3,'g3'); px(d,sx+1,by+3,'g2')
    px(d,sx,by+4,'hi2')
    # 流苏
    tx=sx+1+tsw
    px(d,tx,by+5,'t3'); px(d,tx+1,by+6,'t2'); px(d,tx,by+7,'t1')

def draw_right(img, f):
    """面向右 = 镜像面向左"""
    left=Image.new('RGBA',(48,48),(0,0,0,0))
    draw_left(left,f)
    img.paste(left.transpose(Image.FLIP_LEFT_RIGHT),(0,0))

def draw_idle(img, f):
    """待机动画（呼吸微动 + 流苏飘）"""
    d=ImageDraw.Draw(img)
    cx=24; breath=[0,-1,0,1][f]; tsw=[0,1,0,-1][f]
    # 头发
    box(d,cx-4,3,cx+3,5,'h1'); box(d,cx-3,2,cx+2,3,'h2'); box(d,cx-2,3,cx+1,4,'h4')
    px(d,cx-5,4,'g3'); hr(d,4,cx-4,cx+4,'g2'); px(d,cx+5,4,'g3')
    box(d,cx-6,7,cx-5,14,'h1'); box(d,cx-6,7,cx-5,10,'h3'); px(d,cx-6,8,'h4')
    box(d,cx+5,7,cx+6,14,'h1'); box(d,cx+5,7,cx+6,10,'h3'); px(d,cx+6,8,'h4')
    box(d,cx-5,6,cx+5,7,'h1'); hr(d,6,cx-4,cx+4,'h2'); px(d,cx-2,6,'h4')
    hr(d,8,cx-5,cx+5,'h2'); hr(d,9,cx-4,cx+4,'h3')
    px(d,cx-3,8,'h4'); px(d,cx-1,8,'h5'); px(d,cx+2,8,'h4')
    # 马尾
    mx=cx+5
    for y in range(6,20+breath): px(d,mx,y,'h2' if y<12 else 'h3')
    if 14+breath<20: px(d,mx+1,14+breath,'h4')
    px(d,mx,18+breath,'h4'); px(d,mx+1,19+breath,'h5')
    # 脸
    box(d,cx-4,10,cx+4,15,'s3'); hr(d,15,cx-3,cx+3,'s2')
    px(d,cx-3,12,'s4'); px(d,cx+3,12,'s4'); px(d,cx,11,'s5')
    px(d,cx-3,10,'h2'); px(d,cx-2,10,'h1'); px(d,cx+2,10,'h1'); px(d,cx+3,10,'h2')
    px(d,cx-3,11,'eh'); px(d,cx-2,11,'ed'); px(d,cx+2,11,'ed'); px(d,cx+3,11,'eh')
    px(d,cx-1,14,'mo'); px(d,cx,14,'mo')
    # 身体
    by=16+breath
    box(d,cx-7,by,cx+7,by+12,'c3')
    vr(d,cx-7,by,by+12,'c1'); vr(d,cx+7,by,by+12,'c1')
    box(d,cx-6,by,cx-5,by+12,'c2'); box(d,cx+5,by,cx+6,by+12,'c2')
    box(d,cx-2,by+1,cx+2,by+6,'c4'); px(d,cx,by+2,'c5')
    px(d,cx-1,by,'i2'); px(d,cx,by,'i3'); px(d,cx+1,by,'i2'); px(d,cx,by+1,'i2')
    px(d,cx-5,by,'c5'); px(d,cx+5,by,'c5'); px(d,cx-6,by+1,'c6'); px(d,cx+6,by+1,'c6')
    # 腰带
    belt_y=by+12; hr(d,belt_y,cx-7,cx+7,'b2'); hr(d,belt_y+1,cx-7,cx+7,'b1')
    px(d,cx,belt_y,'g3'); px(d,cx-1,belt_y,'g2'); px(d,cx+1,belt_y,'g2')
    px(d,cx,belt_y,'b4')
    # 手臂
    for y in range(by+2,by+10):
        px(d,cx-9,y,'c2'); px(d,cx-8,y,'c3'); px(d,cx+8,y,'c3'); px(d,cx+9,y,'c2')
    box(d,cx-10,by+9,cx-7,by+10,'c4'); box(d,cx+7,by+9,cx+10,by+10,'c4')
    px(d,cx-9,by+11,'s3'); px(d,cx-8,by+11,'s3'); px(d,cx+8,by+11,'s3'); px(d,cx+9,by+11,'s3')
    # 裤靴
    py=belt_y+2
    for y in range(py,py+8):
        px(d,cx-3,y,'p1'); px(d,cx-2,y,'p2'); px(d,cx-1,y,'p2'); px(d,cx,y,'p1')
        px(d,cx+1,y,'p1'); px(d,cx+2,y,'p2'); px(d,cx+3,y,'p2'); px(d,cx+4,y,'p1')
    px(d,cx-2,py,'p3'); px(d,cx+2,py,'p3')
    bty=py+8
    box(d,cx-4,bty,cx-1,bty+2,'bt2'); vr(d,cx-4,bty,bty+2,'bt1'); px(d,cx-2,bty,'bt3')
    box(d,cx+1,bty,cx+4,bty+2,'bt2'); vr(d,cx+4,bty,bty+2,'bt1'); px(d,cx+2,bty,'bt3')
    # 剑
    sx=cx+9
    for y in range(by-8,by-3): px(d,sx,y,'sw2')
    px(d,sx,by-9,'sw4'); px(d,sx,by-7,'sw3')
    px(d,sx-1,by-2,'g2'); px(d,sx,by-2,'g3'); px(d,sx+1,by-2,'g2')
    # 流苏随风
    tx=sx+1+tsw
    px(d,tx,by-1,'t3'); px(d,tx+1,by,'t2'); px(d,tx,by+1,'t1'); px(d,tx-1,by+2,'t1')

def draw_attack(img, f):
    """攻击动画6帧（面向下）"""
    d=ImageDraw.Draw(img)
    cx=24
    poses = [
        # (body_off, sword_state: 0=back, 1=raise, 2=swing, 3=down, 4=recover, 5=back2)
        (0, 0), (-1, 1), (1, 2), (2, 3), (1, 4), (0, 5)
    ]
    body_off, sword_st = poses[f]
    bob = body_off
    # 头发（简化，和down类似但无行走摆动）
    box(d,cx-4,3,cx+3,5,'h1'); box(d,cx-3,2,cx+2,3,'h2'); box(d,cx-2,3,cx+1,4,'h4')
    px(d,cx-5,4,'g3'); hr(d,4,cx-4,cx+4,'g2'); px(d,cx+5,4,'g3')
    box(d,cx-6,7,cx-5,14,'h1'); box(d,cx-6,7,cx-5,10,'h3'); px(d,cx-6,8,'h4')
    box(d,cx+5,7,cx+6,14,'h1'); box(d,cx+5,7,cx+6,10,'h3'); px(d,cx+6,8,'h4')
    box(d,cx-5,6,cx+5,7,'h1'); hr(d,6,cx-4,cx+4,'h2'); px(d,cx-2,6,'h4')
    hr(d,8,cx-5,cx+5,'h2'); hr(d,9,cx-4,cx+4,'h3')
    px(d,cx-3,8,'h4'); px(d,cx-1,8,'h5'); px(d,cx+2,8,'h4')
    # 马尾
    mx=cx+5
    for y in range(6,20+bob): px(d,mx,y,'h2' if y<12 else 'h3')
    px(d,mx,18+bob,'h4')
    # 脸
    box(d,cx-4,10,cx+4,15,'s3'); hr(d,15,cx-3,cx+3,'s2')
    # 眼睛（攻击时眼睛更锐利）
    if sword_st == 2:
        px(d,cx-3,11,'ed'); px(d,cx-2,11,'ed'); px(d,cx+2,11,'ed'); px(d,cx+3,11,'ed')
    else:
        px(d,cx-3,11,'eh'); px(d,cx-2,11,'ed'); px(d,cx+2,11,'ed'); px(d,cx+3,11,'eh')
    px(d,cx-1,14,'mo'); px(d,cx,14,'mo')
    # 身体
    by=16+bob
    box(d,cx-7,by,cx+7,by+12,'c3')
    vr(d,cx-7,by,by+12,'c1'); vr(d,cx+7,by,by+12,'c1')
    box(d,cx-6,by,cx-5,by+12,'c2'); box(d,cx+5,by,cx+6,by+12,'c2')
    box(d,cx-2,by+1,cx+2,by+6,'c4'); px(d,cx,by+2,'c5')
    px(d,cx-1,by,'i2'); px(d,cx,by,'i3'); px(d,cx+1,by,'i2')
    px(d,cx-5,by,'c5'); px(d,cx+5,by,'c5')
    belt_y=by+12; hr(d,belt_y,cx-7,cx+7,'b2'); hr(d,belt_y+1,cx-7,cx+7,'b1')
    for y in range(by+2,by+10): px(d,cx+8,y,'c3'); px(d,cx+9,y,'c2')
    box(d,cx+7,by+9,cx+10,by+10,'c4')
    px(d,cx+8,by+11,'s3'); px(d,cx+9,by+11,'s3')
    py=belt_y+2
    for y in range(py,py+8):
        px(d,cx-3,y,'p1'); px(d,cx-2,y,'p2'); px(d,cx-1,y,'p2'); px(d,cx,y,'p1')
        px(d,cx+1,y,'p1'); px(d,cx+2,y,'p2'); px(d,cx+3,y,'p2'); px(d,cx+4,y,'p1')
    bty=py+8
    box(d,cx-4,bty,cx-1,bty+2,'bt2'); px(d,cx-2,bty,'bt3')
    box(d,cx+1,bty,cx+4,bty+2,'bt2'); px(d,cx+2,bty,'bt3')
    if sword_st==0:
        for y in range(by-8,by-2): px(d,cx-10,y,'sw2')
        px(d,cx-10,by-9,'sw4')
        px(d,cx-11,by-2,'g2'); px(d,cx-10,by-2,'g3'); px(d,cx-9,by-2,'g2')
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        px(d,cx-10,by+11,'s3'); px(d,cx-9,by+11,'s3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')
    elif sword_st==1:
        for x in range(cx-12,cx-4): px(d,x,by-6,'sw2')
        px(d,cx-12,by-6,'sw5'); px(d,cx-11,by-6,'sw3')
        px(d,cx-4,by-5,'g3'); px(d,cx-4,by-4,'g2')
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        px(d,cx-10,by-2,'s3'); px(d,cx-9,by-2,'s3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')
    elif sword_st==2:
        for x in range(cx-12,cx+2): px(d,x,by+10,'sw3')
        px(d,cx-12,by+10,'sw5'); px(d,cx-11,by+10,'sw4')
        px(d,cx+2,by+9,'g3'); px(d,cx+1,by+9,'g2')
        px(d,cx-13,by+10,(255,255,255,200))
        px(d,cx+3,by+10,(255,255,255,180))
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        px(d,cx-10,by+11,'s3'); px(d,cx-9,by+11,'s3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')
    elif sword_st==3:
        for x in range(cx-10,cx+4): px(d,x,by+12,'sw2')
        px(d,cx-10,by+12,'sw4'); px(d,cx-9,by+12,'sw3')
        px(d,cx+3,by+11,'g3')
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')
    elif sword_st==4:
        for y in range(by-4,by+2): px(d,cx-10,y,'sw2')
        px(d,cx-10,by-5,'sw4'); px(d,cx-10,by-3,'sw3')
        px(d,cx-11,by+1,'g2'); px(d,cx-10,by+1,'g3')
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        px(d,cx-10,by+11,'s3'); px(d,cx-9,by+11,'s3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')
    else:
        for y in range(by-8,by-2): px(d,cx-10,y,'sw2')
        px(d,cx-10,by-9,'sw4')
        px(d,cx-11,by-2,'g2'); px(d,cx-10,by-2,'g3'); px(d,cx-9,by-2,'g2')
        for y in range(by+2,by+10): px(d,cx-10,y,'c2'); px(d,cx-9,y,'c3')
        px(d,cx-10,by+11,'s3'); px(d,cx-9,by+11,'s3')
        box(d,cx-11,by+9,cx-8,by+10,'c4')


def gen_palette():
    colors=[
        ('h1','头发深'),('h2','头发'),('h3','头发中'),('h4','头发亮'),('h5','发丝光'),
        ('s1','皮肤深'),('s2','皮肤影'),('s3','皮肤'),('s4','皮肤亮'),('s5','皮肤光'),
        ('c1','外衣深'),('c2','外衣影'),('c3','外衣'),('c4','外衣亮'),('c5','外衣光'),('c6','外衣强'),
        ('i1','内衬深'),('i2','内衬'),('i3','内衬亮'),
        ('b1','腰带深'),('b2','腰带'),('b3','腰带亮'),('b4','腰带光'),
        ('p1','裤子深'),('p2','裤子'),('p3','裤子亮'),
        ('bt1','靴子深'),('bt2','靴子'),('bt3','靴子亮'),
        ('sw1','剑深'),('sw2','剑身'),('sw3','剑亮'),('sw4','剑高光'),('sw5','剑锋'),
        ('g1','护手深'),('g2','护手'),('g3','护手亮'),
        ('hi1','剑柄深'),('hi2','剑柄'),('hi3','剑柄亮'),
        ('t1','流苏深'),('t2','流苏'),('t3','流苏亮'),
        ('ed','眼睛'),('eh','眼睛光'),
    ]
    sz=20; cols=6; rows=(len(colors)+cols-1)//cols
    img=Image.new('RGB',(sz*cols+10,sz*rows+25),(30,30,40))
    d=ImageDraw.Draw(img)
    for i,(n,l) in enumerate(colors):
        col=i%cols; row=i//cols; x=col*sz+5; y=row*sz+20
        d.rectangle([x,y,x+sz-2,y+sz-2],fill=P[n])
        d.rectangle([x,y,x+sz-2,y+sz-2],outline=(100,100,110))
    return img


def main():
    base=os.path.dirname(os.path.dirname(__file__))
    out=os.path.join(base,'assets','sprites')
    os.makedirs(out,exist_ok=True)
    print("🗡️  剑客「凌霜」精修版 v2")
    print(f"   输出: {out}")
    print("   [1/4] 行走 4方向x4帧...")
    walk=Image.new('RGBA',(192,192),(0,0,0,0))
    fns=[draw_down,draw_left,draw_right,draw_up]
    for row,fn in enumerate(fns):
        for f in range(4):
            fr=Image.new('RGBA',(48,48),(0,0,0,0))
            fn(fr,f)
            walk.paste(fr,(f*48,row*48),fr)
    walk.save(os.path.join(out,'lingshuang_walk_v2.png'))
    print("   ✓ lingshuang_walk_v2.png")
    print("   [2/4] 待机 4帧...")
    idle=Image.new('RGBA',(192,48),(0,0,0,0))
    for f in range(4):
        fr=Image.new('RGBA',(48,48),(0,0,0,0))
        draw_idle(fr,f)
        idle.paste(fr,(f*48,0),fr)
    idle.save(os.path.join(out,'lingshuang_idle_v2.png'))
    print("   ✓ lingshuang_idle_v2.png")
    print("   [3/4] 攻击 6帧...")
    atk=Image.new('RGBA',(288,48),(0,0,0,0))
    for f in range(6):
        fr=Image.new('RGBA',(48,48),(0,0,0,0))
        draw_attack(fr,f)
        atk.paste(fr,(f*48,0),fr)
    atk.save(os.path.join(out,'lingshuang_attack_v2.png'))
    print("   ✓ lingshuang_attack_v2.png")
    print("   [4/4] 调色板...")
    gen_palette().save(os.path.join(out,'palette_v2.png'))
    print("   ✓ palette_v2.png")
    prev=os.path.join(out,'preview')
    os.makedirs(prev,exist_ok=True)
    for scale in [3,6]:
        print(f"   生成{scale}x预览...")
        for name in ['lingshuang_walk_v2','lingshuang_idle_v2','lingshuang_attack_v2']:
            im=Image.open(os.path.join(out,f'{name}.png'))
            big=im.resize((im.width*scale,im.height*scale),Image.NEAREST)
            big.save(os.path.join(prev,f'{name}_{scale}x.png'))
            print(f"   ✓ {name}_{scale}x.png")
    print("\n✅ 精修版完成！")

if __name__=='__main__':
    main()