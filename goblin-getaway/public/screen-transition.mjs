const tileSize=20,frontDepth=3,duration=400,blackoutDuration=400;
const inputEvents=['pointerdown','pointermove','click','dblclick','contextmenu','keydown','touchstart','touchmove','wheel'];

export function createScreenTransition(){
 const canvas=document.createElement('canvas');
 const context=canvas.getContext('2d');
 const popover=typeof canvas.showPopover==='function';
 const reducedMotion=window.matchMedia?.('(prefers-reduced-motion: reduce)');
 let covering,blackoutHold,revealing,inputBlock;

 canvas.setAttribute('aria-hidden','true');
 if(popover)canvas.setAttribute('popover','manual');
 Object.assign(canvas.style,{
   position:'fixed',inset:'0',width:'100%',height:'100%',maxWidth:'none',maxHeight:'none',
   margin:'0',padding:'0',border:'0',background:'transparent',zIndex:'2147483647',touchAction:'none',
 });

 function bringToFront(){
   if(popover){
     if(!canvas.isConnected)document.body.append(canvas);
     if(canvas.matches(':popover-open'))canvas.hidePopover();
     canvas.showPopover();
   }else{
     (document.querySelector('dialog[open]')??document.body).append(canvas);
   }
 }

 function paint(progress,reveal){
   const width=window.innerWidth,height=window.innerHeight;
   if(canvas.width!==width)canvas.width=width;
   if(canvas.height!==height)canvas.height=height;
   if(!context)return;
   context.clearRect(0,0,width,height);
   context.fillStyle='#000';
   if((reveal&&progress===0)||(!reveal&&progress===1)){
     context.fillRect(0,0,width,height);
     return;
   }
   if(progress===0||progress===1)return;

   const columns=Math.ceil(width/tileSize),rows=Math.ceil(height/tileSize);
   const edge=progress*(columns+frontDepth-1);
   const stripStart=Math.max(0,Math.floor(edge-frontDepth)+1);
   const stripEnd=Math.min(columns,Math.floor(edge)+1);
   // Only the uneven edge needs individual tiles. Behind it the tiles form one solid strip.
   if(reveal)context.fillRect(stripEnd*tileSize,0,width,height);
   else context.fillRect(0,0,stripStart*tileSize,height);
   for(let column=stripStart;column<stripEnd;column++){
     for(let row=0;row<rows;row++){
       const stagger=((row*17+column*13+row*column*7)%19)/19*frontDepth;
       const passed=column+stagger<=edge;
       if(passed!==reveal)context.fillRect(column*tileSize,row*tileSize,tileSize,tileSize);
     }
   }
 }

 function animate(reveal){
   paint(0,reveal);
   if(reducedMotion?.matches){
     paint(1,reveal);
     return Promise.resolve();
   }
   const start=performance.now();
   return new Promise(resolve=>{
     function frame(now){
       const progress=Math.max(0,Math.min(1,(now-start)/duration));
       paint(progress,reveal);
       if(progress<1)requestAnimationFrame(frame);
       else resolve();
     }
     requestAnimationFrame(frame);
   });
 }

 function cover(){
   if(covering)return covering;
   inputBlock=new AbortController();
   function block(event){
     if(event.cancelable)event.preventDefault();
     event.stopImmediatePropagation();
   }
   for(const type of inputEvents)window.addEventListener(type,block,{capture:true,passive:false,signal:inputBlock.signal});
   bringToFront();
   covering=animate(false);
   blackoutHold=covering.then(()=>new Promise(resolve=>setTimeout(resolve,blackoutDuration)));
   return covering;
 }

 function reveal(){
   if(!covering)return Promise.resolve();
   revealing??=covering.then(()=>{
     bringToFront();
     return blackoutHold;
   }).then(()=>animate(true)).finally(()=>{
     canvas.remove();
     inputBlock.abort();
     covering=blackoutHold=revealing=undefined;
   });
   return revealing;
 }

 return {cover,reveal,get active(){return Boolean(covering);}};
}
