import {cannonAimCursor,cannonAimInput,occupiedStation} from './sim.mjs?v=nearest-wheel134';
import {worldPoint} from './workstation.mjs?v=nearest-wheel134';

const WASD=['KeyA','KeyD','KeyW','KeyS'];
const ARROWS=['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'];
const SLOTS=['p1','p2'];
const ACTION_KEYS={p1:'Space',p2:'Enter'};
const CANNON_CURSOR_SPEED=1.4;

function axis(keys,layout){
 return [Number(keys.has(layout[1]))-Number(keys.has(layout[0])),Number(keys.has(layout[3]))-Number(keys.has(layout[2]))];
}
function deadzone(x=0,y=0){
 const length=Math.hypot(x,y);
 if(length<.16)return [0,0];
 const magnitude=Math.min(1,(length-.16)/.84);
 return [x/length*magnitude,y/length*magnitude];
}
function bounded(x,y){
 const length=Math.max(1,Math.hypot(x,y));
 return [x/length,y/length];
}
function controlRole(role){
 return role==='driver'||role==='gunner'?role:null;
}
const clampAxis=value=>Math.max(-1,Math.min(1,value));

export function createControls({isDuo,active,onAct,onExit,onEscape,onAddShell,canPlay,getState,getViews,canvas}){
 const keys=new Set(),blockedKeys=new Set(),pointers=new Map();
 let mouseAimPointer=null;
 const state=Object.fromEntries(SLOTS.map(slot=>[slot,{
   actor:null,role:null,whipSequence:null,drag:null,ax:0,ay:0,lastSample:0,waitForPad:false,padAct:false,padExit:false,
   cookPointer:null,waitForCookPad:false
 }]));
 const mapSlot=slot=>isDuo()?slot:active();
 const actionButtons=Object.fromEntries(SLOTS.map(slot=>[slot,document.getElementById(slot+'-action')]));
 const sticks=Object.fromEntries(SLOTS.map(slot=>[slot,document.getElementById(slot+'-stick')]));

 function finish(pointerId){
   const pointer=pointers.get(pointerId);
   if(!pointer)return;
   pointers.delete(pointerId);
   state[pointer.slot].drag=null;
   const {stick,surface}=pointer;
   stick.firstElementChild.style.transform='';
   stick.parentElement.style.transform='';
   delete stick.parentElement.dataset.floating;
   if(surface.hasPointerCapture(pointerId))surface.releasePointerCapture(pointerId);
 }
 function finishCookPointer(slot){
   const local=state[slot],pointer=local.cookPointer,button=actionButtons[slot];
   if(pointer===null)return;
   local.cookPointer=null;
   if(button.hasPointerCapture(pointer))button.releasePointerCapture(pointer);
 }
 function resetDirections(slot){
   if(slot==='p1')mouseAimPointer=null;
   const codes=slot==='p2'?ARROWS:isDuo()?WASD:[...WASD,...ARROWS];
   for(const code of codes)if(keys.delete(code))blockedKeys.add(code);
   for(const [pointerId,pointer] of pointers)if(pointer.slot===slot)finish(pointerId);
   keys.delete(ACTION_KEYS[slot]);
   finishCookPointer(slot);
   state[slot].waitForPad=state[slot].waitForCookPad=true;
 }
 function context(slot){
   const s=getState(),actor=mapSlot(slot),player=s.players[actor],local=state[slot];
   const role=occupiedStation(player,s);
   const changed=local.actor!==actor||local.role!==role;
   if(changed){
     if(local.actor!==null)resetDirections(slot);
     local.actor=actor;
     local.role=role;
     const aim=role==='gunner'?cannonAimCursor(s,player):player;
     local.ax=aim?.ax??0;
     local.ay=aim?.ay??0;
     local.lastSample=performance.now();
   }
   local.whipSequence=role==='driver'?(player.whipSwing?.sequence??0):null;
   return local;
 }
 function readGamepads(){
   try{return Array.from(navigator.getGamepads?.()??[]).filter(Boolean);}catch{return [];}
 }
 function input(slot,gamepads){
   const local=context(slot),second=slot==='p2',playing=canPlay();
   const now=performance.now(),elapsed=Math.min(.05,(now-local.lastSample)/1000);
   local.lastSample=now;
   const pad=(gamepads??readGamepads())[second?1:0];
   const steering=local.role==='driver';
   const [padX,padY]=deadzone(steering?0:pad?.axes[0],pad?.axes[1]);
   if(!padX&&!padY)local.waitForPad=false;
   let [x,y]=axis(keys,second?ARROWS:WASD);
   if(!second&&!isDuo()&&(!x||steering)&&!y)[x,y]=axis(keys,ARROWS);
   if(steering)x=0;
   if(!local.waitForPad&&(padX||padY))[x,y]=[padX,padY];
   if(local.drag)[x,y]=local.drag;
   [x,y]=playing?bounded(x,y):[0,0];

   let mx=0,my=0,ax=0,ay=0;
   const aiming=local.role==='gunner';
   if(steering)my=y;
   else if(aiming){
     local.ax=clampAxis(local.ax+x*elapsed*CANNON_CURSOR_SPEED);
     local.ay=clampAxis(local.ay+y*elapsed*CANNON_CURSOR_SPEED);
     ({ax,ay}=local);
   }else if(!local.role){
     [mx,my]=[x,y];
   }

   const actionDown=!!pad?.buttons[0]?.pressed||!!pad?.buttons[7]?.pressed;
   const exitDown=!!pad?.buttons[1]?.pressed;
   if(!actionDown)local.waitForCookPad=false;
   const atCook=local.role==='cook';
   if(playing){
     if(exitDown&&!local.padExit)queueMicrotask(()=>exit(slot));
     else if(!atCook&&actionDown&&!local.padAct)queueMicrotask(()=>action(slot));
   }
   const cookHeld=playing&&atCook&&!exitDown&&(local.cookPointer!==null||keys.has(ACTION_KEYS[slot])||
     (actionDown&&!local.waitForCookPad));
   local.padAct=actionDown;
   local.padExit=exitDown;
   return {mx,my,ax,ay,aimRole:controlRole(local.role),aimCursor:aiming,whipSequence:local.whipSequence??undefined,cookHeld};
 }
 function action(slot){
   if(canPlay()&&!actionButtons[slot].disabled&&context(slot).role!=='cook')onAct(mapSlot(slot),input(slot));
 }
 function exit(slot){
   if(!canPlay()||!context(slot).role)return false;
   resetDirections(slot);
   onExit(mapSlot(slot));
   return true;
 }

 function updatePointer(e){
   const pointer=pointers.get(e.pointerId);
   if(!pointer)return;
   const {slot,stick,originX,originY,dragRadius,knobTravel}=pointer,local=context(slot);
   if(!pointers.has(e.pointerId))return;
   const horizontal=local.role==='driver'?0:(e.clientX-originX)/dragRadius;
   const [x,y]=bounded(horizontal,(e.clientY-originY)/dragRadius);
   local.drag=deadzone(x,y);
   stick.firstElementChild.style.transform='translate('+x*knobTravel+'px,'+y*knobTravel+'px)';
 }
 function startPointer(e,slot,surface){
   if(e.button!==0||!canPlay()||pointers.has(e.pointerId)||Array.from(pointers.values()).some(pointer=>pointer.slot===slot))return;
   const {role,actor}=context(slot);
   const player=getState().players[actor];
   if((role&&!controlRole(role))||(player?.ball&&player.slipUntil))return;
   const stick=sticks[slot],rect=stick.getBoundingClientRect();
   if(!rect.width)return;
   const centerX=rect.left+rect.width/2,centerY=rect.top+rect.height/2;
   const originX=e.clientX,originY=e.clientY;
   // Keep the thumb gesture short even when the visible control is much larger.
   const inputSize=Math.max(64,Math.min(88,Math.min(canvas.clientWidth,canvas.clientHeight)*.12));
   const dragRadius=Math.min(rect.width,inputSize)*.44,knobTravel=rect.width*.44*.67;
   e.preventDefault();
   surface.setPointerCapture(e.pointerId);
   pointers.set(e.pointerId,{slot,stick,surface,originX,originY,dragRadius,knobTravel});
   const wrap=stick.parentElement;
   wrap.dataset.floating='true';
   wrap.style.transform='translate('+(originX-centerX)+'px,'+(originY-centerY)+'px)';
   updatePointer(e);
 }
 function movementSlot(e){
   const rect=canvas.getBoundingClientRect();
   const halves=isDuo()?2:1;
   const x=(e.clientX-rect.left)/rect.width*halves;
   if(!Number.isFinite(x)||x<0||x>=halves||x%1>=.5)return null;
   return SLOTS[Math.floor(x)];
 }
 for(const slot of SLOTS){
   const stick=sticks[slot],button=actionButtons[slot];
   stick.addEventListener('pointerdown',e=>startPointer(e,slot,stick));
   stick.addEventListener('pointermove',updatePointer);
   button.addEventListener('pointerdown',e=>{
     if(e.button!==0||button.disabled||!canPlay())return;
     const local=context(slot);
     if(local.role!=='cook'||local.cookPointer!==null)return;
     e.preventDefault();
     button.setPointerCapture(e.pointerId);
     local.cookPointer=e.pointerId;
   });
   button.addEventListener('pointermove',e=>{
     if(e.pointerId!==state[slot].cookPointer)return;
     const {left,right,top,bottom}=button.getBoundingClientRect();
     if(e.clientX<left||e.clientX>right||e.clientY<top||e.clientY>bottom)finishCookPointer(slot);
   });
   for(const name of ['pointerup','pointercancel','lostpointercapture'])button.addEventListener(name,e=>{
     if(e.pointerId===state[slot].cookPointer)finishCookPointer(slot);
   });
 }
 for(const surface of [canvas,...Object.values(sticks)]){
   for(const name of ['pointerup','pointercancel','lostpointercapture'])surface.addEventListener(name,e=>{
     if(mouseAimPointer===e.pointerId)mouseAimPointer=null;
     finish(e.pointerId);
   });
 }

 function clear(){
   for(const slot of SLOTS)resetDirections(slot);
   keys.clear();
 }
 window.addEventListener('blur',clear);
 window.addEventListener('resize',clear);
 document.addEventListener('visibilitychange',()=>{if(document.hidden)clear();});
 document.addEventListener('keydown',e=>{
   if(document.querySelector('dialog[open]')||e.target.matches?.('input,textarea')||e.ctrlKey||e.metaKey||e.altKey)return;
   if(e.code==='Escape'){
     e.preventDefault();
     if(!e.repeat)onEscape();
     return;
   }
   if(!canPlay())return;
   if(e.code==='Backquote'||e.key==='`'||e.key==='~'){
     if(e.defaultPrevented||e.isComposing||e.target.isContentEditable||e.target.closest?.('select'))return;
     e.preventDefault();
     if(!e.repeat)onAddShell();
     return;
   }
   if(WASD.includes(e.code)||ARROWS.includes(e.code)){
     e.preventDefault();
     context(isDuo()&&ARROWS.includes(e.code)?'p2':'p1');
     if(!e.repeat)blockedKeys.delete(e.code);
     if(!blockedKeys.has(e.code))keys.add(e.code);
   }
   if(e.code===ACTION_KEYS.p1||(isDuo()&&e.code===ACTION_KEYS.p2)){
     if(e.target.closest?.('[data-exit],.emote-controls'))return;
     e.preventDefault();
     if(e.repeat)return;
     const slot=e.code===ACTION_KEYS.p1?'p1':'p2';
     if(context(slot).role==='cook')keys.add(e.code);
     else action(slot);
   }
   if(e.code==='KeyQ'||(isDuo()&&e.code==='ShiftRight')){
     e.preventDefault();
     if(!e.repeat)exit(e.code==='KeyQ'?'p1':'p2');
   }
 });
 document.addEventListener('keyup',e=>{keys.delete(e.code);blockedKeys.delete(e.code);});

 function mouseAim(e){
   if(e.pointerType!=='mouse'||!(e.buttons&1)||!canPlay())return;
   const local=context('p1');
   if(local.role!=='gunner')return;
   const s=getState(),r=canvas.getBoundingClientRect(),camera=getViews()[0];
   const point=worldPoint(camera,(e.clientX-r.left)*camera.screenWidth/r.width,(e.clientY-r.top)*camera.height/r.height);
   if(!point)return;
   ({ax:local.ax,ay:local.ay}=cannonAimInput(s,point.x,point.y));
 }
 canvas.addEventListener('pointerdown',e=>{
   if(!canPlay())return;
   const slot=movementSlot(e);
   if(e.pointerType==='mouse'&&e.button===0&&slot!=='p2'&&context('p1').role==='gunner'){
     mouseAimPointer=e.pointerId;
     mouseAim(e);
     return;
   }
   if(slot)startPointer(e,slot,canvas);
 });
 canvas.addEventListener('pointermove',e=>{
   if(pointers.has(e.pointerId))updatePointer(e);
   else if(e.pointerId===mouseAimPointer)mouseAim(e);
 });
 return {input,readGamepads,clear,action,exit};
}
