import {LANES, ROAD_TOP, ROAD_BOTTOM, DECK_FLOOR, CELLAR_FLOOR, STAIRS, playerFloor, STATIONS, WHEELS, WHEEL_MOUNT_OFFSET, LOOSE_WHEEL_RADIUS, WHEEL_KNOCK_TICKS, BARREL, cannonAim, cannonScale, cannonShotRadius, cannonBlastWidth, cannonStation, cannonAfterShot, CANNON_IMPACT_TICKS, enemyKnockback, incomingPursuit, HZ, CATAPULT_RADIUS, catapultX, catapultAirX, catapultHitsWagon, ENEMY_RETREAT_TICKS, ENEMY_RECOVERY_TICKS, CAVALRY_ART_X, ROYAL_SEATS, ROYAL_RETURN_REACH, royalStatusNearby, royalTarget, feedTarget, stationAt, horseCondition, canLoad, canTakeSpareWheel,ROAD_CAMERA_MIN_X,ROAD_OBSTACLE_DRAW_MARGIN,roadApproachWarning} from './sim.mjs?v=zoom-speed61';
import {DRAFT_HORSES,DRAFT_HORSE_SCALE,HORSE_BODY,HORSE_NECK_AND_HEAD,draftHorseOrigin,horseBoardPoint,horseWhipTarget,whipAimPoint} from './horse-targets.mjs';
import {obstacleY, roadObstacleProfile, roadsideTrees} from './road-obstacles.mjs?v=loose-wheels34';
const ink='#393933';
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
  const key=tone%pineColors.length,colors=pineColors[key];
  if(pineSprites[key]===undefined)pineSprites[key]=createPineSprite(colors);
  const sprite=pineSprites[key];
  if(!sprite){
    at(x,y,size,()=>drawPineArt(colors));
    return;
  }
  const bounds=pineBounds;
  c.drawImage(sprite,x+bounds.x*size,y+bounds.y*size,bounds.width*size,bounds.height*size);
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
function landscape(scroll,stage){
  const {left,right,top,bottom}=view;
  // A focused camera wholly inside the road only sees the topmost road fill.
  if(top>ROAD_TOP+18&&bottom<ROAD_BOTTOM-20){
    box(left-2,top-2,right-left+4,bottom-top+4,'#ead09e');
  }else{
    box(left-2,top-2,right-left+4,bottom-top+4,['#92b492','#a1b1a0','#acaa90'][(stage-1)%3]);
    woodland(scroll,upperVerge);
    drawShoulder(scroll,upperVerge);
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
  for(const lane of [LANES[0],LANES[2]]){
    for(let j=0;j<12;j++){
      for(let x=firstSceneryTile(j*133+lane+scroll,1470,-120,56);x<=right+4;x+=1470){
        if(inView(x+26,lane,30,5))line([[x,lane],[x+24,lane],[x+30,lane-1],[x+53,lane-1]],'#d5b786',2);
      }
    }
  }
  for(const speck of roadSpecks){
    const {y,shade}=speck;
    for(let x=firstSceneryTile(speck.x+scroll,1450,-65,24);x<=right+24;x+=1450){
      if(!inView(x,y,24,6,8))continue;
      if(shade>.78){
        line([[x,y],[x+8+shade*8,y]],'#d4b887',2);
        line([[x+11,y+4],[x+19,y+4]],'#dfc291',1.5);
      }else oval(x,y,shade*2.4+.6,1.2,'#ccb185');
    }
  }
  drawVerge(scroll,upperVerge);
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
    const rx=obstacle.type==='rock'?34:22;
    oval(4,0,rx,30,'#78674644');
    if(obstacle.type==='log')fallenLog();
    else if(obstacle.type==='deer')woodlandDeer();
    else roadBoulder();
  });
}
function drawRoadsideTree({x,y,r}){
  if(!inView(x,y,48,93,14))return;
  pine(x,y-r,r/9,1);
  oval(x+2,y+2,r*1.2,r*.68,'#62644565');
  poly([
    [x-r,y+2],[x-r*.62,y-r],[x-r*.43,y-r*1.7],[x+r*.46,y-r*1.65],
    [x+r*.7,y-r],[x+r,y+1],[x+r*.53,y+r*.37],[x-r*.52,y+r*.4]
  ],'#886144','#503f35',2);
  line([[x-r*.3,y-r*1.34],[x-r*.45,y-r*.4],[x-r*.69,y]],'#c39463',2);
}
const horseStrideRate=2;
const rumpMarks=[[20,-8],[25,-5],[18,1],[30,0],[24,6],[32,-5],[17,7],[29,6]];
function horse(x,y,z,palette,t,wounds=0){
  const [coat,light,mane]=palette;
  at(x,y,z,()=>{oval(3,7,42,14,'#715f4850');const kick=Math.sin(t)*3.4;
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
    for(const [mx,my] of rumpMarks.slice(0,Math.ceil(wounds))){
      line([[mx-1,my-1],[mx+2,my+2]],'#713c37',1.9);
    }
    poly(HORSE_NECK_AND_HEAD,coat,ink,2.5);
    poly([[-28,-12],[-18,-14],[-9,-10],[-13,-3],[-21,-4],[-25,-7]],mane,ink,1.8);line([[-38,-8],[-37,5]],'#7b4c37',3);oval(-36,-4,1.8,1.8,ink);oval(-48,-1,1.25,1.25,ink);line([[-45,1],[-42,2]],light,1.5);});
}
function crewBody(color,ball,outline=0){
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
  if(ball){
    oval(12,5,7,7,'#454e5b',edge,1.8+outline);
    if(!outline)oval(10,2,2,1.5,'#a5b1b2');
  }
}
function snackPlate(x,y,scale=1){
 at(x,y,scale,()=>{
   oval(0,2,10,6,'#b27f59','#574034',1.7);
   oval(0,-1,8,5,'#ffe8af','#987055',1.2);
   box(-5,-5,10,6,'#e9a955','#895537',3,1.4);
   line([[-2,-4],[-3,-1],[2,-2],[3,-4]],'#fff0b5',1.15);
 });
}
const royalClothes={
 prince:{robe:'#84adcc',trim:'#405a78',hair:'#a46c48',seat:'#8fa9cd'},
 princess:{robe:'#d69ab8',trim:'#794461',hair:'#c6874c',seat:'#c79abb'}
};
function royalStatusVisible(s,royal){
 return view.localPlayers.some(id=>royalStatusNearby(s.players[id],royal));
}
function royalSprite(s,royal,x,y,scale,t,tilt=0){
 const colors=royalClothes[royal.id],seated=royal.state==='seated';
 const gait=royal.state==='wandering'?Math.sin(t*1.25+(royal.id==='prince'?0:2))*2:0;
 const showMood=(royal.bored||royal.crying)&&royalStatusVisible(s,royal);
 at(x,y,scale,()=>{
   if(royal.state!=='carried')oval(1,8,13,6,'#60473647');
   c.save();
   c.rotate(tilt);
   for(const side of [-1,1])box(side*4-2,(seated?5:9)+side*gait,5,4,colors.trim,ink,1.5,1.3);
   poly([[-6,-3],[6,-3],[9,8],[-9,8]],colors.robe,colors.trim,1.8);
   line([[-5,0],[5,5]],'#fff1be',2);
   oval(0,-7,9,8,colors.hair,ink,1.7);
   oval(0,-5,6.6,6,'#efc6a2',null);
   if(showMood&&royal.bored){
     line([[-5,-5],[-1.5,-5]],'#654337',1.25);
     line([[1.5,-5],[5,-5]],'#654337',1.25);
     line([[-2,-1],[2,-1]],'#773e39',1.25);
   }else if(showMood&&royal.crying){
     line([[-5,-5],[-2.2,-6],[0,-4.8],[2.2,-6],[5,-5]],'#654337',1.25);
     oval(0,-1,1.8,2.2,'#773e39');
     for(const side of [-1,1]){
       const tear=mod(t*.58+side*1.7,5);
       oval(side*(4.4+tear*.22),-2+tear,1.4,1.7,'#a7e1fa','#567caa',.6);
     }
   }else{
     oval(-2.3,-5,1,1,ink);oval(2.3,-5,1,1,ink);
   }
   poly([[-7,-11],[-8,-18],[-3,-15],[0,-20],[3,-15],[8,-18],[7,-11]],'#f0cf65','#645239',1.4);
   oval(0,-14,1.6,1.6,colors.trim);
   c.restore();
 });
}
function returnMark(seat){
 oval(seat.x,seat.dropY,ROYAL_RETURN_REACH,ROYAL_RETURN_REACH,'#fff2a936','#6f503c',3);
 c.save();
 c.setLineDash([4,4]);
 oval(seat.x,seat.dropY,ROYAL_RETURN_REACH-2,ROYAL_RETURN_REACH-2,'#00000000','#fff2a7',2);
 c.restore();
 poly([[seat.x-5,seat.dropY-2],[seat.x,seat.dropY-9],[seat.x+5,seat.dropY-2]],'#fff2a7','#6f503c',1.4);
}
function feedMark(seat,selected){
 const {x,dropY:y}=seat;
 oval(x,y,ROYAL_RETURN_REACH,ROYAL_RETURN_REACH,selected?'#ffdf875c':'#ffe3a327',selected?'#ffe1a2':'#d4ad72',selected?2.7:1.8);
 snackPlate(x,y,.82);
}
function looseRoyal(s,royal,t,reachable){
 const {x,y}=royal;
 const upset=royal.upset;
 if(royal.state==='fallen'){
   const side=royal.id==='prince'?-1:1,seat=ROYAL_SEATS[royal.id];
   const age=s.tick-(royal.fallAt??-Infinity);
   const progress=reducedMotion?.matches?1:Math.max(0,Math.min(1,age/9));
   const drawY=seat.y+(y-seat.y)*progress-9*Math.sin(Math.PI*progress);
   if(reachable)oval(x,y,19,13,upset?'#ffc7a85f':'#fff1b858',upset?'#ffc8a2':'#fff2a7',2.2);
   royalSprite(s,royal,x,drawY,1,t,side*1.24*progress);
   return;
 }
 if(reachable)oval(x,y,16,14,'#fff1b858','#fff2a7',2.2);
 royalSprite(s,royal,x,y,1,t);
 if(royal.mischief&&royalStatusVisible(s,royal)){
   oval(x+12,y-20,6,6,'#b66545','#5c3929',1.3);
   line([[x+12,y-23],[x+12,y-20]],'#fff1be',1.4);
   oval(x+12,y-17,1,1,'#fff1be');
 }
}
function drawLooseRoyalsOnFloor(s,t,floor){
 for(const royal of s.royals){
   if(playerFloor(royal)!==floor||(royal.state!=='wandering'&&royal.state!=='fallen'))continue;
   const reachable=view.localPlayers.some(id=>{
     const player=s.players[id];
     return stationAt(player,s)==='royal-pickup'&&royalTarget(s,player)===royal;
   });
   looseRoyal(s,royal,t,reachable);
 }
}
const stationArt={
 driver:['마부석','마','#bea5bd','#795d83'],
 rack:['적재칸','적','#80afc6','#3a6579'],cleaner:['포신 청소','청','#e9cd82','#80632d'],loader:['장전','장','#9bc69a','#476b49'],gunner:['포수','포','#dba476','#865c3d'],supply:['포탄 더미','탄','#92b6c5','#4d7080'],
 cook:['음식 제작대','','#c4a575','#765333'],
 'wheel-pickup':['바퀴 보관함','','#c0a16d','#705635']
};
function spareWheel(x,y,scale,angle){
 at(x,y,scale,()=>{
   if(angle!==undefined)c.rotate(angle);
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
const cellarLabelScale=()=>Math.max(1,.9/(view.zoom*view.cssX));
function floorStation(s,role){
 const onRoute=(view.role==='carry'||view.role==='catch')&&role==='rack';
 const focused=view.role&&view.role!=='driver';
 if(focused&&view.role!==role&&!onRoute)return;
 const spot=STATIONS[role],[name,mark,fill,edge]=stationArt[role],{x,y,r}=spot;
 const inactive=(role==='loader'&&!canLoad(s))||(role==='wheel-pickup'&&!canTakeSpareWheel(s));
 const border=inactive?'#897961':edge;
 oval(x,y,r,r,inactive?'#bda47e':fill,border,2.3);
 c.save();c.strokeStyle='#fff1d8';c.lineWidth=1.8;c.setLineDash([4,4]);c.beginPath();c.arc(x,y,r-4,0,Math.PI*2);c.stroke();c.restore();
 c.textAlign='center';c.textBaseline='middle';c.font='bold 16px system-ui';c.fillStyle=edge;c.fillText(mark,x,y);
 if(role==='supply'||role==='rack'){
   box(x-15,y-8,30,18,'#907050',ink,3);
   for(let i=0;i<Math.min(role==='rack'?s.ammo.rack:s.ammo.stock,5);i++)oval(x-8+(i%3)*8+(i>2?3:0),y+3-Math.floor(i/3)*7,4.8,4.8,'#565b66',ink,1.3);
 }else if(role==='cook'){
   box(x-16,y-10,32,20,'#986f45','#604329',3,2);
   line([[x-13,y+6],[x+13,y+6]],'#b8915a',1.5);
   snackPlate(x-6,y,.82);
   oval(x+8,y-1,5,5,'#7c6c60','#554636',1.3);
   oval(x+8,y-2,3.3,2.5,'#c58159');
 }else if(role==='wheel-pickup'){
   box(x-19,y-12,38,26,'#745434','#473729',3,2);
   box(x-16,y-8,32,18,'#463733','#9d7447',2,1.3);
   spareWheel(x-7,y,.66);
   spareWheel(x+7,y,.66);
   line([[x-15,y+10],[x+15,y+10]],'#b38a58',2);
 }
 if(focused&&!onRoute)return;
 if(role==='cook'){
   const scale=cellarLabelScale();
   at(x,y+r+15*scale,scale,()=>tag(0,0,name,false,border,true));
   return;
 }
 if(role==='wheel-pickup'){
   const scale=cellarLabelScale();
   at(x,y-r-12*scale,scale,()=>tag(0,0,name,false,border));
   return;
 }
 const labelY=y-r-5;
 box(x-23,labelY-6,46,13,border,ink,3,1);c.fillStyle='#fff8e9';c.font='bold 9px system-ui';c.fillText(role==='rack'?'적재 '+s.ammo.rack+'발':inactive?'장전 대기':name,x,labelY+.5);
}
function crewSprite(s,id,t,passenger,recordPosition){
 const p=s.players[id],color=({p1:'#aa7ed4',p2:'#dc9d5d',p3:'#7aa5c5',p4:'#88b995'})[id];
 const moving=Math.hypot(p.mx,p.my)>.15&&s.mode==='run';
 const bounce=moving?Math.sin(t*1.3)*1.8:0;
 oval(p.x+2,p.y+7,17,7,'#4d3e454a');
 const currentRole=stationAt(p,s);
 const aim=currentRole==='driver'?whipAimPoint(p):{x:p.x+p.ax,y:p.y+p.ay};
 const a=Math.atan2(aim.y-p.y,aim.x-p.x);
 c.save();c.translate(p.x,p.y);c.rotate(a);poly([[22,-5],[31,0],[22,5]],color,ink,1.4);c.restore();
 at(p.x,p.y+bounce,1,()=>{
   recordPosition(id);
   if(view.localPlayers.includes(id)){
     crewBody(color,p.ball,7/Math.sqrt(view.zoom));
   }
   crewBody(color,p.ball);
   if(passenger){
     const side=p.ax<0?-1:1;
     const rocking=currentRole==='royal-soothe'&&!passenger.bored&&moving&&!reducedMotion?.matches;
     const sway=rocking?side*Math.sin(t*.65)*.14:0;
     royalSprite(s,passenger,side*13,6,.76,t,sway);
     line([[side*5,8],[side*12,12]],'#a4bd74',3);
   }
   if(p.food)snackPlate(passenger&&p.ax>=0?-12:12,7,.83);
   if(p.spareWheel){
     const side=p.ax<0?-1:1;
     spareWheel(side*15,6,.89);
     line([[side*7,8],[side*12,10]],'#a4bd74',3);
   }
   box(-13,20,26,14,'#454641',ink,4,1);
   c.fillStyle='#fff9e8';c.textAlign='center';c.textBaseline='middle';c.font='bold 10px monospace';c.fillText(id.toUpperCase(),0,27);
 });
}
function drawCrewOnFloor(s,t,floor,recordPosition,carriedRoyals){
 const showCrew=!view.role||view.role==='driver';
 const visible=Object.keys(s.players).filter(id=>playerFloor(s.players[id])===floor
   &&(showCrew||id===view.id||carriedRoyals.some(child=>child.carrier===id)));
 // Draw local characters above the others when crew crowd the same station.
 visible.sort((a,b)=>Number(view.localPlayers.includes(a))-Number(view.localPlayers.includes(b))||s.players[a].y-s.players[b].y);
 for(const id of visible)crewSprite(s,id,t,carriedRoyals.find(child=>child.carrier===id),recordPosition);
}
function drawStairs(floor){
 const {x,y,r}=STAIRS,below=floor===CELLAR_FLOOR,dir=below?-1:1;
 box(x-r-3,y-r-3,r*2+6,r*2+6,below?'#211b1b':'#745237',ink,5,2);
 box(x-r,y-r,r*2,r*2,below?'#2e2523':'#48382b',below?'#c6a36f':'#e9c891',4,1.8);
 for(let i=0;i<5;i++){
   const stepY=y+(i-2)*6;
   line([[x-r+4,stepY],[x+r-4,stepY]],below?'#816d55':'#a27e54',2.2);
 }
 line([[x,y-dir*8],[x,y+dir*8]],'#ffe6aa',3);
 line([[x-6,y+dir*2],[x,y+dir*8],[x+6,y+dir*2]],'#ffe6aa',3);
 tag(x,y-r-10,below?'1층 계단':'지하층 계단',false,below?'#755331':'#69462f');
}
function drawMountedWheel(x,y){
 at(x,y,1,()=>{
   box(-27,-11,54,22,'#514947',ink,5,3);
   for(let i=0;i<5;i++)line([[-21+i*10,-8],[-21+i*10,8]],'#83766c',2.4);
   box(-6,-7,12,14,'#ae976e',ink,3,1.6);oval(0,0,2,2,'#635d51');
 });
}
function wheelMounts(s){
 for(let n=0;n<WHEELS.length;n++){
   const w=WHEELS[n],side=Math.sign(w.y),mount=side*WHEEL_MOUNT_OFFSET;
   if(s.wheel.broken!==n||s.wheel.progress===1)drawMountedWheel(w.x,mount);
   else box(w.x-19,mount-3,38,6,'#92897d',ink,2,2);
 }
}
function drawLooseWheel(wheel,tick){
 const {x,y,vx,vy,angle,bounceTick,launchTick}=wheel;
 const radius=LOOSE_WHEEL_RADIUS;
 if(x<view.left-radius||x>view.left+view.span/view.zoom+radius)return;
 if(y<view.top-40||y>view.bottom+40)return;
 const age=tick-(bounceTick??launchTick);
 const jolt=age>=0&&age<10&&!reducedMotion?.matches;
 const lift=jolt?Math.sin(age*Math.PI/10)*8:0;
 if(jolt){
   const speed=Math.max(1,Math.hypot(vx,vy));
   c.save();c.globalAlpha=(1-age/10)*.65;
   for(let i=0;i<4;i++){
     const reach=9+age*(.5+i*.35),side=i%2?1:-1;
     oval(x-reach*vx/speed+side*i*2,y-reach*vy/speed+4,3+i*1.5,2+i*.5,'#bc9062');
   }
   c.restore();
 }
 oval(x+2,y+4,radius,radius*.4,'#67504468');
 spareWheel(x,y-lift,radius/12,angle);
}
function repairMark(s){
 const w=WHEELS[s.wheel.broken];if(!w)return;
 const pulse=.5+.5*Math.sin(s.tick*.19);
 oval(w.x,w.y,27,24,'#88b6a0','#355c52',2.4);
 c.save();c.setLineDash([6,4]);c.strokeStyle=pulse>.5?'#f6ffda':'#d2ead3';c.lineWidth=2.8;
 c.beginPath();c.ellipse(w.x,w.y,23,20,0,0,2*Math.PI);c.stroke();c.restore();
 at(w.x,w.y,1,()=>{line([[-7,8],[6,-5]],'#3c5e50',5);line([[2,-10],[1,-5],[6,-2],[12,-6]],'#3c5e50',4);});
 const scale=cellarLabelScale(),labelX=w.x<500?w.x+31:w.x-31-48*scale;
 const hasSpare=view.localPlayers.some(id=>s.players[id].spareWheel);
 const label=s.wheel.progress?'핀 고정':hasSpare?'수리  !':'바퀴 필요';
 at(labelX,w.y,scale,()=>{
   box(0,-9,48,18,'#3f6c5a','#365346',4,1);
   c.fillStyle='#fff9e9';c.font='bold 10px system-ui,sans-serif';c.textAlign='center';c.textBaseline='middle';c.fillText(label,24,0);
 });
}
const draftHorseColors={
 right:['#b17b4d','#cf9960','#795139'],
 left:['#cba06d','#e3bb83','#835e42']
};
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

function drawDriverAim(s,p,spot){
 const {x,y}=whipAimPoint(p),target=horseWhipTarget(s,p);
 const ready=target?.zone==='rump'&&horseCondition(s,target.side)==='fresh';
 const color=ready?'#adffc2':target?'#ff9d86':'#fff2d2';
 c.save();c.setLineDash([4,6]);
 line([[spot.x-14,spot.y],[x,y]],'#49382d',3.1);
 line([[spot.x-14,spot.y],[x,y]],color,1.4);
 c.restore();
 oval(x,y,7,7,'#40352d38','#49382d',3.4);
 oval(x,y,7,7,'#00000000',color,1.7);
 for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]])line([[x+dx*7,y+dy*7],[x+dx*11,y+dy*11]],color,1.7);
 oval(x,y,1.4,1.4,color);
}

function drawRunawayTrail(x,y,t,side){
 const drift=side==='right'?1:-1;
 for(let i=0;i<3;i++){
   const reach=mod(t*5+i*11,27),start=x+61+reach;
   const row=y+(i-1)*14;
   line([[start,row],[start+24,row+drift*6]],'#a96a53a6',2);
 }
}

function drawDraftHorses(s,t){
 const spot=STATIONS.driver;
 for(const side of Object.keys(DRAFT_HORSES)){
   const {x,y}=draftHorseOrigin(s,side),wear=s.horseWear?.[side];
   const mood=horseCondition(s,side);
   let gait=t*horseStrideRate;
   if(mood==='tired')gait*=.52;
   if(!reducedMotion?.matches){
     if(mood==='runaway'){
       gait*=2.05;
       drawRunawayTrail(x,y,t,side);
     }else if(mood==='angry')gait*=1.4;
   }
   horse(x,y,DRAFT_HORSE_SCALE,draftHorseColors[side],gait+(side==='left'?1.4:0),wear?.wounds);
   if(mood==='runaway')tag(x,y-28,side==='right'?'폭주 ↑':'폭주 ↓',false,'#a03f39');
   if(mood==='tired')tag(x,y-28,'지침',false,'#685747');
   if(mood==='angry')tag(x,y-28,'놀람',false,'#a36b44');
   const sinceHit=s.tick-(wear?.lastHit??-Infinity);
   if(sinceHit>=0&&sinceHit<whipEffectTicks){
     const impact=horseBoardPoint(s,side,wear?.lastWhip??undefined);
     drawWhip(spot,impact.x,impact.y,sinceHit);
   }
 }
 if(view.role==='driver')drawDriverAim(s,s.players[view.id],spot);
}

function wagon(s,t,offset,recordPosition,carriedRoyals){
 at(offset.x,offset.y,1,()=>{
   for(const q of [-42,42]){
     line([[238,q],[292,q*.84],[355,q*.77]],'#514438',6);
     line([[238,q-1],[292,q*.84-1],[355,q*.77-1]],'#bb9261',2.2);
   }
   box(271,-4,83,9,'#b2865b',ink,3,2);
   box(345,-89,376,204,'#715d484e',null,20);wheelMounts(s);
   box(340,-99,375,199,'#92683f',ink,12,4);
   box(348,-92,359,183,'#cc9f66','#614b37',7,2.5);
   for(let k=0;k<8;k++){
     const py=-89+k*22;box(352,py,349,21,['#d1aa76','#dab37d','#cca06b'][k%3],null,1);
     line([[352,py+21],[701,py+21]],'#a47d57',1.7);
     for(let i=0;i<2;i++){
       const x=370+i*159+(k%3)*38;line([[x,py],[x,py+19]],'#af885e',1.5);
       line([[x+10,py+7],[x+34,py+7]],'#c19564',1.4);
     }
   }
   for(const yy of [-99,90]){
     box(345,yy,366,10,'#af8153',ink,3,2.3);
     for(const xx of [351,426,510,588,691]){box(xx,yy-1,8,12,'#848377',ink,2,1.4);oval(xx+4,yy+4,1.45,1.45,'#d1caba');}
   }
   for(const xx of [340,705])box(xx,-83,9,166,'#a47d50',ink,2,2);
   for(const wheel of WHEELS){
     const side=Math.sign(wheel.y);box(wheel.x-10,side*85-3,20,6,'#8e8473',ink,1.8,1.5);
     oval(wheel.x,side*85,2,2,'#cfbd9e');
   }
   for(const role of ['driver','rack',cannonStation(s)])floorStation(s,role);
   drawStairs(DECK_FLOOR);
   for(const yy of [-23,28]){box(353,yy,16,12,'#745237',ink,3);line([[360,yy+5],[324,yy+5],[282,yy*1.55],[250,yy*1.62]],'#72533b',2);}
   const feeders=view.localPlayers.map(id=>s.players[id]).filter(p=>p.food&&!p.royal&&playerFloor(p)===DECK_FLOOR);
   for(const royal of s.royals){
     if(royal.state==='carried'&&!royal.upset&&view.localPlayers.includes(royal.carrier))returnMark(ROYAL_SEATS[royal.id]);
     if(royal.state==='seated'&&feeders.length)feedMark(ROYAL_SEATS[royal.id],feeders.some(p=>feedTarget(s,p)===royal));
   }
   for(const xx of [478,575])box(xx,-92,7,38,'#9b7952',ink,2,2);
   box(485,-93,90,37,'#8b6c9b',ink,4,2.5);box(490,-89,80,27,'#b79bbb','#795d8b',3,1.6);
   for(const royal of s.royals){
     const {x,y}=ROYAL_SEATS[royal.id],colors=royalClothes[royal.id];
     box(x-15,y-12,30,25,colors.seat,colors.trim,4,1.5);
     if(royal.state==='seated')royalSprite(s,royal,x,y,.83,t);
   }
   line([[482,-57],[579,-57]],'#6f5345',4);
   line([[596,-82],[596,-124]],'#4e4038',4);
   poly([[596,-124],[615,-120],[634,-122],[626,-112],[634,-103],[615,-105],[596,-108]],'#ac80d7',ink,2.5);
   poly([[606,-114],[611,-118],[617,-113],[611,-109]],'#ebd8fc',null);
   const gunner=view.role==='gunner'?s.players[view.id]:Object.values(s.players).find(p=>stationAt(p,s)==='gunner');
   const recoil=s.cannonRecoil;
   const justFired=!s.ammo.loaded&&recoil&&s.tick-recoil.tick<4;
   const angle=justFired?recoil.angle:cannonAim(s,gunner).angle;
   at(BARREL.x,BARREL.y,cannonScale(s),()=>{
     oval(-2,0,19,19,'#766b64',ink,2.8);
     if(s.fuse.end){line([[-8,-9],[-14,-19]],'#6a4537',2);oval(-14,-20,4,4,s.tick%6<3?'#ffe29a':'#f0833e');}
     c.rotate(angle);
     poly([[-14,-10],[27,-8],[31,-12],[BARREL.length,-12],[BARREL.length,12],[31,12],[27,8],[-14,10]],'#77818a',ink,3);
     line([[-7,-5],[24,-4]],'#b9c0be',2.8);line([[34,-8],[34,8]],'#465561',2.5);
   });
   drawLooseRoyalsOnFloor(s,t,DECK_FLOOR);
   drawCrewOnFloor(s,t,DECK_FLOOR,recordPosition,carriedRoyals);
   drawDraftHorses(s,t);
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
   oval(x+2,base+4,7,3,below?'#352c294f':'#745a4945');
   oval(x,base-16*Math.sin(progress*Math.PI),6,6,'#4e5661',ink,1.4);
 }
}
function drawCellar(s,t,offset,recordPosition,carriedRoyals){
 c.fillStyle='#17191b';
 c.fillRect(view.left-1,view.top-1,view.span/view.zoom+2,view.height/view.zoom+2);
 at(offset.x,offset.y,1,()=>{
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
   if(!view.role||view.role==='repair')repairMark(s);
   floorStation(s,'supply');
   floorStation(s,'cook');
   floorStation(s,'wheel-pickup');
   drawStairs(CELLAR_FLOOR);
   drawFallingAmmo(s,CELLAR_FLOOR,0);
   drawLooseRoyalsOnFloor(s,t,CELLAR_FLOOR);
   drawCrewOnFloor(s,t,CELLAR_FLOOR,recordPosition,carriedRoyals);
 });
}
function knight(x,y,kind,t,jab,tilt=0){
  at(x,y,DRAFT_HORSE_SCALE,()=>{
    c.rotate(tilt);
    horse(0,4,1,kind===1?['#b5825f','#ddac7e','#6c5144']:['#b8ab92','#d9ccaf','#86745b'],t*horseStrideRate+kind);
    poly([[-15,3],[-8,-11],[9,-11],[18,4],[11,12],[-9,11]],'#528ca0',ink,2.2);
    line([[-39-jab,-10],[24-jab,-15]],'#554b3c',3.5);
    poly([[-48-jab,-10],[-37-jab,-14],[-31-jab,-12],[-38-jab,-7]],'#c6d5ce',ink,1.7);
    box(-9,-17,18,18,'#a4b9bd',ink,5,2.2);box(-11,-10,18,5,'#687c87',ink,1,1.7);line([[-7,-9],[-7,-7]],'#dce1d9',1.3);
    poly([[-3,-18],[-1,-23],[7,-25],[16,-22],[8,-21],[4,-16]],'#719cb9',ink,1.6);
    poly([[1,3],[11,1],[17,5],[15,13],[9,17],[2,13]],'#689eb1',ink,2);line([[8,4],[9,12]],'#d0d9c4',2.2);
  });
}
function drawThrownKnight(enemy,knockback,tick){
 const {x,y,kind}=enemy;
 const age=Math.max(0,tick-knockback.tick);
 const progress=Math.min(1,age/WHEEL_KNOCK_TICKS);
 const lift=32*Math.sin(progress*Math.PI);
 const tilt=age/HZ*knockback.spin;
 oval(x+CAVALRY_ART_X+4,y+10,43,12,'#68574654');
 c.save();
 c.globalAlpha=1-Math.max(0,progress-.7)/.3;
 knight(x+CAVALRY_ART_X,y-lift,kind,0,0,tilt);
 c.restore();
}
function tag(x,y,title,dot=false,fill='#383c38',pointUp=false){
  c.font='bold 10px system-ui, sans-serif';let w=c.measureText(title).width+19+(dot?9:0);
  const base=y+(pointUp?-11:8),tip=y+(pointUp?-15:12);
  box(x-w/2,y-11,w,19,fill,'#353a35',4,1);poly([[x-3,base],[x+3,base],[x,tip]],fill,null);
  if(dot)oval(x-w/2+9,y-1.5,2.8,2.8,'#bb92ff');
  c.fillStyle='#fff9e9';c.textAlign='center';c.textBaseline='middle';c.fillText(title,x+(dot?4:0),y-1);
}
const boulderOutline=[[-37,10],[-32,-15],[-16,-33],[12,-32],[31,-18],[39,7],[29,24],[-5,31],[-30,24]];
const boulderScale=2;
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
function drawCannonBurst(recoil,age){
 if(!(age>=0&&age<12))return;
 at(recoil.x,recoil.y,recoil.caliber??1,()=>{
   c.rotate(recoil.angle);
   const spread=age/12;
   c.globalAlpha=.55*(1-spread);
   for(let i=0;i<5;i++){
     const haze=hash(i+recoil.tick);
     const lateral=(i-2)*(4+12*spread);
     oval(4+(27+haze*22)*spread,lateral,8+12*spread,5+8*spread,i%2?'#a27d59':'#c89b66');
   }
   if(age>=4)return;
   c.globalAlpha=1-age/4;
   poly([[-12,0],[-1,-10],[16,-14],[13,-7],[38,0],[13,7],[16,14],[-1,10]],'#ed803a','#ae502d',2);
   poly([[-6,0],[7,-6],[28,0],[7,6]],'#fff0b5',null);
   for(const side of [-1,1]){
     line([[8,side*9],[17+age*3,side*(14+age*4)]],'#ffbd62',2.5);
   }
 });
}
function drawShot(shot){
 if(shot.enemy){
   if(!inView(shot.x,shot.y,18))return;
   const angle=Math.atan2(shot.vy,shot.vx);
   line([[shot.x-15*Math.cos(angle),shot.y-15*Math.sin(angle)],[shot.x,shot.y]],'#70554b',3);
   return;
 }
 const radius=cannonShotRadius(shot),trail=radius*7;
 if(!inView(shot.x,shot.y,trail+4))return;
 const angle=Math.atan2(shot.vy,shot.vx);
 at(shot.x,shot.y,1,()=>{
   c.rotate(angle);
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
 if(age<9){
   c.globalAlpha=Math.pow(1-age/9,.75);
   poly([[x-16,y],[x-2,y-17],[x+11,y-13],[x+40,y-23],[x+28,y-8],[x+76,y],[x+28,y+8],[x+40,y+23],[x+11,y+13],[x-2,y+14]],'#ef8f43','#af5637',2.4);
   poly([[x-9,y],[x+13,y-7],[x+39,y],[x+13,y+7]],'#fff4c2',null);
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
function drawCannonAim(s,carriage,aim){
 const {x,y,spreadX,spreadY,angle}=aim;
 const muzzle=BARREL.length*cannonScale(s);
 const startX=BARREL.x+carriage.x+Math.cos(angle)*muzzle;
 const startY=BARREL.y+carriage.y+Math.sin(angle)*muzzle;
 const col=s.fuse.end?'#ffba76':'#fff1c9',rim='#513a35';
 c.save();
 c.setLineDash([5,7]);line([[startX,startY],[x,y]],rim,1.5);c.setLineDash([]);
 if(inView(x,y,0)){
   c.setLineDash([3,5]);oval(x,y,spreadX,spreadY,'#c36a3533',rim,1.6);c.setLineDash([]);
   oval(x,y,8,8,'#563c3844',rim,3.2);
   oval(x,y,8,8,'#00000000',col,1.35);
   for(const [ux,uy] of [[1,0],[-1,0],[0,1],[0,-1]]){
     const mark=[[x+ux*5,y+uy*5],[x+ux*12,y+uy*12]];
     line(mark,rim,2.8);line(mark,col,1.3);
   }
   oval(x,y,1.7,1.7,col);
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
function drawBoulder(x,y,fall){
 at(x,y-31*(boulderScale-1),boulderScale,()=>{
   c.rotate((1-fall)*.46);
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

function drawCatapult(s,impactAge){
 const rock=s.catapult;
 if(!rock)return;
 const untilLanding=rock.landAt-s.tick;
 let warning;
 c.save();
 if(untilLanding>0){
   const locked=s.tick>=rock.lockAt;
   const approach=Math.min(1,(s.tick-rock.launchedAt)/(rock.lockAt-rock.launchedAt));
   const x=catapultAirX(s);
   const rx=landingMarkerScale*(22+(CATAPULT_RADIUS.x-22)*approach);
   const ry=landingMarkerScale*(13+(CATAPULT_RADIUS.y-13)*approach);
   c.globalAlpha=.28+.24*approach;
   oval(x,rock.y,rx,ry,'#47342b');
   if(locked){
     const dangerous=catapultHitsWagon(s,x);
     if(dangerous)warning={x,y:rock.y};
     c.globalAlpha=dangerous?1:.68+.22*Math.sin(s.tick*.33);
     c.setLineDash([5,4]);
     oval(x,rock.y,rx,ry,'#00000000',dangerous?'#ed3845':'#ffcf8c',3.2);
     c.setLineDash([]);
     const fall=1-untilLanding/(HZ*.6);
     if(fall>0){
       c.globalAlpha=Math.min(1,fall*4);
       drawBoulder(x,rock.y-182*(1-fall*fall),fall);
     }
   }
 }else if(impactAge>=0){
   const x=catapultX(s),age=Math.min(1,impactAge/(HZ*.7));
   c.globalAlpha=Math.min(1,(1-age)*3);
   oval(x,rock.y+22,69,21,'#52433386');
   if(age<.55){
     const wave=age/.55;
     c.globalAlpha=(1-wave)*.88;
     oval(x,rock.y+17,46+110*wave,21+49*wave,'#00000000','#f9e2b9',5-3*wave);
   }
   c.globalAlpha=Math.min(1,(1-age)*3);
   drawBoulder(x,rock.y,1);
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
   const radius=Math.max(12,Math.min(20,7+5*scale));
   const gap=Math.max(21,30*scale)+radius+4;
   const side=right-x>=gap+radius+2||right-x>=x-left?1:-1;
   const bubbleX=Math.max(left+radius+2,Math.min(right-radius-2,x+side*gap));
   const bubbleY=Math.max(radius+2,Math.min(bottom-radius-2,y-13*scale));
   const color='#fff4da',edge='#4c3d4a';
   poly([[bubbleX-side*radius*.8,bubbleY],[bubbleX-side*(radius+5),bubbleY+radius*.66],[bubbleX-side*radius*.35,bubbleY+radius*.58]],color,edge,1.5);
   oval(bubbleX,bubbleY,radius,radius,color,edge,2);
   c.font=Math.round(radius*1.45)+'px "Apple Color Emoji", "Segoe UI Emoji", "Noto Color Emoji", sans-serif';
   c.textAlign='center';c.textBaseline='middle';c.fillStyle='#302638';c.fillText(icon,bubbleX,bubbleY+1);
 }
 c.restore();
}
function drawApproachWarning(warning,tick,left,right,bottom,cssX,cssY,edge,tools){
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
 const pulse=reducedMotion?.matches?0:(1+Math.sin(tick*.19))/2;
 c.save();
 c.setTransform(1/cssX,0,0,1/cssY,0,0);
 at(x,y,radius/20,()=>{
   oval(0,1,23+5*pulse,22+5*pulse,'#b92f3c38');
   poly([[0,-20],[20,14],[-20,14]],'#fff3d7','#ab2835',3);
   box(-2.1,-9,4.2,12,'#ad2533',null,1.8);
   oval(0,8,2.25,2.25,'#ad2533');
 });
 c.restore();
}
export function render(canvas,s,wall,views,emoteIcons={},rect=canvas.getBoundingClientRect(),sceneScale=1,playToolsBounds=null){
 // Bound only the game canvas backing buffer; DOM controls keep native screen resolution.
 const pixelScale=Math.sqrt(MAX_CANVAS_PIXELS/Math.max(1,rect.width*rect.height));
 const dpr=Math.min(2,window.devicePixelRatio||1,pixelScale)*sceneScale;
 const cw=Math.max(1,Math.round(rect.width*dpr)),ch=Math.max(1,Math.round(rect.height*dpr));
 if(canvas.width!==cw||canvas.height!==ch){canvas.width=cw;canvas.height=ch;}
 c=canvas.getContext('2d');c.setTransform(cw/views[0].screenWidth,0,0,ch/views[0].height,0,0);c.lineJoin='round';c.lineCap='round';
 const anchors={},catapultAlerts=[];
 const cssX=rect.width/cw,cssY=rect.height/ch;
 const viewCssX=rect.width/views[0].screenWidth,viewCssY=rect.height/views[0].height;
 const t=s.mode==='ready'?wall/470:s.tick*.37;
 const rock=s.catapult;
 const impactAge=rock?.hit===undefined?-1:s.tick-rock.landAt;
 const recoil=s.cannonRecoil,recoilAge=recoil?s.tick-recoil.tick:-1;
 const kick=cannonKick(recoil,recoilAge);
 const carriage={x:kick.x,y:s.y+s.shake*Math.sin(t*3)*3+kick.y};
 const carriedRoyals=s.royals.filter(royal=>royal.state==='carried');
 const blasts=s.cannonImpacts?.length?s.cannonImpacts.map(impact=>({
   impact,x:impact.x+s.road-(impact.road??s.road),age:s.tick-impact.tick,
 })):null;
 let roadside,enemyOrder;
 for(const camera of views){
 view={...impactCamera(camera,impactAge),cssX:viewCssX};
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
   drawCellar(s,t,carriage,recordPosition,carriedRoyals);
   drawEmoteBubbles(bubbles,left,right,bottom,cssX,cssY);
   c.restore();
   continue;
 }
 landscape(s.road,s.stage);
 roadside??=roadsideTrees(s.road,ROAD_TOP,ROAD_BOTTOM);
 for(const tree of roadside)if(tree.y<LANES[1])drawRoadsideTree(tree);
 for(const obstacle of s.obstacles)drawRoadObstacle(obstacle);
 if(blasts)for(const {impact,x,age} of blasts){
   if(age>=0&&age<CANNON_IMPACT_TICKS)drawCannonScorch(impact,x,age);
 }
 enemyOrder??=[...s.enemies].sort((a,b)=>b.x-a.x);
 for(const e of enemyOrder){
   const x=e.x+CAVALRY_ART_X;
   const knockback=enemyKnockback(e);
   if(knockback){
     if(inView(x,e.y,125,154,125))drawThrownKnight(e,knockback,s.tick);
     continue;
   }
   if(!inView(x,e.y,120,46))continue;
   const sinceStrike=s.tick-(e.lastStrike??-Infinity);
   const dustSide=sinceStrike<=ENEMY_RETREAT_TICKS?-1:1;
   const dustCount=sinceStrike<=ENEMY_RETREAT_TICKS+ENEMY_RECOVERY_TICKS?2:4;
   for(let i=0;i<dustCount;i++){
     const q=mod(i*11+t*1.4,45);
     oval(x+dustSide*(59+q),e.y+20+Math.sin(i*4)*7,4+q*.09,3.2,'#f6dfb4');
   }
   const jab=16*Math.max(0,1-sinceStrike/7);
   knight(x,e.y,e.kind,t,jab);
 }
 for(const wheel of s.looseWheels??[])drawLooseWheel(wheel,s.tick);
 if(inView(777,s.y+19,54,20)){
   for(let i=0;i<8;i++){
     const q=mod(i*11+t*2.4,84);
     oval(735+q,s.y+19+Math.sin(i*7)*12,3.5+q*.053,2.7+q*.02,'#f7e1b6');
   }
 }
 wagon(s,t,carriage,recordPosition,carriedRoyals);

 // Lower roadside plants overlap the wagon, with gameplay effects above them.
 woodland(s.road,lowerVerge);
 drawShoulder(s.road,lowerVerge);
 drawVerge(s.road,lowerVerge);
 for(const tree of roadside)if(tree.y>LANES[1])drawRoadsideTree(tree);

 drawCannonBurst(recoil,recoilAge);
 for(const shot of s.shots)drawShot(shot);
 if(blasts)for(const {impact,x,age} of blasts){
   if(age>=0&&age<CANNON_IMPACT_TICKS)drawCannonImpact(impact,x,age);
 }
 drawParticles(s.particles);
 drawFallingAmmo(s,DECK_FLOOR,s.y);
 const warning=drawCatapult(s,impactAge);
 if(warning){
   const x=(warning.x-view.left)*steadyScaleX,y=(warning.y-view.top)*steadyScaleY;
   if(x>=0&&x<=right-left&&y>=0&&y<=bottom)catapultAlerts.push({left,width:right-left,x,y});
 }
 if(view.role==='gunner'&&s.ammo.loaded&&!cannonAfterShot(s,s.players[view.id]))drawCannonAim(s,carriage,cannonAim(s,s.players[view.id]));
 drawApproachWarning(roadApproachWarning(s,camera),s.tick,left,right,bottom,cssX,cssY,'left');
 if(!view.role||view.role==='driver'||view.role==='gunner'){
   drawApproachWarning(incomingPursuit(s,view),s.tick,left,right,bottom,cssX,cssY,'right',playToolsBounds);
 }
 drawEmoteBubbles(bubbles,left,right,bottom,cssX,cssY);
 c.restore();
 }
 if(views.length===2){box(views[0].span-1.5,0,3,views[0].height,'#34382d');}
 return {anchors,catapultAlerts};
}
