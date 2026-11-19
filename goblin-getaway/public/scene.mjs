import {LANES, ROAD_TOP, ROAD_BOTTOM, DECK_FLOOR, CELLAR_FLOOR, STAIRS, TELESCOPE_POINT, atTelescope, playerFloor, STATIONS, SUPPLY_PILE, WHEELS, wheelIsMounted, WHEEL_MOUNT_OFFSET, LOOSE_WHEEL_RADIUS, LOOSE_WHEEL_HOP_TICKS, WHEEL_KNOCK_TICKS, HOOFPRINT_RANGE, BARREL, CANNON_IDS, cannonAim, cannonScale, cannonShotRadius, cannonBlastWidth, cannonStation, cannonIndexFor, cannonPad, cannonMount, cannonState, isCannonRole, cannonAfterShot, FUSE_TICKS, CANNON_IMPACT_TICKS, CANNON_NEAR_MISS_TICKS, enemyKnockback, incomingPursuit, HZ, CATAPULT_RADIUS, activeCatapultRocks, catapultX, catapultAirX, catapultHitsWagon, ENEMY_RETREAT_TICKS, CAVALRY_SIZE_FACTOR, CAVALRY_ART_X, CAVALRY_DEATH_FRAME_TICKS, CAVALRY_DEATH_FRAME_COUNT, CAVALRY_DEATH_TICKS, ROYAL_SEATS, royalStatusNearby, royalTarget, royalActionTarget, canGiveRoyalAttention, royalRequests, feedTarget, stationAt, occupiedStation, horseRageStatus, canLoad, canTakeSpareWheel,ROAD_CAMERA_MIN_X,ROAD_OBSTACLE_DRAW_MARGIN,roadApproachWarning,operationElapsedTicks,draftGaitDistance} from './sim.mjs?v=v15-half-distance';
import {DRAFT_HORSES,DRAFT_HORSE_SCALE,DRAFT_HORSE_WALK,CAVALRY_RUN,HORSE_BODY,HORSE_NECK_AND_HEAD,draftHorseFrameOffset,draftHorseOrigin,horseBoardPoint} from './horse-targets.mjs?v=cavalry-hooves169';
import {obstacleY, roadObstacleProfile} from './road-obstacles.mjs?v=loose-wheels34';
import {art,emoteArt,sprite,frame} from './pixel-art.mjs?v=jinsoo-world178';
const ink='#393933';
const pixelFont='"NeoDunggeunmo", system-ui, sans-serif';
let c,view;
function poly(points,fill,edge=ink,lw=2.4){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.closePath();c.fillStyle=fill;c.fill();if(edge){c.lineWidth=lw;c.strokeStyle=edge;c.stroke();}}
function box(x,y,w,h,fill,edge=null,r=0,lw=2){c.beginPath();c.roundRect(x,y,w,h,r);c.fillStyle=fill;c.fill();if(edge){c.lineWidth=lw;c.strokeStyle=edge;c.stroke();}}
function oval(x,y,rx,ry,fill,edge=null,lw=2){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();if(edge){c.strokeStyle=edge;c.lineWidth=lw;c.stroke();}}
function line(points,col,width=2){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.strokeStyle=col;c.lineWidth=width;c.stroke();}
function at(x,y,z,fn){c.save();c.translate(x,y);c.scale(z,z);fn();c.restore();}
const mod=(n,d)=>((n%d)+d)%d;
function hash(n){const x=Math.sin(n*127.1+311.7)*43758.5453;return x-Math.floor(x);}
function inView(x,y,width,above=width,below=above){
  return x+width>=view.left&&x-width<=view.right&&y+below>=view.top&&y-above<=view.bottom;
}
const pineColors=[['#639775','#477e63','#89b38a'],['#598870','#40735f','#77a18a'],['#77a780','#53916d','#99bc91'],['#91b483','#6c9e79','#adca95']];
const pineBounds={x:-30,y:-62,width:64,height:77};
const pineSprites=new Array(pineColors.length);
function drawPineArt(colors){
  oval(6,5,23,7,'#446f5e66');box(-4,-6,8,12,'#887652','#416652',1,1.7);
  poly([[-25,-4],[-18,-17],[-21,-16],[-13,-31],[-17,-29],[0,-58],[17,-29],[13,-31],[22,-14],[17,-16],[26,-4],[14,-5],[8,-2],[-3,-5],[-13,-2]],colors[0],'#436d59',2);
  poly([[0,-55],[8,-30],[5,-33],[15,-16],[11,-18],[21,-6],[12,-7],[7,-4],[0,-6]],colors[1],null);
  line([[-12,-29],[-5,-32]],colors[2],2);line([[-18,-15],[-10,-18],[-6,-17]],colors[2],2);
}
function createPineSprite(colors){
  const density=3,{x,y,width,height}=pineBounds;
  let canvas=typeof OffscreenCanvas==='function'?new OffscreenCanvas(width*density,height*density):null;
  let context=canvas?.getContext('2d');
  if(!context&&typeof document!=='undefined'){
    canvas=document.createElement('canvas');
    canvas.width=width*density;
    canvas.height=height*density;
    context=canvas.getContext?.('2d');
  }
  if(!context)return null;
  context.setTransform(density,0,0,density,-x*density,-y*density);
  context.lineCap='round';
  context.lineJoin='round';
  const previous=c;
  try{
    c=context;
    drawPineArt(colors);
  }finally{
    c=previous;
  }
  return canvas;
}
function pine(x,y,size,tone){
  if(sprite(c,art['roadside-tree'],x,y-28*size,92*size))return;
  const key=tone%pineColors.length,colors=pineColors[key];
  if(pineSprites[key]===undefined)pineSprites[key]=createPineSprite(colors);
  const cached=pineSprites[key];
  if(!cached){
    at(x,y,size,()=>drawPineArt(colors));
    return;
  }
  const bounds=pineBounds;
  c.drawImage(cached,x+bounds.x*size,y+bounds.y*size,bounds.width*size,bounds.height*size);
}
function stone(x,y,z){
  at(x,y,z,()=>{oval(3,5,20,6,'#685b4240');poly([[-18,4],[-14,-7],[-3,-13],[10,-11],[18,-1],[14,5],[-4,7]],'#82958b','#59695f',2.2);
  poly([[-14,-6],[-3,-12],[8,-10],[3,-4],[-7,-3]],'#bac4a9',null);line([[3,-4],[7,3],[14,4]],'#69776c',1.6);
  poly([[19,9],[20,3],[26,2],[30,7],[25,10]],'#9aa79a','#6c796c',1.5);});
}
function shrub(x,y,z){
  at(x,y,z,()=>{oval(2,3,16,4,'#547f6144');poly([[-14,2],[-14,-4],[-9,-8],[-5,-7],[-1,-12],[5,-13],[9,-6],[14,-6],[18,1],[11,3],[-6,4]],'#76a877','#557c5f',1.8);line([[-8,-4],[-5,-6],[-2,-4]],'#a3c394',2);});
}
function grass(x,y,z){line([[x-3*z,y-3*z],[x,y],[x+2*z,y-5*z]],'#608d6c',1.5*z);line([[x+5*z,y],[x+8*z,y-3*z]],'#608d6c',1.5*z);}
function fence(x,y){
  at(x,y,1,()=>{line([[-39,0],[43,0]],'#5c5b44',7);line([[-39,-1],[43,-1]],'#b9a479',3);for(const q of [-35,0,38]){poly([[q-3,3],[q-3,-9],[q,-12],[q+3,-10],[q+3,4]],'#a99165','#585b48',1.8);}});
}
const MAX_CANVAS_PIXELS = 1920 * 1080;
const upperVerge=0,lowerVerge=1;
const shoulderPlants=[upperVerge,lowerVerge].map(band=>Array.from({length:28},(_,i)=>({
  x:i*53+hash(i+51*band)*31,
  y:band?ROAD_BOTTOM-10+hash(i+77)*53:ROAD_TOP-54+hash(i+38)*74,
  width:24+hash(i)*28,height:3+hash(i+9)*4,
  shrub:i%4===1?.8+hash(i)*.3:0,
})));
const roadSpecks=Array.from({length:105},(_,i)=>({
  x:hash(i)*1450,
  y:ROAD_TOP+12+hash(i+215)*(ROAD_BOTTOM-ROAD_TOP-24),
  shade:hash(i+6),
}));
const vergePines=[upperVerge,lowerVerge].map(band=>Array.from({length:42},(_,i)=>{
  const seed=i+band*100;
  return {
    x:i*35+hash(seed+9)*32,
    y:band?ROAD_BOTTOM+42+hash(seed+58)*48:ROAD_TOP-50+hash(seed+28)*38,
    size:.68+hash(seed+167)*.64,tone:Math.floor(hash(seed+13)*4),
  };
}).sort((a,b)=>a.y-b.y));
function firstSceneryTile(phase,period,origin,padding){
  const start=view.left-padding;
  return start+mod(phase+origin-start,period);
}
// Extend the roadside forest into the newly visible ground on taller displays.
function woodland(scroll,band){
  const {top,bottom,right}=view;
  const spacing=68;
  const firstRow=Math.floor((top-55)/spacing);
  const lastRow=Math.ceil((bottom+70)/spacing);
  const first=band===lowerVerge?Math.max(firstRow,Math.floor((ROAD_BOTTOM+54)/spacing)+1):firstRow;
  const last=band===upperVerge?Math.min(lastRow,Math.ceil((ROAD_TOP-54)/spacing)-1):lastRow;
  for(let row=first;row<=last;row++){
    const y=row*spacing;
    for(let col=0;col<15;col++){
      const seed=(row+90)*53+col*13;
      const base=y+hash(seed+3)*34;
      for(let x=firstSceneryTile(col*100+hash(seed)*68+scroll,1500,-120,66);x<=right+60;x+=1500){
        if(!inView(x,base,66,85,29))continue;
        oval(x+7,base+3,30+hash(seed+2)*26,5,'#749d7f');
        if(hash(seed+1)>.32){
          pine(x,base,.75+hash(seed+7)*.55,Math.floor(hash(seed+2)*4));
          if(hash(seed+5)>.52)pine(x+26,base+12,.62+hash(seed+12)*.45,Math.floor(hash(seed+13)*4));
        }else if(hash(seed+6)>.4){shrub(x,base,1.1);grass(x+28,base+8,1);}
        else{stone(x,base,.65);grass(x-20,base+2,.9);}
      }
    }
  }
}
function drawShoulder(scroll,band){
  for(const plant of shoulderPlants[band]){
    const {y}=plant;
    for(let x=firstSceneryTile(plant.x+scroll,1470,-110,54);x<=view.right+54;x+=1470){
      if(!inView(x,y,54,23))continue;
      oval(x,y,plant.width,plant.height,'#7fa78a');
      grass(x,y,1);
      if(plant.shrub)shrub(x+13,y-3,plant.shrub);
    }
  }
}
function drawVerge(scroll,band){
  for(const tree of vergePines[band]){
    for(let x=firstSceneryTile(tree.x+scroll,1470,-100,43);x<=view.right+43;x+=1470){
      if(inView(x,tree.y,43,82,18))pine(x,tree.y,tree.size,tree.tone);
    }
  }
  for(let j=band;j<10;j+=2){
    const y=band===lowerVerge?ROAD_BOTTOM+8+hash(j)*7:ROAD_TOP-9-hash(j)*5;
    for(let x=firstSceneryTile(j*159+scroll,1470,-85,49);x<=view.right+49;x+=1470){
      if(!inView(x,y,49,23))continue;
      if(j%4===0)fence(x,y);
      else if(j%3===0)stone(x,y,.72);
      else shrub(x,y,.9);
    }
  }
}
function roadShape(edges,upperOffset,lowerOffset,fill){
  c.beginPath();
  edges.forEach(([x,upper],i)=>i?c.lineTo(x,upper+upperOffset):c.moveTo(x,upper+upperOffset));
  for(let i=edges.length-1;i>=0;i--){
    const [x,,lower]=edges[i];
    c.lineTo(x,lower+lowerOffset);
  }
  c.closePath();c.fillStyle=fill;c.fill();
}
function sceneryTile(name,scroll,scale=1,y=LANES[1]){
  const tile=art[name];
  if(!spriteReady(tile))return false;
  const width=tile.naturalWidth*scale,height=tile.naturalHeight*scale;
  const start=view.left-mod(view.left-scroll,width);
  for(let x=start;x<view.right;x+=width)sprite(c,tile,x+width/2,y,width,height);
  return true;
}
const roadArtScale=(ROAD_BOTTOM-ROAD_TOP)/149;
const roadArtY=LANES[1]+(135-133.5)*roadArtScale; // Source drivable strip runs from y=59 to y=208.
function vergeArt(scroll,band){
  const prefix=band===upperVerge?'upper':'lower';
  const top=LANES[1]-450,bottom=LANES[1]+450;
  if(!sceneryTile(prefix+'-woodland',scroll))woodland(scroll,band);
  else if(band===upperVerge?view.top<top:view.bottom>bottom){
    // Continue the forest on unusually tall or wide camera views.
    const edge=band===upperVerge?top:bottom;
    c.save();c.beginPath();
    c.rect(view.left,band===upperVerge?view.top:edge,view.right-view.left,
      band===upperVerge?edge-view.top:view.bottom-edge);
    c.clip();woodland(scroll,band);c.restore();
  }
  if(!sceneryTile(prefix+'-shoulder',scroll))drawShoulder(scroll,band);
}
function landscape(scroll,stage){
  const {left,right,top,bottom}=view;
  if(spriteReady(art.bg)){
    c.fillStyle='#346246';
    c.fillRect(left-2,top-2,right-left+4,bottom-top+4);
    sceneryTile('bg',scroll,roadArtScale,roadArtY);
    return;
  }
  // A focused camera wholly inside the road only sees the topmost road fill.
  if(top>ROAD_TOP+18&&bottom<ROAD_BOTTOM-20){
    c.fillStyle=spriteReady(art['road-surface'])?'#e8d3a2':'#ead09e';
    c.fillRect(left-2,top-2,right-left+4,bottom-top+4);
  }else{
    box(left-2,top-2,right-left+4,bottom-top+4,['#92b492','#a1b1a0','#acaa90'][(stage-1)%3]);
    vergeArt(scroll,upperVerge);
    if(!sceneryTile('road-surface',scroll)){
      const edges=[];
      for(let x=Math.floor(left/20)*20-20;x<=right+20;x+=20){
        const t=(x-scroll)*Math.PI/720;
        edges.push([x,
          ROAD_TOP+5*Math.sin(t*4)+3*Math.sin(t*14),
          ROAD_BOTTOM+6*Math.sin(t*3+.7)+3*Math.sin(t*13),
        ]);
      }
      roadShape(edges,-6,7,'#c3bd87');
      roadShape(edges,0,0,'#d9ba84');
      roadShape(edges,8,-9,'#ead09e');
    }
  }
  if(!sceneryTile('road-specks',scroll))for(const speck of roadSpecks){
    const {y,shade}=speck;
    for(let x=firstSceneryTile(speck.x+scroll,1450,-65,24);x<=right+24;x+=1450){
      if(!inView(x,y,24,6,8))continue;
      if(shade>.78){
        line([[x,y],[x+8+shade*8,y]],'#d4b887',2);
        line([[x+11,y+4],[x+19,y+4]],'#dfc291',1.5);
      }else oval(x,y,shade*2.4+.6,1.2,'#ccb185');
    }
  }
  if(!sceneryTile('upper-verge',scroll))drawVerge(scroll,upperVerge);
}
const hoofprintRows=[[2,4],[1,2],[0,2],[1,2],[2,4]];
function drawHoofprints(s,prints){
 if(!prints?.length)return;
 c.save();
 c.fillStyle='#765131';
 for(const [road,footX,y] of prints){
   const x=footX+s.road-road;
   if(!inView(x,y,8))continue;
   c.globalAlpha=.52*Math.min(1,Math.max(0,HOOFPRINT_RANGE-(s.road-road))/280);
   for(const [row,[left,width]] of hoofprintRows.entries()){
     c.fillRect(x+left*2-6,y+row*2-5,width*2,2);
   }
 }
 c.restore();
}
function drawWheelTracks(s){
 const tracks=s.wheelTracks??[],looseTracks=s.looseWheelTracks??[];
 if(tracks.length<2&&!looseTracks.length)return;
 c.save();
 c.beginPath();
 for(const [index,wheel] of WHEELS.entries()){
   const bit=1<<index,roadX=s.road+wheel.x;
   const trackY=mountedWheelY(Math.sign(wheel.y))+wheelTreadOffset;
   let drawing=false;
   for(let i=1;i<tracks.length;i++){
     const [startRoad,startY,startMask]=tracks[i-1], [endRoad,endY,endMask]=tracks[i];
     const startX=roadX-startRoad,endX=roadX-endRoad;
     if(!(startMask&endMask&bit)||startX<view.left-8||endX>view.right+8){drawing=false;continue;}
     if(!drawing)c.moveTo(startX,startY+trackY);
     c.lineTo(endX,endY+trackY);
     drawing=true;
   }
 }
 for(const [fromX,fromY,toX,toY] of looseTracks){
   const startX=fromX+s.road,endX=toX+s.road;
   if(Math.max(startX,endX)<view.left-8||Math.min(startX,endX)>view.right+8)continue;
   c.moveTo(startX,fromY+looseWheelTreadOffset);
   c.lineTo(endX,toY+looseWheelTreadOffset);
 }
 // Stroke all wheels together so overlapping ruts have the same color as a single rut.
 c.lineCap='butt';c.lineJoin='bevel';
 c.strokeStyle='#79533258';c.lineWidth=12;c.stroke();
 c.strokeStyle='#68442582';c.lineWidth=6;c.stroke();
 c.restore();
}
function fallenLog(){
  poly([[-17,-25],[-7,-30],[9,-26],[18,21],[9,26],[-7,25]],'#996d49');
  poly([[-17,-25],[-7,-30],[9,-26],[1,-20],[-13,-19]],'#cf9b66','#775b40',1.8);
  line([[-8,-17],[-4,-5],[-1,8],[1,16]],'#c79562',3);
  line([[2,-21],[5,-10],[6,3],[12,16]],'#795139',2.5);
  line([[0,5],[-13,0],[-20,4]],'#6f5138',5.5);
  oval(6,21,13,8,'#d6a778',ink,2.2);
  oval(6,21,8,4.5,'#c18b58','#a07146',1.4);
  oval(6,21,2,1.5,'#9e623c');
}
function roadBoulder(){
  poly([[-30,12],[-28,-11],[-16,-25],[3,-30],[21,-21],[29,-8],[28,15],[13,28],[-9,29]],'#858e85','#4f5c54',2.8);
  poly([[-28,-11],[-16,-25],[3,-30],[21,-21],[11,-9],[-7,-6]],'#bec7ab',null);
  poly([[11,-9],[21,-21],[29,-8],[28,15],[13,28],[3,20],[7,8]],'#67736b',null);
  poly([[-30,12],[-7,-6],[7,8],[3,20],[-9,29]],'#96a090',null);
  line([[11,-9],[-4,-4],[-8,5],[-18,10]],'#56665d',2);
  line([[7,8],[2,14],[3,20]],'#5f6d65',1.8);
}
function woodlandDeer(){
  const fur='#ad764d',leg='#795039';
  for(const side of [-1,1]){
    line([[side*6,-4],[side*14,-11],[side*17,-15]],ink,5.5);
    line([[side*6,-4],[side*14,-11],[side*17,-15]],leg,3.2);
    line([[side*7,16],[side*15,20],[side*14,26]],ink,5.5);
    line([[side*7,16],[side*15,20],[side*14,26]],leg,3.2);
  }
  poly([[-5,20],[0,28],[5,20]],'#f3e0b8',ink,1.6);
  oval(0,10,12,13,fur,ink,2.3);
  oval(0,17,8,4,'#cf9b72');
  for(const [x,y] of [[-6,5],[5,7],[-4,12],[7,14]])oval(x,y,1.65,1.25,'#f4deba');
  poly([[-7,0],[-5,-17],[5,-17],[7,0],[3,3],[-3,3]],fur,ink,2.1);
  for(const side of [-1,1]){
    poly([[side*4,-12],[side*13,-19],[side*13,-13],[side*7,-8]],'#cc956b',ink,1.8);
    line([[side*4,-21],[side*10,-25],[side*12,-30]],'#6b4c39',2.1);
    line([[side*10,-25],[side*7,-30]],'#6b4c39',1.9);
  }
  oval(0,-18,6,9,'#c18a62',ink,1.9);
  oval(0,-24,3.3,2.5,'#564238');
  oval(-3,-17,1.15,1.15,ink);oval(3,-17,1.15,1.15,ink);
}
function drawRoadObstacle(obstacle){
  const y=obstacleY(obstacle);
  if(!inView(obstacle.x,y,ROAD_OBSTACLE_DRAW_MARGIN))return;
  const {halfWidth}=roadObstacleProfile(obstacle.type);
  at(obstacle.x,y,halfWidth/30,()=>{
    if(obstacle.type==='rock'&&anchoredSprite(art.bg_rock,0,0,1.25,[27.2,23.3]))return;
    if(obstacle.type==='log'&&anchoredSprite(art.bg_wood,0,0,1.15,[16.3,29.9]))return;
    if(sprite(c,art['road-'+obstacle.type],0,0,90))return;
    const rx=obstacle.type==='rock'?34:22;
    oval(4,0,rx,30,'#78674644');
    if(obstacle.type==='log')fallenLog();
    else if(obstacle.type==='deer')woodlandDeer();
    else roadBoulder();
  });
}
const horseStrideRate=2;
function horse(x,y,z,palette,t,showShadow=true){
  const [coat,light,mane]=palette;
  at(x,y,z,()=>{
    if(showShadow)oval(3,7,42,14,'#715f4850');
    const kick=Math.sin(t)*3.4;
    for(let i=0;i<4;i++){
      const side=i<2?-1:1,front=i%2===0,x=front?-15:19;
      const gait=side*kick*(front?1:-1),hoof=x+(front?-12:8)+gait*1.7;
      line([[x,side*8],[x+(front?-6:4)+gait,side*16],[hoof,side*19]],ink,6);
      line([[x,side*8],[x+(front?-6:4)+gait,side*16],[hoof,side*19]],coat,3.4);
      box(hoof-3,side*19-2,6,4,'#524a42',ink,1,1);
    }
    poly([[32,-4],[46,-4],[51,0],[47,3],[54,7],[39,6],[31,3]],mane,ink,2);
    poly(HORSE_BODY,coat,ink,2.8);
    poly([[-11,-10],[12,-11],[23,-7],[9,-6],[-5,-7]],light,null);box(7,-13,7,26,'#805737',ink,1,1.6);
    poly(HORSE_NECK_AND_HEAD,coat,ink,2.5);
    poly([[-28,-12],[-18,-14],[-9,-10],[-13,-3],[-21,-4],[-25,-7]],mane,ink,1.8);line([[-38,-8],[-37,5]],'#7b4c37',3);oval(-36,-4,1.8,1.8,ink);oval(-48,-1,1.25,1.25,ink);line([[-45,1],[-42,2]],light,1.5);});
}
function crewBody(color,outline=0){
  const edge=outline?'#ffffff':ink;
  box(-11,0,22,15,color,edge,5,2+outline);
  box(-10,10,7,5,'#685743',edge,2,1.6+outline);
  box(3,10,7,5,'#685743',edge,2,1.6+outline);
  at(0,-3,.98,()=>{
    poly([[-11,-4],[-17,-10],[-17,0],[-10,4],[8,4],[16,0],[17,-9],[9,-6]],'#91aa6e',edge,2.2+outline);
    box(-11,-10,23,20,'#a4bd74',edge,6,2.6+outline);
    poly([[-11,-7],[-8,-17],[0,-20],[9,-16],[12,-8],[6,-9],[0,-7]],color,edge,2.4+outline);
    if(!outline){
      line([[-8,-4],[-4,-3]],ink,2);line([[3,-3],[7,-4]],ink,2);box(-3,2,5,2,'#586147');
    }
  });
}
function snackPlate(x,y,scale=1){
 at(x,y,scale,()=>{
   oval(0,2,10,6,'#b27f59','#574034',1.7);
   oval(0,-1,8,5,'#ffe8af','#987055',1.2);
   box(-5,-5,10,6,'#e9a955','#895537',3,1.4);
   line([[-2,-4],[-3,-1],[2,-2],[3,-4]],'#fff0b5',1.15);
 });
}
const royalTileWidth=36,royalTileHeight=44,royalHeight=40;
const royalPoses={seated:0,standing:1,crying:2,bored:3};
const royalArtwork={
 prince:{seated:['bottom_price_sit',[16.5,26],.88],standing:['bottom_price_stand',[16,23],.89]},
 princess:{seated:['bottom_pricess_sit',[17,19],.96],standing:['bottom_pricess_stand',[15.5,19],.96]},
};
function royalStatusVisible(s,royal){
 return view.localPlayers.some(id=>royalStatusNearby(s.players[id],royal));
}
function royalSprite(s,royal,x,y,scale,t,tilt=0){
 const seated=royal.state==='seated';
 const playingShell=royal.state==='wandering'&&royal.shellPlayUntil>s.tick;
 const wandering=royal.state==='wandering'&&!playingShell&&!(royal.snackPauseUntil>s.tick);
 const showMood=(royal.bored||royal.crying)&&royalStatusVisible(s,royal);
 const pose=showMood?(royal.bored?'bored':'crying'):seated?'seated':'standing';
 const row=royal.id==='prince'?0:1;
 const bob=wandering&&!reducedMotion?.matches?-Math.abs(Math.sin(t*1.25+row*2))*2:0;
 const crop=[royalPoses[pose]*royalTileWidth,row*royalTileHeight,royalTileWidth,royalTileHeight];
 at(x,y,scale,()=>{
   if(royal.state!=='carried')oval(1,8,13,6,'#60473647');
   c.save();
   c.rotate(tilt);
   const [name,pivot,pixelScale]=royalArtwork[royal.id][seated?'seated':'standing'];
   const frameTick=reducedMotion?.matches||(!seated&&!wandering)?0:operationElapsedTicks(s)+row*4;
   if(!anchoredSprite(frame(art[name],frameTick,seated?8:4),0,-9+bob,pixelScale,pivot)){
     sprite(c,art['royal-children'],0,10-royalHeight/2+bob,royalHeight*royalTileWidth/royalTileHeight,royalHeight,crop);
   }
   const heart=royal.attentionHeart;
   const heartFlying=heart&&heart.floor===view.floor&&s.tick>=heart.tick&&s.tick<heart.until;
   const needs=royalRequests(royal);
   if(heartFlying||needs){
     const {a,b,c:skewX,d:scaleY,e:x,f:y}=c.getTransform();
     const faceY=-14+bob;
     const faceX=x+skewX*faceY,faceScreenY=y+scaleY*faceY;
     if(heartFlying)view.royalHearts.push({heart,x:faceX,y:faceScreenY});
     if(needs)view.royalRequestAnchors.push({id:royal.id,needs,x:faceX,y:faceScreenY,scale:Math.hypot(a,b)});
   }
   if(playingShell){
     const lift=reducedMotion?.matches?0:Math.abs(Math.sin((royal.shellPlayUntil-s.tick)*.23))*9;
     const ballY=4-lift;
     line([[5,2],[10,5]],'#efc6a2',2);
     if(!sprite(c,art.bottom_cannonball,12,ballY,14)){
       oval(12,ballY,7,7,'#454e5b',ink,1.8);
       oval(10,ballY-3,2,1.5,'#a5b1b2');
     }
   }
   c.restore();
 });
}
const floorPadWidth=48,floorPadHeight=floorPadWidth*19/32;
function drawFloorPad(x,y,fill,edge,strokeWidth){
 oval(x,y,floorPadWidth/2,floorPadHeight/2,fill,edge,strokeWidth);
}
function telescopeMark(s){
 const {x,y}=TELESCOPE_POINT;
 const selected=view.localPlayers.some(id=>atTelescope(s.players[id]));
 drawFloorPad(x,y,selected?'#a4eed47d':'#6dad9c45',selected?'#d7fff1':'#51867a',selected?2.7:1.8);
 at(x+17,y-1,1,()=>{
   oval(0,11,12,4,'#513c3447');
   poly([[-2,-2],[3,-2],[8,11],[5,11],[1,3],[-3,11],[-7,11]],'#91604a','#50392f',1.6);
   poly([[-12,-4],[-7,-4],[-7,-5],[11,-11],[16,-11],[16,-4],[12,-4],[-6,2],[-7,1],[-12,1]],'#bd8650','#4f3a32',2);
   line([[-6,-4],[11,-9]],'#fadd93',1.6);
   poly([[10,-12],[15,-13],[18,-12],[18,-5],[14,-3],[10,-4]],'#e7af64','#4f3a32',1.4);
   c.fillStyle=selected?'#c7ffed':'#8ad7dc';
   c.fillRect(15,-10,2,4);
 });
}
function returnMark(seat){
 drawFloorPad(seat.x,seat.dropY,'#fff2a936',null);
 if(!sprite(c,art.bottom_crown,seat.x,seat.dropY,floorPadWidth)){
   poly([[seat.x-5,seat.dropY-2],[seat.x,seat.dropY-9],[seat.x+5,seat.dropY-2]],'#fff2a7','#6f503c',1.4);
 }
}
function feedMark(seat,selected){
 const {x,dropY:y}=seat;
 drawFloorPad(x,y,selected?'#ffdf875c':'#ffe3a327',selected?'#ffe1a2':'#d4ad72',selected?2.7:1.8);
 snackPlate(x,y,.82);
}
function drawPixelHeart(){
 poly([
   [-8,-5],[-6,-5],[-6,-7],[-2,-7],[-2,-5],[2,-5],[2,-7],[6,-7],[6,-5],[8,-5],
   [8,-1],[6,-1],[6,1],[4,1],[4,3],[2,3],[2,5],[-2,5],[-2,3],[-4,3],[-4,1],[-6,1],[-6,-1],[-8,-1],
 ],'#fa689b','#7b324c',1.8);
 c.fillStyle='#ffd0dd';
 c.fillRect(-5,-5,3,2);
}
function attentionMark(royal,selected){
 const seat=ROYAL_SEATS[royal.id];
 const x=seat.x+(royal.id==='prince'?-10:10),y=seat.dropY-6;
 drawFloorPad(x,y,selected?'#f6a4c7c4':'#d169964d',selected?'#ffe0e9':'#a05372',selected?2.7:1.8);
 at(x,y+1,1.25,drawPixelHeart);
}
function royalSeatLift(s,tick=s.tick){
 const bounce=s.royalSeatBounce;
 if(!bounce||tick<bounce.tick||tick>=bounce.until||reducedMotion?.matches||(s.mode!=='run'&&s.mode!=='paused'))return 0;
 const progress=(tick-bounce.tick)/(bounce.until-bounce.tick);
 return (12+7*bounce.strength)*4*progress*(1-progress);
}
function looseRoyal(s,royal,t,reachable){
 const {x,y}=royal;
 const upset=royal.upset;
 if(royal.state==='fallen'){
   const side=royal.id==='prince'?-1:1,seat=ROYAL_SEATS[royal.id];
   const age=s.tick-(royal.fallAt??-Infinity);
   const progress=reducedMotion?.matches?1:Math.max(0,Math.min(1,age/9));
   const launchLift=royalSeatLift(s,royal.fallAt??-Infinity)*(1-progress);
   const drawY=seat.y+(y-seat.y)*progress-9*Math.sin(Math.PI*progress)-launchLift;
   if(reachable)oval(x,y,19,13,upset?'#ffc7a85f':'#fff1b858',upset?'#ffc8a2':'#fff2a7',2.2);
   royalSprite(s,royal,x,drawY,1,t,side*1.24*progress);
   return;
 }
 if(reachable)oval(x,y,16,14,'#fff1b858','#fff2a7',2.2);
 royalSprite(s,royal,x,y,1,t);
 const waitingForSnack=royal.snackPauseUntil>s.tick;
 const mischievous=royal.mischief&&royal.mischief!=='cook'&&!(royal.shellPlayUntil>s.tick);
 if(!(waitingForSnack||mischievous)||!royalStatusVisible(s,royal))return;
 if(waitingForSnack)snackPlate(x+14,y-20,.65);
 else{
   oval(x+12,y-20,6,6,'#b66545','#5c3929',1.3);
   line([[x+12,y-23],[x+12,y-20]],'#fff1be',1.4);
   oval(x+12,y-17,1,1,'#fff1be');
 }
}
function drawRoyal(s,royal,t){
 if(royal.state==='seated'){
   const {x,y}=ROYAL_SEATS[royal.id];
   royalSprite(s,royal,x,y-15-royalSeatLift(s),1.42,t);
   return;
 }
 const reachable=view.localPlayers.some(id=>{
   const player=s.players[id];
   return stationAt(player,s)==='royal-pickup'&&royalTarget(s,player)===royal;
 });
 looseRoyal(s,royal,t,reachable);
}
const stationArt={
 driver:['#bea5bd','#795d83','bottom_horse_move'],
 rack:['#80afc6','#3a6579','bottom_bombbox'],
 cleaner:['#e9cd82','#80632d','bottom_cannon'],
 loader:['#9bc69a','#476b49','bottom_bomb'],
 gunner:['#dba476','#865c3d','bottom_cannon'],
 supply:['#92b6c5','#4d7080','bottom_bombplus'],
 cook:['#c4a575','#765333'],
 'wheel-pickup':['#c0a16d','#705635','cellar_refill']
};
const occupiedPadStyle={fill:'#999c9b',edge:'#565a59'};
function occupiedByAnother(station){
 const owner=view.stationOwners.get(station);
 return owner!==undefined&&!view.localPlayers.includes(owner);
}
function spareWheel(x,y,scale,angle){
 at(x,y,scale,()=>{
   if(angle!==undefined)c.rotate(angle);
   if(sprite(c,art.bottom_wheel[0],0,0,26))return;
   oval(0,0,12,12,'#544842','#302829',2);
   oval(0,0,9,9,'#493630','#c29960',1.7);
   for(let spoke=0;spoke<6;spoke++){
     const angle=spoke*Math.PI/3;
     line([[0,0],[Math.cos(angle)*8,Math.sin(angle)*8]],'#bd935e',1.9);
   }
   oval(0,0,3.1,3.1,'#b4a292','#634e3b',1.1);
   if(angle!==undefined)line([[-2,-10.5],[2,-11]],'#e8c48b',2);
 });
}
function drawSpareWheelStack(x,y,count){
 const width=42,height=23;
 oval(x,y+7,23,7,'#21160965');
 for(let level=0;level<count;level++){
   const wheelY=y-level*8;
   if(sprite(c,art.cellar_wheelrefill,x,wheelY,width,height))continue;
   oval(x,wheelY+3,width/2,height*.38,'#65432e','#382419',1.5);
   if(!sprite(c,art.bottom_wheel[0],x,wheelY,width,height)){
     at(x,wheelY,1,()=>{
       c.scale(1,height/width);
       spareWheel(0,0,width/26);
     });
   }
 }
}
const cellarLabelScale=()=>Math.max(1,.9/(view.zoom*view.cssX));
const rackPile={x:STATIONS.rack.x+STATIONS.rack.r+18,y:STATIONS.rack.y};
function drawRackPile(count){
 const {x,y}=rackPile;
 box(x-17,y-9,34,21,'#907050',ink,3,1.6);
 for(let i=0;i<Math.min(count,5);i++){
   const shellX=x-9+(i%3)*9+(i>=3?4.5:0);
   const shellY=y+5-Math.floor(i/3)*8;
   if(!sprite(c,art.bottom_cannonball,shellX,shellY,12)&&!sprite(c,art['rack-shell'],shellX,shellY,12)){
     oval(shellX,shellY,5.6,5.6,'#565b66',ink,1.3);
     oval(shellX-1.5,shellY-1.8,2,1.35,'#9297a1');
   }
 }
}
function floorStation(s,role,cannonIndex=null){
 const onRoute=(view.role==='carry'||view.role==='catch')&&role==='rack';
 const focused=view.role&&view.role!=='driver';
 const currentPad=cannonIndex===null?view.role===role:cannonIndex===view.cannonIndex;
 const occupied=occupiedByAnother(cannonIndex??role);
 if(focused&&!currentPad&&!onRoute&&!occupied)return;
 const {x,y}=cannonIndex===null?STATIONS[role]:cannonPad(cannonIndex);
 const [fill,edge,image]=stationArt[role];
 const hasSupply=s.testMode||s.ammo.stock>0;
 const inactive=(role==='loader'&&!canLoad(s,cannonIndex))||(role==='wheel-pickup'&&!canTakeSpareWheel(s))||(role==='supply'&&!hasSupply);
 const border=occupied?occupiedPadStyle.edge:inactive?'#897961':edge;
 const padFill=occupied?occupiedPadStyle.fill:inactive?'#bda47e':fill;
 if(role==='supply'&&hasSupply){
   if(!sprite(c,art.cellar_cannonballs,SUPPLY_PILE.x,SUPPLY_PILE.y,SUPPLY_PILE.width)){
     sprite(c,art['supply-pile'],SUPPLY_PILE.x,SUPPLY_PILE.y,SUPPLY_PILE.width);
   }
 }
 c.save();
 if(occupied||inactive)c.filter='grayscale(1)';
 const padDrawn=image&&sprite(c,art[image],x,y,floorPadWidth);
 c.restore();
 if(!padDrawn)drawFloorPad(x,y,padFill,border,2.3);
 c.textAlign='center';c.textBaseline='middle';
 if(role==='rack'){
   const count='x '+s.ammo.rack,labelX=rackPile.x,labelY=y-17;
   c.font='bold 9px '+pixelFont;
   const width=c.measureText(count).width+12;
   box(labelX-width/2,labelY-6,width,13,border,ink,3,1);
   c.fillStyle='#fff8e9';c.fillText(count,labelX,labelY+.5);
 }else if(role==='supply'&&!s.testMode){
   box(SUPPLY_PILE.x+7,SUPPLY_PILE.y+10,20,14,border,ink,3,1);
   c.fillStyle='#fff8e9';c.font='bold 10px '+pixelFont;c.fillText(s.ammo.stock,SUPPLY_PILE.x+17,SUPPLY_PILE.y+17);
 }else if(role==='cook'&&!sprite(c,art.cellar_food,x-32,y+5,28)){
   box(x-16,y-10,32,20,'#986f45','#604329',3,2);
   line([[x-13,y+6],[x+13,y+6]],'#b8915a',1.5);
   snackPlate(x-6,y,.82);
   oval(x+8,y-1,5,5,'#7c6c60','#554636',1.3);
   oval(x+8,y-2,3.3,2.5,'#c58159');
 }
}
const crewArt={
 p1:['#aa7ed4','bottom_purple',0],
 p2:['#dc9d5d','bottom_orange',3],
 p3:['#7aa5c5','bottom_blue',6],
 p4:['#d887b9','bottom_pink',9],
};
function drawCargoShell(x,y,selected=false){
 at(x,y,2,()=>{
   if(selected)oval(0,0,9,9,'#fff');
   if(sprite(c,art.bottom_cannonball,0,0,14))return;
   oval(0,0,7,7,'#454e5b',ink,1.8);
   oval(-2,-3,2,1.5,'#a5b1b2');
 });
}
function crewSprite(s,id,t,passenger,recordPosition){
 const p=s.players[id],[color,image,phase]=crewArt[id];
 const currentRole=stationAt(p,s);
 const side=currentRole==='driver'?-1:p.facing??(p.ax<0?-1:1);
 const moving=currentRole!=='driver'&&Math.hypot(p.mx,p.my)>.15&&s.mode==='run';
 const reduced=reducedMotion?.matches;
 const animationTick=reduced?0:(s.mode==='ready'?Math.floor(t*14):operationElapsedTicks(s))+phase;
 const bounce=moving&&!reduced?Math.sin(t*1.3)*1.8:0;
 if(!sprite(c,art.bottom_shadow,p.x+2,p.y+10,32))oval(p.x+2,p.y+7,17,7,'#4d3e454a');
 at(p.x,p.y+bounce,1,()=>{
   recordPosition(id);
   const local=view.localPlayers.includes(id);
   c.save();
   c.scale(-side,1); // The original goblin sprites face left.
   if(local){
     c.filter='drop-shadow(2px 0 white) drop-shadow(-2px 0 white) drop-shadow(0 2px white) drop-shadow(0 -2px white)';
   }
   const avatarDrawn=sprite(c,frame(art[image],animationTick,moving?4:13),0,-5,46);
   if(local)c.filter='none';
   if(!avatarDrawn){
     if(local)crewBody(color,7/Math.sqrt(view.zoom));
     crewBody(color);
   }
   if(p.ball)drawCargoShell(-22,4,local);
   c.restore();
   if(passenger){
     const rocking=currentRole==='royal-soothe'&&!passenger.bored&&moving&&!reducedMotion?.matches;
     const sway=rocking?side*Math.sin(t*.65)*.14:0;
     royalSprite(s,passenger,side*13,6,.76,t,sway);
     line([[side*5,8],[side*12,12]],'#a4bd74',3);
   }
   if(p.food)snackPlate((passenger?-side:side)*12,7,.83);
   if(p.spareWheel){
     spareWheel(side*15,6,.89);
     line([[side*7,8],[side*12,10]],'#a4bd74',3);
   }
   box(-13,20,26,14,'#454641',ink,4,1);
   c.fillStyle='#fff9e8';c.textAlign='center';c.textBaseline='middle';c.font='bold 10px '+pixelFont;c.fillText(id.toUpperCase(),0,27);
 });
}
function actorsOnFloor(s,floor,localPlayers){
 const actors=[];
 if(floor===DECK_FLOOR){
   for(const enemy of s.enemies){
     const strike=enemyKnockback(enemy)?null:
       cavalryStrikePose(enemy.x+CAVALRY_ART_X,enemy.y,enemy.strikeTarget,s.tick-(enemy.lastStrike??-Infinity));
     actors.push({y:enemy.y+(strike?.bodyY??0),enemy,strike});
   }
 }
 for(const royal of s.royals){
   const {state}=royal;
   if(playerFloor(royal)!==floor||(state!=='seated'&&state!=='wandering'&&state!=='fallen'))continue;
   const y=state==='seated'?ROYAL_SEATS[royal.id].y:royal.y;
   actors.push({y:s.y+y,royal});
 }
 for(const [id,player] of Object.entries(s.players)){
   if(playerFloor(player)===floor)actors.push({y:s.y+player.y,id});
 }
 // Canvas Y grows downward. On deck the lower world-Y actor is drawn last.
 return actors.sort((a,b)=>{
   const depth=floor===DECK_FLOOR?a.y-b.y:b.y-a.y;
   if(depth)return depth;
   if(a.enemy&&b.enemy)return b.enemy.x-a.enemy.x;
   return Number(Boolean(b.enemy))-Number(Boolean(a.enemy))
     ||Number(localPlayers.includes(a.id))-Number(localPlayers.includes(b.id));
 });
}
function drawActors(s,t,floor,offset,recordPosition,carriedRoyals,cavalryFrame){
 const showCrew=!view.role||view.role==='driver';
 for(const {enemy,strike,royal,id} of actorsOnFloor(s,floor,view.localPlayers)){
   if(enemy){
     drawEnemy(enemy,s.tick,t,cavalryFrame,strike,s.mode==='run');
   }else if(royal){
     at(offset.x,offset.y,1,()=>drawRoyal(s,royal,t));
   }else{
     const passenger=carriedRoyals.find(child=>child.carrier===id);
     if(showCrew||id===view.id||passenger)at(offset.x,offset.y,1,()=>crewSprite(s,id,t,passenger,recordPosition));
   }
 }
}
function drawRoyalAttentionHearts(s,carriage){
 if(!view.royalHearts.length)return;
 const {a,d,e,f}=c.getTransform();
 for(const {heart,x,y} of view.royalHearts){
   const age=(s.tick-heart.tick)/(heart.until-heart.tick);
   const travel=reducedMotion?.matches?1:Math.min(1,age/.65);
   const fromX=carriage.x+heart.fromX,fromY=carriage.y+heart.fromY-12;
   const targetX=(x-e)/a,targetY=(y-f)/d;
   const heartX=fromX+(targetX-fromX)*travel;
   const heartY=fromY+(targetY-fromY)*travel-15*Math.sin(travel*Math.PI);
   at(heartX,heartY,1,()=>{
     c.globalAlpha=Math.min(1,(1-age)*5);
     drawPixelHeart();
   });
 }
}
function drawStairs(floor){
 const {x,y,r}=STAIRS,below=floor===CELLAR_FLOOR,dir=below?-1:1;
 const stairs=below&&spriteReady(art.cellar_stairs)?art.cellar_stairs:art.bottom_cellar;
 if(!sprite(c,stairs,x,y,r*2+6)){
   box(x-r-3,y-r-3,r*2+6,r*2+6,below?'#211b1b':'#745237',ink,5,2);
   box(x-r,y-r,r*2,r*2,below?'#2e2523':'#48382b',below?'#c6a36f':'#e9c891',4,1.8);
   for(let i=0;i<5;i++){
     const stepY=y+(i-2)*6;
     line([[x-r+4,stepY],[x+r-4,stepY]],below?'#816d55':'#a27e54',2.2);
   }
 }
 line([[x,y-dir*8],[x,y+dir*8]],'#ffe6aa',3);
 line([[x-6,y+dir*2],[x,y+dir*8],[x+6,y+dir*2]],'#ffe6aa',3);
 if(below)drawStairLabel(x,y-r-10,'1층 계단','#755331');
}
function drawMountedWheel(x,y){
 at(x,y,1,()=>{
   box(-27,-11,54,22,'#514947',ink,5,3);
   for(let i=0;i<5;i++)line([[-21+i*10,-8],[-21+i*10,8]],'#83766c',2.4);
   box(-6,-7,12,14,'#ae976e',ink,3,1.6);oval(0,0,2,2,'#635d51');
 });
}
const wagonPixelScale=375/221;
const wheelWidth=35*wagonPixelScale;
const wheelTreadOffset=(29/2-1)*wagonPixelScale; // One source pixel up from the rim's lower edge.
const looseWheelDrawRadius=LOOSE_WHEEL_RADIUS*1.5;
const looseWheelTreadOffset=looseWheelDrawRadius-3;
const wheelExitBounds=[[0,22,35,23],[20,0,43,35],[54,8,37,37],[90,16,35,29]];
const wheelMountPlaybackRate=.85,wheelMountFrameTicks=2;

function mountedWheelY(side){return side*WHEEL_MOUNT_OFFSET+(side>0?0:2);}

function wheelMounts(s,side){
 const reduced=reducedMotion?.matches;
 const rolling=!reduced&&s.mode==='run'&&s.speed>0;
 const phase=rolling?operationElapsedTicks(s):0;
 const {broken,progress,mountedAt,mountedIndex=progress===1?broken:-1}=s.wheel;
 const equip=side<0?art.bottom_wheel_topequip:art.bottom_wheel_bottomequip;
 const equipAge=(s.tick-(mountedAt??-Infinity))*wheelMountPlaybackRate;
 const equipping=!reduced&&equipAge>=0&&equipAge<equip.length*wheelMountFrameTicks;
 for(let n=0;n<WHEELS.length;n++){
   const w=WHEELS[n],mount=side*WHEEL_MOUNT_OFFSET;
   if(Math.sign(w.y)!==side)continue;
   const attached=broken===n&&progress===1;
   if(wheelIsMounted(s,n)){
     const mounting=equipping&&mountedIndex===n;
     if(!mounting||!sprite(c,frame(equip,equipAge,wheelMountFrameTicks),w.x,mount-side*6,wheelWidth)){
       const y=mountedWheelY(side);
       if(!sprite(c,frame(art.bottom_wheel,phase,3),w.x,y,wheelWidth))drawMountedWheel(w.x,y);
     }
     if(side>0&&rolling&&!attached&&!mounting){
       sprite(c,frame(art.bottom_wheel_dust,phase+n,3),w.x+55,mount+20,63*wagonPixelScale);
     }
   }else{
     box(w.x-16,mount-side*10-3,32,6,'#92897d',ink,2,2);
   }
 }
 if(reduced)return;
 for(const wheel of s.looseWheels??[]){
   const w=WHEELS[wheel.mount],age=s.tick-wheel.launchTick;
   if(w&&Math.sign(w.y)===side&&age>=0&&age<8){
     sprite(c,frame(art.bottom_wheel_out_effect,age,2),w.x,side*WHEEL_MOUNT_OFFSET,82);
   }
 }
}
function drawLooseWheel(wheel,tick){
 const {x,y,vx,vy,angle,bounceTick,launchTick}=wheel;
 const radius=looseWheelDrawRadius,margin=radius*1.5;
 if(!inView(x,y,margin,margin+8,margin))return;
 const age=tick-(bounceTick??launchTick);
 const jolt=age>=0&&age<LOOSE_WHEEL_HOP_TICKS&&!reducedMotion?.matches;
 const lift=jolt?Math.sin(age*Math.PI/LOOSE_WHEEL_HOP_TICKS)*8:0;
 if(jolt){
   const speed=Math.max(1,Math.hypot(vx,vy));
   c.save();c.globalAlpha=(1-age/LOOSE_WHEEL_HOP_TICKS)*.65;
   for(let i=0;i<4;i++){
     const reach=9+age*(.5+i*.35),side=i%2?1:-1;
     oval(x-reach*vx/speed+side*i*2,y-reach*vy/speed+4,3+i*1.5,2+i*.5,'#bc9062');
   }
   c.restore();
 }
 oval(x+2,y+4,radius,radius*.4,'#67504468');
 const launchAge=tick-launchTick;
 if(!reducedMotion?.matches&&launchAge>=0&&launchAge<8){
   const index=Math.floor(launchAge/2),bounds=wheelExitBounds[index];
   if(sprite(c,art.bottom_wheel_out[index],x,y-lift,bounds[2]*radius/17.5,undefined,bounds))return;
 }
 spareWheel(x,y-lift,radius/12,angle);
}
function wheelRepairLabel(w,label,fill,stroke){
 const scale=cellarLabelScale(),labelX=w.x<500?w.x+31:w.x-31-48*scale;
 at(labelX,w.y,scale,()=>{
   box(0,-9,48,18,fill,stroke,4,1);
   c.fillStyle='#fff9e9';c.font='bold 10px '+pixelFont;c.textAlign='center';c.textBaseline='middle';c.fillText(label,24,0);
 });
}
function repairMark(s){
 if(!view.role)for(const n of s.wheel.pending??[]){
   const queued=WHEELS[n];
   if(!queued)continue;
   drawFloorPad(queued.x,queued.y,'#80746585','#a79884',1.6);
   wheelRepairLabel(queued,'수리 대기','#65594b','#41392f');
 }
 const w=WHEELS[s.wheel.broken];if(!w)return;
 const occupied=occupiedByAnother('repair');
 const edge=occupied?occupiedPadStyle.edge:'#355c52';
 const tool=occupied?occupiedPadStyle.edge:'#3c5e50';
 drawFloorPad(w.x,w.y,occupied?occupiedPadStyle.fill:'#88b6a0',edge,2.4);
 c.save();
 if(occupied)c.filter='grayscale(1)';
 const padDrawn=sprite(c,art.bottom_repair,w.x,w.y,floorPadWidth);
 c.restore();
 if(!padDrawn)at(w.x,w.y,1,()=>{line([[-7,8],[6,-5]],tool,5);line([[2,-10],[1,-5],[6,-2],[12,-6]],tool,4);});
 const hasSpare=view.localPlayers.some(id=>s.players[id].spareWheel);
 const label=s.wheel.progress?'핀 고정':hasSpare?'수리  !':'바퀴 필요';
 wheelRepairLabel(w,label,occupied?occupiedPadStyle.edge:'#3f6c5a',occupied?occupiedPadStyle.edge:'#365346');
}
const draftHorseColors={
 right:['#b17b4d','#cf9960','#795139'],
 left:['#cba06d','#e3bb83','#835e42']
};
// Hoof bounds in each source frame: left, right, bottom.
const draftHorseHooves=[[30,84,77],[29,87,82],[23,101,83],[20,103,82],[14,101,82],[19,105,82],[39,87,81],[39,87,81]];
const draftHorseStillHooves=[25,97,76];
const whipEffectTicks=7;

function drawWhip(spot,x,y,sinceHit){
 const side=Math.sign(y-spot.y),fade=1-sinceHit/whipEffectTicks;
 c.save();
 c.globalAlpha=fade;
 c.beginPath();
 c.moveTo(spot.x-12,spot.y);
 c.bezierCurveTo(spot.x-44,spot.y-side*23*fade,x+35,y+side*32*fade,x,y);
 c.strokeStyle='#5e4036';c.lineWidth=4.8;c.stroke();
 c.strokeStyle='#ffe4a4';c.lineWidth=2;c.stroke();
 for(const [dx,dy] of [[-9,0],[-6,-6],[-6,6]]){
   line([[x+dx*.44,y+dy*.44],[x+dx,y+dy]],'#fff1c9',1.8);
 }
 c.restore();
}

function drawHorseSpeedTrail(x,y,t){
 for(let i=0;i<3;i++){
   const reach=mod(t*5+i*11,27),start=x+61+reach;
   const row=y+(i-1)*14;
   line([[start,row],[start+24,row]],'#a96a53a6',2);
 }
}

function spriteReady(image){return image?.complete&&image.naturalWidth>0;}
function anchoredSprite(image,x,y,scale,[pivotX,pivotY]){
 if(!spriteReady(image))return false;
 return sprite(c,image,x+(image.naturalWidth/2-pivotX)*scale,y+(image.naturalHeight/2-pivotY)*scale,
   image.naturalWidth*scale,image.naturalHeight*scale);
}
let hoofDustTexture;
function drawHoofDust(x,y,scale,tick,direction){
 if(reducedMotion?.matches)return;
 if(!hoofDustTexture){
   const smoke=art['cannon-muzzle-smoke'];
   if(!spriteReady(smoke))return;
   const texture=document.createElement('canvas');
   texture.width=smoke.naturalWidth;texture.height=smoke.naturalHeight;
   const tint=texture.getContext('2d');
   if(!tint)return;
   tint.drawImage(smoke,0,0);
   tint.globalCompositeOperation='source-atop';
   tint.fillStyle='#8d5f40';tint.globalAlpha=.76;
   tint.fillRect(0,0,texture.width,texture.height);
   hoofDustTexture=texture;
 }
 const lifetime=21,spacing=7;
 c.save();c.imageSmoothingEnabled=false;
 for(let i=0;i<3;i++){
   const progress=mod(tick+i*spacing,lifetime)/lifetime;
   const width=(13+progress*17)*scale,height=width*hoofDustTexture.height/hoofDustTexture.width;
   const drift=(3+progress*32)*scale*direction;
   c.globalAlpha=.68*(1-progress)**1.45;
   c.drawImage(hoofDustTexture,x+drift-width/2,y-progress*8*scale-height/2,width,height);
 }
 c.restore();
}
function drawHoofGround(left,top,scale,[front,back,bottom],depth,color,dustTick,dustDirection=1){
 const x=left+(front+back)*scale/2;
 const y=top+(bottom-1)*scale;
 const radius=(back-front)*scale/2+4;
 oval(x,y,radius,depth,color);
 oval(x,y,radius*.83,depth*.65,color);
 if(dustTick!=null)drawHoofDust(left+(dustDirection>0?back:front)*scale,y,scale,dustTick,dustDirection);
}

const draftHorseHitchPixels=[[55,36],[55,37],[55,39],[55,40],[53,41],[54,39],[53,36],[53,37]];
const draftHorseStillHitchPixel=[49,33];

function drawDraftHorseHitch(horseX,horseY,side){
 const railX=354,railY=DRAFT_HORSES[side].y+8;
 const width=84,height=17,hook={x:3,y:3},bolt={x:80,y:13};
 const span=bolt.x-hook.x;
 const scaleY=DRAFT_HORSE_SCALE;
 const scaleX=(railX-horseX)/span;
 const slope=(railY-horseY-(bolt.y-hook.y)*scaleY)/span;
 // Keep the bolt on the rail as the saddle moves.
 c.save();
 c.translate(railX,railY);
 c.transform(scaleX,slope,0,scaleY,0,0);
 const hitched=sprite(c,art.horse_equip,width/2-bolt.x,height/2-bolt.y,width,height);
 c.restore();
 if(!hitched){
   line([[horseX,horseY],[railX,railY]],'#5b5148',6);
   oval(railX,railY,4,4,'#b48654','#4c4744',1.5);
 }
}

function drawDraftHorses(s,t){
 const reduced=reducedMotion?.matches;
 const {phase}=horseRageStatus(s);
 const lastWhip=s.horseRage?.lastWhipTick;
 const sinceHit=lastWhip==null?Infinity:s.tick-lastWhip;
 // Distance keeps the gait continuous as rage changes the wagon speed.
 const {width:frameWidth,height:frameHeight,frames,framesPerMeter,offsetX,offsetY}=DRAFT_HORSE_WALK;
 const gait=draftGaitDistance(s)*framesPerMeter;
 const atlas=art['draft-horse-walk'],animated=spriteReady(atlas);
 const image=animated?atlas:art.horse1;
 const width=(animated?frameWidth:119)*DRAFT_HORSE_SCALE;
 const height=(animated?frameHeight:76)*DRAFT_HORSE_SCALE;
 for(const side of Object.keys(DRAFT_HORSES)){
   const {x,y}=draftHorseOrigin(s,side);
   const frameOffset=draftHorseFrameOffset(side);
   if(phase==='boost'&&!reduced)drawHorseSpeedTrail(x,y,t+frameOffset);
   const walkFrame=reduced||s.mode==='ready'?3:mod(Math.floor(gait)+frameOffset,frames);
   const crop=animated?[walkFrame*frameWidth,0,frameWidth,frameHeight]:undefined;
   const stride=(gait+frameOffset)*2*Math.PI/frames;
   const bob=animated||reduced||s.mode!=='run'?0:Math.sin(stride)*2.2;
   const drawnX=x+offsetX,drawnY=y+offsetY+bob;
   if(spriteReady(image)){
     const hooves=animated?draftHorseHooves[walkFrame]:draftHorseStillHooves;
     const dustTick=s.mode==='run'?operationElapsedTicks(s)+frameOffset*3:null;
     drawHoofGround(drawnX-width/2,drawnY-height/2,DRAFT_HORSE_SCALE,hooves,8,'#715f482d',dustTick);
   }else{
     oval(x+3,y+30,45,8,'#715f4850');
   }
   const [hitchPixelX,hitchPixelY]=animated?draftHorseHitchPixels[walkFrame]:draftHorseStillHitchPixel;
   const hitchX=drawnX-width/2+hitchPixelX*DRAFT_HORSE_SCALE;
   const hitchY=drawnY-height/2+hitchPixelY*DRAFT_HORSE_SCALE;
   const foreground=side==='left';
   if(foreground)drawDraftHorseHitch(hitchX,hitchY,side);
   if(!sprite(c,image,drawnX,drawnY,width,height,crop)){
     horse(x,y,DRAFT_HORSE_SCALE,draftHorseColors[side],stride,false);
   }
   if(!foreground)drawDraftHorseHitch(hitchX,hitchY,side);
   if(sinceHit>=0&&sinceHit<whipEffectTicks){
     const impact=horseBoardPoint(s,side);
     drawWhip(STATIONS.driver,impact.x,impact.y,sinceHit);
   }
 }
}

const cannonGatherTicks=HZ*.3,cannonSettleTicks=HZ*.4;
const cannonRestScale={x:1,y:1};
// The bore sits just inside the front edge of the barrel sprite.
const cannonMouthX=BARREL.length-4;

function cannonBarrelScale(tick,fuse,recoilAge){
 if(reducedMotion?.matches)return cannonRestScale;
 if(fuse.end){
   const elapsed=Math.max(0,Math.min(FUSE_TICKS,tick-fuse.end+FUSE_TICKS));
   const charge=elapsed/FUSE_TICKS;
   const lateTicks=Math.max(0,elapsed-(FUSE_TICKS-cannonGatherTicks));
   const late=lateTicks/cannonGatherTicks;
   const gather=late*late*(3-2*late);
   // Keep the pulse continuous when it speeds up for the final part of the fuse.
   const pulseTime=elapsed+lateTicks*.15;
   const pulse=(1-Math.cos(pulseTime*2*Math.PI/(HZ*.48)))*.5*(.4+.6*charge)*(1-gather);
   return {x:1+.075*charge+.075*gather+.018*pulse,y:1-.09*charge-.07*gather-.028*pulse};
 }
 if(recoilAge>=0&&recoilAge<cannonSettleTicks){
   const progress=recoilAge/cannonSettleTicks;
   return {x:1,y:1+.08*(1-progress)**2*Math.cos(2*Math.PI*progress)};
 }
 return cannonRestScale;
}

function renderedCannonMuzzle(mount,carriage,angle,caliber,barrelScale){
 const length=cannonMouthX*caliber*barrelScale.x;
 return {
   x:mount.x+carriage.x+Math.cos(angle)*length,
   y:mount.y+carriage.y+Math.sin(angle)*length,
 };
}

function drawCannonFuseFlame(s,x,y){
 const flame=frame(art.bottom_cannon_aim_count_fire,reducedMotion?.matches?0:s.tick,3);
 return sprite(c,flame,x,y-8,8,16);
}

function drawCannonBody(angle,barrelScale){
 if(!sprite(c,art.bottom_cannonbar,-3,3,32))oval(-2,0,19,19,'#766b64',ink,2.8);
 c.save();
 c.rotate(angle);
 c.scale(barrelScale.x,barrelScale.y);
 const barrelWidth=78;
 if(!sprite(c,art.bottom_cannonshot,BARREL.length-barrelWidth/2,0,barrelWidth)){
   poly([[-14,-10],[27,-8],[31,-12],[BARREL.length,-12],[BARREL.length,12],[31,12],[27,8],[-14,10]],'#77818a',ink,3);
   line([[-7,-5],[24,-4]],'#b9c0be',2.8);line([[34,-8],[34,8]],'#465561',2.5);
   c.fillStyle='#202126';
   c.fillRect(cannonMouthX-2,-5,4,10);
   c.fillRect(cannonMouthX-1,-7,2,14);
 }
 c.restore();
}

function drawWagonCannon(s,{mount,scale,age,recoil,aim,fuse},caliber){
 const angle=age>=0&&age<cannonSettleTicks?recoil.angle:aim.angle;
 at(mount.x,mount.y,caliber,()=>{
   drawCannonBody(angle,scale);
   if(fuse.end&&!drawCannonFuseFlame(s,-14,-18)){
     line([[-8,-9],[-14,-19]],'#6a4537',2);
     oval(-14,-20,4,4,s.tick%6<3?'#ffe29a':'#f0833e');
   }
 });
}

const wagonLampWidth=52;
const lowerLampCrop=[0,0,37,32];
// The lamp's foot is behind the inner edge of the lower wall at y=67.
const lowerLampY=67-wagonLampWidth*lowerLampCrop[3]/lowerLampCrop[2]/2;
const unlitLampGlass=new Map([
 [0xffffff,0x625c53],[0xffe058,0x565047],[0xffd000,0x49443d],
 [0xff8c00,0x3f3933],[0xff5100,0x332e29],[0x614c16,0x302b25],
]);
let unlitWagonLamp;
function drawWagonLamp(image,x,y,crop){
 if(image){
   sprite(c,image,x,y,wagonLampWidth,undefined,crop);
   return;
 }
 if(!unlitWagonLamp){
   const neutral=art.bottom_light[3];
   if(!spriteReady(neutral))return;
   const texture=document.createElement('canvas');
   texture.width=neutral.naturalWidth;texture.height=neutral.naturalHeight;
   const lamp=texture.getContext('2d');
   if(!lamp)return;
   lamp.drawImage(neutral,0,0);
   // Darken the glass on the halo-free frame and keep the metal housing intact.
   const pixels=lamp.getImageData(0,0,texture.width,texture.height),data=pixels.data;
   for(let i=0;i<data.length;i+=4){
     const shade=unlitLampGlass.get(data[i]<<16|data[i+1]<<8|data[i+2]);
     if(shade===undefined)continue;
     data[i]=shade>>16;data[i+1]=(shade>>8)&255;data[i+2]=shade&255;
   }
   lamp.putImageData(pixels,0,0);
   unlitWagonLamp=texture;
 }
 const [sx,sy,sw,sh]=crop??[0,0,unlitWagonLamp.width,unlitWagonLamp.height];
 const height=wagonLampWidth*sh/sw;
 c.save();c.imageSmoothingEnabled=false;
 c.drawImage(unlitWagonLamp,sx,sy,sw,sh,x-wagonLampWidth/2,y-height/2,wagonLampWidth,height);
 c.restore();
}
function drawWagonLamps(s){
 const running=s.mode==='run';
 const boosting=running&&horseRageStatus(s).phase==='boost';
 const direction=running?s.wagonMoveY:0;
 const startedAt=boosting?s.horseRage.lastWhipTick:s.wagonMoveSince;
 const elapsed=Math.max(0,s.tick-(startedAt??0)),steady=reducedMotion?.matches;
 const lit=steady?art.bottom_light[3]:elapsed%16<8?frame(art.bottom_light,elapsed,2):null;
 drawWagonLamp(boosting||direction<0?lit:null,370,-89);
 drawWagonLamp(boosting||direction>0?lit:null,373,lowerLampY,lowerLampCrop);
}
function wagon(s,t,offset,cannons,caliber){
 at(offset.x,offset.y,1,()=>{
   const phase=reducedMotion?.matches?0:s.mode==='ready'?t*12:operationElapsedTicks(s);
   box(345,-89,376,204,'#715d484e',null,20);
   wheelMounts(s,-1);
   if(!sprite(c,art.bottom,527.5,.5,375,199))box(340,-99,375,199,'#cc9f66','#614b37',8,3);
   wheelMounts(s,1);
   sprite(c,art.bottom_paper,374,-62,28);
   drawWagonLamps(s);
   sprite(c,frame(art.bottom_flag,phase,5),614,-126,35*wagonPixelScale);
   const seatLift=royalSeatLift(s);
   if(!sprite(c,art.bottom_sofa,529.5,-86-seatLift,103))box(479,-108-seatLift,101,46,'#8b6c9b',ink,5,2.5);
   drawRackPile(s.ammo.rack);
   for(const role of ['driver','rack'])floorStation(s,role);
   for(const {index,role} of cannons)floorStation(s,role,index);
   telescopeMark(s);
   drawStairs(DECK_FLOOR);
   const deckPlayers=view.localPlayers.map(id=>s.players[id]).filter(p=>playerFloor(p)===DECK_FLOOR);
   const feeders=deckPlayers.filter(p=>p.food&&!p.royal);
   const attentionTargets=s.mode==='run'&&!feeders.length?deckPlayers
     .filter(p=>stationAt(p,s)==='royal-attention').map(p=>royalActionTarget(s,p,'royal-attention')):[];
   for(const royal of s.royals){
     if(royal.state==='carried'&&!royal.upset&&view.localPlayers.includes(royal.carrier))returnMark(ROYAL_SEATS[royal.id]);
     if(royal.state!=='seated')continue;
     if(feeders.length)feedMark(ROYAL_SEATS[royal.id],feeders.some(p=>feedTarget(s,p)===royal));
     else if(100-royal.attention>10)attentionMark(royal,attentionTargets.includes(royal)&&canGiveRoyalAttention(s,royal));
   }
   for(let index=cannons.length-1;index>=0;index--)drawWagonCannon(s,cannons[index],caliber);
 });
}
function cellarLantern(x,y,t){
 const glow=c.createRadialGradient(x,y,4,x,y,91);
 glow.addColorStop(0,'#ffc56a56');
 glow.addColorStop(.45,'#e8a25130');
 glow.addColorStop(1,'#e8a25100');
 oval(x,y,91,91,glow);
 box(x-8,y-7,16,14,'#59443a','#282225',3,2);
 box(x-5,y-4,10,8,'#b67f4a','#674a33',2,1.4);
 oval(x,y,3,3,'#ffe0a4');
 oval(x,y,3+Math.sin(t*.7+x)*.35,2.5,'#ffeac050');
}
function drawFallingAmmo(s,floor,y){
 for(const ball of s.falling){
   if(playerFloor(ball)!==floor)continue;
   const progress=(s.tick-ball.tick)/31,below=floor===CELLAR_FLOOR;
   const x=below?Math.min(692,ball.x+14*progress):ball.x+35*progress;
   const drift=below?Math.min(10,81-Math.abs(ball.y)):123-Math.abs(ball.y);
   const base=y+ball.y+ball.side*drift*progress;
   oval(x+2,base+4,14,6,below?'#352c294f':'#745a4945');
   drawCargoShell(x,base-16*Math.sin(progress*Math.PI));
 }
}
function drawCellar(s,t,offset){
 c.fillStyle='#17191b';
 c.fillRect(view.left-1,view.top-1,view.span/view.zoom+2,view.height/view.zoom+2);
 at(offset.x,offset.y,1,()=>{
   if(!sprite(c,art.cellar,527.5,0,397,221)&&!sprite(c,art['wagon-cellar'],527.5,0,397,221)){
   box(340,-99,375,199,'#312725','#211e21',12,5);
   box(348,-91,359,183,'#4d3f37','#6e5444',7,2.5);
   for(let i=0;i<8;i++){
     const py=-87+i*22,seam=i%3*54;
     box(351,py,353,21,i%2?'#514338':'#59483a',null,1);
     line([[351,py+20],[704,py+20]],'#3c312d',1.2);
     for(const x of [370+seam,570+seam])line([[x,py],[x,py+20]],'#40332e',1.2);
   }
   c.save();c.beginPath();c.roundRect(349,-91,357,183,7);c.clip();
   cellarLantern(459,-78,t);
   cellarLantern(599,-78,t);
   c.restore();
   for(const rail of [-94,88]){
     box(347,rail,361,8,'#745b48','#322925',3,1.7);
     for(const x of [359,453,532,614,695])oval(x,rail+4,2.2,2.2,'#b0a08a','#5c4f48',.9);
   }
   for(const wheel of WHEELS){
     const side=Math.sign(wheel.y),mount=side*85;
     line([[wheel.x,mount],[wheel.x,wheel.y]],'#342e2d',9);
     box(wheel.x-18,mount-4,36,8,'#686564','#30292a',2,2);
     for(const sign of [-1,1])oval(wheel.x+sign*12,mount,2.1,2.1,'#c1ad91','#473b36',1);
   }
   }
   WHEELS.forEach(({x,y},index)=>{
     const side=Math.sign(y);
     const mount=art[wheelIsMounted(s,index)?'cellar_wheeleqip':'cellar_wheeleqip_broke'];
     at(x,y+side*13,1,()=>{
       c.scale(1,side<0?1:-1);
       sprite(c,mount,0,0,84);
     });
   });
   if(!view.role||view.role==='repair'||occupiedByAnother('repair'))repairMark(s);
   floorStation(s,'supply');
   floorStation(s,'cook');
   floorStation(s,'wheel-pickup');
   const {x:wheelX,y:wheelY}=STATIONS['wheel-pickup'];
   drawSpareWheelStack(wheelX-49,wheelY+14,2);
   drawSpareWheelStack(wheelX+49,wheelY+14,3);
   drawStairs(CELLAR_FLOOR);
   drawFallingAmmo(s,CELLAR_FLOOR,0);
 });
}
const {width:cavalryFrameWidth,height:cavalryFrameHeight,frames:cavalryRunCount,fps:cavalryRunFps,
 left:cavalryLeft,top:cavalryTop,scale:cavalryArtScale}=CAVALRY_RUN;
const cavalryScale=cavalryArtScale*CAVALRY_SIZE_FACTOR;
const cavalryHooves=[[62,136,172],[49,131,175],[41,156,175],[41,156,175],[35,155,178],[37,174,177],[64,154,177],[64,154,177]];
const cavalryStillHooves=[9,49,56];
// The spear, fallback art, and marker offsets use the original half-scale as their baseline.
const cavalryDetailScale=CAVALRY_SIZE_FACTOR/.5;
const knightScale=1.25*cavalryDetailScale;
const spearCrop=[0,28,85,13];
const spearGrip={x:-10*cavalryDetailScale,y:-3*cavalryDetailScale};
const spearGripPixel=49;
const spearReach=spearGripPixel*knightScale;
const spearHoldTicks=3,spearReturnTicks=9;
function loadCavalrySheet(filename){
 if(typeof Image!=='function')return null;
 const sprite=new Image();
 sprite.src=new URL(filename,import.meta.url).href;
 return sprite;
}
const cavalryRunSprite=loadCavalrySheet('./cavalry-run.png');
const cavalryHitSprite=loadCavalrySheet('./cavalry-death-hit.png');
const cavalryBlastSprite=loadCavalrySheet('./cavalry-death-blast.png');

function drawCavalryFrame(sprite,x,y,frame,tilt=0){
 if(!spriteReady(sprite))return false;
 // Align the horse's body with the existing collision shape.
 at(x,y,cavalryScale,()=>{
   if(tilt)c.rotate(tilt);
   c.imageSmoothingEnabled=false;
   c.drawImage(sprite,frame*cavalryFrameWidth,0,cavalryFrameWidth,cavalryFrameHeight,
     cavalryLeft,cavalryTop,cavalryFrameWidth,cavalryFrameHeight);
 });
 return true;
}
function cavalryStrikePose(x,y,target,age){
 if(age<0||age>=spearHoldTicks+spearReturnTicks)return null;
 if(reducedMotion?.matches&&age>=spearHoldTicks)return null;
 const retract=Math.max(0,age-spearHoldTicks)/spearReturnTicks;
 const extension=1-retract*retract*(3-2*retract);
 const dx=(target?.x??x-104)-(x+spearGrip.x);
 const dy=(target?.y??y+spearGrip.y)-(y+spearGrip.y);
 const angle=Math.atan2(-dy,-dx);
 // A longer spear can pull back to meet the same strike point.
 const travel=(Math.hypot(dx,dy)-spearReach)*extension;
 const thrustX=-Math.cos(angle)*travel,thrustY=-Math.sin(angle)*travel;
 const bodyShare=.65;
 const bodyX=thrustX*bodyShare*1.3,bodyY=thrustY*bodyShare;
 return {bodyX,bodyY,handX:thrustX-bodyX,handY:thrustY-bodyY,angle:angle*extension};
}
function drawKnight(x,y,kind,t,runFrame,tilt=0,strike=null){
  let painted=false;
  at(x,y,1,()=>{
    if(tilt)c.rotate(tilt);
    painted=drawCavalryFrame(cavalryRunSprite,0,0,runFrame)||sprite(c,art.enemy,-5*cavalryDetailScale,-11*cavalryDetailScale,60*knightScale);
    if(!painted)return;
    at(spearGrip.x+(strike?.handX??0),spearGrip.y+(strike?.handY??0),1,()=>{
      if(strike)c.rotate(strike.angle);
      const width=spearCrop[2]*knightScale;
      sprite(c,art.enemy_spear,width/2-spearReach,0,width,undefined,spearCrop);
    });
  });
  if(painted)return;
  at(x,y,DRAFT_HORSE_SCALE*CAVALRY_SIZE_FACTOR,()=>{
    c.rotate(tilt);
    horse(0,4,1,kind===1?['#b5825f','#ddac7e','#6c5144']:['#b8ab92','#d9ccaf','#86745b'],t*horseStrideRate+kind,false);
    poly([[-15,3],[-8,-11],[9,-11],[18,4],[11,12],[-9,11]],'#528ca0',ink,2.2);
    line([[-39,-10],[24,-15]],'#554b3c',3.5);
    poly([[-48,-10],[-37,-14],[-31,-12],[-38,-7]],'#c6d5ce',ink,1.7);
    box(-9,-17,18,18,'#a4b9bd',ink,5,2.2);box(-11,-10,18,5,'#687c87',ink,1,1.7);line([[-7,-9],[-7,-7]],'#dce1d9',1.3);
    poly([[-3,-18],[-1,-23],[7,-25],[16,-22],[8,-21],[4,-16]],'#719cb9',ink,1.6);
    poly([[1,3],[11,1],[17,5],[15,13],[9,17],[2,13]],'#689eb1',ink,2);line([[8,4],[9,12]],'#d0d9c4',2.2);
  });
}
function drawCavalryShadow(x,y,runFrame=4,dustTick,dustDirection){
 const scale=cavalryDetailScale,depth=4.5*scale;
 if(spriteReady(cavalryRunSprite)){
   drawHoofGround(x+cavalryLeft*cavalryScale,y+cavalryTop*cavalryScale,cavalryScale,cavalryHooves[runFrame],depth,'#55402b25',dustTick,dustDirection);
 }else if(spriteReady(art.enemy)){
   drawHoofGround(x-5*scale-30*knightScale,y-11*scale-28*knightScale,knightScale,cavalryStillHooves,depth,'#55402b25',dustTick,dustDirection);
 }else{
   oval(x-3*scale,y+20*scale,31*scale,5*scale,'#55402b35');
 }
}
function drawThrownKnight(enemy,knockback,tick){
 const {x,y,kind}=enemy;
 const age=Math.max(0,tick-knockback.tick);
 const progress=Math.min(1,age/WHEEL_KNOCK_TICKS);
 const lift=32*Math.sin(progress*Math.PI);
 const tilt=age/HZ*knockback.spin;
 c.save();
 c.globalAlpha=1-Math.max(0,progress-.7)/.3;
 drawCavalryShadow(x+CAVALRY_ART_X,y,0);
 drawKnight(x+CAVALRY_ART_X,y-lift,kind,0,0,tilt);
 c.restore();
}
function drawCannonDefeat(enemy,knockback,tick){
 const age=Math.max(0,tick-knockback.tick);
 if(age>=CAVALRY_DEATH_TICKS)return;
 const frame=Math.min(CAVALRY_DEATH_FRAME_COUNT-1,Math.floor(age/CAVALRY_DEATH_FRAME_TICKS));
 const fadeTicks=8;
 const x=enemy.x+CAVALRY_ART_X,y=enemy.y;
 const sprite=knockback.kind==='direct'?cavalryHitSprite:cavalryBlastSprite;
 c.save();
 c.globalAlpha=Math.min(1,(CAVALRY_DEATH_TICKS-age)/fadeTicks);
 drawCavalryShadow(x,y);
 if(!drawCavalryFrame(sprite,x,y,frame))drawKnight(x,y,enemy.kind,0,0);
 c.restore();
}
function drawEnemy(enemy,tick,t,cavalryFrame,strike,running){
 const {y,kind,id}=enemy,x=enemy.x+CAVALRY_ART_X;
 const detailScale=cavalryDetailScale;
 const knockback=enemyKnockback(enemy);
 if(knockback){
   if(inView(x,y,150*detailScale,182*detailScale,150*detailScale)){
     if(enemy.cannonKnock)drawCannonDefeat(enemy,knockback,tick);
     else drawThrownKnight(enemy,knockback,tick);
   }
   return;
 }
 if(!inView(x,y,124*detailScale,108*detailScale,65*detailScale))return;
 const sinceStrike=tick-(enemy.lastStrike??-Infinity);
 const dustSide=sinceStrike<=ENEMY_RETREAT_TICKS?-1:1;
 const dustTick=running?tick+id:null;
 const bodyX=x+(strike?.bodyX??0),bodyY=y+(strike?.bodyY??0);
 const runFrame=mod(cavalryFrame+id,cavalryRunCount);
 const nearMissLeft=(enemy.nearMissUntil??0)-tick;
 const startled=!enemy.spent&&nearMissLeft>0;
 const lean=startled&&!reducedMotion?.matches
   ? .14*Math.max(0,1-(CANNON_NEAR_MISS_TICKS-nearMissLeft)/8) : 0;
 drawCavalryShadow(bodyX,bodyY,runFrame,dustTick,dustSide);
 drawKnight(bodyX,bodyY,kind,t,runFrame,lean,strike);
 if(startled){
   c.save();
   c.textAlign='center';c.textBaseline='bottom';c.font='bold 22px '+pixelFont;
   c.strokeStyle='#ffefcf';c.lineWidth=3.5;
   c.fillStyle='#d5232d';
   const mark=c.measureText('!');
   const halfSpacing=(mark.actualBoundingBoxLeft+mark.actualBoundingBoxRight+2)/2;
   const markX=bodyX+8*detailScale,markY=bodyY-54*detailScale;
   c.strokeText('!',markX-halfSpacing,markY);
   c.strokeText('!',markX+halfSpacing,markY);
   c.fillText('!',markX-halfSpacing,markY);
   c.fillText('!',markX+halfSpacing,markY);
   c.restore();
 }
}
function drawStairLabel(x,y,title,fill){
 c.font='bold 10px '+pixelFont;
 const width=c.measureText(title).width+19;
 box(x-width/2,y-11,width,19,fill,'#353a35',4,1);poly([[x-3,y+8],[x+3,y+8],[x,y+12]],fill,null);
 c.fillStyle='#fff9e9';c.textAlign='center';c.textBaseline='middle';c.fillText(title,x,y-1);
}
const boulderOutline=[[-37,10],[-32,-15],[-16,-33],[12,-32],[31,-18],[39,7],[29,24],[-5,31],[-30,24]];
const boulderScale=2;
const catapultPixelScale=2.7;
const boulderDropHeight=364,boulderDropTicks=HZ*.4;
const boulderLandingAngle=-.24,boulderDropTurn=.32;
const landingMarkerScale=2;
const impactDust=['#b58a60','#d2ab7b','#e8c495'];
const dustColumnColors=['#9d754f','#c2976a','#ddb587'];
const reducedMotion=typeof window!=='undefined'&&window.matchMedia?.('(prefers-reduced-motion: reduce)');

function impactCamera(camera,age){
 if(!(age>=0&&age<13)||reducedMotion?.matches)return camera;
 const decay=(1-age/13)**2/camera.zoom;
 return {...camera,left:camera.left+Math.sin(age*1.9+.55)*9*decay,top:camera.top+Math.cos(age*1.7)*17*decay};
}
function cannonKick(recoil,age){
 const duration=HZ*.5;
 if(!(age>=0&&age<duration)||reducedMotion?.matches)return {x:0,y:0};
 const decay=(1-age/duration)**2;
 const push=18*Math.cos(age*.76)*decay;
 return {x:-Math.cos(recoil.angle)*push,y:-Math.sin(recoil.angle)*push+10*Math.cos(age*1.38)*decay};
}
const cannonExhaustTicks=24;
const muzzleSmokePuffs=[
 {delay:0,travel:74,rise:27,side:-10,size:37},
 {delay:2,travel:58,rise:18,side:12,size:30},
 {delay:4,travel:44,rise:22,side:-5,size:25},
 {delay:6,travel:32,rise:12,side:6,size:20},
];
function drawCannonBurst({age,mount,recoil,scale},carriage,caliber){
 if(!(age>=0&&age<cannonExhaustTicks))return;
 const {angle}=recoil;
 const settling=age<cannonSettleTicks;
 const muzzle=renderedCannonMuzzle(mount,carriage,angle,caliber,settling?scale:cannonRestScale);
 const dx=Math.cos(angle),dy=Math.sin(angle);
 at(muzzle.x,muzzle.y,caliber,()=>{
   for(const {delay,travel,rise,side,size} of muzzleSmokePuffs){
     if(age<delay)continue;
     const progress=(age-delay)/(cannonExhaustTicks-delay);
     const forward=travel*progress*(2-progress),lateral=side*progress;
     c.globalAlpha=.56*(1-progress)**1.3;
     sprite(c,art['cannon-muzzle-smoke'],dx*forward-dy*lateral,dy*forward+dx*lateral-rise*progress**2,12+size*progress);
   }
   if(!settling)return;
   c.globalAlpha=1;
   c.rotate(angle);
   const blast=frame(art.bottom_cannon_fier,reducedMotion?.matches?0:age,3);
   if(sprite(c,blast,53,-3,119)||age>=4)return;
   c.globalAlpha=1-age/4;
   poly([[-12,0],[-1,-10],[16,-14],[13,-7],[38,0],[13,7],[16,14],[-1,10]],'#ed803a','#ae502d',2);
   poly([[-6,0],[7,-6],[28,0],[7,6]],'#fff0b5',null);
   for(const side of [-1,1]){
     line([[8,side*9],[17+age*3,side*(14+age*4)]],'#ffbd62',2.5);
   }
 });
}
function drawShot(shot,tick){
 if(shot.enemy)return;
 const radius=cannonShotRadius(shot),trail=98*(shot.caliber??1);
 if(!inView(shot.x,shot.y,trail+4))return;
 const angle=Math.atan2(shot.vy,shot.vx);
 at(shot.x,shot.y,1,()=>{
   c.rotate(angle);
   const bomb=frame(art.bottom_cannon_bomb,reducedMotion?.matches?0:tick,3);
   const scale=radius/10;
   // The ball sits right of center in its source canvas.
   if(sprite(c,bomb,-4.5*scale,0,45*scale))return;
   const wake=c.createLinearGradient(-trail,0,0,0);
   wake.addColorStop(0,'#b74e2800');
   wake.addColorStop(.65,'#e1733aae');
   wake.addColorStop(1,'#ffd176ec');
   poly([[-trail,0],[-trail*.45,-radius*.48],[-radius*.55,-radius*.9],[0,radius*.76],[-trail*.36,radius*.44]],wake,null);
   line([[-trail*.76,0],[-radius*.35,0]],'#ffe1a3bd',radius*.25);
   oval(0,0,radius,radius,'#493b42','#39272d',Math.max(2,radius*.14));
   line([[radius*.3,-radius*.76],[radius*.71,-radius*.48],[radius*.86,-radius*.09]],'#ffb563',radius*.17);
   oval(radius*.16,-radius*.29,radius*.34,radius*.2,'#9b7881');
 });
}
const cannonWaveTicks=HZ*.64;
const cannonDust=Array.from({length:12},(_,i)=>({
 forward:.16+.88*hash(i+93),side:hash(i+121)*2-1,lift:12+hash(i+194)*25,
 size:5+hash(i+163)*11,
}));
const cannonSmoke=Array.from({length:9},(_,i)=>({
 delay:i*.041,drift:21+hash(i+216)*41,lift:48+hash(i+316)*44,
 spread:hash(i+336)*20-10,size:9+hash(i+416)*15,
}));
function cannonImpactInView(impact,x){
 return inView(x+impact.length/2,impact.y,impact.length/2+51*impact.caliber,Math.max(120*impact.caliber,impact.halfWidth+24),impact.halfWidth+22);
}
function drawCannonScorch(impact,x,age){
 if(!cannonImpactInView(impact,x))return;
 const y=impact.y,k=impact.caliber;
 c.save();
 c.globalAlpha=Math.min(.66,(1-age/CANNON_IMPACT_TICKS)*1.9);
 if(sprite(c,art['cannon-scorch'],x+10*k,y,94*k)){c.restore();return;}
 poly([[x-29*k,y+4*k],[x-19*k,y-6*k],[x-4*k,y-11*k],[x+24*k,y-9*k],
   [x+51*k,y-2*k],[x+33*k,y+7*k],[x+9*k,y+13*k],[x-18*k,y+10*k]],'#6e4d39',null);
 line([[x-18*k,y+2*k],[x-4*k,y-4*k],[x+19*k,y-5*k],[x+35*k,y-2*k]],'#453c32',4*k);
 c.restore();
}
function cannonWavePath(x,y,reach,shoulder,full){
 c.beginPath();
 c.moveTo(x,y-shoulder*.2);
 c.bezierCurveTo(x+reach*.22,y-shoulder*.82,x+reach*.46,y-shoulder*1.1,x+reach*.72,y-full*.76);
 c.quadraticCurveTo(x+reach,y-full*.66,x+reach,y);
 c.quadraticCurveTo(x+reach,y+full*.66,x+reach*.72,y+full*.76);
 c.bezierCurveTo(x+reach*.46,y+shoulder*1.1,x+reach*.22,y+shoulder*.82,x,y+shoulder*.2);
 c.closePath();
}
function drawCannonShockwave(impact,x,age){
 if(age>=cannonWaveTicks)return;
 const y=impact.y,progress=age/cannonWaveTicks,energy=1-progress;
 const brightness=Math.pow(energy,.65);
 const spread=.34+.66*(1-energy*energy);
 const reach=impact.length*spread;
 const full=impact.halfWidth*spread;
 const shoulder=Math.max(full*.58,cannonBlastWidth(impact,impact.length*.36)*spread);
 cannonWavePath(x,y,reach,shoulder,full);
 c.globalAlpha=brightness*.91;
 const heat=c.createLinearGradient(x,y,x+reach,y);
 heat.addColorStop(0,'#fff3c7f0');heat.addColorStop(.32,'#ffaf55ec');heat.addColorStop(1,'#e76e37b8');
 c.fillStyle=heat;c.fill();
 c.strokeStyle='#a95834';c.lineWidth=7*brightness+1;c.stroke();
 c.strokeStyle='#fff4cb';c.lineWidth=3.6*brightness+1;c.stroke();
 for(const offset of [.66,.82]){
   const edge=x+reach*offset;
   c.beginPath();c.moveTo(edge-reach*.21,y-full*.6);
   c.quadraticCurveTo(edge+reach*.08,y,edge-reach*.21,y+full*.6);
   c.globalAlpha=brightness*.94;c.strokeStyle='#ffe4a6';c.lineWidth=2+brightness*3;c.stroke();
 }
 if(age<24){
   const flame=frame(art.bottom_cannon_bomb_effect,reducedMotion?.matches?0:age,6);
   const width=impact.length*1.15;
   c.globalAlpha=1-age/30;
   if(!sprite(c,flame,x+width/2,y-width*.22,width)&&age<9){
     c.globalAlpha=Math.pow(1-age/9,.75);
     poly([[x-16,y],[x-2,y-17],[x+11,y-13],[x+40,y-23],[x+28,y-8],[x+76,y],[x+28,y+8],[x+40,y+23],[x+11,y+13],[x-2,y+14]],'#ef8f43','#af5637',2.4);
     poly([[x-9,y],[x+13,y-7],[x+39,y],[x+13,y+7]],'#fff4c2',null);
   }
 }
 const liftPhase=.67*Math.sin(progress*Math.PI);
 for(const {forward,side,lift,size} of cannonDust){
   const dx=reach*forward*.87,dy=side*full*.82;
   const px=x+dx,py=y+dy-lift*liftPhase;
   c.globalAlpha=energy*.61;
   oval(px,py,size*(.65+progress*.95),size*.48,side<0?'#cb9363':'#a97952');
   c.globalAlpha=energy*.9;
   poly([[px-7,py+1],[px+size*.48,py-4],[px+size*.7,py+2]],'#fce2a4',null);
 }
}
function drawCannonImpact(impact,x,age){
 if(!cannonImpactInView(impact,x))return;
 const y=impact.y,lifetime=age/CANNON_IMPACT_TICKS;
 c.save();
 drawCannonShockwave(impact,x,age);
 for(const {delay,drift,lift,spread,size} of cannonSmoke){
   const time=lifetime-delay;
   if(time<=0)continue;
   const sway=Math.sin(time*8+size)*9*time;
   const puff=size+time*19,px=x+spread+(drift*time+sway)*impact.caliber;
   const py=y-lift*time*impact.caliber;
   const opacity=Math.min(1,time*18)*(1-lifetime)**1.2;
   c.globalAlpha=opacity*.47;
   oval(px,py,puff,puff*.72,'#72594d');
   c.globalAlpha=opacity*.26;
   oval(px+puff*.22,py-puff*.24,puff*.72,puff*.56,'#b18b71');
 }
 c.restore();
}
function drawCannonAimFuse(s,fuse,x,y){
 const scale=2.5,width=32*scale;
 // The circle is two source pixels below the center of the sprite with its loose tip.
 const spriteY=y-2*scale,radius=13.5*scale;
 if(!fuse.end){
   sprite(c,art.bottom_cannon_aim_count,x,spriteY,width);
   return;
 }

 const remaining=Math.max(0,Math.min(1,(fuse.end-s.tick)/FUSE_TICKS));
 sprite(c,art.bottom_cannon_aim_count_bar,x,spriteY,width);
 if(!remaining)return;

 const top=-Math.PI/2,fullTurn=top+2*Math.PI;
 const burnAngle=fullTurn-remaining*2*Math.PI;
 c.save();
 c.beginPath();
 c.moveTo(x,y);
 c.arc(x,y,22*scale,burnAngle,fullTurn);
 c.closePath();
 c.clip();
 sprite(c,art.bottom_cannon_aim_count,x,spriteY,width);
 c.restore();
 drawCannonFuseFlame(s,x+Math.cos(burnAngle)*radius,y+Math.sin(burnAngle)*radius);
}

function drawCannonAim(s,carriage,{mount,aim,scale,fuse},caliber){
 const {x,y,angle}=aim;
 const muzzle=renderedCannonMuzzle(mount,carriage,angle,caliber,scale);
 const col=fuse.end?'#ffba76':'#fff1c9',rim='#513a35';
 c.save();
 c.setLineDash([5,7]);line([[muzzle.x,muzzle.y],[x,y]],rim,1.5);c.setLineDash([]);
 if(inView(x,y,0)){
   const crosshair=frame(art.bottom_cannon_aim,reducedMotion?.matches?0:s.tick,4);
   if(!sprite(c,crosshair,x,y,(crosshair?.naturalWidth??0)*22/12)){
     oval(x,y,8,8,'#00000000',rim,6.4);
     oval(x,y,8,8,'#00000000',col,2.7);
     for(const [ux,uy] of [[1,0],[-1,0],[0,1],[0,-1]]){
       const mark=[[x+ux*5,y+uy*5],[x+ux*12,y+uy*12]];
       line(mark,rim,5.6);line(mark,col,2.6);
     }
     oval(x,y,1.7,1.7,col);
   }
   drawCannonAimFuse(s,fuse,x,y);
 }else{
   const edgeX=Math.max(view.left+12,Math.min(view.right-12,x));
   const edgeY=Math.max(view.top+12,Math.min(view.bottom-12,y));
   at(edgeX,edgeY,1,()=>{
     c.rotate(Math.atan2(y-edgeY,x-edgeX));
     poly([[7,0],[-7,-5],[-4,0],[-7,5]],col,rim,1.7);
   });
 }
 c.restore();
}
const particleDirections=Array.from({length:7},(_,i)=>[Math.cos(i*.91),Math.sin(i*.91)]);
function drawParticles(particles){
 for(const {x,y,life,kind} of particles){
   const age=18-life;
   if(!inView(x,y,Math.abs(age)*1.7+8))continue;
   const fill=kind==='dust'?'#b09066':'#ec974e';
   for(const [dx,dy] of particleDirections){
     const px=x+dx*age*1.7,py=y+dy*age*1.5;
     poly([[px,py],[px+4,py-5],[px+6,py+2]],fill,null);
   }
 }
}
function drawBoulder(x,y,angle){
 at(x,y-31*(boulderScale-1),boulderScale,()=>{
   c.rotate(angle);
   if(sprite(c,art['catapult-rock'],0,3,93))return;
   poly(boulderOutline,'#787a70','#49483f',3);
   poly([[-32,-15],[-16,-33],[12,-32],[24,-19],[7,-12],[-16,-9]],'#a9aa95',null);
   poly([[7,-12],[24,-19],[31,-18],[39,7],[29,24],[7,27],[15,10]],'#5f625b',null);
   line([[-24,17],[-14,2],[-16,-9]],'#55584f',2);
   line([[7,-12],[-1,1],[3,9],[15,10]],'#50534a',2.2);
 });
}

function drawImpactDustColumn(x,y,age,seed){
 const rise=Math.min(1,age/.4);
 const height=20+150*(1-(1-rise)**3);
 const opacity=(1-age)**1.6;
 const count=14;
 for(let i=0;i<count;i++){
   const n=seed+360+i*5;
   const level=(i+.35+hash(n)*.4)/count;
   const drift=(hash(n+1)-.5)*(16+14*level)+Math.sin(level*7+age*5)*7*level;
   const puff=8+12*level+hash(n+2)*7+age*9;
   c.globalAlpha=opacity*(.54+hash(n+3)*.26);
   oval(x+drift,y-height*level,puff,puff*(.76+.18*level),dustColumnColors[i%3]);
 }
}
function drawImpactDust(x,y,age,seed){
 if(!inView(x,y,204,200,90))return;
 const spread=1-(1-age)**2;
 const opacity=(1-age)**1.3;
 const count=48;
 drawImpactDustColumn(x,y,age,seed);
 for(let i=0;i<count;i++){
   const n=seed+i*5,angle=(i+hash(n))*Math.PI*2/count;
   const reach=30+(68+hash(n+1)*60)*spread;
   const lift=(12+hash(n+2)*34)*Math.sin(age*Math.PI*.85);
   const puff=9+hash(n+3)*12+19*spread;
   c.globalAlpha=opacity*(.32+hash(n+4)*.25);
   oval(x+Math.cos(angle)*reach,y+Math.sin(angle)*reach*.38-lift,puff,puff*.57,impactDust[i%3]);
 }
 for(let i=0;i<12;i++){
   const n=seed+i*3+240,angle=(i+hash(n))*Math.PI/6;
   const reach=21+(80+hash(n+1)*59)*age;
   const lift=(18+hash(n+2)*47)*Math.sin(age*Math.PI);
   const size=2+hash(n)*4;
   const chipX=x+Math.cos(angle)*reach,chipY=y+Math.sin(angle)*reach*.44-lift;
   c.globalAlpha=opacity;
   poly([[chipX-size,chipY+1],[chipX,chipY-size],[chipX+size,chipY+2]],'#846f58',null);
 }
}

function drawCatapult(s,rock){
 if(s.tick<rock.launchedAt)return;
 const untilLanding=rock.landAt-s.tick;
 let warning;
 c.save();
 if(untilLanding>0){
   const locked=s.tick>=rock.lockAt;
   const approach=Math.min(1,(s.tick-rock.launchedAt)/(rock.lockAt-rock.launchedAt));
   const x=catapultAirX(s,rock);
   const rx=landingMarkerScale*(22+(CATAPULT_RADIUS.x-22)*approach);
   const ry=landingMarkerScale*(13+(CATAPULT_RADIUS.y-13)*approach);
   c.globalAlpha=.28+.24*approach;
   const hitsWagon=locked&&catapultHitsWagon(s,x,rock);
   const authoredMarker=hitsWagon&&spriteReady(art.bg_rock_danger);
   const landingArt=authoredMarker?art.bg_rock_danger:art[hitsWagon?'catapult-landing-danger':'catapult-landing-safe'];
   const markerWidth=rx*2.2,markerHeight=ry*2.2;
   const markerX=authoredMarker?x+markerWidth*.5/49:x;
   const markerY=authoredMarker?rock.y+markerHeight*.5/29:rock.y;
   if(!sprite(c,landingArt,markerX,markerY,markerWidth,markerHeight))oval(x,rock.y,rx,ry,'#47342b');
   if(locked){
     if(hitsWagon)warning={x,y:rock.y};
     if(untilLanding<=boulderDropTicks){
       const fall=1-untilLanding/boulderDropTicks;
       c.globalAlpha=Math.min(1,fall*4);
       const angle=boulderLandingAngle+boulderDropTurn*(1-fall);
       const y=rock.y-boulderDropHeight*(1-fall*fall);
       if(spriteReady(art.bg_rock1)){
         at(x,y,1,()=>{
           c.rotate(angle);
           anchoredSprite(art.bg_rock1,0,0,catapultPixelScale,[157.5,42]);
         });
       }else drawBoulder(x,y,angle);
     }
   }
 }else if(rock.hit!==undefined){
   const x=catapultX(s,rock),age=Math.min(1,-untilLanding/(HZ*.7));
   const frameIndex=Math.min(10,Math.floor(age*11));
   const impactFrame=[frameIndex%3*928,Math.floor(frameIndex/3)*606,928,606];
   c.globalAlpha=Math.min(1,(1-age)*3);
   const elapsed=-untilLanding/HZ;
   const paintedFrame=elapsed<.0667?2:elapsed<.2?3:elapsed<.3667?4:5;
   if(anchoredSprite(art['bg_rock'+paintedFrame],x,rock.y,catapultPixelScale,[71.75,133.5])){c.restore();return;}
   if(sprite(c,art['catapult-impact'],x,rock.y-39,464,303,impactFrame)){c.restore();return;}
   c.globalAlpha=Math.min(1,(1-age)*3);
   oval(x,rock.y+22,69,21,'#52433386');
   if(age<.55){
     const wave=age/.55;
     c.globalAlpha=(1-wave)*.88;
     oval(x,rock.y+17,46+110*wave,21+49*wave,'#00000000','#f9e2b9',5-3*wave);
   }
   c.globalAlpha=Math.min(1,(1-age)*3);
   drawBoulder(x,rock.y,boulderLandingAngle);
   drawImpactDust(x,rock.y+19,age,rock.launchedAt);
 }
 c.restore();
 return warning;
}
function drawEmoteBubbles(bubbles,left,right,bottom,cssX,cssY){
 if(!bubbles.length)return;
 c.save();
 c.setTransform(1/cssX,0,0,1/cssY,0,0);
 for(const {icon,x,y,scale} of bubbles){
   const iconSprite=emoteArt[icon];
   if(!spriteReady(iconSprite))continue;
   const radius=Math.max(12,Math.min(20,7+5*scale));
   const gap=Math.max(21,30*scale)+radius+4;
   const side=right-x>=gap+radius+2||right-x>=x-left?1:-1;
   const bubbleX=Math.max(left+radius+2,Math.min(right-radius-2,x+side*gap));
   const bubbleY=Math.max(radius+2,Math.min(bottom-radius-2,y-13*scale));
   c.save();c.translate(bubbleX,bubbleY);c.scale(side,1);
   const bubbleDrawn=sprite(c,art.EmoteBubble,0,3,radius*2.5);
   c.restore();
   if(!bubbleDrawn){
     const color='#fff4da',edge='#4c3d4a';
     poly([[bubbleX-side*radius*.8,bubbleY],[bubbleX-side*(radius+5),bubbleY+radius*.66],[bubbleX-side*radius*.35,bubbleY+radius*.58]],color,edge,1.5);
     oval(bubbleX,bubbleY,radius,radius,color,edge,2);
   }
   sprite(c,iconSprite,bubbleX,bubbleY,radius*1.75);
 }
 c.restore();
}
const royalRequestLines=[null,['하트를 주세요!'],['간식 주세요!'],['하트를 주세요!','간식 주세요!']];
function speechOverlap(a,b){
 const width=Math.max(0,Math.min(a.right,b.right+4)-Math.max(a.left,b.left-4));
 const height=Math.max(0,Math.min(a.bottom,b.bottom+4)-Math.max(a.top,b.top-4));
 return width*height;
}
function royalSpeechPosition(x,y,w,h,scale,side,left,right,bottom,occupied){
 const gap=Math.max(13,20*scale),sideX=x+side*gap-(side<0?w:0),otherX=x-side*gap-(side>0?w:0);
 const centerX=x-w/2;
 const candidates=[[centerX,y-gap-h],[sideX,y-h],[sideX,y-h/2],
   [otherX,y-h/2],[centerX,y+gap],[sideX,y+gap],[otherX,y+gap]];
 for(const area of occupied){
   candidates.push([centerX,area.top-h-9],[centerX,area.bottom+9]);
 }
 let best,bestOverlap=Infinity;
 for(const [desiredX,desiredY] of candidates){
   const leftEdge=Math.min(Math.max(left+4,desiredX),right-w-4);
   const topEdge=Math.min(Math.max(4,desiredY),bottom-h-4);
   const candidate={left:leftEdge,top:topEdge,right:leftEdge+w,bottom:topEdge+h};
   const overlap=occupied.reduce((sum,area)=>sum+speechOverlap(candidate,area),0);
   if(overlap<bestOverlap){best=candidate;bestOverlap=overlap;}
   if(!overlap)break;
 }
 return best;
}
function drawRoyalRequestBubbles(left,right,bottom,cssX,cssY,avoid){
 if(!view.royalRequestAnchors.length)return;
 const anchors=view.royalRequestAnchors.map(anchor=>({
   ...anchor,x:anchor.x*cssX,y:anchor.y*cssY,scale:anchor.scale*cssX,
 })).filter(({x,y})=>x>=left&&x<=right&&y>=0&&y<=bottom);
 const occupied=[...avoid,...anchors.map(({x,y,scale})=>({
   left:x-12*scale,top:y-12*scale,right:x+12*scale,bottom:y+12*scale,
 }))];
 c.save();
 c.setTransform(1/cssX,0,0,1/cssY,0,0);
 for(const {x,y,scale,needs,id} of anchors){
   const lines=royalRequestLines[needs];
   const font=Math.max(12,Math.min(16,Math.round(11+scale*3)));
   const lineHeight=font+2;
   c.font=`bold ${font}px ${pixelFont}`;
   const w=Math.min(right-left-8,Math.max(...lines.map(line=>c.measureText(line).width))+16);
   const h=lines.length*lineHeight+8;
   if(w<=12||bottom<h+8)continue;
   const side=id==='prince'?-1:1;
   const rect=royalSpeechPosition(x,y,w,h,scale,side,left,right,bottom,occupied);
   occupied.push(rect);
   const color='#fff4da',edge='#4c3d4a';
   if(x<rect.left||x>rect.right){
     const dir=x<rect.left?-1:1,tailX=dir<0?rect.left:rect.right;
     const tailY=Math.max(rect.top+7,Math.min(rect.bottom-7,y));
     const tip=Math.max(left+1,Math.min(right-1,tailX+dir*8));
     poly([[tailX,tailY-5],[tip,tailY],[tailX,tailY+5]],color,edge,1.4);
   }else{
     const dir=y<rect.top?-1:1,tailY=dir<0?rect.top:rect.bottom;
     const tailX=Math.max(rect.left+8,Math.min(rect.right-8,x));
     const tip=Math.max(1,Math.min(bottom-1,tailY+dir*8));
     poly([[tailX-5,tailY],[tailX,tip],[tailX+5,tailY]],color,edge,1.4);
   }
   box(rect.left,rect.top,w,h,color,edge,4,1.4);
   c.textAlign='center';c.textBaseline='middle';c.fillStyle='#3b2934';
   for(const [index,line] of lines.entries())c.fillText(line,rect.left+w/2,rect.top+4+lineHeight*(index+.5),w-10);
 }
 c.restore();
}
function drawApproachWarning(warning,left,right,bottom,cssX,cssY,edge,tools){
 if(!warning)return;
 const radius=2*Math.max(14,Math.min(22,bottom*.05));
 const width=right-left;
 const fromRight=edge==='right';
 const worldX=warning.x+(fromRight?CAVALRY_ART_X:0);
 const roadX=(worldX-view.left)*view.zoom*width/view.span;
 const roadY=(warning.y-view.top)*view.zoom*bottom/view.height;
 const inset=radius*1.4+4;
 const offset=Math.max(inset,Math.min(width*.28,fromRight?width-roadX:roadX));
 const x=fromRight?right-offset:left+offset;
 const minY=tools&&x+inset>tools.left?Math.min(bottom-inset,tools.bottom+inset):inset;
 const y=Math.max(minY,Math.min(bottom-inset,roadY));
 c.save();
 c.setTransform(1/cssX,0,0,1/cssY,0,0);
 at(x,y,radius/20,()=>{
   if(sprite(c,art.RoadWarning,0,0,43))return;
   poly([[0,-20],[20,14],[-20,14]],'#fff3d7','#ab2835',3);
   box(-2.1,-9,4.2,12,'#ad2533',null,1.8);
   oval(0,8,2.25,2.25,'#ad2533');
 });
 c.restore();
}
const comboTilt=20*Math.PI/180,comboSize=2.5,comboGap=8;
function drawCannonCombo(s,width,height,cssX,cssY,avoid,ranking){
 const combo=s.cannonCombo;
 if(!ranking||!(combo?.count>=2)||(s.mode!=='run'&&s.mode!=='paused'))return;
 const count='×'+combo.count;
 const age=Math.max(0,s.tick-combo.tick);
 const burst=reducedMotion?.matches?0:Math.max(0,1-age/(HZ*.42));
 c.save();
 c.setTransform(1/cssX,0,0,1/cssY,0,0);
 c.textAlign='left';c.textBaseline='middle';c.lineJoin='bevel';
 c.font=`bold 17px ${pixelFont}`;
 const labelWidth=c.measureText('COMBO').width;
 c.font=`bold 33px ${pixelFont}`;
 const countWidth=c.measureText(count).width;
 const half=(labelWidth+countWidth+35)/2;
 const sin=Math.sin(comboTilt),cos=Math.cos(comboTilt);
 const halfX=(half+9)*cos+28*sin,halfY=(half+9)*sin+28*cos;
 const pulse=1+.18*burst*burst;
 const left=Math.max(comboGap,ranking.left);
 const requested=comboSize*Math.min(1,width/(2*half+56),height/320);
 const maxWidth=(width-left-comboGap)/(2*halfX);
 const maxHeight=top=>(height-top-comboGap)/(2*halfY);
 let top=ranking.bottom+comboGap;
 for(const area of avoid){
   const room=Math.min(requested*pulse,maxWidth,maxHeight(top));
   if(area.right>left&&area.left<left+2*halfX*room&&area.bottom>top&&area.top<top+2*halfY*room
     &&maxHeight(area.bottom+comboGap)>.45)top=area.bottom+comboGap;
 }
 const scale=Math.min(requested*pulse,maxWidth,maxHeight(top));
 if(scale<=0){c.restore();return;}
 at(left+halfX*scale,top+halfY*scale,scale,()=>{
   c.rotate(-comboTilt);
   const shape=[[-half+12,-22],[half+6,-22],[half+6,-13],[half-8,22],[-half-6,22],[-half-6,13]];
   if(burst){
     c.save();c.globalAlpha=burst;
     const travel=(1-burst)*4;
     for(const side of [-1,1]){
       const x=side*(half+9+travel),y=-side*(13+travel*.45);
       c.fillStyle='#ffd782';c.fillRect(x-5,y-1,10,2);c.fillRect(x-1,y-5,2,10);
       c.fillStyle='#ffab76';c.fillRect(x+side*5-1,y+side*9-1,3,3);
     }
     c.restore();
   }
   poly(shape.map(([x,y])=>[x,y+4]),'#291726',null);
   poly(shape,'#392341','#231728',4);
   c.strokeStyle=burst?'#ffe6a2':'#c98959';c.lineWidth=1.6;c.stroke();
   line([[-half+16,-17],[half-4,-17]],'#fabb73',1.6);
   line([[-half+2,16],[half-12,16]],'#9d5366',1.6);
   c.font=`bold 17px ${pixelFont}`;
   c.fillStyle='#ffdb9b';c.fillText('COMBO',-half+13,1);
   c.font=`bold 33px ${pixelFont}`;
   c.lineWidth=2.8;c.strokeStyle='#3b1f31';
   const x=half-countWidth-10;
   c.fillStyle='#9d4e40';c.fillText(count,x,4);
   c.strokeText(count,x,0);
   c.fillStyle='#fff0ae';c.fillText(count,x,0);
 });
 c.restore();
}
export function render(canvas,s,wall,views,emoteIcons={},rect=canvas.getBoundingClientRect(),sceneScale=1,playToolsBounds=null,royalRequestAvoid=[],crewRankingBounds=null){
 // Bound only the game canvas backing buffer; DOM controls keep native screen resolution.
 const pixelScale=Math.sqrt(MAX_CANVAS_PIXELS/Math.max(1,rect.width*rect.height));
 const dpr=Math.min(2,window.devicePixelRatio||1,pixelScale)*sceneScale;
 const cw=Math.max(1,Math.round(rect.width*dpr)),ch=Math.max(1,Math.round(rect.height*dpr));
 if(canvas.width!==cw||canvas.height!==ch){canvas.width=cw;canvas.height=ch;}
 c=canvas.getContext('2d');c.setTransform(cw/views[0].screenWidth,0,0,ch/views[0].height,0,0);c.lineJoin='round';c.lineCap='round';
 const anchors={},catapultAlerts=[];
 const cssX=rect.width/cw,cssY=rect.height/ch;
 const viewCssX=rect.width/views[0].screenWidth,viewCssY=rect.height/views[0].height;
 const t=s.mode==='ready'?wall/470:operationElapsedTicks(s)*.37;
 const cavalryFrame=reducedMotion?.matches?0:Math.floor(s.tick*cavalryRunFps/HZ);
 const catapultRocks=activeCatapultRocks(s);
 const lastImpact=catapultRocks.reduce((tick,rock)=>rock.hit===undefined?tick:Math.max(tick,rock.landAt),-Infinity);
 const impactAge=Number.isFinite(lastImpact)?s.tick-lastImpact:-1;
 const carriedRoyals=s.royals.filter(royal=>royal.state==='carried');
 const stationOwners=new Map();
 for(const [id,p] of Object.entries(s.players)){
   const role=occupiedStation(p,s);
   if(role)stationOwners.set(isCannonRole(role)?cannonIndexFor(s,p):role,id);
 }
 const caliber=cannonScale(s);
 const cannons=CANNON_IDS.map(index=>{
   const {ammo,fuse,cannonRecoil:recoil}=cannonState(s,index);
   const age=recoil?s.tick-recoil.tick:-1;
   const role=cannonStation(s,index);
   const gunner=role==='gunner'?s.players[stationOwners.get(index)]:null;
   return {index,role,fuse,recoil,age,loaded:ammo.loaded,mount:cannonMount(s,index),
     aim:cannonAim(s,gunner,index),scale:cannonBarrelScale(s.tick,fuse,age)};
 });
 const kick={x:0,y:0};
 for(const {recoil,age} of cannons){
   const offset=cannonKick(recoil,age);
   kick.x+=offset.x;kick.y+=offset.y;
 }
 const carriage={x:kick.x,y:s.y+s.shake*Math.sin(t*3)*3+kick.y};
 const blasts=s.cannonImpacts?.length?s.cannonImpacts.map(impact=>({
   impact,x:impact.x+s.road-(impact.road??s.road),age:s.tick-impact.tick,
 })):null;
 for(const camera of views){
 view={...impactCamera(camera,impactAge),cssX:viewCssX,stationOwners,royalHearts:[],royalRequestAnchors:[]};
 if(kick.x||kick.y)view={...view,left:view.left-kick.x*.56/view.zoom,top:view.top-kick.y*.7/view.zoom};
 if(view.floor!==CELLAR_FLOOR)view.left=Math.max(view.left,ROAD_CAMERA_MIN_X);
 view.right=view.left+view.span/view.zoom;
 view.bottom=view.top+view.height/view.zoom;
 const left=view.start*viewCssX,right=(view.start+view.span)*viewCssX,top=0,bottom=view.height*viewCssY;
 const steadyScaleX=camera.zoom*viewCssX,steadyScaleY=camera.zoom*viewCssY;
 const steadyOriginX=left-camera.left*steadyScaleX,steadyOriginY=(s.y-camera.top)*steadyScaleY;
 const steadyScale=Math.max(steadyScaleX,steadyScaleY);
 const bubbles=[];
 const recordPosition=id=>{
   const local=view.localPlayers.includes(id),icon=emoteIcons[id];
   if(!local&&!icon)return;
   const {a,d,e,f}=c.getTransform();
   const x=e*cssX,y=f*cssY,scale=Math.max(a*cssX,d*cssY);
   if(x<left||x>right||y<top||y>bottom)return;
   if(local){
     // The timing prompt follows the crew through the unshaken camera.
     const player=s.players[id];
     const steadyX=steadyOriginX+player.x*steadyScaleX,steadyY=steadyOriginY+player.y*steadyScaleY;
     anchors[id]={x,y,scale,left,right,top,bottom,steadyX,steadyY,steadyScale};
   }
   if(icon)bubbles.push({icon,x,y,scale});
 };
 c.save();
 if(views.length!==1||view.start!==0||view.span!==view.screenWidth){
   c.beginPath();c.rect(view.start,0,view.span,view.height);c.clip();
 }
 c.translate(view.start,0);c.scale(view.zoom,view.zoom);c.translate(-view.left,-view.top);
 if(view.floor===CELLAR_FLOOR){
   drawCellar(s,t,carriage);
   drawActors(s,t,CELLAR_FLOOR,carriage,recordPosition,carriedRoyals,cavalryFrame);
   drawRoyalAttentionHearts(s,carriage);
   drawEmoteBubbles(bubbles,left,right,bottom,cssX,cssY);
   drawRoyalRequestBubbles(left,right,bottom,cssX,cssY,royalRequestAvoid);
   c.restore();
   continue;
 }
 landscape(s.road,s.stage);
 drawHoofprints(s,s.hoofprints);
 drawHoofprints(s,s.cavalryHoofprints);
 drawWheelTracks(s);
 for(const obstacle of s.obstacles)drawRoadObstacle(obstacle);
 if(blasts)for(const {impact,x,age} of blasts){
   if(age>=0&&age<CANNON_IMPACT_TICKS)drawCannonScorch(impact,x,age);
 }
 for(const wheel of s.looseWheels??[])drawLooseWheel(wheel,s.tick);
 wagon(s,t,carriage,cannons,caliber);
 drawActors(s,t,DECK_FLOOR,carriage,recordPosition,carriedRoyals,cavalryFrame);
 at(carriage.x,carriage.y,1,()=>drawDraftHorses(s,t));

 // Lower roadside plants overlap the wagon, with gameplay effects above them.
 if(!spriteReady(art.bg)){
   vergeArt(s.road,lowerVerge);
   if(!sceneryTile('lower-verge',s.road))drawVerge(s.road,lowerVerge);
 }

 for(const cannon of cannons)drawCannonBurst(cannon,carriage,caliber);
 for(const shot of s.shots)drawShot(shot,s.tick);
 if(blasts)for(const {impact,x,age} of blasts){
   if(age>=0&&age<CANNON_IMPACT_TICKS)drawCannonImpact(impact,x,age);
 }
 drawParticles(s.particles);
 drawRoyalAttentionHearts(s,carriage);
 drawFallingAmmo(s,DECK_FLOOR,s.y);
 for(const rock of catapultRocks){
   const warning=drawCatapult(s,rock);
   if(!warning)continue;
   const x=(warning.x-view.left)*steadyScaleX,y=(warning.y-view.top)*steadyScaleY;
   if(x>=0&&x<=right-left&&y>=0&&y<=bottom)catapultAlerts.push({left,width:right-left,x,y});
 }
 const focusedCannon=cannons[view.cannonIndex];
 if(view.role==='gunner'&&focusedCannon?.loaded&&!cannonAfterShot(s,s.players[view.id]))drawCannonAim(s,carriage,focusedCannon,caliber);
 drawApproachWarning(roadApproachWarning(s,camera),left,right,bottom,cssX,cssY,'left');
 if(!view.role||view.role==='driver'||view.role==='gunner'){
   drawApproachWarning(incomingPursuit(s,view),left,right,bottom,cssX,cssY,'right',playToolsBounds);
 }
 drawEmoteBubbles(bubbles,left,right,bottom,cssX,cssY);
 drawRoyalRequestBubbles(left,right,bottom,cssX,cssY,royalRequestAvoid);
 c.restore();
 }
 if(views.length===2){box(views[0].span-1.5,0,3,views[0].height,'#34382d');}
 drawCannonCombo(s,rect.width,rect.height,cssX,cssY,royalRequestAvoid,crewRankingBounds);
 return {anchors,catapultAlerts};
}
