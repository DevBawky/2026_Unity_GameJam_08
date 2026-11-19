export const DRAFT_HORSES = {
 right:{x:228,y:-41},
 left:{x:215,y:29}
};
export const DRAFT_HORSE_SCALE=1.58;
export const DRAFT_HORSE_WALK={width:128,height:84,frames:8,framesPerMeter:1.1,offsetX:5,offsetY:4};
export const CAVALRY_RUN={width:200,height:184,frames:8,fps:16,left:-106,top:-114,scale:.88};
export const draftHorseFrameOffset=side=>side==='left'?2:0;
export const HORSE_BODY=[[-22,-10],[-12,-14],[13,-14],[30,-10],[36,-4],[36,5],[27,11],[6,14],[-13,11],[-21,6]];
export const HORSE_NECK_AND_HEAD=[[-17,-11],[-28,-16],[-29,-19],[-34,-18],[-31,-10],[-43,-8],[-51,-4],[-51,1],[-47,5],[-37,3],[-25,10],[-20,6],[-13,2]];
export const HORSE_ZONE_NAMES={rump:'엉덩이',waist:'허리',neck:'목',head:'머리'};

const aimCenterX=218,aimRangeX=115,aimRangeY=88;
const rump={x:27,y:0};

export function draftHorseOrigin(s,side){
 const mount=DRAFT_HORSES[side];
 return {x:mount.x-(s.horses?.[side]??0)*.65,y:mount.y};
}

export function horseBoardPoint(s,side,point=rump){
 const mount=draftHorseOrigin(s,side);
 return {x:mount.x+point.x*DRAFT_HORSE_SCALE,y:mount.y+point.y*DRAFT_HORSE_SCALE};
}

export function whipAimInput(x,y){
 const ax=(x-aimCenterX)/aimRangeX,ay=y/aimRangeY,scale=Math.max(1,Math.hypot(ax,ay));
 return {ax:ax/scale,ay:ay/scale};
}

function whipSway(s){
 const power=s.shake??0;
 if(!power)return {x:0,y:0};
 const phase=s.tick*.46+(s.shakeSign??0);
 return {x:Math.sin(phase)*power*7,y:Math.cos(phase)*power*10};
}

export function whipAimPoint(s,aim){
 const sway=whipSway(s);
 return {x:aimCenterX+(aim?.ax??0)*aimRangeX+sway.x,y:(aim?.ay??0)*aimRangeY+sway.y};
}

export function whipFollowThrough(s,target){
 const sway=whipSway(s);
 const inward=target.side==='right'?36:-36;
 return whipAimInput(target.x+24-sway.x,target.y+inward-sway.y);
}

export function horseRumpAim(s,side){
 const {x,y}=horseBoardPoint(s,side),sway=whipSway(s);
 return whipAimInput(x-sway.x,y-sway.y);
}

function containsPoint(polygon,x,y){
 let inside=false;
 for(let i=0,j=polygon.length-1;i<polygon.length;j=i++){
   const [x1,y1]=polygon[i], [x2,y2]=polygon[j];
   if((y1>y)!==(y2>y)&&x<(x2-x1)*(y-y1)/(y2-y1)+x1)inside=!inside;
 }
 return inside;
}

export function horseWhipTarget(s,aim){
 const {x,y}=whipAimPoint(s,aim);
 const side=y<(DRAFT_HORSES.right.y+DRAFT_HORSES.left.y)/2?'right':'left';
 const mount=draftHorseOrigin(s,side);
 const local={x:(x-mount.x)/DRAFT_HORSE_SCALE,y:(y-mount.y)/DRAFT_HORSE_SCALE};
 if(!containsPoint(HORSE_BODY,local.x,local.y)&&!containsPoint(HORSE_NECK_AND_HEAD,local.x,local.y))return null;
 const zone=local.x>=17?'rump':local.x>=-16?'waist':local.x>-31?'neck':'head';
 return {side,zone,x,y,local};
}

export function spookHorse(wear,tick,duration,random){
 wear.rebellion={start:tick,end:tick+duration,direction:random()<.5?-1:1,turnTicks:10+Math.floor(random()*9)};
}

export function horseRebellion(s){
 let pace=1,steer=0;
 for(const side of Object.keys(DRAFT_HORSES)){
   const rebellion=s.horseWear?.[side]?.rebellion;
   if(!rebellion||s.tick>=rebellion.end)continue;
   const beat=Math.floor((s.tick-rebellion.start)/rebellion.turnTicks);
   const turn=rebellion.direction*(beat%2===0?1:-1);
   steer+=turn*(beat%3===1?3.5:6);
   pace=Math.min(pace,beat%3===1?.88:.64);
 }
 return {pace,steer};
}
