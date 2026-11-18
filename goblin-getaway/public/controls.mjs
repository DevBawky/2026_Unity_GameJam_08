import {BARREL,STATIONS,stationAt} from './sim.mjs?v=crew7';
import {worldPoint} from './workstation.mjs?v=outline8';

const WASD=['KeyA','KeyD','KeyW','KeyS'];
const ARROWS=['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'];
const IJKL=['KeyJ','KeyL','KeyI','KeyK'];
function axis(keys,layout){return [Number(keys.has(layout[1]))-Number(keys.has(layout[0])),Number(keys.has(layout[3]))-Number(keys.has(layout[2]))];}
function deadzone(x=0,y=0){
 const len=Math.hypot(x,y);if(len<.16)return [0,0];
 const magnitude=Math.min(1,(len-.16)/.84);
 return [x/len*magnitude,y/len*magnitude];
}
export function createControls({isDuo,active,onAct,onEscape,canPlay,getState,getViews,canvas}){
 const keys=new Set(),pointers=new Map();
 const state=Object.fromEntries(['p1','p2'].map(slot=>[slot,{move:null,aim:null,ax:slot==='p1'?-1:1,ay:0,trigger:false}]));
 const mapSlot=slot=>isDuo()?slot:active();
 function rememberAim(slot,x,y){const len=Math.hypot(x,y);if(len>.08){state[slot].ax=x/len;state[slot].ay=y/len;}}
 function input(slot){
   const local=state[slot],second=slot==='p2',duo=isDuo();
   const moveLayout=second?ARROWS:WASD;
   const aimLayout=second?IJKL:duo?null:ARROWS;
   let [mx,my]=axis(keys,moveLayout);
   if(aimLayout){const [x,y]=axis(keys,aimLayout);rememberAim(slot,x,y);}
   let pads=[];try{pads=Array.from(navigator.getGamepads?.()??[]).filter(Boolean);}catch{}
   const pad=pads[second?1:0];
   if(pad){
     const [x,y]=deadzone(pad.axes[0],pad.axes[1]);if(x||y){mx=x;my=y;}
     const [ax,ay]=deadzone(pad.axes[2],pad.axes[3]);rememberAim(slot,ax,ay);
     const down=!!pad.buttons[7]?.pressed||!!pad.buttons[0]?.pressed;
     if(down&&!local.trigger&&canPlay())queueMicrotask(()=>onAct(mapSlot(slot),inputWithoutPolling(slot,mx,my)));
     local.trigger=down;
   }else local.trigger=false;
   if(local.move)[mx,my]=local.move;
   if(local.aim)rememberAim(slot,...local.aim);
   return inputWithoutPolling(slot,mx,my);
 }
 function inputWithoutPolling(slot,mx,my){
   const p=state[slot],len=Math.max(1,Math.hypot(mx,my));
   return {mx:mx/len,my:my/len,ax:p.ax,ay:p.ay};
 }
 function action(slot){if(canPlay())onAct(mapSlot(slot),input(slot));}
 function moved(e){
   const v=pointers.get(e.pointerId);if(!v)return;
   const r=v.el.getBoundingClientRect(),scale=r.width*.44;
   const rawX=(e.clientX-r.left-r.width/2)/scale,rawY=(e.clientY-r.top-r.height/2)/scale;
   const len=Math.max(1,Math.hypot(rawX,rawY)),x=rawX/len,y=rawY/len;
   state[v.slot][v.kind]=deadzone(x,y);
   if(v.kind==='aim')rememberAim(v.slot,...state[v.slot].aim);
   v.el.firstElementChild.style.transform='translate('+x*scale*.67+'px,'+y*scale*.67+'px)';
 }
 function finish(e,commit){
   const v=pointers.get(e.pointerId);if(!v)return;
   pointers.delete(e.pointerId);
   if(commit&&v.kind==='aim'){
     const s=getState(),role=stationAt(s.players[mapSlot(v.slot)],s);
     if(role!=='gunner'&&role!=='driver'&&!role?.startsWith('royal-'))action(v.slot);
   }
   state[v.slot][v.kind]=null;v.el.firstElementChild.style.transform='';
   if(v.el.hasPointerCapture(e.pointerId))v.el.releasePointerCapture(e.pointerId);
 }
 for(const el of document.querySelectorAll('[data-pad]')){
   const [slot,kind]=el.dataset.pad.split('-');
   el.addEventListener('pointerdown',e=>{
     if(e.button!==0||!canPlay()||Array.from(pointers.values()).some(v=>v.el===el))return;
     e.preventDefault();el.setPointerCapture(e.pointerId);pointers.set(e.pointerId,{slot,kind,el});moved(e);
   });
   el.addEventListener('pointermove',moved);el.addEventListener('pointerup',e=>finish(e,true));
   for(const name of ['pointercancel','lostpointercapture'])el.addEventListener(name,e=>finish(e,false));
 }
 function clear(){
   keys.clear();
   for(const [id] of pointers)finish({pointerId:id},false);
 }
 window.addEventListener('blur',clear);document.addEventListener('visibilitychange',()=>{if(document.hidden)clear();});
 document.addEventListener('keydown',e=>{
   if(document.querySelector('dialog[open]')||e.target.matches('input,textarea')||e.ctrlKey||e.metaKey||e.altKey)return;
   if(e.code==='Escape'){if(!e.repeat)onEscape();return;}
   if(!canPlay())return;
   if([...WASD,...ARROWS,...(isDuo()?IJKL:[])].includes(e.code)){e.preventDefault();keys.add(e.code);}
   if(e.code==='Space'||(isDuo()&&e.code==='Enter')){
     e.preventDefault();if(!e.repeat)action(e.code==='Space'?'p1':'p2');
   }
 });
 document.addEventListener('keyup',e=>keys.delete(e.code));
 function mouseAim(e){
   if(e.pointerType!=='mouse'||!canPlay())return false;
   const s=getState(),p=s.players[mapSlot('p1')],r=canvas.getBoundingClientRect(),role=stationAt(p,s);
   const origin=role==='driver'?STATIONS.driver:role==='gunner'?BARREL:p;
   const camera=getViews()[0];
   const pt=worldPoint(camera,(e.clientX-r.left)*camera.screenWidth/r.width,(e.clientY-r.top)*camera.height/r.height);
   if(!pt)return false;
   rememberAim('p1',pt.x-origin.x,pt.y-s.y-origin.y);return true;
 }
 canvas.addEventListener('pointermove',mouseAim);
 canvas.addEventListener('pointerdown',e=>{if(e.pointerType==='mouse'&&e.button===0&&canPlay()){if(mouseAim(e)&&stationAt(getState().players[mapSlot('p1')],getState())!=='gunner')action('p1');}});
 return {input,clear,action};
}
