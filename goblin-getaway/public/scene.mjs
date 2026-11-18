import {LANES, ROAD_TOP, ROAD_BOTTOM, STATIONS, WHEELS, BARREL, cannonScale, HZ, CATAPULT_RADIUS, catapultX, catapultAirX, ENEMY_RETREAT_TICKS, ENEMY_RECOVERY_TICKS, ROYAL_SEATS, ROYAL_RETURN_REACH, royalTarget, stationAt, aimAngle, firingAngle, horseAim, horseCondition, canLoad, phase, target, width} from './sim.mjs?v=crew7';
import {obstacleY, roadObstacleProfile} from './road-obstacles.mjs';
const ink='#393933';
let c,view;
function poly(points,fill,edge=ink,lw=2.4){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.closePath();c.fillStyle=fill;c.fill();if(edge){c.lineWidth=lw;c.strokeStyle=edge;c.stroke();}}
function box(x,y,w,h,fill,edge=null,r=0,lw=2){c.beginPath();c.roundRect(x,y,w,h,r);c.fillStyle=fill;c.fill();if(edge){c.lineWidth=lw;c.strokeStyle=edge;c.stroke();}}
function oval(x,y,rx,ry,fill,edge=null,lw=2){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();if(edge){c.strokeStyle=edge;c.lineWidth=lw;c.stroke();}}
function line(points,col,width=2){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.strokeStyle=col;c.lineWidth=width;c.stroke();}
function at(x,y,z,fn){c.save();c.translate(x,y);c.scale(z,z);fn();c.restore();}
const mod=(n,d)=>((n%d)+d)%d;
function hash(n){const x=Math.sin(n*127.1+311.7)*43758.5453;return x-Math.floor(x);}
function pine(x,y,size,tone){
  const colors=[['#639775','#477e63','#89b38a'],['#598870','#40735f','#77a18a'],['#77a780','#53916d','#99bc91'],['#91b483','#6c9e79','#adca95']][tone%4];
  at(x,y,size,()=>{
    oval(6,5,23,7,'#446f5e66');box(-4,-6,8,12,'#887652','#416652',1,1.7);
    poly([[-25,-4],[-18,-17],[-21,-16],[-13,-31],[-17,-29],[0,-58],[17,-29],[13,-31],[22,-14],[17,-16],[26,-4],[14,-5],[8,-2],[-3,-5],[-13,-2]],colors[0],'#436d59',2);
    poly([[0,-55],[8,-30],[5,-33],[15,-16],[11,-18],[21,-6],[12,-7],[7,-4],[0,-6]],colors[1],null);
    line([[-12,-29],[-5,-32]],colors[2],2);line([[-18,-15],[-10,-18],[-6,-17]],colors[2],2);
  });
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
const BACKGROUND_SCROLL_MULTIPLIER = 5;
// Extend the roadside forest into the newly visible ground on taller displays.
function woodland(scroll,top,bottom,left,right){
  const spacing=68;
  for(let row=Math.floor((top-55)/spacing);row<=Math.ceil((bottom+70)/spacing);row++){
    const y=row*spacing;
    if(y>=ROAD_TOP-54&&y<=ROAD_BOTTOM+54)continue;
    for(let col=0;col<15;col++){
      const seed=(row+90)*53+col*13;
      const x=mod(col*100+hash(seed)*68+scroll,1500)-120;
      if(x<left-70||x>right+60)continue;
      const base=y+hash(seed+3)*34;
      oval(x+7,base+3,30+hash(seed+2)*26,5,'#749d7f');
      if(hash(seed+1)>.32){
        pine(x,base,.75+hash(seed+7)*.55,Math.floor(hash(seed+2)*4));
        if(hash(seed+5)>.52)pine(x+26,base+12,.62+hash(seed+12)*.45,Math.floor(hash(seed+13)*4));
      }else if(hash(seed+6)>.4){shrub(x,base,1.1);grass(x+28,base+8,1);}
      else{stone(x,base,.65);grass(x-20,base+2,.9);}
    }
  }
}
function landscape(roadScroll,stage){
  const scroll=roadScroll*BACKGROUND_SCROLL_MULTIPLIER;
  const left=view.left,right=left+view.span/view.zoom,top=view.top,bottom=top+view.height/view.zoom;
  box(left-2,top-2,right-left+4,bottom-top+4,['#92b492','#a1b1a0','#acaa90'][(stage-1)%3]);
  woodland(scroll,top,bottom,left,right);
  for(let band=0;band<2;band++){
    for(let j=0;j<28;j++){let x=mod(j*53+hash(j+51*band)*31+scroll,1470)-110;const y=band?ROAD_BOTTOM-10+hash(j+77)*53:ROAD_TOP-54+hash(j+38)*74;oval(x,y,24+hash(j)*28,3+hash(j+9)*4,'#7fa78a');grass(x,y,1);if(j%4===1)shrub(x+13,y-3,.8+hash(j)*.3);}
  }
  const upper=[],lower=[];
  for(let x=Math.floor(left/20)*20-20;x<=right+20;x+=20){
    const t=(x-scroll)*Math.PI/720;
    upper.push([x,ROAD_TOP+5*Math.sin(t*4)+3*Math.sin(t*14)]);
    lower.push([x,ROAD_BOTTOM+6*Math.sin(t*3+.7)+3*Math.sin(t*13)]);
  }
  const shape=(a,b)=>[...a,...b.slice().reverse()];
  poly(shape(upper.map(([x,y])=>[x,y-6]),lower.map(([x,y])=>[x,y+7])),'#c3bd87',null);
  poly(shape(upper,lower),'#d9ba84',null);poly(shape(upper.map(([x,y])=>[x,y+8]),lower.map(([x,y])=>[x,y-9])),'#ead09e',null);
  for(const lane of [LANES[0],LANES[2]]){for(let j=0;j<12;j++){const x=mod(j*133+lane+roadScroll,1470)-120;line([[x,lane],[x+24,lane],[x+30,lane-1],[x+53,lane-1]],'#d5b786',2);}}
  for(let j=0;j<105;j++){let x=mod(hash(j)*1450+roadScroll,1450)-65;let y=ROAD_TOP+12+hash(j+215)*(ROAD_BOTTOM-ROAD_TOP-24);const a=hash(j+6);if(a>.78){line([[x,y],[x+8+a*8,y]],'#d4b887',2);line([[x+11,y+4],[x+19,y+4]],'#dfc291',1.5);}else oval(x,y,a*2.4+.6,1.2,'#ccb185');}
  for(let b=0;b<2;b++){
    const items=[];
    for(let j=0;j<42;j++){const idx=j+b*100;let x=mod(j*35+hash(idx+9)*32+scroll,1470)-100;
      let y=b?ROAD_BOTTOM+42+hash(idx+58)*48:ROAD_TOP-50+hash(idx+28)*38;
      items.push({x,y,z:.68+hash(idx+167)*.64,t:Math.floor(hash(idx+13)*4)});
    }
    items.sort((a,b)=>a.y-b.y).forEach(p=>pine(p.x,p.y,p.z,p.t));
  }
  for(let j=0;j<10;j++){const x=mod(j*159+scroll,1470)-85,y=j%2?ROAD_BOTTOM+8+hash(j)*7:ROAD_TOP-9-hash(j)*5;if(j%4===0)fence(x,y);else if(j%3===0)stone(x,y,.72);else shrub(x,y,.9);}
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
  const {halfWidth}=roadObstacleProfile(obstacle.type);
  at(obstacle.x,obstacleY(obstacle),halfWidth/30,()=>{
    const rx=obstacle.type==='rock'?34:22;
    oval(4,0,rx,30,'#78674644');
    if(obstacle.type==='log')fallenLog();
    else if(obstacle.type==='deer')woodlandDeer();
    else roadBoulder();
  });
}
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
    poly([[-22,-10],[-12,-14],[13,-14],[30,-10],[36,-4],[36,5],[27,11],[6,14],[-13,11],[-21,6]],coat,ink,2.8);
    poly([[-11,-10],[12,-11],[23,-7],[9,-6],[-5,-7]],light,null);box(7,-13,7,26,'#805737',ink,1,1.6);
    for(const [mx,my] of rumpMarks.slice(0,Math.ceil(wounds))){
      line([[mx-1,my-1],[mx+2,my+2]],'#713c37',1.9);
    }
    poly([[-17,-11],[-28,-16],[-29,-19],[-34,-18],[-31,-10],[-43,-8],[-51,-4],[-51,1],[-47,5],[-37,3],[-25,10],[-20,6],[-13,2]],coat,ink,2.5);
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
const royalClothes={
 prince:{robe:'#84adcc',trim:'#405a78',hair:'#a46c48',seat:'#8fa9cd'},
 princess:{robe:'#d69ab8',trim:'#794461',hair:'#c6874c',seat:'#c79abb'}
};
function royalSprite(royal,x,y,scale,t){
 const colors=royalClothes[royal.id],seated=royal.state==='seated';
 const gait=royal.state==='wandering'?Math.sin(t*1.25+(royal.id==='prince'?0:2))*2:0;
 at(x,y,scale,()=>{
   if(royal.state!=='carried')oval(1,8,13,6,'#60473647');
   for(const side of [-1,1])box(side*4-2,(seated?5:9)+side*gait,5,4,colors.trim,ink,1.5,1.3);
   poly([[-6,-3],[6,-3],[9,8],[-9,8]],colors.robe,colors.trim,1.8);
   line([[-5,0],[5,5]],'#fff1be',2);
   oval(0,-7,9,8,colors.hair,ink,1.7);
   oval(0,-5,6.6,6,'#efc6a2',null);
   oval(-2.3,-5,1,1,ink);oval(2.3,-5,1,1,ink);
   poly([[-7,-11],[-8,-18],[-3,-15],[0,-20],[3,-15],[8,-18],[7,-11]],'#f0cf65','#645239',1.4);
   oval(0,-14,1.6,1.6,colors.trim);
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
function wanderingRoyal(royal,t,nearby){
 const {x,y}=royal;
 if(nearby)oval(x,y,16,14,'#fff1b858','#fff2a7',2.2);
 royalSprite(royal,x,y,1,t);
 if(royal.mischief){
   oval(x+12,y-20,6,6,'#b66545','#5c3929',1.3);
   line([[x+12,y-23],[x+12,y-20]],'#fff1be',1.4);
   oval(x+12,y-17,1,1,'#fff1be');
 }
}
const floors={
 driver:['마부석','마','#bea5bd','#795d83'],
 rack:['적재칸','적','#80afc6','#3a6579'],loader:['장전수','장','#9bc69a','#476b49'],gunner:['포수','포','#dba476','#865c3d'],supply:['포탄 더미','탄','#92b6c5','#4d7080']
};
function floorStation(s,role){
 const onRoute=(view.role==='carry'||view.role==='catch')&&role==='rack';
 if(view.role&&view.role!==role&&!onRoute)return;
 const spot=STATIONS[role],[name,mark,fill,edge]=floors[role],{x,y,r}=spot;
 const inactive=role==='loader'&&!canLoad(s);
 const border=inactive?'#897961':edge;
 oval(x,y,r,r,inactive?'#bda47e':fill,border,2.3);
 c.save();c.strokeStyle='#fff1d8';c.lineWidth=1.8;c.setLineDash([4,4]);c.beginPath();c.arc(x,y,r-4,0,Math.PI*2);c.stroke();c.restore();
 c.textAlign='center';c.textBaseline='middle';c.font='bold 16px system-ui';c.fillStyle=edge;c.fillText(mark,x,y);
 if(role==='supply'||role==='rack'){
   box(x-15,y-8,30,18,'#907050',ink,3);
   for(let i=0;i<Math.min(role==='rack'?s.ammo.rack:s.ammo.stock,5);i++)oval(x-8+(i%3)*8+(i>2?3:0),y+3-Math.floor(i/3)*7,4.8,4.8,'#565b66',ink,1.3);
 }
 if(view.role&&!onRoute)return;
 const labelY=y-r-5;
 box(x-23,labelY-6,46,13,border,ink,3,1);c.fillStyle='#fff8e9';c.font='bold 9px system-ui';c.fillText(role==='rack'?'적재 '+s.ammo.rack+'발':inactive?'장전 대기':name,x,labelY+.5);
}
function crewSprite(s,id,t,passenger){
 const p=s.players[id],color=({p1:'#aa7ed4',p2:'#dc9d5d',p3:'#7aa5c5',p4:'#88b995'})[id];
 const moving=Math.hypot(p.mx,p.my)>.15&&s.mode==='run';
 const bounce=moving?Math.sin(t*1.3)*1.8:0;
 oval(p.x+2,p.y+7,17,7,'#4d3e454a');
 const currentRole=stationAt(p,s);
 if(!view.role&&currentRole&&Number.isFinite(target(s,currentRole))&&currentRole!=='catch'){
   const role=currentRole,q=phase(s,role),center=target(s,role),window=width(s,role);
   const offset=s.shake*Math.sin(s.tick*.83)*4;
   at(p.x+offset,p.y,1,()=>{c.lineWidth=3;c.strokeStyle='#584c49';c.beginPath();c.arc(0,0,27,0,Math.PI*2);c.stroke();
    c.strokeStyle=role==='repair'?'#a7ffe1':'#ffcb83';c.lineWidth=5;c.beginPath();c.arc(0,0,27,(center-window/2-.25)*Math.PI*2,(center+window/2-.25)*Math.PI*2);c.stroke();
    const angle=(q-.25)*Math.PI*2;oval(Math.cos(angle)*27,Math.sin(angle)*27,3.7,3.7,'#fffdeb','#504444',1.5);});
 }
 const a=Math.atan2(p.ay,p.ax);
 c.save();c.translate(p.x,p.y);c.rotate(a);poly([[22,-5],[31,0],[22,5]],color,ink,1.4);c.restore();
 at(p.x,p.y+bounce,1,()=>{
   if(view.localPlayers.includes(id))crewBody(color,p.ball,7/Math.sqrt(view.zoom));
   crewBody(color,p.ball);
   if(passenger){
     const side=p.ax<0?-1:1;
     royalSprite(passenger,side*13,6,.76,t);
     line([[side*5,8],[side*12,12]],'#a4bd74',3);
   }
   box(-13,20,26,14,'#454641',ink,4,1);
   c.fillStyle='#fff9e8';c.textAlign='center';c.textBaseline='middle';c.font='bold 10px monospace';c.fillText(id.toUpperCase(),0,27);
 });
}
function drawWheel(x,y,t,loose=false){
 at(x,y,1,()=>{
   if(loose)c.rotate(t*.16);
   box(-27,-11,54,22,'#514947',ink,5,3);
   for(let i=0;i<5;i++)line([[-21+i*10,-8],[-21+i*10,8]],'#83766c',2.4);
   box(-6,-7,12,14,'#ae976e',ink,3,1.6);oval(0,0,2,2,'#635d51');
 });
}
function wheelMounts(s,t){
 for(let n=0;n<WHEELS.length;n++){
   const w=WHEELS[n],side=Math.sign(w.y),mount=side*103;
   if(s.wheel.broken!==n||s.wheel.progress===1)drawWheel(w.x,mount,t);
   else{
     box(w.x-19,mount-3,38,6,'#92897d',ink,2,2);
     const age=s.tick-s.wheel.since;
     if(age<52){const dx=age*3.1,dy=side*(age*.34+Math.sin(age*.12)*7);drawWheel(w.x+dx,mount+dy,t,true);}
   }
 }
}
function repairMark(s){
 const w=WHEELS[s.wheel.broken];if(!w)return;
 const pulse=.5+.5*Math.sin(s.tick*.19);
 oval(w.x,w.y,27,24,'#88b6a0','#355c52',2.4);
 c.save();c.setLineDash([6,4]);c.strokeStyle=pulse>.5?'#f6ffda':'#d2ead3';c.lineWidth=2.8;
 c.beginPath();c.ellipse(w.x,w.y,23,20,0,0,2*Math.PI);c.stroke();c.restore();
 at(w.x,w.y,1,()=>{line([[-7,8],[6,-5]],'#3c5e50',5);line([[2,-10],[1,-5],[6,-2],[12,-6]],'#3c5e50',4);});
 const labelX=w.x<500?w.x+31:w.x-79;
 box(labelX,w.y-9,48,18,'#3f6c5a','#365346',4,1);
 c.fillStyle='#fff9e9';c.font='bold 10px system-ui,sans-serif';c.textAlign='center';c.textBaseline='middle';c.fillText(s.wheel.progress?'핀 고정':'수리  !',labelX+24,w.y);
}
const draftHorses=[
 {side:'right',x:228,y:-41,palette:['#b17b4d','#cf9960','#795139']},
 {side:'left',x:215,y:41,palette:['#cba06d','#e3bb83','#835e42']}
];
const draftHorseScale=1.58;
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

function drawDraftHorses(s,t){
 const spot=STATIONS.driver;
 const aimed=view.role==='driver'?horseAim(s.players[view.id]):null;
 for(const mount of draftHorses){
   const {side,y,palette}=mount,wear=s.horseWear?.[side];
   const x=mount.x-s.horses[side]*.65,rumpX=x+29*draftHorseScale;
   const mood=horseCondition(s,side);
   const gait=mood==='tired'?t*.58:mood==='angry'?t*1.4:t;
   horse(x,y,draftHorseScale,palette,gait+(side==='left'?1.4:0),wear?.wounds);
   if(mood==='tired')tag(x,y-32,'지침');
   if(mood==='angry')tag(x,y-32,'화남',false,'#a03f39');
   if(aimed===side){
     c.save();c.setLineDash([5,5]);
     line([[spot.x-14,spot.y],[rumpX+9,y]],'#594334',3.2);
     line([[spot.x-14,spot.y],[rumpX+9,y]],'#ffdc9d',1.4);
     c.restore();
     oval(rumpX,y,8,8,'#f8d89a3d','#594334',3);
     oval(rumpX,y,8,8,'#f8d89a3d','#ffdc9d',1.5);
   }
   const sinceHit=s.tick-(wear?.lastHit??-Infinity);
   if(sinceHit>=0&&sinceHit<whipEffectTicks)drawWhip(spot,rumpX,y,sinceHit);
 }
}

function wagon(y,s,t){
 const j=s.shake*Math.sin(t*3)*3;
 at(0,y+j,1,()=>{
   for(const q of [-42,42]){
     line([[238,q],[292,q*.84],[355,q*.77]],'#514438',6);
     line([[238,q-1],[292,q*.84-1],[355,q*.77-1]],'#bb9261',2.2);
   }
   box(271,-4,83,9,'#b2865b',ink,3,2);
   box(345,-89,376,204,'#715d484e',null,20);wheelMounts(s,t);
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
   for(const role of Object.keys(floors))floorStation(s,role);
   if(!view.role||view.role==='repair')repairMark(s);
   for(const yy of [-23,28]){box(353,yy,16,12,'#745237',ink,3);line([[360,yy+5],[324,yy+5],[282,yy*1.55],[250,yy*1.62]],'#72533b',2);}
   for(const royal of s.royals){
     if(royal.state==='carried'&&view.localPlayers.includes(royal.carrier))returnMark(ROYAL_SEATS[royal.id]);
   }
   for(const xx of [478,575])box(xx,-92,7,38,'#9b7952',ink,2,2);
   box(485,-93,90,37,'#8b6c9b',ink,4,2.5);box(490,-89,80,27,'#b79bbb','#795d8b',3,1.6);
   for(const royal of s.royals){
     const {x,y}=ROYAL_SEATS[royal.id],colors=royalClothes[royal.id];
     box(x-15,y-12,30,25,colors.seat,colors.trim,4,1.5);
     if(royal.state==='seated')royalSprite(royal,x,y,.83,t);
   }
   line([[482,-57],[579,-57]],'#6f5345',4);
   line([[596,-82],[596,-124]],'#4e4038',4);
   poly([[596,-124],[615,-120],[634,-122],[626,-112],[634,-103],[615,-105],[596,-108]],'#ac80d7',ink,2.5);
   poly([[606,-114],[611,-118],[617,-113],[611,-109]],'#ebd8fc',null);
   const gunner=view.role==='gunner'?s.players[view.id]:Object.values(s.players).find(p=>stationAt(p,s)==='gunner');
   const angle=firingAngle(s,s.fuse.end?s.gunAngle:gunner?aimAngle(gunner):s.gunAngle);
   at(BARREL.x,BARREL.y,cannonScale(s),()=>{
     oval(-2,0,19,19,'#766b64',ink,2.8);
     if(s.fuse.end){line([[-8,-9],[-14,-19]],'#6a4537',2);oval(-14,-20,4,4,s.tick%6<3?'#ffe29a':'#f0833e');}
     c.rotate(angle);
     poly([[-14,-10],[27,-8],[31,-12],[BARREL.length,-12],[BARREL.length,12],[31,12],[27,8],[-14,10]],'#77818a',ink,3);
     line([[-7,-5],[24,-4]],'#b9c0be',2.8);line([[34,-8],[34,8]],'#465561',2.5);
   });
   const children=s.royals.filter(r=>r.state==='carried');
   const visible=Object.keys(s.players).filter(id=>!view.role||id===view.id||children.some(r=>r.carrier===id));
   for(const royal of s.royals){
     if(royal.state==='wandering'){
       const nearby=view.localPlayers.some(id=>!s.players[id].ball&&royalTarget(s,s.players[id])===royal);
       wanderingRoyal(royal,t,nearby);
     }
   }
   // Draw local characters above the others when crew crowd the same station.
   visible.sort((a,b)=>Number(view.localPlayers.includes(a))-Number(view.localPlayers.includes(b))||s.players[a].y-s.players[b].y);
   for(const id of visible)crewSprite(s,id,t,children.find(r=>r.carrier===id));
   drawDraftHorses(s,t);
 });
}
function knight(x,y,kind,t,jab){
  at(x,y,.96,()=>{
    horse(0,4,.72,kind===1?['#b5825f','#ddac7e','#6c5144']:['#b8ab92','#d9ccaf','#86745b'],t+kind);
    poly([[-15,3],[-8,-11],[9,-11],[18,4],[11,12],[-9,11]],'#528ca0',ink,2.2);
    line([[-39-jab,-10],[24-jab,-15]],'#554b3c',3.5);
    poly([[-48-jab,-10],[-37-jab,-14],[-31-jab,-12],[-38-jab,-7]],'#c6d5ce',ink,1.7);
    box(-9,-17,18,18,'#a4b9bd',ink,5,2.2);box(-11,-10,18,5,'#687c87',ink,1,1.7);line([[-7,-9],[-7,-7]],'#dce1d9',1.3);
    poly([[-3,-18],[-1,-23],[7,-25],[16,-22],[8,-21],[4,-16]],'#719cb9',ink,1.6);
    poly([[1,3],[11,1],[17,5],[15,13],[9,17],[2,13]],'#689eb1',ink,2);line([[8,4],[9,12]],'#d0d9c4',2.2);
  });
}
function tag(x,y,title,dot=false,fill='#383c38'){
  c.font='bold 10px system-ui, sans-serif';let w=c.measureText(title).width+19+(dot?9:0);
  box(x-w/2,y-11,w,19,fill,'#353a35',4,1);poly([[x-3,y+8],[x+3,y+8],[x,y+12]],fill,null);
  if(dot)oval(x-w/2+9,y-1.5,2.8,2.8,'#bb92ff');
  c.fillStyle='#fff9e9';c.textAlign='center';c.textBaseline='middle';c.fillText(title,x+(dot?4:0),y-1);
}
const boulderOutline=[[-37,10],[-32,-15],[-16,-33],[12,-32],[31,-18],[39,7],[29,24],[-5,31],[-30,24]];
function drawBoulder(x,y,fall){
 at(x,y,1,()=>{
   c.rotate((1-fall)*.46);
   poly(boulderOutline,'#787a70','#49483f',3);
   poly([[-32,-15],[-16,-33],[12,-32],[24,-19],[7,-12],[-16,-9]],'#a9aa95',null);
   poly([[7,-12],[24,-19],[31,-18],[39,7],[29,24],[7,27],[15,10]],'#5f625b',null);
   line([[-24,17],[-14,2],[-16,-9]],'#55584f',2);
   line([[7,-12],[-1,1],[3,9],[15,10]],'#50534a',2.2);
 });
}
function drawCatapult(s){
 const rock=s.catapult;
 if(!rock)return;
 const untilLanding=rock.landAt-s.tick;
 c.save();
 if(untilLanding>0){
   const locked=s.tick>=rock.lockAt;
   const approach=Math.min(1,(s.tick-rock.launchedAt)/(rock.lockAt-rock.launchedAt));
   const x=catapultAirX(s),rx=22+(CATAPULT_RADIUS.x-22)*approach,ry=13+(CATAPULT_RADIUS.y-13)*approach;
   c.globalAlpha=.28+.24*approach;
   oval(x,rock.y,rx,ry,'#47342b');
   if(locked){
     c.globalAlpha=.68+.22*Math.sin(s.tick*.33);
     c.setLineDash([5,4]);
     oval(x,rock.y,rx,ry,'#00000000','#ffcf8c',3.2);
     c.setLineDash([]);
     const fall=1-untilLanding/(HZ*.6);
     if(fall>0){
       c.globalAlpha=Math.min(1,fall*4);
       drawBoulder(x,rock.y-142*(1-fall*fall),fall);
     }
   }
 }else{
   const x=catapultX(s),age=-untilLanding/(HZ*.7);
   c.globalAlpha=Math.max(0,1-age);
   oval(x,rock.y+5,37,23,'#52433386');
   drawBoulder(x,rock.y,1);
   oval(x,rock.y,32+36*age,19+25*age,'#00000000','#f1dbad',4.5);
   for(let i=0;i<8;i++){
     const angle=i*Math.PI/4;
     oval(x+Math.cos(angle)*(42+28*age),rock.y+Math.sin(angle)*(25+19*age),7+5*age,4+3*age,'#c9a073');
   }
 }
 c.restore();
}
export function render(canvas,s,wall,views){
 const dpr=Math.min(2,window.devicePixelRatio||1);const rect=canvas.getBoundingClientRect();
 const cw=Math.max(1,Math.round(rect.width*dpr)),ch=Math.max(1,Math.round(rect.height*dpr));
 if(canvas.width!==cw||canvas.height!==ch){canvas.width=cw;canvas.height=ch;}
 c=canvas.getContext('2d');c.setTransform(cw/views[0].screenWidth,0,0,ch/views[0].height,0,0);c.lineJoin='round';c.lineCap='round';
 const t=s.mode==='ready'?wall/470:s.tick*.37;
 for(const camera of views){
 view=camera;c.save();c.beginPath();c.rect(view.start,0,view.span,view.height);c.clip();
 c.translate(view.start,0);c.scale(view.zoom,view.zoom);c.translate(-view.left,-view.top);
 landscape(s.road,s.stage);
 for(const obstacle of s.obstacles)drawRoadObstacle(obstacle);
 for(const e of s.enemies){
   const sinceStrike=s.tick-(e.lastStrike??-Infinity);
   const dustSide=sinceStrike<=ENEMY_RETREAT_TICKS?-1:1;
   const dustCount=sinceStrike<=ENEMY_RETREAT_TICKS+ENEMY_RECOVERY_TICKS?2:4;
   for(let i=0;i<dustCount;i++){
     const q=mod(i*9+t*1.4,38);
     oval(e.x+dustSide*(31+q),e.y+11+Math.sin(i*4)*5,3+q*.09,2.5,'#f6dfb4');
   }
   const jab=16*Math.max(0,1-sinceStrike/7);
   knight(e.x,e.y,e.kind,t,jab);
 }
 for(let i=0;i<8;i++){const q=mod(i*11+t*2.4,84);oval(735+q,s.y+19+Math.sin(i*7)*12,3.5+q*.053,2.7+q*.02,'#f7e1b6');}
 wagon(s.y,s,t);

 for(const b of s.shots){
   const caliber=b.caliber??1;
   const trail=(b.enemy?15:27*caliber)/(Math.hypot(b.vx,b.vy)||1);
   line([[b.x-b.vx*trail,b.y-b.vy*trail],[b.x,b.y]],b.enemy?'#70554b':'#f3d59b',b.enemy?3:6*caliber);
   if(!b.enemy)oval(b.x,b.y,6*caliber,6*caliber,'#49444a',ink,2);
 }
 for(const p of s.particles){let age=18-p.life;for(let i=0;i<7;i++){let a=i*.91;poly([[p.x+Math.cos(a)*age*1.7,p.y+Math.sin(a)*age*1.5],[p.x+Math.cos(a)*age*1.7+4,p.y+Math.sin(a)*age*1.5-5],[p.x+Math.cos(a)*age*1.7+6,p.y+Math.sin(a)*age*1.5+2]],p.kind==='dust'?'#b09066':'#ec974e',null);}}
 for(const ball of s.falling){
   const age=s.tick-ball.tick,progress=age/31;
   const x=ball.x+35*progress,y=s.y+ball.y+ball.side*(123-Math.abs(ball.y))*progress;
   oval(x+2,y+4,7,3,'#745a4945');oval(x,y-16*Math.sin(progress*Math.PI),6,6,'#4e5661',ink,1.4);
 }
 drawCatapult(s);
 if(view.role==='gunner'&&s.ammo.loaded){
   const p=s.players[view.id],angle=firingAngle(s,s.fuse.end?s.gunAngle:aimAngle(p));
   const dx=Math.cos(angle),dy=Math.sin(angle),originY=s.y+BARREL.y+s.shake*Math.sin(t*3)*3;
   const muzzle=BARREL.length*cannonScale(s);
   const alongX=(view.left+view.span/view.zoom-15-BARREL.x)/dx;
   const alongY=Math.abs(dy)<.001?Infinity:(dy>0?view.top+view.height/view.zoom-12-originY:view.top+12-originY)/dy;
   const reach=Math.max(muzzle,Math.min(alongX,alongY)),x=BARREL.x+dx*reach,y=originY+dy*reach;
   const col=s.fuse.end?'#ffbd73':'#fff0c5';
   c.setLineDash([5,7]);line([[BARREL.x+muzzle*dx,originY+muzzle*dy],[x,y]],'#654735',1.5);c.setLineDash([]);
   c.beginPath();c.arc(x,y,8,0,Math.PI*2);c.strokeStyle='#573f32';c.lineWidth=3;c.stroke();c.strokeStyle=col;c.lineWidth=1.4;c.stroke();
   for(const [ux,uy] of [[1,0],[-1,0],[0,1],[0,-1]]){const mark=[[x+ux*5,y+uy*5],[x+ux*12,y+uy*12]];line(mark,'#503c30',2.6);line(mark,col,1);}
   oval(x,y,1.7,1.7,col);
 }
 c.restore();
 }
 if(views.length===2){box(views[0].span-1.5,0,3,views[0].height,'#34382d');}

}
