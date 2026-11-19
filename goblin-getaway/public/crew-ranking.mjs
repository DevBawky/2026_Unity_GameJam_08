const portraits={p1:'purple',p2:'orange',p3:'blue',p4:'pink'};
const rankLabels=['1st','2nd','3rd','4th'];

export function normalizeCrewNickname(value){
 if(typeof value!=='string')return '';
 const clean=value.normalize('NFC').replace(/[\p{Cc}\u202a-\u202e\u2066-\u2069]/gu,'').trim();
 return Array.from(clean).slice(0,16).join('').trimEnd();
}

export function crewName(names,id){
 return names?.[id]||'플레이어 '+id.slice(1);
}

function makeRow(id){
 const row=document.createElement('li');
 const place=document.createElement('span'),portrait=document.createElement('img');
 const name=document.createElement('b'),score=document.createElement('strong');
 row.className='crew-ranking-row';
 row.dataset.player=id;
 place.className='crew-ranking-place';
 place.lang='en';
 portrait.className='crew-ranking-portrait';
 portrait.src='/pixel-art/crew-face-'+portraits[id]+'.png';
 portrait.alt='';
 name.className='crew-ranking-name';
 score.className='crew-ranking-score';
 row.append(place,portrait,name,score);
 return {row,place,name,score};
}

function label(element,value){
 if(element.textContent!==value)element.textContent=value;
}

export function createCrewRanking(panel,list){
 const rows=new Map();
 let previousOrder='';
 return ({ids,humans=ids,names,scores,isLocal,reduceMotion})=>{
   if(!ids.length){
     if(!panel.hidden){
       panel.hidden=true;
       previousOrder='';
       rows.clear();
       list.replaceChildren();
     }
     return;
   }
   panel.hidden=false;
   const scoreOf=id=>Math.trunc(scores[id]??0);
   const ranked=[...ids].sort((a,b)=>scoreOf(b)-scoreOf(a)||a.localeCompare(b));
   const order=ranked.join(',');
   const moved=order!==previousOrder;
   const origins=moved&&!reduceMotion?new Map([...rows].map(([id,{row}])=>[id,row.getBoundingClientRect().left])):null;
   for(const id of rows.keys())if(!ranked.includes(id))rows.delete(id);
   for(const [index,id] of ranked.entries()){
     if(!rows.has(id))rows.set(id,makeRow(id));
     const {row,place,name,score}=rows.get(id);
     const human=humans.includes(id);
     const local=String(human&&isLocal(id));
     const rank=String(index+1);
     if(row.dataset.local!==local)row.dataset.local=local;
     if(row.dataset.rank!==rank)row.dataset.rank=rank;
     label(place,rankLabels[index]);
     const displayName=human?crewName(names,id):'봇 '+id.slice(1);
     if(name.textContent!==displayName){
       name.textContent=displayName;
       const longName=Array.from(displayName).length>10;
       name.toggleAttribute('data-long',longName);
       name.toggleAttribute('data-wide',longName&&/[\p{Extended_Pictographic}\p{Emoji_Presentation}]/u.test(displayName));
     }
     label(score,String(scoreOf(id)));
   }
   if(!moved)return;
   previousOrder=order;
   list.replaceChildren(...ranked.map(id=>rows.get(id).row));
   for(const id of ranked){
     if(!origins?.has(id))continue;
     const {row}=rows.get(id);
     for(const animation of row.getAnimations())animation.cancel();
     const shift=origins.get(id)-row.getBoundingClientRect().left;
     if(Math.abs(shift)<1)continue;
     row.animate([
       {transform:`translateX(${shift}px)`},
       {transform:'translateX(0)'},
     ],{duration:280,easing:'ease-in-out'});
   }
 };
}
